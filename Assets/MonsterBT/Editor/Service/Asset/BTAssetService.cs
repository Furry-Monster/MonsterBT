using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonsterBT.Editor.Service.Operation;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEngine;

namespace MonsterBT.Editor.Service.Asset
{
    public static class BTAssetService
    {
        public static bool AutoFixBehaviourTree(BehaviorTree behaviorTree)
        {
            if (behaviorTree == null)
                return false;

            var modified = false;

            if (behaviorTree.Blackboard == null)
            {
                BTBehaviorTreeService.EnsureBlackboardExists(behaviorTree);
                modified = true;
            }

            if (behaviorTree.RootNode == null)
            {
                var rootNode = ScriptableObject.CreateInstance<RootNode>();
                rootNode.name = "Root";
                rootNode.Position = new Vector2(400, 100);
                behaviorTree.RootNode = rootNode;

                var assetPath = AssetDatabase.GetAssetPath(behaviorTree);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.AddObjectToAsset(rootNode, behaviorTree);
                    modified = true;
                }
            }

            modified |= CleanupDestroyedNodeReferences(behaviorTree);
            modified |= ValidateNodeConnections(behaviorTree);

            if (modified)
                EditorUtility.SetDirty(behaviorTree);

            return modified;
        }

        private static bool CleanupDestroyedNodeReferences(BehaviorTree behaviorTree)
        {
            var modified = false;

            if (behaviorTree.RootNode != null && behaviorTree.RootNode.Equals(null))
            {
                behaviorTree.RootNode = null;
                modified = true;
            }

            var allNodes = GetAllNodesInAsset(behaviorTree);
            foreach (var node in allNodes)
            {
                if (node == null || node.Equals(null))
                    continue;

                modified |= CleanupNodeChildren(node);
            }

            return modified;
        }

        private static bool CleanupNodeChildren(BTNode node)
        {
            var modified = false;

            var childrenProperty = node.GetType().GetProperty("Children",
                BindingFlags.Public | BindingFlags.Instance);
            if (childrenProperty != null)
            {
                if (childrenProperty.GetValue(node) is IList children)
                {
                    var toRemove = children
                        .Cast<BTNode>()
                        .Where(child => child == null || child.Equals(null))
                        .ToList();

                    foreach (var child in toRemove)
                    {
                        children.Remove(child);
                        modified = true;
                    }
                }
            }

            var childProperty = node.GetType().GetProperty("Child",
                BindingFlags.Public | BindingFlags.Instance);
            if (childProperty != null && childProperty.CanWrite)
            {
                var child = childProperty.GetValue(node) as BTNode;
                if (child != null && child.Equals(null))
                {
                    childProperty.SetValue(node, null);
                    modified = true;
                }
            }

            return modified;
        }

        private static bool ValidateNodeConnections(BehaviorTree behaviorTree)
        {
            var modified = false;
            var allNodes = GetAllNodesInAsset(behaviorTree).ToHashSet();

            foreach (var node in allNodes)
            {
                if (node == null || node.Equals(null))
                    continue;

                var children = BTNodeEditorService.GetChildren(node).ToList();
                foreach (var child in children)
                {
                    if (child == null || child.Equals(null))
                    {
                        modified |= CleanupNodeChildren(node);
                    }
                    else if (!allNodes.Contains(child))
                    {
                        BTNodeEditorService.RemoveChild(node, child);
                        modified = true;
                    }
                }
            }

            return modified;
        }

        private static IEnumerable<BTNode> GetAllNodesInAsset(BehaviorTree behaviorTree)
        {
            var assetPath = AssetDatabase.GetAssetPath(behaviorTree);
            if (string.IsNullOrEmpty(assetPath))
                yield break;

            var allNodes = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<BTNode>()
                .Where(node => node != null && !node.Equals(null));

            foreach (var node in allNodes)
            {
                yield return node;
            }
        }
    }
}