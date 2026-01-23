using System;
using System.Collections.Generic;
using System.Reflection;
using MonsterBT.Runtime;
using UnityEditor.Experimental.GraphView;

namespace MonsterBT.Editor.Services
{
    public static class BTNodeEditorService
    {
        public static IEnumerable<BTNode> GetChildren(BTNode node)
        {
            if (node == null)
                yield break;

            var childrenProperty = node.GetType().GetProperty("Children",
                BindingFlags.Public | BindingFlags.Instance);
            if (childrenProperty != null)
            {
                var children = childrenProperty.GetValue(node) as System.Collections.IList;
                if (children != null)
                {
                    foreach (BTNode child in children)
                    {
                        if (child != null)
                            yield return child;
                    }
                }

                yield break;
            }

            var childProperty = node.GetType().GetProperty("Child",
                BindingFlags.Public | BindingFlags.Instance);
            if (childProperty != null)
            {
                var child = childProperty.GetValue(node) as BTNode;
                if (child != null)
                    yield return child;
            }
        }

        public static bool SetChild(BTNode parent, BTNode child)
        {
            if (parent == null || child == null)
                return false;

            var childrenProperty = parent.GetType().GetProperty("Children",
                BindingFlags.Public | BindingFlags.Instance);
            if (childrenProperty != null)
            {
                var children = childrenProperty.GetValue(parent) as System.Collections.IList;
                if (children != null)
                {
                    children.Add(child);
                    return true;
                }
            }

            var childProperty = parent.GetType().GetProperty("Child",
                BindingFlags.Public | BindingFlags.Instance);
            if (childProperty != null && childProperty.CanWrite)
            {
                childProperty.SetValue(parent, child);
                return true;
            }

            return false;
        }

        public static bool RemoveChild(BTNode parent, BTNode child)
        {
            if (parent == null || child == null)
                return false;

            var childrenProperty = parent.GetType().GetProperty("Children",
                BindingFlags.Public | BindingFlags.Instance);
            if (childrenProperty != null)
            {
                var children = childrenProperty.GetValue(parent) as System.Collections.IList;
                if (children != null)
                {
                    children.Remove(child);
                    return true;
                }
            }

            var childProperty = parent.GetType().GetProperty("Child",
                BindingFlags.Public | BindingFlags.Instance);
            if (childProperty != null && childProperty.CanWrite)
            {
                var currentChild = childProperty.GetValue(parent) as BTNode;
                if (currentChild == child)
                {
                    childProperty.SetValue(parent, null);
                    return true;
                }
            }

            return false;
        }

        public static bool HasOutputPort(BTNode node)
        {
            if (node == null)
                return false;

            return typeof(CompositeNode).IsAssignableFrom(node.GetType()) ||
                   typeof(DecoratorNode).IsAssignableFrom(node.GetType()) ||
                   node is RootNode;
        }

        public static bool HasInputPort(BTNode node)
        {
            if (node == null)
                return false;

            return !(node is RootNode);
        }

        public static Port.Capacity GetOutputPortCapacity(BTNode node)
        {
            if (node == null)
                return Port.Capacity.Single;

            if (typeof(CompositeNode).IsAssignableFrom(node.GetType()) || node is RootNode)
                return Port.Capacity.Multi;

            if (typeof(DecoratorNode).IsAssignableFrom(node.GetType()))
                return Port.Capacity.Single;

            return Port.Capacity.Single;
        }

        public static string GetNodeStyleClass(BTNode node)
        {
            if (node == null)
                return "node";

            if (node is RootNode)
                return "root-node";
            if (typeof(CompositeNode).IsAssignableFrom(node.GetType()))
                return "composite-node";
            if (typeof(DecoratorNode).IsAssignableFrom(node.GetType()))
                return "decorator-node";
            if (typeof(ActionNode).IsAssignableFrom(node.GetType()))
                return "action-node";

            return "node";
        }

        public static bool CanDeleteNode(BTNode node)
        {
            return !(node is RootNode);
        }
    }
}