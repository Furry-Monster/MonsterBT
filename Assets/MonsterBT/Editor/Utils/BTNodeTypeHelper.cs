using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MonsterBT.Runtime;

namespace MonsterBT.Editor
{
    public static class BTNodeTypeHelper
    {
        /// <summary>
        /// 获取所有节点类型，按分类组织
        /// </summary>
        public static Dictionary<string, List<Type>> GetAllNodeTypes()
        {
            var nodeTypes = new Dictionary<string, List<Type>>
            {
                ["Composite"] = new List<Type>(),
                ["Decorator"] = new List<Type>(),
                ["Action"] = new List<Type>(),
                ["Condition"] = new List<Type>(),
            };

            var assembly = typeof(BTNode).Assembly;
            var allTypes = assembly.GetTypes()
                .Where(type => typeof(BTNode).IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               type != typeof(BTNode) &&
                               type != typeof(RootNode))
                .ToList();

            foreach (var type in allTypes)
            {
                var category = DetermineNodeCategory(type);
                if (nodeTypes.ContainsKey(category))
                {
                    nodeTypes[category].Add(type);
                }
            }

            foreach (var category in nodeTypes.Keys.ToList())
            {
                nodeTypes[category] = nodeTypes[category].OrderBy(t => t.Name).ToList();
            }

            return nodeTypes;
        }

        /// <summary>
        /// 根据类型确定节点分类
        /// </summary>
        public static string DetermineNodeCategory(Type type)
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