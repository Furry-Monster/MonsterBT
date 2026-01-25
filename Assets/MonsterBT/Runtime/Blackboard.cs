using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MonsterBT.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace MonsterBT.Runtime
{
    [Serializable]
    public struct BlackboardVariableInfo
    {
        public string name;
        public string typeName;
        public bool isExposed; // 是否在编辑器中暴露
    }

    [CreateAssetMenu(fileName = "Blackboard", menuName = "MonsterBT/Blackboard")]
    public class Blackboard : ScriptableObject
    {
        // 序列化的默认值存储（用于 Editor 中设置的默认值）
        [SerializeField] private List<SerializablePair<string, bool>> boolValues = new();
        [SerializeField] private List<SerializablePair<string, float>> floatValues = new();
        [SerializeField] private List<SerializablePair<string, string>> stringValues = new();
        [SerializeField] private List<SerializablePair<string, Vector3>> vector3Values = new();
        [SerializeField] private List<SerializablePair<string, GameObject>> gameObjectValues = new();
        [SerializeField] private List<SerializablePair<string, Transform>> transformValues = new();

        // 运行时黑板，用于和行为树交互
        private readonly SerializableDictionary<string, object> data = new();

        // 黑板变量信息表，用于提供Editor界面信息
        [SerializeField] [ReadOnly] private List<BlackboardVariableInfo> variableInfos = new();

        private bool initialized = false;

        private void OnEnable()
        {
            if (!initialized)
            {
                LoadDefaultValuesToRuntime();
                initialized = true;
            }
        }

        private void LoadDefaultValuesToRuntime()
        {
            // 从序列化数据加载默认值到运行时字典
            foreach (var pair in boolValues.Where(pair => !data.ContainsKey(pair.key)))
            {
                data[pair.key] = pair.value;
            }

            foreach (var pair in floatValues.Where(pair => !data.ContainsKey(pair.key)))
            {
                data[pair.key] = pair.value;
            }

            foreach (var pair in stringValues.Where(pair => !data.ContainsKey(pair.key)))
            {
                data[pair.key] = pair.value;
            }

            foreach (var pair in vector3Values.Where(pair => !data.ContainsKey(pair.key)))
            {
                data[pair.key] = pair.value;
            }

            foreach (var pair in gameObjectValues.Where(pair => !data.ContainsKey(pair.key)))
            {
                data[pair.key] = pair.value;
            }

            foreach (var pair in transformValues.Where(pair => !data.ContainsKey(pair.key)))
            {
                data[pair.key] = pair.value;
            }
        }

        #region Public Methods

        public T GetValue<T>(string key)
        {
            // 首先尝试从运行时字典获取
            if (data.TryGetValue(key, out var value))
            {
                if (value is T output)
                    return output;
            }

            // 如果运行时字典中没有，检查序列化数据中是否有该键
            if (!HasKeyInSerializedData(key))
            {
                return default(T);
            }

            // 从序列化的默认值中加载
            var defaultValue = LoadDefaultValue<T>(key);
            // 将默认值加载到运行时字典
            data[key] = defaultValue;
            return defaultValue;
        }

        private T LoadDefaultValue<T>(string key)
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)LoadFromSerializedList(boolValues, key, false);
            }

            if (typeof(T) == typeof(float))
            {
                return (T)(object)LoadFromSerializedList(floatValues, key, 0f);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)LoadFromSerializedList(stringValues, key, "");
            }

            if (typeof(T) == typeof(Vector3))
            {
                return (T)(object)LoadFromSerializedList(vector3Values, key, Vector3.zero);
            }

            if (typeof(T) == typeof(GameObject))
            {
                return (T)(object)LoadFromSerializedList(gameObjectValues, key, null);
            }

            if (typeof(T) == typeof(Transform))
            {
                return (T)(object)LoadFromSerializedList(transformValues, key, null);
            }

            return default(T);
        }

        private T LoadFromSerializedList<T>(List<SerializablePair<string, T>> list, string key, T defaultValue)
        {
            foreach (var pair in list)
            {
                if (pair.key == key)
                    return pair.value;
            }

            return defaultValue;
        }

        private bool HasKeyInSerializedData(string key)
        {
            return HasKeyInList(boolValues, key) ||
                   HasKeyInList(floatValues, key) ||
                   HasKeyInList(stringValues, key) ||
                   HasKeyInList(vector3Values, key) ||
                   HasKeyInList(gameObjectValues, key) ||
                   HasKeyInList(transformValues, key);
        }

        private bool HasKeyInList<T>(List<SerializablePair<string, T>> list, string key)
        {
            foreach (var pair in list)
            {
                if (pair.key == key)
                    return true;
            }

            return false;
        }

        public void SetValue<T>(string key, T value)
        {
            data[key] = value;
            SaveDefaultValue(key, value);
        }

        private void SaveDefaultValue<T>(string key, T value)
        {
            switch (value)
            {
                // 保存默认值到序列化列表，以便 Unity 可以序列化
                case bool boolVal:
                    SaveToSerializedList(boolValues, key, boolVal);
                    break;
                case float floatVal:
                    SaveToSerializedList(floatValues, key, floatVal);
                    break;
                case string stringVal:
                    SaveToSerializedList(stringValues, key, stringVal);
                    break;
                case Vector3 vector3Val:
                    SaveToSerializedList(vector3Values, key, vector3Val);
                    break;
                case GameObject gameObjectVal:
                    CheckAndWarnSceneObject(key, gameObjectVal);
                    SaveToSerializedList(gameObjectValues, key, gameObjectVal);
                    break;
                case Transform transformVal:
                    CheckAndWarnSceneObject(key, transformVal.gameObject);
                    SaveToSerializedList(transformValues, key, transformVal);
                    break;
            }
        }

