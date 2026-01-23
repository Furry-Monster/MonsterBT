using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public class BTStatusBar : VisualElement
    {
        private readonly Label statusText;
        private readonly Label nodeCount;

        public BTStatusBar()
        {
            var styleSheet = BTEditorResources.LoadStyleSheet("BTEditorStyle.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);

            name = "status-bar";
            AddToClassList("status-bar");

            statusText = new Label("Ready")
            {
                name = "status-text",
                style =
                {
                    color = new Color(0.78f, 0.78f, 0.78f),
                    fontSize = 11
                }
            };
            Add(statusText);

            nodeCount = new Label("Nodes: 0")
            {
                name = "node-count",
                style =
                {
                    color = new Color(0.59f, 0.59f, 0.59f),
                    fontSize = 10
                }
            };
            Add(nodeCount);
        }

        public void SetStatus(string status)
        {
            statusText.text = status;
        }

        public void SetNodeCount(int count)
        {
            nodeCount.text = $"Nodes: {count}";
        }
    }
}
