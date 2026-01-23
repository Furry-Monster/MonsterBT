using System;
using System.Collections.Generic;
using MonsterBT.Editor.Services;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public class BTNodeLibrary : VisualElement
    {
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

            // 动态获取所有分类，而不是硬编码
            var categoryLabels = new Dictionary<string, string>
            {
                ["Composite"] = "Composite Nodes",
                ["Decorator"] = "Decorator Nodes",
                ["Action"] = "Action Nodes",
                ["Condition"] = "Condition Nodes",
                ["Other"] = "Other Nodes"
            };

            // 按分类顺序显示
            var categoryOrder = new[] { "Composite", "Decorator", "Action", "Condition", "Other" };

            foreach (var category in categoryOrder)
            {
                if (!nodeTypes.ContainsKey(category) || nodeTypes[category].Count == 0)
                    continue;

                var section = new VisualElement { name = $"{category.ToLower()}-nodes" };
                section.AddToClassList("sidebar-section");

                var sectionTitle = new Label(categoryLabels.GetValueOrDefault(category, $"{category} Nodes"));
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

            item.RegisterCallback<ClickEvent>(_ => BTEditorEventBus.PublishNodeRequested(nodeType));
            item.RegisterCallback<MouseDownEvent>(_ => { });

            return item;
        }
    }
}