using System.Collections.Generic;
using System.Linq;
using MonsterBT.Editor.Service.Operation;
using MonsterBT.Editor.View.Graph;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEngine;

namespace MonsterBT.Editor.Service.Layout
{
    public static class BTAutoLayoutService
    {
        private const float HorizontalSpacing = 200f;
        private const float VerticalSpacing = 150f;
        private const float StartX = 400f;
        private const float StartY = 100f;
        private const float DefaultNodeWidth = 200f;
        private const float DefaultNodeHeight = 100f;

        public static void AutoLayout(BehaviorTree behaviorTree, Dictionary<BTNode, BTNodeView> nodeViews)
        {
            if (behaviorTree?.RootNode == null || nodeViews == null || nodeViews.Count == 0)
                return;

            var rootNode = behaviorTree.RootNode;
            if (!nodeViews.ContainsKey(rootNode))
                return;

            var nodeSizes = GetNodeSizes(nodeViews);
            var layoutData = CalculateLayout(rootNode, nodeViews, nodeSizes);
            ApplyLayout(layoutData, nodeViews);
        }

        private static Dictionary<BTNode, Vector2> GetNodeSizes(Dictionary<BTNode, BTNodeView> nodeViews)
        {
            var sizes = new Dictionary<BTNode, Vector2>();

            foreach (var (node, nodeView) in nodeViews)
            {
                if (node == null || nodeView == null)
                    continue;

                var rect = nodeView.GetPosition();
                var width = rect.width > 0 ? rect.width : DefaultNodeWidth;
                var height = rect.height > 0 ? rect.height : DefaultNodeHeight;
                sizes[node] = new Vector2(width, height);
            }

            return sizes;
        }

        private static Dictionary<BTNode, Rect> CalculateLayout(BTNode rootNode,
            Dictionary<BTNode, BTNodeView> nodeViews, Dictionary<BTNode, Vector2> nodeSizes)
        {
            var layoutData = new Dictionary<BTNode, Rect>();
            var subtreeHeights = new Dictionary<BTNode, float>();

            CalculateSubtreeHeights(rootNode, nodeSizes, subtreeHeights);

            var levels = BuildLevels(rootNode);
            var levelX = StartX;

            foreach (var level in levels)
            {
                if (level.Count == 0)
                    continue;

                var totalHeight = level.Sum(node => subtreeHeights.GetValueOrDefault(node, nodeSizes[node].y));
                var startY = StartY - totalHeight / 2f;
                var currentY = startY;

                foreach (var node in level)
                {
                    var subtreeHeight = subtreeHeights.GetValueOrDefault(node, nodeSizes[node].y);
                    var nodeHeight = nodeSizes[node].y;
                    var nodeX = levelX;
                    var nodeY = currentY + (subtreeHeight - nodeHeight) / 2f;

                    layoutData[node] = new Rect(nodeX, nodeY, nodeSizes[node].x, nodeSizes[node].y);
                    currentY += subtreeHeight + VerticalSpacing;
                }

                var maxWidth = level.Max(node => nodeSizes[node].x);
                levelX += maxWidth + HorizontalSpacing;
            }

            return layoutData;
        }

        private static void CalculateSubtreeHeights(BTNode node, Dictionary<BTNode, Vector2> nodeSizes,
            Dictionary<BTNode, float> subtreeHeights)
        {
            var children = BTNodeEditorService.GetChildren(node).ToList();

            if (children.Count == 0)
            {
                subtreeHeights[node] = nodeSizes[node].y;
                return;
            }

            foreach (var child in children.Where(child => child != null))
            {
                CalculateSubtreeHeights(child, nodeSizes, subtreeHeights);
            }

            var childrenHeight = children
                .Where(c => c != null)
                .Sum(c => subtreeHeights.GetValueOrDefault(c, nodeSizes[c].y) + VerticalSpacing) - VerticalSpacing;

            if (childrenHeight < 0)
                childrenHeight = 0;

            subtreeHeights[node] = Mathf.Max(nodeSizes[node].y, childrenHeight);
        }

        private static List<List<BTNode>> BuildLevels(BTNode rootNode)
        {
            var levels = new List<List<BTNode>>();
            var currentLevel = new List<BTNode> { rootNode };
            var visited = new HashSet<BTNode> { rootNode };

            while (currentLevel.Count > 0)
            {
                levels.Add(new List<BTNode>(currentLevel));
                var nextLevel = new List<BTNode>();

                foreach (var node in currentLevel)
                {
                    foreach (var child in BTNodeEditorService.GetChildren(node))
                    {
                        if (child != null && !visited.Contains(child))
                        {
                            nextLevel.Add(child);
                            visited.Add(child);
                        }
                    }
                }

                currentLevel = nextLevel;
            }

            return levels;
        }

        private static void ApplyLayout(Dictionary<BTNode, Rect> layoutData, Dictionary<BTNode, BTNodeView> nodeViews)
        {
            Undo.SetCurrentGroupName("Auto Layout Nodes");

            foreach (var (node, rect) in layoutData)
            {
                if (node == null || node.Equals(null))
                    continue;

                if (!nodeViews.TryGetValue(node, out var nodeView) || nodeView == null)
                    continue;

                Undo.RecordObject(node, "Auto Layout");
                nodeView.SetPosition(rect);
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }
    }
}