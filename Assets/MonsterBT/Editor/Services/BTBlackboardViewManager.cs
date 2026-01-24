using System;
using System.Collections.Generic;
using MonsterBT.Runtime;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.Experimental.GraphView.Blackboard;

namespace MonsterBT.Editor.Services
{
    public class BTBlackboardViewManager
    {
        private readonly BehaviorTree behaviorTree;
        private readonly Blackboard blackboardView;

        private static readonly Dictionary<Type, (object defaultValue, string displayName)> TypeInfo =
            new()
            {
                { typeof(bool), (false, "bool") },
                { typeof(float), (0f, "float") },
                { typeof(Vector3), (Vector3.zero, "Vector3") },
                { typeof(GameObject), (null, "GameObject") },
                { typeof(string), ("", "string") }
            };

        public BTBlackboardViewManager(BehaviorTree behaviorTree, Blackboard blackboardView)
        {
            this.behaviorTree = behaviorTree;
            this.blackboardView = blackboardView;
        }

        public void RefreshView()
        {
            if (blackboardView == null || behaviorTree?.Blackboard == null)
                return;

            blackboardView.Clear();

            foreach (var varInfo in behaviorTree.Blackboard.GetVariableInfos())
            {
                if (!varInfo.isExposed)
                    continue;

                var variableRow = CreateVariableRow(varInfo.name, Type.GetType(varInfo.typeName));
                blackboardView.Add(variableRow);
            }
        }

        public void AddVariable(string varName, Type varType)
        {
            if (behaviorTree?.Blackboard == null)
                return;

            var defaultValue = GetDefaultValue(varType);
            behaviorTree.Blackboard.AddVariable(varName, varType, defaultValue);
            RefreshView();
            BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
        }

        public void RemoveVariable(string varName)
        {
            if (behaviorTree?.Blackboard == null)
                return;

            behaviorTree.Blackboard.RemoveVariable(varName);
            RefreshView();
            BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
        }

        public void RenameVariable(string oldName, string newName)
        {
            if (behaviorTree?.Blackboard == null)
                return;

            if (string.IsNullOrEmpty(newName) || oldName == newName)
                return;

            if (behaviorTree.Blackboard.HasKey(newName))
            {
                Debug.LogWarning($"Variable '{newName}' already exists!");
                RefreshView();
                return;
            }

            behaviorTree.Blackboard.RenameVariable(oldName, newName);
            RefreshView();
            BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
        }

        private VisualElement CreateVariableRow(string varName, Type varType)
        {
            var row = new VisualElement();
            row.AddToClassList("blackboard-variable-row");

            var infoRow = new VisualElement();
            infoRow.AddToClassList("blackboard-variable-info");

            var nameField = new TextField { value = varName };
            nameField.AddToClassList("blackboard-variable-name");
            nameField.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (evt.target is TextField textField)
                    RenameVariable(varName, textField.value);
            });

            var typeLabel = new Label(GetTypeDisplayName(varType));
            typeLabel.AddToClassList("blackboard-variable-type");

            var deleteButton = new Button(() => RemoveVariable(varName)) { text = "×" };
            deleteButton.AddToClassList("blackboard-delete-button");

            infoRow.Add(nameField);
            infoRow.Add(typeLabel);
            infoRow.Add(deleteButton);

            var valueRow = new VisualElement();
            valueRow.AddToClassList("blackboard-variable-value");

            var valueEditor = CreateValueEditor(varName, varType);
            if (valueEditor != null)
            {
                valueEditor.AddToClassList("blackboard-value-editor");
                valueRow.Add(valueEditor);
            }

            row.Add(infoRow);
            row.Add(valueRow);

            return row;
        }

        private VisualElement CreateValueEditor(string varName, Type varType)
        {
            var callback = new Action(() => BTEditorAssetService.MarkDirty(behaviorTree.Blackboard));

            if (varType == typeof(bool))
            {
                var toggle = new Toggle { value = behaviorTree.Blackboard.GetBool(varName) };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetBool(varName, evt.newValue);
                    callback();
                });
                return toggle;
            }

            if (varType == typeof(float))
            {
                var floatField = new FloatField { value = behaviorTree.Blackboard.GetFloat(varName) };
                floatField.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetFloat(varName, evt.newValue);
                    callback();
                });
                return floatField;
            }

            if (varType == typeof(string))
            {
                var textField = new TextField { value = behaviorTree.Blackboard.GetString(varName) };
                textField.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetString(varName, evt.newValue);
                    callback();
                });
                return textField;
            }

            if (varType == typeof(Vector3))
            {
                var vector3Field = new Vector3Field { value = behaviorTree.Blackboard.GetVector3(varName) };
                vector3Field.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetVector3(varName, evt.newValue);
                    callback();
                });
                return vector3Field;
            }

            if (varType == typeof(GameObject))
            {
                var objectField = new ObjectField
                {
                    objectType = typeof(GameObject),
                    value = behaviorTree.Blackboard.GetGameObject(varName)
                };
                objectField.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetGameObject(varName, evt.newValue as GameObject);
                    callback();
                });
                return objectField;
            }

            return null;
        }

        private static object GetDefaultValue(Type type) =>
            TypeInfo.TryGetValue(type, out var info) ? info.defaultValue : null;

        private static string GetTypeDisplayName(Type type) =>
            TypeInfo.TryGetValue(type, out var info) ? info.displayName : type.Name;
    }
}