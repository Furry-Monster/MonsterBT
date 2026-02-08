using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MonsterBT.Runtime;
using UnityEditor;

namespace MonsterBT.Editor.Base
{
    public static class BTNodeTypeHelper
    {
        public static Dictionary<string, List<Type>> GetAllNodeTypes()
        {
            var nodeTypes = new Dictionary<string, List<Type>>();
            var allTypes = GetBTNodeTypesFromAllAssemblies();

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

        /// <summary>
        /// 从所有已加载程序集中收集 BTNode 子类，使通过 Git URL 安装插件后，
        /// 项目内自定义节点也能被 Editor 识别。
        /// </summary>
        private static List<Type> GetBTNodeTypesFromAllAssemblies()
        {
#if UNITY_2021_2_OR_NEWER
            return TypeCache.GetTypesDerivedFrom<BTNode>()
                .Where(type => !type.IsAbstract && type != typeof(BTNode) && type != typeof(RootNode))
                .ToList();
#else
            var list = new List<Type>();
            var baseType = typeof(BTNode);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type == baseType || type == typeof(RootNode))
                            continue;
                        if (baseType.IsAssignableFrom(type))
                            list.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException) { /* 忽略无法完全加载的程序集 */ }
            }
            return list;
#endif
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