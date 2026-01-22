using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MonsterBT.Runtime.Utils;
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
        // 运行时黑板，用于和行为树交互，使用线程安全的字典
        private readonly SerializableDictionary<string, object> data = new SerializableDictionary<string, object>();

        // 黑板变量信息表，用于提供Editor界面信息
        [SerializeField] [ReadOnly] private List<BlackboardVariableInfo> variableInfos =
            new List<BlackboardVariableInfo>();

        #region Public Methods

        public T GetValue<T>(string key)
        {
            if (data.TryGetValue(key, out object value))
            {
                if (value is T output)
                    return output;
            }
            return default(T);
        }

        public void SetValue<T>(string key, T value)
        {
            data[key] = value;
        }

        public bool HasKey(string key)
        {
            return data.ContainsKey(key);
        }

        public void RemoveKey(string key)
        {
            data.Remove(key);
        }

        public void Clear()
        {
            data.Clear();
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

            SetValue(name, defaultValue);

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

            var value = data[oldName];
            RemoveKey(oldName);
            SetValue(newName, value);

            for (int i = 0; i < variableInfos.Count; i++)
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