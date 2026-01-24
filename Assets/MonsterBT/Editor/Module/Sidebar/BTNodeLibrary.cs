using System;
using System.Collections.Generic;
using System.Linq;
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

            var categoryOrder = new[] { "Composite", "Decorator", "Action", "Condition", "Other" };
            var processedCategories = new HashSet<string>();

            foreach (var baseCategory in categoryOrder)
            {
                var matchingCategories = nodeTypes.Keys
                    .Where(k => k == baseCategory || k.StartsWith(baseCategory + "/"))
                    .OrderBy(k => k)
                    .ToList();

                foreach (var category in matchingCategories)
                {
                    if (processedCategories.Contains(category) || nodeTypes[category].Count == 0)
                        continue;

                    processedCategories.Add(category);

                    var section = new VisualElement { name = $"{category.ToLower().Replace("/", "-")}-nodes" };
                    section.AddToClassList("sidebar-section");

                    var displayLabel = FormatCategoryLabel(category);
                    var sectionTitle = new Label(displayLabel);
                    sectionTitle.AddToClassList("sidebar-title");
                    section.Add(sectionTitle);

                    foreach (var nodeType in nodeTypes[category])
                    {
                        var displayName = BTNodeTypeHelper.GetNodeDisplayName(nodeType);
                        var itemName = $"{category.ToLower().Replace("/", "-")}-{nodeType.Name.ToLower()}-item";
                        section.Add(CreateNodeListItem(itemName, displayName, category, nodeType));
                    }

                    scrollView.Add(section);
                }
            }

            foreach (var category in nodeTypes.Keys)
            {
                if (!processedCategories.Contains(category) && nodeTypes[category].Count > 0)
                {
                    var section = new VisualElement { name = $"{category.ToLower().Replace("/", "-")}-nodes" };
                    section.AddToClassList("sidebar-section");

                    var displayLabel = FormatCategoryLabel(category);
                    var sectionTitle = new Label(displayLabel);
                    sectionTitle.AddToClassList("sidebar-title");
                    section.Add(sectionTitle);

                    foreach (var nodeType in nodeTypes[category])
                    {
                        var displayName = BTNodeTypeHelper.GetNodeDisplayName(nodeType);
                        var itemName = $"{category.ToLower().Replace("/", "-")}-{nodeType.Name.ToLower()}-item";
                        section.Add(CreateNodeListItem(itemName, displayName, category, nodeType));
                    }

                    scrollView.Add(section);
                }
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

        private static string FormatCategoryLabel(string category)
        {
            if (category.Contains("/"))
            {
                var parts = category.Split('/');
                var baseCategory = parts[0];
                var subCategory = parts[1];
                return $"{baseCategory} / {subCategory}";
            }

            var labels = new Dictionary<string, string>
            {
                ["Composite"] = "Composite Nodes",
                ["Decorator"] = "Decorator Nodes",
                ["Action"] = "Action Nodes",
                ["Condition"] = "Condition Nodes",
                ["Other"] = "Other Nodes"
            };

            return labels.GetValueOrDefault(category, $"{category} Nodes");
        }
    }
}