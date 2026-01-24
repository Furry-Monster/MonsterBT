using System;
using System.Collections.Generic;
using MonsterBT.Editor.Service.Asset;
using MonsterBT.Runtime;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.Experimental.GraphView.Blackboard;

namespace MonsterBT.Editor.Service.Misc
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

            var nameField = new TextField { value = varName, isDelayed = true };
            nameField.AddToClassList("blackboard-variable-name");
            
            // 保存原始名称，用于重命名
            var originalName = varName;
            
            // 处理重命名的辅助方法
            Action<string> applyRename = (newName) =>
            {
                if (string.IsNullOrEmpty(newName) || originalName == newName)
                    return;
                
                UnityEditor.Undo.RecordObject(behaviorTree.Blackboard,
                    $"Rename Blackboard Variable: {originalName}");
                RenameVariable(originalName, newName);
            };
            
            // 使用 isDelayed 时，ValueChanged 事件会在失焦时触发
            nameField.RegisterValueChangedCallback(evt =>
            {
                applyRename(evt.newValue);
            });
            
            // 监听回车键事件，立即应用
            nameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    if (evt.target is TextField textField)
                    {
                        applyRename(textField.value);
                        textField.Blur();
                        evt.StopPropagation();
                    }
                }
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
            var callback = new Action(() =>
            {
                UnityEditor.Undo.RecordObject(behaviorTree.Blackboard, $"Change Blackboard Value: {varName}");
                BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
            });

            if (varType == typeof(bool))
            {
                var toggle = new Toggle { value = behaviorTree.Blackboard.GetBool(varName) };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    UnityEditor.Undo.RecordObject(behaviorTree.Blackboard, $"Change Blackboard Value: {varName}");
                    behaviorTree.Blackboard.SetBool(varName, evt.newValue);
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
                });
                return toggle;
            }

            if (varType == typeof(float))
            {
                var floatField = new FloatField { value = behaviorTree.Blackboard.GetFloat(varName) };
                floatField.RegisterValueChangedCallback(evt =>
                {
                    UnityEditor.Undo.RecordObject(behaviorTree.Blackboard, $"Change Blackboard Value: {varName}");
                    behaviorTree.Blackboard.SetFloat(varName, evt.newValue);
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
                });
                return floatField;
            }

            if (varType == typeof(string))
            {
                var textField = new TextField { value = behaviorTree.Blackboard.GetString(varName) };
                textField.RegisterValueChangedCallback(evt =>
                {
                    UnityEditor.Undo.RecordObject(behaviorTree.Blackboard, $"Change Blackboard Value: {varName}");
                    behaviorTree.Blackboard.SetString(varName, evt.newValue);
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
                });
                return textField;
            }

            if (varType == typeof(Vector3))
            {
                var vector3Field = new Vector3Field { value = behaviorTree.Blackboard.GetVector3(varName) };
                vector3Field.RegisterValueChangedCallback(evt =>
                {
                    UnityEditor.Undo.RecordObject(behaviorTree.Blackboard, $"Change Blackboard Value: {varName}");
                    behaviorTree.Blackboard.SetVector3(varName, evt.newValue);
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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
                    UnityEditor.Undo.RecordObject(behaviorTree.Blackboard, $"Change Blackboard Value: {varName}");
                    behaviorTree.Blackboard.SetGameObject(varName, evt.newValue as GameObject);
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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