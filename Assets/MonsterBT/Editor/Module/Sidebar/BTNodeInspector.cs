using System.Linq;
using System.Reflection;
using MonsterBT.Editor.Services;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public class BTNodeInspector : VisualElement
    {
        private BTNode currentNode;
        private readonly ScrollView contentScrollView;
        private readonly Label emptyStateLabel;

        public BTNodeInspector()
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

        private void RefreshInspector()
        {
            contentScrollView.Clear();

            if (currentNode == null)
            {
                emptyStateLabel.style.display = DisplayStyle.Flex;
                return;
            }

            emptyStateLabel.style.display = DisplayStyle.None;
            BuildNodeProperties(currentNode);
        }

        private void BuildNodeProperties(BTNode node)
        {
            var nameField = new TextField("Name") { value = node.name };
            nameField.AddToClassList("inspector-field");
            nameField.RegisterValueChangedCallback(CreatePropertyChangeCallback(node, "name", "Change Node Name",
                (n, v) => n.name = v));
            contentScrollView.Add(nameField);

            var descriptionField = new TextField("Description")
            {
                value = node.Description ?? "",
                multiline = true
            };
            descriptionField.AddToClassList("description-field");
            descriptionField.RegisterValueChangedCallback(CreatePropertyChangeCallback(node, "description",
                "Change Node Description", (n, v) => n.Description = v));
            contentScrollView.Add(descriptionField);

            BuildCustomFields(node);
        }

        private EventCallback<ChangeEvent<string>> CreatePropertyChangeCallback(
            BTNode node, string propertyName, string undoMessage, System.Action<BTNode, string> setter)
        {
            return evt =>
            {
                Undo.RecordObject(node, undoMessage);
                setter(node, evt.newValue);
                EditorUtility.SetDirty(node);
                BTEditorEventBus.PublishPropertyChanged(node, propertyName);
            };
        }

        private void BuildCustomFields(BTNode node)
        {
            var fields = GetEditableFields(node);
            if (fields.Length == 0)
                return;

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

        private FieldInfo[] GetEditableFields(BTNode node)
        {
            return node.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .Where(f => !IsIgnoredField(f))
                .ToArray();
        }

        private bool IsIgnoredField(FieldInfo field)
        {
            string[] ignoredFields = { "name", "hideFlags", "description", "position" };
            return ignoredFields.Contains(field.Name.ToLower()) ||
                   field.Name.StartsWith("m_");
        }

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
    }
}