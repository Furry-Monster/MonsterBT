using System;
using System.Reflection;
using MonsterBT.Editor.Base;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor.Service.Field
{
    public static class BTFieldEditorService
    {
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

        public static TextField CreateStringField(FieldInfo field, BTNode node, string displayName)
        {
            var textField = new TextField(displayName)
            {
                value = (string)field.GetValue(node) ?? ""
            };
            textField.RegisterValueChangedCallback(CreateFieldChangeCallback<string>(node, field, displayName));
            return textField;
        }

        public static FloatField CreateFloatField(FieldInfo field, BTNode node, string displayName)
        {
            var floatField = new FloatField(displayName)
            {
                value = (float)field.GetValue(node)
            };
            floatField.RegisterValueChangedCallback(CreateFieldChangeCallback<float>(node, field, displayName));
            return floatField;
        }

        public static IntegerField CreateIntField(FieldInfo field, BTNode node, string displayName)
        {
            var intField = new IntegerField(displayName)
            {
                value = (int)field.GetValue(node)
            };
            intField.RegisterValueChangedCallback(CreateFieldChangeCallback<int>(node, field, displayName));
            return intField;
        }

        public static Toggle CreateBoolField(FieldInfo field, BTNode node, string displayName)
        {
            var toggle = new Toggle(displayName)
            {
                value = (bool)field.GetValue(node)
            };
            toggle.RegisterValueChangedCallback(CreateFieldChangeCallback<bool>(node, field, displayName));
            return toggle;
        }

        public static Vector3Field CreateVector3Field(FieldInfo field, BTNode node, string displayName)
        {
            var vector3Field = new Vector3Field(displayName)
            {
                value = (Vector3)field.GetValue(node)
            };
            vector3Field.RegisterValueChangedCallback(CreateFieldChangeCallback<Vector3>(node, field, displayName));
            return vector3Field;
        }

        public static ObjectField CreateGameObjectField(FieldInfo field, BTNode node, string displayName)
        {
            var objectField = new ObjectField(displayName)
            {
                objectType = typeof(GameObject),
                value = (GameObject)field.GetValue(node)
            };
            objectField.RegisterValueChangedCallback(CreateObjectFieldChangeCallback(field, node, displayName));
            return objectField;
        }

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

        public static ObjectField CreateComponentField(FieldInfo field, BTNode node, string displayName)
        {
            var fieldType = field.FieldType;
            var objectField = new ObjectField(displayName)
            {
                objectType = fieldType,
                value = field.GetValue(node) as UnityEngine.Object
            };
            objectField.RegisterValueChangedCallback(CreateObjectFieldChangeCallback(field, node, displayName));
            return objectField;
        }

        private static EventCallback<ChangeEvent<UnityEngine.Object>> CreateObjectFieldChangeCallback(
            FieldInfo field, BTNode node, string displayName)
        {
            return evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                BTEditorEventBus.PublishPropertyChanged(node, field.Name);
            };
        }

        public static ObjectField CreateTransformField(FieldInfo field, BTNode node, string displayName)
        {
            return CreateComponentField(field, node, displayName);
        }
    }
}