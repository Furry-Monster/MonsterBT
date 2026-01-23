using MonsterBT.Runtime;
using MonsterBT.Runtime.Composite;
using MonsterBT.Runtime.Decorator;
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
            if (Node is not RootNode)
            {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                    typeof(bool));
                InputPort.portName = "Input";
                inputContainer.Add(InputPort);
            }

            // Output Part
            if (Node is RootNode or CompositeNode)
            {
                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi,
                    typeof(bool));
            }
            else if (Node is DecoratorNode)
            {
                OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                    typeof(bool));
            }

            if (OutputPort != null)
            {
                OutputPort.portName = "Output";
                outputContainer.Add(OutputPort);
            }
        }

        private void SetupNodeStyle()
        {
            switch (Node)
            {
                case RootNode:
                    AddToClassList("root-node");
                    break;
                case CompositeNode:
                    AddToClassList("composite-node");
                    break;
                case DecoratorNode:
                    AddToClassList("decorator-node");
                    break;
                case ActionNode:
                    AddToClassList("action-node");
                    break;
            }
        }

        private string GetNodeDescription()
        {
            return Node switch
            {
                RootNode => "Root of the behavior tree",
                Selector => "Execute children until one succeeds",
                Sequence => "Execute children in sequence until one fails",
                Inverter => "Invert the result of child node",
                ActionNode action => $"Action: {action.GetType().Name}",
                _ => "Behavior tree node"
            };
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