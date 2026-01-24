using MonsterBT.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor.Services
{
    public class BTGraphContextMenuBuilder
    {
        private readonly BTNodeGraphView graphView;
        private readonly BTBlackboardViewManager blackboardManager;
        private readonly BehaviorTree behaviorTree;

        public BTGraphContextMenuBuilder(BTNodeGraphView graphView, BTBlackboardViewManager blackboardManager,
            BehaviorTree behaviorTree)
        {
            this.graphView = graphView;
            this.blackboardManager = blackboardManager;
            this.behaviorTree = behaviorTree;
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt, Vector2 mousePosition)
        {
            switch (evt.target)
            {
                case GraphView:
                    BuildGraphContextMenu(evt, mousePosition);
                    break;
                case BTNodeView nodeView:
                    BuildNodeContextMenu(evt, nodeView);
                    break;
            }
        }

        private void BuildGraphContextMenu(ContextualMenuPopulateEvent evt, Vector2 mousePosition)
        {
            BuildNodeCreationMenu(evt, mousePosition);

            evt.menu.AppendAction("Blackboard/Add Boolean", _ => blackboardManager.AddVariable("NewBool", typeof(bool)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add Float", _ => blackboardManager.AddVariable("NewFloat", typeof(float)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add Vector3",
                _ => blackboardManager.AddVariable("NewVector3", typeof(Vector3)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add GameObject",
                _ => blackboardManager.AddVariable("NewGameObject", typeof(GameObject)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add String",
                _ => blackboardManager.AddVariable("NewString", typeof(string)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Refresh", _ => blackboardManager.RefreshView(),
                DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendAction("Paste", _ => graphView.PasteNode(),
                action => graphView.HasCopiedNode()
                    ? DropdownMenuAction.AlwaysEnabled(action)
                    : DropdownMenuAction.AlwaysDisabled(action));
            evt.menu.AppendAction("Reload", _ => graphView.PopulateView(), DropdownMenuAction.AlwaysEnabled);
        }

        private void BuildNodeContextMenu(ContextualMenuPopulateEvent evt, BTNodeView nodeView)
        {
            var node = nodeView.Node;
            var canCopy = BTNodeEditorService.CanCopyNode(node, behaviorTree);
            var canCut = BTNodeEditorService.CanCutNode(node, behaviorTree);
            var canDuplicate = BTNodeEditorService.CanDuplicateNode(node, behaviorTree);
            var canDelete = BTNodeEditorService.CanDeleteNode(node);

            evt.menu.AppendAction("Copy", _ => graphView.CopyNode(nodeView),
                canCopy ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
            evt.menu.AppendAction("Cut", _ => graphView.CutNode(nodeView),
                canCut ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
            evt.menu.AppendAction("Duplicate", _ => graphView.DuplicateNode(nodeView),
                canDuplicate ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
            evt.menu.AppendAction("Delete", _ => graphView.DeleteNode(nodeView),
                canDelete ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
        }

        private void BuildNodeCreationMenu(ContextualMenuPopulateEvent evt, Vector2 mousePosition)
        {
            var nodeTypes = BTNodeTypeHelper.GetAllNodeTypes();

            foreach (var (category, types) in nodeTypes)
            {
                foreach (var type in types)
                {
                    var displayName = BTNodeTypeHelper.GetNodeDisplayName(type);
                    var menuPath = $"Create Node/{category}/{displayName}";

                    evt.menu.AppendAction(menuPath, _ => graphView.Create(type, mousePosition),
                        DropdownMenuAction.AlwaysEnabled);
                }
            }
        }
    }
}