using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
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
            // Input Part
            if (BTNodeEditorHelper.HasInputPort(Node))
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                    typeof(bool));
                InputPort.portName = "Input";
                inputContainer.Add(InputPort);
            }

            // Output Part
            if (BTNodeEditorHelper.HasOutputPort(Node))
            {
                var outputCapacity = BTNodeEditorHelper.GetOutputPortCapacity(Node);
                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, outputCapacity,
                    typeof(bool));
                OutputPort.portName = "Output";
                outputContainer.Add(OutputPort);
            }
        }

        private void SetupNodeStyle()
        {
            var styleClass = BTNodeEditorHelper.GetNodeStyleClass(Node);
            AddToClassList(styleClass);
        }

        private string GetNodeDescription()
        {
            // 如果节点有自定义描述，使用自定义描述
            if (!string.IsNullOrEmpty(Node.Description))
                return Node.Description;

            // 否则使用类型名称生成描述
            var typeName = Node.GetType().Name;
            if (typeName.EndsWith("Node"))
                typeName = typeName.Substring(0, typeName.Length - 4);

            // 特殊处理 RootNode
            if (Node is RootNode)
                return "Root of the behavior tree";

            return $"Behavior tree node: {typeName}";
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.Position = new Vector2(newPos.xMin, newPos.yMin);
            EditorUtility.SetDirty(Node);
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