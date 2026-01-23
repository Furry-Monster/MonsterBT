using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public static class BTEditorResources
    {
        private static string editorDirectory;

        public static StyleSheet LoadStyleSheet(string fileName)
        {
            var editorDir = GetEditorDirectory();
            if (string.IsNullOrEmpty(editorDir))
            {
                Debug.LogError("BTEditorResources: 无法获取编辑器目录");
                return null;
            }

            var stylesPath = Path.Combine(editorDir, "Styles", fileName).Replace('\\', '/');
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesPath);
            if (styleSheet != null) return styleSheet;

            var ussPath = Path.Combine(editorDir, fileName).Replace('\\', '/');
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet != null) return styleSheet;

            var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName) + " t:StyleSheet");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Contains("/Editor/") && assetPath.EndsWith(fileName))
                {
                    styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
                    if (styleSheet != null) return styleSheet;
                }
            }

            Debug.LogWarning($"BTEditorResources: 无法找到样式表 '{fileName}'");
            return null;
        }

        private static string GetEditorDirectory()
        {
            if (!string.IsNullOrEmpty(editorDirectory)) return editorDirectory;

            var guids = AssetDatabase.FindAssets("BTEditorWindow t:MonoScript");
            if (guids.Length > 0)
            {
                editorDirectory = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));
                return editorDirectory;
            }

            guids = AssetDatabase.FindAssets("BTEditorResources t:MonoScript");
            if (guids.Length > 0)
            {
                editorDirectory = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));
                return editorDirectory;
            }

            return null;
        }
    }
}