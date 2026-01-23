using MonsterBT.Runtime;
using UnityEditor;
using UnityEngine;

namespace MonsterBT.Editor.Services
{
    public static class BTBehaviorTreeService
    {
        public static BehaviorTree CreateNewBehaviorTree(string defaultName = "New BehaviorTree")
        {
            var tree = ScriptableObject.CreateInstance<BehaviorTree>();
            var rootNode = ScriptableObject.CreateInstance<RootNode>();
            var blackboard = ScriptableObject.CreateInstance<Blackboard>();

            rootNode.name = "Root";
            rootNode.Position = new Vector2(400, 100);
            blackboard.name = "Blackboard";

            tree.RootNode = rootNode;
            tree.Blackboard = blackboard;
            tree.name = defaultName;

            var path = EditorUtility.SaveFilePanelInProject("Save Behavior Tree", defaultName, "asset", "");
            if (string.IsNullOrEmpty(path))
                return null;

            AssetDatabase.CreateAsset(tree, path);
            AssetDatabase.AddObjectToAsset(rootNode, tree);
            AssetDatabase.AddObjectToAsset(blackboard, tree);
            AssetDatabase.SaveAssets();

            return tree;
        }

        public static void SaveBehaviorTree(BehaviorTree tree)
        {
            if (tree == null)
                return;

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            Debug.Log("Behavior tree saved.");
        }

        public static void EnsureBlackboardExists(BehaviorTree tree)
        {
            if (tree == null || tree.Blackboard != null)
                return;

            var blackboard = ScriptableObject.CreateInstance<Blackboard>();
            blackboard.name = "Blackboard";
            tree.Blackboard = blackboard;

            var assetPath = AssetDatabase.GetAssetPath(tree);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.AddObjectToAsset(blackboard, tree);
                EditorUtility.SetDirty(tree);
                AssetDatabase.SaveAssets();
            }
        }
    }
}