#if UNITY_EDITOR
       private void CheckAndWarnSceneObject(string key, GameObject gameObject)
        {
            if (gameObject == null)
                return;

            if (EditorUtility.IsPersistent(gameObject)) 
                return;
            
            var scenePath = gameObject.scene.path;
            var objectPath = GetGameObjectPath(gameObject);
            Debug.LogWarning(
                $"[Blackboard] GameObject '{key}' is set to a scene object '{objectPath}' in scene '{scenePath}'. " +
                "Scene object references cannot be persisted in ScriptableObject. " +
                "The reference will be lost when Unity Editor is closed. " +
                "Consider using a Prefab reference or set the value at runtime instead.",
                gameObject
            );
        }

       private static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null)
                return "null";

            var path = obj.name;
            var current = obj.transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
#else
        private void CheckAndWarnSceneObject(string key, GameObject gameObject)
        {
        }
#endif

        private void SaveToSerializedList<T>(List<SerializablePair<string, T>> list, string key, T value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].key == key)
                {
                    var pair = list[i];
                    pair.value = value;
                    list[i] = pair;
                    return;
                }
            }

            list.Add(new SerializablePair<string, T> { key = key, value = value });
        }

        public bool HasKey(string key)
        {
            return data.ContainsKey(key) || HasKeyInSerializedData(key);
        }

        public void RemoveKey(string key)
        {
            data.Remove(key);
            RemoveFromSerializedList(boolValues, key);
            RemoveFromSerializedList(floatValues, key);
            RemoveFromSerializedList(stringValues, key);
            RemoveFromSerializedList(vector3Values, key);
            RemoveFromSerializedList(gameObjectValues, key);
            RemoveFromSerializedList(transformValues, key);
        }

        private void RemoveFromSerializedList<T>(List<SerializablePair<string, T>> list, string key)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].key == key)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }

        public void Clear()
        {
            data.Clear();
            boolValues.Clear();
            floatValues.Clear();
            stringValues.Clear();
            vector3Values.Clear();
            gameObjectValues.Clear();
            transformValues.Clear();
        }

        #endregion

        #region Helpers

        public GameObject GetGameObject(string key) => GetValue<GameObject>(key);
        public void SetGameObject(string key, GameObject value) => SetValue(key, value);

        public Transform GetTransform(string key) => GetValue<Transform>(key);
        public void SetTransform(string key, Transform value) => SetValue(key, value);

        public Vector3 GetVector3(string key) => GetValue<Vector3>(key);
        public void SetVector3(string key, Vector3 value) => SetValue(key, value);

        public float GetFloat(string key) => GetValue<float>(key);
        public void SetFloat(string key, float value) => SetValue(key, value);

        public bool GetBool(string key) => GetValue<bool>(key);
        public void SetBool(string key, bool value) => SetValue(key, value);

        public string GetString(string key) => GetValue<string>(key);
        public void SetString(string key, string value) => SetValue(key, value);

        #endregion

        #region Serialization

        public IReadOnlyList<BlackboardVariableInfo> GetVariableInfos()
        {
            return variableInfos;
        }

        /// <summary>
        /// 添加变量
        /// </summary>
        /// <param name="name">变量名（Key名）</param>
        /// <param name="type">变量类型</param>
        /// <param name="defaultValue">默认值，可为空</param>
        /// <param name="isExposed">是否在Editor暴露</param>
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public void AddVariable(string name, Type type, object defaultValue = null, bool isExposed = true)
        {
            if (HasKey(name)) return;

            // 设置默认值（会同时保存到序列化列表）
            if (defaultValue != null)
            {
                if (type == typeof(bool))
                    SetBool(name, (bool)defaultValue);
                else if (type == typeof(float))
                    SetFloat(name, (float)defaultValue);
                else if (type == typeof(string))
                    SetString(name, (string)defaultValue);
                else if (type == typeof(Vector3))
                    SetVector3(name, (Vector3)defaultValue);
                else if (type == typeof(GameObject))
                    SetGameObject(name, (GameObject)defaultValue);
                else if (type == typeof(Transform))
                    SetTransform(name, (Transform)defaultValue);
                else
                    SetValue(name, defaultValue);
            }
            else
            {
                // 即使默认值为 null，也要初始化序列化列表中的条目
                if (type == typeof(bool))
                    SaveToSerializedList(boolValues, name, false);
                else if (type == typeof(float))
                    SaveToSerializedList(floatValues, name, 0f);
                else if (type == typeof(string))
                    SaveToSerializedList(stringValues, name, "");
                else if (type == typeof(Vector3))
                    SaveToSerializedList(vector3Values, name, Vector3.zero);
                else if (type == typeof(GameObject))
                    SaveToSerializedList(gameObjectValues, name, null);
                else if (type == typeof(Transform))
                    SaveToSerializedList(transformValues, name, null);
            }

            var info = new BlackboardVariableInfo
            {
                name = name,
                typeName = type.AssemblyQualifiedName,
                isExposed = isExposed
            };

            variableInfos.Add(info);
        }

        /// <summary>
        /// 删除指定变量
        /// </summary>
        /// <param name="name">Key名</param>
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public void RemoveVariable(string name)
        {
            RemoveKey(name);
            variableInfos.RemoveAll(info => info.name == name);
        }

        /// <summary>
        /// 重命名变量
        /// </summary>
        /// <param name="oldName">旧Key名</param>
        /// <param name="newName">新Key名</param>
        public void RenameVariable(string oldName, string newName)
        {
            if (!HasKey(oldName) || HasKey(newName)) return;

            // 从序列化列表中获取值并重命名
            RenameInSerializedList(boolValues, oldName, newName);
            RenameInSerializedList(floatValues, oldName, newName);
            RenameInSerializedList(stringValues, oldName, newName);
            RenameInSerializedList(vector3Values, oldName, newName);
            RenameInSerializedList(gameObjectValues, oldName, newName);
            RenameInSerializedList(transformValues, oldName, newName);

            // 从运行时字典中获取值并重命名
            if (data.TryGetValue(oldName, out var value))
            {
                data.Remove(oldName);
                data[newName] = value;
            }

            // 更新变量信息
            for (var i = 0; i < variableInfos.Count; i++)
            {
                if (variableInfos[i].name == oldName)
                {
                    var info = variableInfos[i];
                    info.name = newName;
                    variableInfos[i] = info;
                    break;
                }
            }
        }

        private void RenameInSerializedList<T>(List<SerializablePair<string, T>> list, string oldName, string newName)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].key == oldName)
                {
                    var pair = list[i];
                    pair.key = newName;
                    list[i] = pair;
                    break;
                }
            }
        }

        /// <summary>
        /// 获取指定Key名变量的类型
        /// </summary>
        /// <param name="name">Key名</param>
        /// <returns>变量类型</returns>
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public Type GetVariableType(string name)
        {
            var info = variableInfos.Find(v => v.name == name);
            return string.IsNullOrEmpty(info.typeName) ? null : Type.GetType(info.typeName);
        }

        /// <summary>
        /// 获取所有Key
        /// </summary>
        /// <returns>Key的一个迭代器</returns>
        public IEnumerable<string> GetAllKeys()
        {
            return data.Keys;
        }

        #endregion
    }
}