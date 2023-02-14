using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace Kurisu.AkiBT.Editor
{
    public interface ITreeView
    {
        /// <summary>
        /// 将选中结点加入Group并创建Block
        /// </summary>
        /// <param name="node"></param>
        void SelectGroup(BehaviorTreeNode node);
        /// <summary>
        /// 取消Group
        /// </summary>
        void UnSelectGroup();
        /// <summary>
        /// 复制结点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        BehaviorTreeNode DuplicateNode(BehaviorTreeNode node);
        /// <summary>
        /// 编辑器名称
        /// </summary>
        string treeEditorName{get;}
        /// <summary>
        /// 共享变量名称修改事件(手动触发)
        /// </summary>
        event System.Action<SharedVariable> OnPropertyNameChangeEvent;
        /// <summary>
        /// 共享变量名称编辑事件(自动触发)
        /// </summary>
        event System.Action<SharedVariable> OnPropertyNameEditingEvent;
        List<SharedVariable> ExposedProperties{get;}
        /// <summary>
        /// 是否在Restore中
        /// </summary>
        /// <value></value>
        bool IsRestored{get;}
        /// <summary>
        /// 添加共享变量到黑板
        /// </summary>
        /// <param name="variable"></param>
        /// <typeparam name="T"></typeparam>
        void AddPropertyToBlackBoard(SharedVariable variable);
    }
}