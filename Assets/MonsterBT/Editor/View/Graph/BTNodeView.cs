using MonsterBT.Editor.Service.Operation;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor.View.Graph
{
    public sealed class BTNodeView : Node
    {
        public BTNode Node { get; }
        public Label descriptionLabel { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        public BTNodeView(BTNode node)
        {
            if (node == null)
            {
                throw new System.ArgumentNullException(nameof(node), "Cannot create BTNodeView with null node");
            }

            Node = node;

            SetupNodeContent();
            SetupPorts();
            SetupNodeStyle();

            SetPosition(new Rect(node.Position, Vector2.zero));
        }

        private void SetupNodeContent()
        {
            if (Node == null)
            {
                title = "Invalid Node";
                return;
            }

            title = string.IsNullOrEmpty(Node.name) ? Node.GetType().Name : Node.name;

            var description = string.IsNullOrEmpty(Node.Description)
                ? GetNodeDescription()
                : Node.Description;
            descriptionLabel = new Label(description)
            {
                name = "description"
            };
            descriptionLabel.AddToClassList("node-description");
            mainContainer.Add(descriptionLabel);
        }

        private void SetupPorts()
        {
            if (BTNodeEditorService.HasInputPort(Node))
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                    typeof(bool));
                InputPort.portName = "Input";
                inputContainer.Add(InputPort);
            }

            if (BTNodeEditorService.HasOutputPort(Node))
            {
                var outputCapacity = BTNodeEditorService.GetOutputPortCapacity(Node);
                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, outputCapacity,
                    typeof(bool));
                OutputPort.portName = "Output";
                outputContainer.Add(OutputPort);
            }
        }

        private void SetupNodeStyle()
        {
            var styleClass = BTNodeEditorService.GetNodeStyleClass(Node);
            AddToClassList(styleClass);
        }

        private string GetNodeDescription()
        {
            if (!string.IsNullOrEmpty(Node.Description))
                return Node.Description;

            var typeName = Node.GetType().Name;
            if (typeName.EndsWith("Node"))
                typeName = typeName.Substring(0, typeName.Length - 4);

            if (Node is RootNode)
                return "Root of the behavior tree";

            return $"Behavior tree node: {typeName}";
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (Node != null && !Node.Equals(null))
            {
                Node.Position = new Vector2(newPos.xMin, newPos.yMin);
                EditorUtility.SetDirty(Node);
            }
        }

        public void RefreshContent(string propertyName)
        {
            switch (propertyName)
            {
                case "name":
                    title = string.IsNullOrEmpty(Node.name) ? Node.GetType().Name : Node.name;
                    break;
                case "description":
                    descriptionLabel.text =
                        string.IsNullOrEmpty(Node.Description) ? GetNodeDescription() : Node.Description;
                    break;
            }
        }
    }
}