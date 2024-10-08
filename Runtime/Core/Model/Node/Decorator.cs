using UnityEngine;
namespace Kurisu.AkiBT
{
    /// <summary>
    /// 装饰器结点行为
    /// </summary>
    public class Decorator : NodeBehavior
    {

        [SerializeReference]
        private NodeBehavior child;

        public NodeBehavior Child
        {
            get => child;
            set => child = value;
        }
        private bool isRunning = false;

        protected sealed override void OnRun()
        {
            child?.Run(GameObject, Tree);
        }
        public sealed override void Awake()
        {
            OnAwake();
            child?.Awake();
        }
        /// <summary>
        /// Decorator Awake方法
        /// 在该方法调用后再遍历子结点
        /// </summary>
        protected virtual void OnAwake()
        {
        }
        public sealed override void Start()
        {
            OnStart();
            child?.Start();
        }
        protected virtual void OnStart()
        {
        }
        protected override Status OnUpdate()
        {
            var status = child.Update();
            status = OnDecorate(status);
            isRunning = status == Status.Running;
            return status;
        }
        /// <summary>
        /// 装饰子结点返回值
        /// </summary>
        /// <param name="childStatus"></param>
        /// <returns></returns>
        protected virtual Status OnDecorate(Status childStatus)
        {
            return childStatus;
        }
        public override bool CanUpdate()
        {
            return OnDecorate(child.CanUpdate());
        }
        /// <summary>
        /// 装饰子判断结点(Conditional)的CanUpdate返回值
        /// </summary>
        /// <param name="childCanUpdate"></param>
        /// <returns></returns>
        protected virtual bool OnDecorate(bool childCanUpdate)
        {
            return childCanUpdate;
        }
        public sealed override void PreUpdate()
        {
            child?.PreUpdate();
        }
        public sealed override void PostUpdate()
        {
            child?.PostUpdate();
        }
        public override void Abort()
        {
            if (isRunning)
            {
                isRunning = false;
                child?.Abort();
            }
        }
        public sealed override void Dispose()
        {
            base.Dispose();
            child?.Dispose();
            child = null;
        }
        public sealed override NodeBehavior GetChildAt(int index)
        {
            return child;
        }

        public sealed override int GetChildrenCount()
        {
            return child == null ? 0 : 1;
        }
        public sealed override void ClearChildren()
        {
            child = null;
        }
        public sealed override void AddChild(NodeBehavior nodeBehavior)
        {
            child = nodeBehavior;
        }
        public sealed override void SetChildren(NodeBehavior[] inChildren)
        {
            child = inChildren[0];
        }
        public sealed override NodeBehavior[] GetChildren()
        {
            if (child == null) return base.GetChildren();
            return new NodeBehavior[1] { child };
        }
    }
}
