using System;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public class BTNodeLibrary : VisualElement
    {
        public event Action<Type> OnNodeRequested;

        public BTNodeLibrary()
        {
            var styleSheet = BTEditorResources.LoadStyleSheet("BTEditorStyle.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);

            name = "node-library-panel";
            AddToClassList("sidebar");
            style.flexDirection = FlexDirection.Column;

            var title = new Label("Node Library") { name = "node-library-title" };
            title.AddToClassList("sidebar-title");
            Add(title);

            var scrollView = new ScrollView { name = "node-library-scroll" };
            scrollView.AddToClassList("node-library-scroll");
            scrollView.style.flexGrow = 1;
            scrollView.style.flexShrink = 1;

            var nodeTypes = BTNodeTypeHelper.GetAllNodeTypes();

            string[] categories = { "Composite", "Decorator", "Action", "Condition" };
            string[] categoryLabels = { "Composite Nodes", "Decorator Nodes", "Action Nodes", "Condition Nodes" };

            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                if (!nodeTypes.ContainsKey(category) || nodeTypes[category].Count == 0)
                    continue;

                var section = new VisualElement { name = $"{category.ToLower()}-nodes" };
                section.AddToClassList("sidebar-section");

                var sectionTitle = new Label(categoryLabels[i]);
                sectionTitle.AddToClassList("sidebar-title");
                section.Add(sectionTitle);

                foreach (var nodeType in nodeTypes[category])
                {
                    var displayName = BTNodeTypeHelper.GetNodeDisplayName(nodeType);
                    var itemName = $"{category.ToLower()}-{nodeType.Name.ToLower()}-item";
                    section.Add(CreateNodeListItem(itemName, displayName, category, nodeType));
                }

                scrollView.Add(section);
            }

            Add(scrollView);
        }

        private VisualElement CreateNodeListItem(string name, string labelText, string typeText, Type nodeType)
        {
            var item = new VisualElement { name = name };
            item.AddToClassList("node-list-item");
            item.userData = nodeType;

            item.Add(new Label(labelText));
            var typeLabel = new Label(typeText) { name = "blackboard-type" };
            typeLabel.AddToClassList("blackboard-type");
            item.Add(typeLabel);

            item.RegisterCallback<ClickEvent>(_ => OnNodeRequested?.Invoke(nodeType));
            item.RegisterCallback<MouseDownEvent>(_ => { });

            return item;
        }
    }
}