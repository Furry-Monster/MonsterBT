using MonsterBT.Runtime;
using UnityEditor;
using UnityEngine;

namespace MonsterBT.Editor.Service.Asset
{
    public static class BTEditorAssetService
    {
        public static BTNode CreateNodeInAsset(BehaviorTree behaviorTree, System.Type nodeType, string nodeName = null)
        {
            if (behaviorTree == null || nodeType == null)
                return null;

            if (!typeof(BTNode).IsAssignableFrom(nodeType))
                return null;

            var assetPath = AssetDatabase.GetAssetPath(behaviorTree);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("BehaviorTree must be saved to disk before adding nodes.");
                return null;
            }

            try
            {
                var node = ScriptableObject.CreateInstance(nodeType) as BTNode;
                if (node == null)
                    return null;

                node.name = nodeName ?? nodeType.Name;
                AssetDatabase.AddObjectToAsset(node, behaviorTree);
                MarkDirty(behaviorTree);

                return node;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create node in asset: {ex.Message}");
                return null;
            }
        }

        public static void MarkDirty(Object obj)
        {
            if (obj == null)
                return;

            EditorUtility.SetDirty(obj);
        }

        public static void MarkDirty(params Object[] objects)
        {
            if (objects == null)
                return;

            foreach (var obj in objects)
            {
                if (obj != null)
                    EditorUtility.SetDirty(obj);
            }
        }
    }
}