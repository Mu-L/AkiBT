using UnityEngine;
namespace Kurisu.AkiBT.Editor
{
    public class GameObjectManipulator : DragDropManipulator
    {
        protected override void OnDragOver(Object[] droppedObjects, Vector2 mousePosition)
        {
            foreach (var data in droppedObjects)
            {
                if (data is GameObject gameObject)
                {
                    if (gameObject.TryGetComponent(out IBehaviorTree tree))
                    {
                        TreeView.CopyFromTree(tree, mousePosition);
                        TreeView.EditorWindow.ShowNotification(new GUIContent("GameObject dropped succeed!"));
                    }
                    else
                    {
                        TreeView.EditorWindow.ShowNotification(new GUIContent("Invalid dragged gameObject!"));
                        break;
                    }
                }
            }
        }
    }
}