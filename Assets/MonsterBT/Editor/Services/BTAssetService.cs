using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEngine;

namespace MonsterBT.Editor.Services
{
    public static class BTAssetService
    {
        public static bool ValidateAndFixBehaviorTree(BehaviorTree behaviorTree)
        {
            if (behaviorTree == null)
                return false;

            bool needsSave = false;

            if (behaviorTree.Blackboard == null)
            {
                BTBehaviorTreeService.EnsureBlackboardExists(behaviorTree);
                needsSave = true;
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
                    needsSave = true;
                }
            }

            needsSave |= CleanupDestroyedNodeReferences(behaviorTree);
            needsSave |= ValidateNodeConnections(behaviorTree);

            if (needsSave)
            {
                EditorUtility.SetDirty(behaviorTree);
                AssetDatabase.SaveAssets();
            }

            return needsSave;
        }

        private static bool CleanupDestroyedNodeReferences(BehaviorTree behaviorTree)
        {
            bool needsSave = false;

            if (behaviorTree.RootNode != null && behaviorTree.RootNode.Equals(null))
            {
                behaviorTree.RootNode = null;
                needsSave = true;
            }

            var allNodes = GetAllNodesInAsset(behaviorTree);
            foreach (var node in allNodes)
            {
                if (node == null || node.Equals(null))
                    continue;

                needsSave |= CleanupNodeChildren(node);
            }

            return needsSave;
        }

        private static bool CleanupNodeChildren(BTNode node)
        {
            bool needsSave = false;

            var childrenProperty = node.GetType().GetProperty("Children",
                BindingFlags.Public | BindingFlags.Instance);
            if (childrenProperty != null)
            {
                var children = childrenProperty.GetValue(node) as System.Collections.IList;
                if (children != null)
                {
                    var toRemove = new List<BTNode>();
                    foreach (BTNode child in children)
                    {
                        if (child == null || child.Equals(null))
                        {
                            toRemove.Add(child);
                        }
                    }

                    foreach (var child in toRemove)
                    {
                        children.Remove(child);
                        needsSave = true;
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
                    needsSave = true;
                }
            }

            return needsSave;
        }

        private static bool ValidateNodeConnections(BehaviorTree behaviorTree)
        {
            bool needsSave = false;
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
                        needsSave |= CleanupNodeChildren(node);
                    }
                    else if (!allNodes.Contains(child))
                    {
                        BTNodeEditorService.RemoveChild(node, child);
                        needsSave = true;
                    }
                }
            }

            return needsSave;
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