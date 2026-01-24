using System;
using System.Collections.Generic;
using System.Linq;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonsterBT.Editor.Services
{
    public class BTNodeOperationService
    {
        private readonly BehaviorTree behaviorTree;
        private readonly Dictionary<BTNode, BTNodeView> nodeViews;
        private readonly GraphView graphView;

        public BTNodeOperationService(BehaviorTree behaviorTree, Dictionary<BTNode, BTNodeView> nodeViews,
            GraphView graphView)
        {
            this.behaviorTree = behaviorTree;
            this.nodeViews = nodeViews;
            this.graphView = graphView;
        }

        public BTNodeView CreateNode(Type nodeType, Vector2 position, Func<BTNode, BTNodeView> createNodeView)
        {
            if (behaviorTree == null || createNodeView == null)
                return null;

            var node = BTEditorAssetService.CreateNodeInAsset(behaviorTree, nodeType);
            if (node == null)
                return null;

            var nodeView = createNodeView(node);
            if (nodeView != null)
            {
                nodeView.SetPosition(new Rect(position, Vector2.zero));
            }

            return nodeView;
        }

        public void DeleteNode(BTNodeView nodeView)
        {
            if (!BTNodeEditorService.CanDeleteNode(nodeView.Node))
            {
                Debug.LogWarning("Cannot delete root node!");
                return;
            }

            RemoveNodeFromGraph(nodeView.Node, nodeView);
            BTAssetService.AutoFixBehaviourTree(behaviorTree);
            BTEditorAssetService.MarkDirty(behaviorTree);
        }

        public void DuplicateNode(BTNodeView nodeView, Vector2 offset, Func<BTNode, BTNodeView> createNodeView)
        {
            if (behaviorTree == null || nodeView?.Node == null || nodeView.Node.Equals(null))
                return;

            if (!BTNodeEditorService.CanDuplicateNode(nodeView.Node, behaviorTree))
            {
                Debug.LogWarning("Cannot duplicate root node!");
                return;
            }

            var originalPos = nodeView.GetPosition();
            var newPos = new Rect(originalPos.x + offset.x, originalPos.y + offset.y, originalPos.width,
                originalPos.height);
            CreateNodeFromTemplate(nodeView.Node, " (Copy)", newPos, createNodeView);
        }

        public void PasteNode(BTNode copiedNode, Vector2 position, Func<BTNode, BTNodeView> createNodeView)
        {
            if (copiedNode == null || behaviorTree == null)
                return;

            if (copiedNode.Equals(null))
            {
                Debug.LogWarning("Copied node has been destroyed.");
                return;
            }

            if (!BTNodeEditorService.CanCopyNode(copiedNode, behaviorTree))
            {
                Debug.LogWarning("Cannot paste root node!");
                return;
            }

            CreateNodeFromTemplate(copiedNode, " (Paste)", new Rect(position, Vector2.zero), createNodeView);
        }


        private void CreateNodeFromTemplate(BTNode templateNode, string nameSuffix, Rect position,
            Func<BTNode, BTNodeView> createNodeView)
        {
            try
            {
                var newNode = Object.Instantiate(templateNode);
                newNode.name = templateNode.name + nameSuffix;

                foreach (var child in BTNodeEditorService.GetChildren(newNode).ToList())
                {
                    BTNodeEditorService.RemoveChild(newNode, child);
                }

                AssetDatabase.AddObjectToAsset(newNode, behaviorTree);
                BTEditorAssetService.MarkDirty(behaviorTree);

                var nodeView = createNodeView?.Invoke(newNode);
                if (nodeView != null)
                {
                    nodeView.SetPosition(position);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create node from template: {ex.Message}");
            }
        }

        public void RemoveNodeFromGraph(BTNode node, BTNodeView nodeView)
        {
            var edgesToRemove = graphView.graphElements.OfType<Edge>()
                .Where(edge => edge.output.node == nodeView || edge.input.node == nodeView)
                .ToList();

            foreach (var edge in edgesToRemove)
            {
                graphView.RemoveElement(edge);
            }

            if (node != null)
            {
                foreach (var (parentNode, _) in nodeViews)
                {
                    if (parentNode == null || parentNode.Equals(null))
                        continue;

                    BTNodeEditorService.RemoveChild(parentNode, node);
                }
            }

            graphView.RemoveElement(nodeView);
            nodeViews.Remove(node);

            try
            {
                Object.DestroyImmediate(node, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to destroy node: {ex.Message}");
            }
        }
    }
}