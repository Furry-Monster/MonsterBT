using MonsterBT.Runtime;
using UnityEditor;
using UnityEngine;

namespace MonsterBT.Editor
{
    /// <summary>
    /// 编辑器资源管理辅助类，统一处理节点和资源的创建、保存等操作
    /// </summary>
    public static class BTEditorAssetHelper
    {
        /// <summary>
        /// 创建节点并添加到行为树资源中
        /// </summary>
        public static BTNode CreateNodeInAsset(BehaviorTree behaviorTree, System.Type nodeType, string nodeName = null)
        {
            if (behaviorTree == null || nodeType == null)
                return null;

            if (!typeof(BTNode).IsAssignableFrom(nodeType))
                return null;

            var node = ScriptableObject.CreateInstance(nodeType) as BTNode;
            if (node == null)
                return null;

            node.name = nodeName ?? nodeType.Name;
            AssetDatabase.AddObjectToAsset(node, behaviorTree);
            MarkDirtyAndSave(behaviorTree);

            return node;
        }

        /// <summary>
        /// 标记对象为脏并保存资源
        /// </summary>
        public static void MarkDirtyAndSave(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 标记多个对象为脏并保存资源
        /// </summary>
        public static void MarkDirtyAndSave(params UnityEngine.Object[] objects)
        {
            if (objects == null)
                return;

            foreach (var obj in objects)
            {
                if (obj != null)
                    EditorUtility.SetDirty(obj);
            }

            AssetDatabase.SaveAssets();
        }
    }
}