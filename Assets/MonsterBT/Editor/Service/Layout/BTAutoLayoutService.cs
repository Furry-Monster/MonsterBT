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
            var subtreeWidths = new Dictionary<BTNode, float>();

            CalculateSubtreeWidths(rootNode, nodeSizes, subtreeWidths);

            var levels = BuildLevels(rootNode);
            var levelY = StartY;

            foreach (var level in levels)
            {
                if (level.Count == 0)
                    continue;

                var totalWidth = level.Sum(node => subtreeWidths.GetValueOrDefault(node, nodeSizes[node].x));
                var startX = StartX - totalWidth / 2f;
                var currentX = startX;

                foreach (var node in level)
                {
                    var subtreeWidth = subtreeWidths.GetValueOrDefault(node, nodeSizes[node].x);
                    var nodeWidth = nodeSizes[node].x;
                    var nodeX = currentX + (subtreeWidth - nodeWidth) / 2f;
                    var nodeY = levelY;

                    layoutData[node] = new Rect(nodeX, nodeY, nodeSizes[node].x, nodeSizes[node].y);
                    currentX += subtreeWidth + HorizontalSpacing;
                }

                var maxHeight = level.Max(node => nodeSizes[node].y);
                levelY += maxHeight + VerticalSpacing;
            }

            return layoutData;
        }

        private static void CalculateSubtreeWidths(BTNode node, Dictionary<BTNode, Vector2> nodeSizes,
            Dictionary<BTNode, float> subtreeWidths)
        {
            var children = BTNodeEditorService.GetChildren(node).ToList();

            if (children.Count == 0)
            {
                subtreeWidths[node] = nodeSizes[node].x;
                return;
            }

            foreach (var child in children.Where(child => child != null))
            {
                CalculateSubtreeWidths(child, nodeSizes, subtreeWidths);
            }

            var childrenWidth = children
                .Where(c => c != null)
                .Sum(c => subtreeWidths.GetValueOrDefault(c, nodeSizes[c].x) + HorizontalSpacing) - HorizontalSpacing;

            if (childrenWidth < 0)
                childrenWidth = 0;

            subtreeWidths[node] = Mathf.Max(nodeSizes[node].x, childrenWidth);
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