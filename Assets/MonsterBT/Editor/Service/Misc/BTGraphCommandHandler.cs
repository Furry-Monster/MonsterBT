using System.Linq;
using MonsterBT.Editor.Service.Operation;
using MonsterBT.Editor.View.Graph;
using MonsterBT.Runtime;
using UnityEngine.UIElements;

namespace MonsterBT.Editor.Service.Misc
{
    public class BTGraphCommandHandler
    {
        private readonly BTNodeGraphView graphView;
        private readonly BehaviorTree behaviorTree;

        public BTGraphCommandHandler(BTNodeGraphView graphView, BehaviorTree behaviorTree)
        {
            this.graphView = graphView;
            this.behaviorTree = behaviorTree;
        }

        public void HandleExecuteCommand(ExecuteCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case "Copy":
                    graphView.CopySelectedNodes();
                    evt.StopPropagation();
                    break;

                case "Paste":
                    graphView.PasteNode();
                    evt.StopPropagation();
                    break;

                case "Cut":
                    graphView.CutSelectedNodes();
                    evt.StopPropagation();
                    break;

                case "Delete":
                case "SoftDelete":
                    graphView.DeleteSelectedNodes();
                    evt.StopPropagation();
                    break;

                case "Duplicate":
                    graphView.DuplicateSelectedNodes();
                    evt.StopPropagation();
                    break;
            }
        }

        public void HandleValidateCommand(ValidateCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case "Paste":
                    if (graphView.HasCopiedNode())
                        evt.StopPropagation();
                    break;

                case "Copy":
                    var copyableNodes = graphView.selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanCopyNode(nv.Node, behaviorTree))
                        .ToList();
                    if (copyableNodes.Count > 0)
                        evt.StopPropagation();
                    break;

                case "Cut":
                    var cuttableNodes = graphView.selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanCutNode(nv.Node, behaviorTree))
                        .ToList();
                    if (cuttableNodes.Count > 0)
                        evt.StopPropagation();
                    break;

                case "Delete":
                case "SoftDelete":
                    var deletableNodes = graphView.selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanDeleteNode(nv.Node))
                        .ToList();
                    if (deletableNodes.Count > 0)
                        evt.StopPropagation();
                    break;

                case "Duplicate":
                    var duplicatableNodes = graphView.selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanDuplicateNode(nv.Node, behaviorTree))
                        .ToList();
                    if (duplicatableNodes.Count > 0)
                        evt.StopPropagation();
                    break;
            }
        }
    }
}