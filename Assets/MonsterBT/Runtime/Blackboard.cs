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
        // 序列化的值存储（用于 Editor 中设置的值）
        [SerializeField] private List<SerializablePair<string, bool>> boolValues = new();
        [SerializeField] private List<SerializablePair<string, float>> floatValues = new();
        [SerializeField] private List<SerializablePair<string, string>> stringValues = new();
        [SerializeField] private List<SerializablePair<string, Vector3>> vector3Values = new();
        [SerializeField] private List<SerializablePair<string, GameObject>> gameObjectValues = new();
        [SerializeField] private List<SerializablePair<string, Transform>> transformValues = new();
        // 黑板变量信息表，用于提供Editor界面信息
        [SerializeField] [ReadOnly] private List<BlackboardVariableInfo> variableInfos = new();

        // 运行时黑板，用于和行为树交互
        private readonly SerializableDictionary<string, object> data = new();
        private bool initialized = false;

        private void OnEnable()
        {
            if (initialized) 
                return;
            
            LoadSerializedValues();
            initialized = true;
        }

        private void LoadSerializedValues()
        {
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
                return default;
            }

            // 从序列化值中加载
            var serializedValue = LoadSerializedValue<T>(key);
            data[key] = serializedValue;
            return serializedValue;
        }

        private T LoadSerializedValue<T>(string key)
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

            return default;
        }

        private T LoadFromSerializedList<T>(List<SerializablePair<string, T>> list, string key, T value)
        {
            foreach (var pair in list.Where(pair => pair.key == key))
            {
                return pair.value;
            }

            return value;
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
            return list.Any(pair => pair.key == key);
        }

        public void SetValue<T>(string key, T value)
        {
            data[key] = value;
            SaveSerializedValue(key, value);
        }

        private void SaveSerializedValue<T>(string key, T value)
        {
            switch (value)
            {
                // 保存值到序列化列表，以便 Unity 可以序列化
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
                    CheckSceneObjectRef(key, gameObjectVal);
                    SaveToSerializedList(gameObjectValues, key, gameObjectVal);
                    break;
                case Transform transformVal:
                    CheckSceneObjectRef(key, transformVal.gameObject);
                    SaveToSerializedList(transformValues, key, transformVal);
                    break;
            }
        }

#if UNITY_EDITOR
       private static void CheckSceneObjectRef(string key, GameObject gameObject)
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
        private static void CheckAndWarnSceneObject(string key, GameObject gameObject)
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

       [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public void AddVariable(string name, Type type, object value = null, bool isExposed = true)
        {
            if (HasKey(name)) return;

            if (value != null)
            {
                if (type == typeof(bool))
                    SetBool(name, (bool)value);
                else if (type == typeof(float))
                    SetFloat(name, (float)value);
                else if (type == typeof(string))
                    SetString(name, (string)value);
                else if (type == typeof(Vector3))
                    SetVector3(name, (Vector3)value);
                else if (type == typeof(GameObject))
                    SetGameObject(name, (GameObject)value);
                else if (type == typeof(Transform))
                    SetTransform(name, (Transform)value);
                else
                    SetValue(name, value);
            }
            else
            {
                // 即使序列化值为 null，也要初始化序列化列表中的条目
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

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public void RemoveVariable(string name)
        {
            RemoveKey(name);
            variableInfos.RemoveAll(info => info.name == name);
        }

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

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public Type GetVariableType(string name)
        {
            var info = variableInfos.Find(v => v.name == name);
            return string.IsNullOrEmpty(info.typeName) ? null : Type.GetType(info.typeName);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return data.Keys;
        }

        #endregion
    }
}