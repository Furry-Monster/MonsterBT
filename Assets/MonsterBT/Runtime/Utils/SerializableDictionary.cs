using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonsterBT.Runtime.Utils
{
    /// <summary>
    /// 可序列化键值对KVP
    /// </summary>
    /// <typeparam name="TKey">Key类型</typeparam>
    /// <typeparam name="TValue">Value类型</typeparam>
    [Serializable]
    public struct SerializablePair<TKey, TValue>
    {
        public TKey key;
        public TValue value;
    }

    /// <summary>
    /// 线程安全的Editor可序列化字典
    /// </summary>
    /// <typeparam name="TKey">Key类型</typeparam>
    /// <typeparam name="TValue">Value类型</typeparam>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> :
        ICollection<SerializablePair<TKey, TValue>>, ISerializationCallbackReceiver
    {
        // 序列化字典，用于显示在Inspector中
        [SerializeField]
        private List<SerializablePair<TKey, TValue>> serializableDict = new List<SerializablePair<TKey, TValue>>();

        // 运行时字典，用于处理数据
        private Dictionary<TKey, TValue> rawDict;

        // 线程锁
        private readonly object syncLock = new object();

        public int Count
        {
            get
            {
                lock (syncLock)
                {
                    return rawDict.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys
        {
            get
            {
                lock (syncLock)
                {
                    return rawDict.Keys.ToList();
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (syncLock)
                {
                    return rawDict.Values.ToList();
                }
            }
        }

        public SerializableDictionary()
        {
            rawDict = new Dictionary<TKey, TValue>();
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            rawDict = new Dictionary<TKey, TValue>(dictionary);
            lock (syncLock)
            {
                SyncToSerializable();
            }
        }

        #region API Methods

        public TValue this[TKey key]
        {
            get
            {
                lock (syncLock)
                {
                    return rawDict[key];
                }
            }
            set
            {
                lock (syncLock)
                {
                    rawDict[key] = value;
                    SyncToSerializable();
                }
            }
        }


        public void Add(TKey key, TValue value)
        {
            lock (syncLock)
            {
                rawDict.Add(key, value);
                SyncToSerializable();
            }
        }

        public bool Remove(TKey key)
        {
            lock (syncLock)
            {
                bool removed = rawDict.Remove(key);
                if (removed)
                    SyncToSerializable();
                return removed;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (syncLock)
            {
                return rawDict.TryGetValue(key, out value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (syncLock)
            {
                return rawDict.ContainsKey(key);
            }
        }

        public IEnumerator<SerializablePair<TKey, TValue>> GetEnumerator()
        {
            lock (syncLock)
            {
                return rawDict
                    .Select(kvp => new SerializablePair<TKey, TValue> { key = kvp.Key, value = kvp.Value })
                    .ToList()
                    .GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(SerializablePair<TKey, TValue> item)
        {
            Add(item.key, item.value);
        }

        public void Clear()
        {
            lock (syncLock)
            {
                rawDict.Clear();
                serializableDict.Clear();
            }
        }

        public bool Contains(SerializablePair<TKey, TValue> item)
        {
            lock (syncLock)
            {
                return rawDict.TryGetValue(item.key, out TValue value) &&
                       EqualityComparer<TValue>.Default.Equals(value, item.value);
            }
        }

        public void CopyTo(SerializablePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            lock (syncLock)
            {
                if (array.Length - arrayIndex < rawDict.Count)
                    throw new ArgumentException("Array is too small");

                int index = arrayIndex;
                foreach (var kvp in rawDict)
                {
                    array[index++] = new SerializablePair<TKey, TValue> { key = kvp.Key, value = kvp.Value };
                }
            }
        }

        public bool Remove(SerializablePair<TKey, TValue> item)
        {
            lock (syncLock)
            {
                if (rawDict.TryGetValue(item.key, out var value) &&
                    EqualityComparer<TValue>.Default.Equals(value, item.value))
                {
                    bool removed = rawDict.Remove(item.key);
                    if (removed)
                        SyncToSerializable();
                    return removed;
                }
                return false;
            }
        }

        #endregion

        #region Sync

        /// <summary>
        /// 同步到序列化字典,耗时
        /// </summary>
        private void SyncToSerializable()
        {
            serializableDict.Clear();
            foreach (var kvp in rawDict)
            {
                serializableDict.Add(new SerializablePair<TKey, TValue> { key = kvp.Key, value = kvp.Value });
            }
        }

        /// <summary>
        /// 同步到运行时字典,耗时
        /// </summary>
        private void SyncFromSerializable()
        {
            rawDict.Clear();
            foreach (var pair in serializableDict)
            {
                if (!rawDict.ContainsKey(pair.key))
                {
                    rawDict[pair.key] = pair.value;
                }
            }
        }

        public void OnBeforeSerialize()
        {
            if (rawDict != null)
            {
                lock (syncLock)
                {
                    SyncToSerializable();
                }
            }
        }

        public void OnAfterDeserialize()
        {
            lock (syncLock)
            {
                // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
                // NOTE:
                // 如果 rawDict 为 null，在检查和赋值之间存在一个时间窗口。
                // 尽管有锁保护，但在多线程环境下，??= 操作符本身不是原子的。
                if (rawDict == null)
                    rawDict = new Dictionary<TKey, TValue>();
                SyncFromSerializable();
            }
        }

        #endregion

    }
}