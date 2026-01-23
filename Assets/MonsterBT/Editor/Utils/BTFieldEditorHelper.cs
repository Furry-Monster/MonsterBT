using System;
using System.Reflection;
using MonsterBT.Editor.Services;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    /// <summary>
    /// 字段编辑器辅助类，统一处理节点字段的编辑器创建和回调
    /// </summary>
    public static class BTFieldEditorHelper
    {
        /// <summary>
        /// 创建字段值变更回调，统一处理 Undo、设置值、标记脏和发布事件
        /// </summary>
        public static EventCallback<ChangeEvent<TValue>> CreateFieldChangeCallback<TValue>(
            BTNode node, FieldInfo field, string displayName)
        {
            return evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                BTEditorEventBus.PublishPropertyChanged(node, field.Name);
            };
        }

        /// <summary>
        /// 创建字符串字段编辑器
        /// </summary>
        public static TextField CreateStringField(FieldInfo field, BTNode node, string displayName)
        {
            var textField = new TextField(displayName)
            {
                value = (string)field.GetValue(node) ?? ""
            };
            textField.RegisterValueChangedCallback(CreateFieldChangeCallback<string>(node, field, displayName));
            return textField;
        }

        /// <summary>
        /// 创建浮点数字段编辑器
        /// </summary>
        public static FloatField CreateFloatField(FieldInfo field, BTNode node, string displayName)
        {
            var floatField = new FloatField(displayName)
            {
                value = (float)field.GetValue(node)
            };
            floatField.RegisterValueChangedCallback(CreateFieldChangeCallback<float>(node, field, displayName));
            return floatField;
        }

        /// <summary>
        /// 创建整数字段编辑器
        /// </summary>
        public static IntegerField CreateIntField(FieldInfo field, BTNode node, string displayName)
        {
            var intField = new IntegerField(displayName)
            {
                value = (int)field.GetValue(node)
            };
            intField.RegisterValueChangedCallback(CreateFieldChangeCallback<int>(node, field, displayName));
            return intField;
        }

        /// <summary>
        /// 创建布尔字段编辑器
        /// </summary>
        public static Toggle CreateBoolField(FieldInfo field, BTNode node, string displayName)
        {
            var toggle = new Toggle(displayName)
            {
                value = (bool)field.GetValue(node)
            };
            toggle.RegisterValueChangedCallback(CreateFieldChangeCallback<bool>(node, field, displayName));
            return toggle;
        }

        /// <summary>
        /// 创建 Vector3 字段编辑器
        /// </summary>
        public static Vector3Field CreateVector3Field(FieldInfo field, BTNode node, string displayName)
        {
            var vector3Field = new Vector3Field(displayName)
            {
                value = (Vector3)field.GetValue(node)
            };
            vector3Field.RegisterValueChangedCallback(CreateFieldChangeCallback<Vector3>(node, field, displayName));
            return vector3Field;
        }

        /// <summary>
        /// 创建 GameObject 字段编辑器
        /// </summary>
        public static ObjectField CreateGameObjectField(FieldInfo field, BTNode node, string displayName)
        {
            var objectField = new ObjectField(displayName)
            {
                objectType = typeof(GameObject),
                value = (GameObject)field.GetValue(node)
            };
            objectField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                BTEditorEventBus.PublishPropertyChanged(node, field.Name);
            });
            return objectField;
        }

        /// <summary>
        /// 创建枚举字段编辑器
        /// </summary>
        public static EnumField CreateEnumField(FieldInfo field, BTNode node, string displayName)
        {
            var enumValue = (Enum)field.GetValue(node);
            var enumField = new EnumField(displayName, enumValue);
            enumField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                BTEditorEventBus.PublishPropertyChanged(node, field.Name);
            });
            return enumField;
        }
    }
}