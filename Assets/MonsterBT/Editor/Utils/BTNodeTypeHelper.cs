using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MonsterBT.Runtime;

namespace MonsterBT.Editor
{
    public static class BTNodeTypeHelper
    {
        public static Dictionary<string, List<Type>> GetAllNodeTypes()
        {
            var nodeTypes = new Dictionary<string, List<Type>>();

            var assembly = typeof(BTNode).Assembly;
            var allTypes = assembly.GetTypes()
                .Where(type => typeof(BTNode).IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               type != typeof(BTNode) &&
                               type != typeof(RootNode))
                .ToList();

            foreach (var type in allTypes)
            {
                var category = GetNodeCategory(type);
                var subCategory = GetNodeSubCategory(type);

                var finalCategory = category;
                if (!string.IsNullOrEmpty(subCategory))
                {
                    finalCategory = $"{category}/{subCategory}";
                }

                if (!nodeTypes.ContainsKey(finalCategory))
                {
                    nodeTypes[finalCategory] = new List<Type>();
                }

                nodeTypes[finalCategory].Add(type);
            }

            foreach (var category in nodeTypes.Keys.ToList())
            {
                nodeTypes[category] = nodeTypes[category].OrderBy(t => t.Name).ToList();
            }

            return nodeTypes;
        }

        public static string GetNodeCategory(Type type)
        {
            if (typeof(CompositeNode).IsAssignableFrom(type))
                return "Composite";

            if (typeof(DecoratorNode).IsAssignableFrom(type))
                return "Decorator";

            if (typeof(ActionNode).IsAssignableFrom(type))
            {
                if (type.Namespace?.Contains("Conditions") == true ||
                    type.Name.Contains("Condition") ||
                    type.Name.Contains("Check"))
                {
                    return "Condition";
                }

                return "Action";
            }

            return "Other";
        }

        public static string GetNodeSubCategory(Type type)
        {
            var namespaceName = type.Namespace ?? "";
            var typeName = type.Name;

            if (namespaceName.Contains("Animation") || typeName.Contains("Animation") || typeName.Contains("Animator"))
                return "Animation";

            if (namespaceName.Contains("Navigation") || namespaceName.Contains("NavMesh") ||
                typeName.Contains("Navigation") || typeName.Contains("NavMesh") || typeName.Contains("Path"))
                return "Navigation";

            if (namespaceName.Contains("Movement") || typeName.Contains("Move") || typeName.Contains("Patrol"))
                return "Movement";

            if (namespaceName.Contains("Combat") || typeName.Contains("Attack") || typeName.Contains("Combat"))
                return "Combat";

            return null;
        }

        /// <summary>
        /// 获取节点的显示名称
        /// </summary>
        public static string GetNodeDisplayName(Type type)
        {
            var typeName = type.Name;

            if (typeName.EndsWith("Node"))
                typeName = typeName[..^4];

            if (typeName.EndsWith("Action"))
                typeName = typeName[..^6];
            else if (typeName.EndsWith("Condition") || typeName.EndsWith("Decorator") || typeName.EndsWith("Composite"))
                typeName = typeName[..^9];

            return Regex.Replace(typeName, "(?<!^)([A-Z])", " $1");
        }
    }
}