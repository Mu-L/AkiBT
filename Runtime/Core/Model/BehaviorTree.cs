using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using System;
using UnityEngine.Pool;
using System.Reflection;
using UnityEngine.Assertions;
namespace Kurisu.AkiBT
{
    [Serializable]
    public class BehaviorTree : IEnumerable<NodeBehavior>, IDisposable
    {
        [SerializeReference]
        internal List<SharedVariable> variables;
        [SerializeReference]
        internal Root root;
        public List<SharedVariable> SharedVariables => variables;
#if UNITY_EDITOR
        [SerializeField]
        internal List<GroupBlockData> blockData = new();
#endif
        // Exposed blackboard for data exchange
        public BlackBoard BlackBoard { get; private set; }
        private readonly HashSet<SharedVariable> internalVariables = new();
        private static readonly Dictionary<Type, List<FieldInfo>> fieldInfoLookup = new();
        public BehaviorTree() { }
        public BehaviorTree(BehaviorTreeData behaviorTreeData)
        {
            variables = behaviorTreeData.variables.ToList();
            BlackBoard = BlackBoard.Create(variables, false);
            root = behaviorTreeData.Build() as Root;
            root ??= new Root();
#if UNITY_EDITOR
            blockData = behaviorTreeData.blockData.ToList();
#endif
        }
        /// <summary>
        /// Initialize behavior tree's shared variables
        /// </summary>
        public void InitVariables()
        {
            BlackBoard ??= BlackBoard.Create(variables, false);
            InitVariables_Imp(this);
        }
        public void Run(GameObject gameObject)
        {
            root.Run(gameObject, this);
        }
        public void Awake()
        {
            root.Awake();
        }
        public void Start()
        {
            root.Start();
        }
        public void Tick()
        {
            root.PreUpdate();
            root.Update();
            root.PostUpdate();
        }
        public Status TickWithStatus()
        {
            root.PreUpdate();
            var status = root.Update();
            root.PostUpdate();
            return status;
        }
        public void Abort()
        {
            root.Abort();
        }
        public void Dispose()
        {
            foreach (var variable in variables)
            {
                variable.Unbind();
            }
            foreach (var variable in internalVariables)
            {
                variable.Unbind();
            }
            variables.Clear();
            internalVariables.Clear();
            root.Dispose();
        }
        public IEnumerator<NodeBehavior> GetEnumerator()
        {
            return new Enumerator(root);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(root);
        }
        /// <summary>
        /// Get better format data for serialization of this tree
        /// </summary>
        /// <returns></returns>
        public BehaviorTreeData GetData()
        {
#if UNITY_EDITOR
            // Should not serialize data in playing mode which will modify behavior tree structure
            Assert.IsFalse(Application.isPlaying);
#endif
            // since this function used in editor most time
            // use clone to prevent modify source tree
            return new BehaviorTreeData(this).Clone();
        }
        public static BehaviorTree Deserialize(string serializedData)
        {
            // not cache behavior tree data!
            return BehaviorTreeData.Deserialize(serializedData).CreateInstance();
        }
        public string Serialize(bool indented = false, bool serializeEditorData = false)
        {
            return Serialize(this, indented, serializeEditorData);
        }
        public static string Serialize(BehaviorTree tree, bool indented = false, bool serializeEditorData = false)
        {
            if (tree == null) return null;
            return BehaviorTreeData.Serialize(tree.GetData(), indented, serializeEditorData);
        }

        /// <summary>
        /// Traverse the behavior tree and automatically init all shared variables
        /// </summary>
        /// <param name="behaviorTree"></param>
        private static void InitVariables_Imp(BehaviorTree behaviorTree)
        {
            HashSet<SharedVariable> internalVariables = behaviorTree.internalVariables;
            foreach (var behavior in behaviorTree)
            {
                var behaviorType = behavior.GetType();
                if (!fieldInfoLookup.TryGetValue(behaviorType, out var fields))
                {
                    fields = behaviorType
                            .GetAllFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .Where(x => x.FieldType.IsSubclassOf(typeof(SharedVariable)) || IsIListVariable(x.FieldType))
                            .ToList();
                    fieldInfoLookup.Add(behaviorType, fields);
                }
                foreach (var fieldInfo in fields)
                {
                    var value = fieldInfo.GetValue(behavior);
                    // shared variables should not be null (dsl/builder will cause null variables)
                    if (value == null)
                    {
                        value = Activator.CreateInstance(fieldInfo.FieldType);
                        fieldInfo.SetValue(behavior, value);
                    }
                    if (value is SharedVariable sharedVariable)
                    {
                        sharedVariable.MapTo(behaviorTree.BlackBoard);
                        internalVariables.Add(sharedVariable);
                    }
                    else if (value is IList sharedVariableList)
                    {
                        foreach (var variable in sharedVariableList)
                        {
                            var sv = variable as SharedVariable;
                            internalVariables.Add(sv);
                            sv.MapTo(behaviorTree.BlackBoard);
                        }
                    }
                }
            }
        }
        private static bool IsIListVariable(Type fieldType)
        {
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type genericArgument = fieldType.GetGenericArguments()[0];
                if (typeof(SharedVariable).IsAssignableFrom(genericArgument))
                {
                    return true;
                }
            }
            else if (fieldType.IsArray)
            {
                Type elementType = fieldType.GetElementType();
                if (typeof(SharedVariable).IsAssignableFrom(elementType))
                {
                    return true;
                }
            }
            return false;
        }
        private struct Enumerator : IEnumerator<NodeBehavior>
        {
            private readonly Stack<NodeBehavior> stack;
            private static readonly ObjectPool<Stack<NodeBehavior>> pool = new(() => new(), null, s => s.Clear());
            private NodeBehavior currentNode;
            public Enumerator(NodeBehavior root)
            {
                stack = pool.Get();
                currentNode = null;
                if (root != null)
                {
                    stack.Push(root);
                }
            }

            public readonly NodeBehavior Current
            {
                get
                {
                    if (currentNode == null)
                    {
                        throw new InvalidOperationException();
                    }
                    return currentNode;
                }
            }

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                pool.Release(stack);
                currentNode = null;
            }
            public bool MoveNext()
            {
                if (stack.Count == 0)
                {
                    return false;
                }

                currentNode = stack.Pop();
                int childrenCount = currentNode.GetChildrenCount();
                for (int i = childrenCount - 1; i >= 0; i--)
                {
                    stack.Push(currentNode.GetChildAt(i));
                }
                return true;
            }
            public void Reset()
            {
                stack.Clear();
                if (currentNode != null)
                {
                    stack.Push(currentNode);
                }
                currentNode = null;
            }
        }
    }
}