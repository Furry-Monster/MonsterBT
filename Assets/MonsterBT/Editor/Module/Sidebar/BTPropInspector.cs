using System.Linq;
using System.Reflection;
using MonsterBT.Editor.Services;
using MonsterBT.Runtime;
using UnityEditor;
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
                BTEditorEventBus.PublishPropertyChanged(node, "name");
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
                BTEditorEventBus.PublishPropertyChanged(node, "description");
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
            var fieldName = ObjectNames.NicifyVariableName(field.Name);

            if (fieldType == typeof(string))
                return BTFieldEditorService.CreateStringField(field, node, fieldName);
            if (fieldType == typeof(float))
                return BTFieldEditorService.CreateFloatField(field, node, fieldName);
            if (fieldType == typeof(int))
                return BTFieldEditorService.CreateIntField(field, node, fieldName);
            if (fieldType == typeof(bool))
                return BTFieldEditorService.CreateBoolField(field, node, fieldName);
            if (fieldType == typeof(Vector3))
                return BTFieldEditorService.CreateVector3Field(field, node, fieldName);
            if (fieldType == typeof(GameObject))
                return BTFieldEditorService.CreateGameObjectField(field, node, fieldName);
            if (fieldType.IsEnum)
                return BTFieldEditorService.CreateEnumField(field, node, fieldName);
            if (fieldType == typeof(Transform))
                return BTFieldEditorService.CreateTransformField(field, node, fieldName);
            if (typeof(Component).IsAssignableFrom(fieldType))
                return BTFieldEditorService.CreateComponentField(field, node, fieldName);

            return null;
        }

        #endregion
    }
}