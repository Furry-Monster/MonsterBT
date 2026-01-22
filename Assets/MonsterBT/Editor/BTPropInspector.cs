using System;
using System.Linq;
using System.Reflection;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public class BTPropInspector : VisualElement
    {
        private BTNode currentNode;
        private readonly ScrollView contentScrollView;
        private readonly Label emptyStateLabel;

        public BTPropInspector()
        {
            var styleSheet = BTEditorResources.LoadStyleSheet("BTPropInspectorStyle.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);

            name = "inspector-container";
            AddToClassList("inspector-container");

            contentScrollView = new ScrollView { name = "content-scroll" };
            contentScrollView.AddToClassList("content-scroll");
            Add(contentScrollView);

            emptyStateLabel = new Label("No Node Selected") { name = "empty-state" };
            emptyStateLabel.AddToClassList("empty-state");
            Add(emptyStateLabel);
        }

        #region Public Methods

        public void SetSelectedNode(BTNode node)
        {
            if (currentNode == node) return;

            currentNode = node;
            RefreshInspector();
        }

        public void ClearSelection()
        {
            currentNode = null;
            RefreshInspector();
        }

        #endregion

        #region Content Management

        private void RefreshInspector()
        {
            contentScrollView.Clear();

            if (currentNode == null)
            {
                emptyStateLabel.style.display = DisplayStyle.Flex;
                return;
            }

            emptyStateLabel.style.display = DisplayStyle.None;
            BuildGeneralProps(currentNode);
        }

        private void BuildGeneralProps(BTNode node)
        {
            // 节点名称
            var nameField = new TextField("Name")
            {
                value = node.name
            };
            nameField.AddToClassList("inspector-field");
            nameField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, "Change Node Name");
                node.name = evt.newValue;
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, "name");
            });
            contentScrollView.Add(nameField);

            // 节点描述
            var descriptionField = new TextField("Description")
            {
                value = node.Description ?? "",
                multiline = true
            };
            descriptionField.AddToClassList("description-field");
            descriptionField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, "Change Node Description");
                node.Description = evt.newValue;
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, "description");
            });
            contentScrollView.Add(descriptionField);

            // 自定义属性
            BuildCustomProps(node);
        }

        private void BuildCustomProps(BTNode node)
        {
            var fields = GetEditableFields(node);
            if (fields.Length == 0)
                return;

            // 添加分隔符
            var separator = new VisualElement();
            separator.AddToClassList("field-separator");
            contentScrollView.Add(separator);

            foreach (var field in fields)
            {
                var fieldEditor = CreateFieldEditor(field, node);
                if (fieldEditor == null)
                    continue;

                fieldEditor.AddToClassList("inspector-field");
                contentScrollView.Add(fieldEditor);
            }
        }

        #endregion

        #region Field Editors

        /// <summary>
        /// 遵从Unity设计，仅仅考虑非静态的Public字段和[SerializedField]标记的字段
        /// </summary>
        /// <param name="node">需要被解析的节点实例</param>
        /// <returns>字段反射表</returns>
        private FieldInfo[] GetEditableFields(BTNode node)
        {
            return node.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null) // 遵从Unity的设计
                .Where(f => !IsIgnoredField(f))
                .ToArray();
        }

        /// <summary>
        /// 是否为默认会被忽视的序列化的字段
        /// </summary>
        /// <param name="field">需要被检测的反射字段</param>
        /// <returns>true / false</returns>
        private bool IsIgnoredField(FieldInfo field)
        {
            string[] ignoredFields = { "name", "hideFlags", "description", "position" };
            return ignoredFields.Contains(field.Name.ToLower()) ||
                   field.Name.StartsWith("m_");
        }

        /// <summary>
        /// 为节点node的指定field字段(通过反射),创建Inspector视图(VisualElement类型)
        /// </summary>
        /// <param name="field">需要被展示的字段反射表</param>
        /// <param name="node">字段所属的节点实例，该实例将被解析</param>
        /// <returns>创建的视图元素</returns>
        private VisualElement CreateFieldEditor(FieldInfo field, BTNode node)
        {
            var fieldType = field.FieldType;
            string fieldName = ObjectNames.NicifyVariableName(field.Name);

            return fieldType switch
            {
                var t when t == typeof(string) => CreateStringField(field, node, fieldName),
                var t when t == typeof(float) => CreateFloatField(field, node, fieldName),
                var t when t == typeof(int) => CreateIntField(field, node, fieldName),
                var t when t == typeof(bool) => CreateBoolField(field, node, fieldName),
                var t when t == typeof(Vector3) => CreateVector3Field(field, node, fieldName),
                var t when t == typeof(GameObject) => CreateGameObjectField(field, node, fieldName),
                var t when t.IsEnum => CreateEnumField(field, node, fieldName),
                _ => null
            };
        }

        #region Field Elements Creator (可扩展)

        public event Action<BTNode, string> OnPropertyChanged;

        private TextField CreateStringField(FieldInfo field, BTNode node, string displayName)
        {
            var textField = new TextField(displayName)
            {
                value = (string)field.GetValue(node) ?? ""
            };

            textField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, displayName);
            });

            return textField;
        }

        private FloatField CreateFloatField(FieldInfo field, BTNode node, string displayName)
        {
            var floatField = new FloatField(displayName)
            {
                value = (float)field.GetValue(node)
            };

            floatField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, displayName);
            });

            return floatField;
        }

        private IntegerField CreateIntField(FieldInfo field, BTNode node, string displayName)
        {
            var intField = new IntegerField(displayName)
            {
                value = (int)field.GetValue(node)
            };

            intField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, displayName);
            });

            return intField;
        }

        private Toggle CreateBoolField(FieldInfo field, BTNode node, string displayName)
        {
            var toggle = new Toggle(displayName)
            {
                value = (bool)field.GetValue(node),
            };

            toggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, displayName);
            });

            return toggle;
        }

        private Vector3Field CreateVector3Field(FieldInfo field, BTNode node, string displayName)
        {
            var vector3Field = new Vector3Field(displayName)
            {
                value = (Vector3)field.GetValue(node)
            };

            vector3Field.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, displayName);
            });

            return vector3Field;
        }

        private ObjectField CreateGameObjectField(FieldInfo field, BTNode node, string displayName)
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
                OnPropertyChanged?.Invoke(node, displayName);
            });

            return objectField;
        }

        private EnumField CreateEnumField(FieldInfo field, BTNode node, string displayName)
        {
            var enumValue = (Enum)field.GetValue(node);
            var enumField = new EnumField(displayName, enumValue);

            enumField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, $"Change {displayName}");
                field.SetValue(node, evt.newValue);
                EditorUtility.SetDirty(node);
                OnPropertyChanged?.Invoke(node, displayName);
            });

            return enumField;
        }

        #endregion

        #endregion
    }
}