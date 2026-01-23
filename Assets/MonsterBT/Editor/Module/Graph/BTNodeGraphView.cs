using System;
using System.Collections.Generic;
using System.Linq;
using MonsterBT.Editor.Services;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.Experimental.GraphView.Blackboard;
using Object = UnityEngine.Object;

namespace MonsterBT.Editor
{
    public class BTNodeGraphView : GraphView
    {
        private BehaviorTree behaviorTree;
        private readonly Dictionary<BTNode, BTNodeView> nodeViews;

        #region Content Methods

        public BTNodeGraphView()
        {
            behaviorTree = null;
            nodeViews = new Dictionary<BTNode, BTNodeView>();

            var styleSheet = BTEditorResources.LoadStyleSheet("BTNodeGraphStyle.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            var miniMap = new MiniMap { name = "mini-map" };
            miniMap.AddToClassList("mini-map");
            Add(miniMap);

            var blackboard = new Blackboard { name = "blackboard", title = "Variables" };
            blackboard.AddToClassList("blackboard");
            blackboard.addItemRequested += _ => Debug.Log("I don't know how to make this invisible.");
            Add(blackboard);

            // 添加grid
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            name = "node-graph";
            AddToClassList("node-graph-view");

            graphViewChanged += OnGraphViewChanged;

            // 轮询监听选中节点变化
            schedule.Execute(CheckSelection).Every(100);

            // 广播视图更新
            PopulateView();
        }

        ~BTNodeGraphView()
        {
            // 清理事件订阅
            graphViewChanged -= OnGraphViewChanged;
        }

        public void SetBehaviorTree(BehaviorTree tree)
        {
            behaviorTree = tree;
            PopulateView();
            RefreshBlackboardView();
        }

        /// <summary>
        /// 广播视图更新，此方法将从头递归地、重新加载整个行为树视图，不应频繁调用
        /// </summary>
        public void PopulateView()
        {
            // 填充当前视图
            graphViewChanged -= OnGraphViewChanged;

            // 清除现有内容
            DeleteElements(graphElements);
            nodeViews.Clear();

            graphViewChanged += OnGraphViewChanged;

            if (behaviorTree?.RootNode != null && !behaviorTree.RootNode.Equals(null))
            {
                var rootView = CreateNodeViewFromNode(behaviorTree.RootNode);
                if (rootView != null)
                {
                    CreateNodeViewsRecursive(behaviorTree.RootNode);
                    LoadOtherNodes();
                    CreateConnections();
                }
            }
        }

        private void CreateNodeViewsRecursive(BTNode node)
        {
            if (node == null || node.Equals(null))
                return;

            if (node is CompositeNode composite)
            {
                if (composite.Children == null || composite.Children.Count == 0)
                    return;

                foreach (var child in composite.Children)
                {
                    if (child == null || child.Equals(null))
                        continue;

                    CreateNodeViewFromNode(child);
                    CreateNodeViewsRecursive(child);
                }
            }
            else if (node is DecoratorNode decorator)
            {
                if (decorator.Child == null || decorator.Child.Equals(null))
                    return;

                CreateNodeViewFromNode(decorator.Child);
                CreateNodeViewsRecursive(decorator.Child);
            }
            else if (node is RootNode root)
            {
                if (root.Child == null || root.Child.Equals(null))
                    return;

                CreateNodeViewFromNode(root.Child);
                CreateNodeViewsRecursive(root.Child);
            }
        }

        /// <summary>
        /// 加载所有存储在BehaviorTreeAsset中但尚未连接的节点
        /// </summary>
        private void LoadOtherNodes()
        {
            if (behaviorTree == null)
                return;

            // 获取asset中所有的BTNode对象
            var allNodesInAsset = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(behaviorTree))
                .OfType<BTNode>()
                .Where(node => node != null && !node.Equals(null)) // 过滤已销毁的节点
                .ToList();

            // 为所有尚未加载的节点创建视图
            foreach (var node in allNodesInAsset)
            {
                if (!nodeViews.ContainsKey(node))
                {
                    CreateNodeViewFromNode(node);
                }
            }
        }

        private BTNodeView CreateNodeViewFromNode(BTNode node)
        {
            if (node == null)
                return null;

            // 检查节点是否已被销毁
            if (node == null || node.Equals(null))
                return null;

            if (nodeViews.ContainsKey(node))
                return null;

            try
            {
                var nodeView = new BTNodeView(node);
                nodeViews[node] = nodeView;
                AddElement(nodeView);
                return nodeView;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to create node view for node: {ex.Message}");
                return null;
            }
        }

        private void CreateConnections()
        {
            foreach (var (node, nodeView) in nodeViews)
            {
                // 检查节点是否已被销毁
                if (node == null || node.Equals(null) || nodeView == null)
                    continue;

                // 连接到子节点
                if (node is RootNode root)
                {
                    if (root.Child == null || root.Child.Equals(null))
                        continue;

                    if (nodeViews.TryGetValue(root.Child, out var childView) && childView != null)
                    {
                        ConnectNodes(nodeView, childView);
                    }
                }
                else if (node is DecoratorNode decorator)
                {
                    if (decorator.Child == null || decorator.Child.Equals(null))
                        continue;

                    if (nodeViews.TryGetValue(decorator.Child, out var childView) && childView != null)
                    {
                        ConnectNodes(nodeView, childView);
                    }
                }
                else if (node is CompositeNode composite)
                {
                    if (composite.Children == null || composite.Children.Count == 0)
                        continue;

                    foreach (var child in composite.Children)
                    {
                        if (child == null || child.Equals(null))
                            continue;

                        if (nodeViews.TryGetValue(child, out var childView) && childView != null)
                        {
                            ConnectNodes(nodeView, childView);
                        }
                    }
                }
            }
        }

        private void ConnectNodes(BTNodeView parentNode, BTNodeView childNode)
        {
            if (parentNode.OutputPort == null || childNode.InputPort == null)
                return;

            var edge = parentNode.OutputPort.ConnectTo(childNode.InputPort);
            AddElement(edge);
        }

        /// <summary>
        /// 仅仅更新修改过的GraphView元素，相比广播，性能更优，通过事件自动调用
        /// </summary>
        /// <param name="graphViewChange">原始元素更新记录</param>
        /// <returns>处理后的元素更新记录</returns>
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            // 处理删除的元素
            if (graphViewChange.elementsToRemove != null)
            {
                // 先收集要删除的节点，以便后续处理它们的边
                var nodesToRemove = new List<BTNodeView>();
                
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is BTNodeView nodeView)
                    {
                        nodesToRemove.Add(nodeView);
                    }
                    else if (element is Edge edge)
                    {
                        // 断开连接
                        var parentView = edge.output.node as BTNodeView;
                        var childView = edge.input.node as BTNodeView;

                        if (parentView?.Node != null && !parentView.Node.Equals(null))
                        {
                            switch (parentView.Node)
                            {
                                case RootNode rootNode when childView != null:
                                    rootNode.Child = null;
                                    break;
                                case DecoratorNode decoratorNode when childView != null:
                                    decoratorNode.Child = null;
                                    break;
                                case CompositeNode compositeNode when childView != null && childView.Node != null:
                                    compositeNode.Children.Remove(childView.Node);
                                    break;
                            }
                        }
                    }
                }

                // 对于删除的节点，删除所有相关的边
                foreach (var nodeView in nodesToRemove)
                {
                    if (nodeView?.Node == null || nodeView.Node.Equals(null))
                        continue;

                    // 从字典中移除
                    nodeViews.Remove(nodeView.Node);

                    // 删除所有连接到该节点的边
                    var edgesToRemove = new List<Edge>();
                    foreach (var graphElement in graphElements)
                    {
                        if (graphElement is Edge edge)
                        {
                            if (edge.output.node == nodeView || edge.input.node == nodeView)
                            {
                                edgesToRemove.Add(edge);
                            }
                        }
                    }

                    // 从父节点的子节点列表中移除
                    foreach (var edge in edgesToRemove)
                    {
                        var parentView = edge.output.node as BTNodeView;
                        var childView = edge.input.node as BTNodeView;

                        if (parentView?.Node != null && !parentView.Node.Equals(null) && childView == nodeView)
                        {
                            switch (parentView.Node)
                            {
                                case RootNode rootNode:
                                    rootNode.Child = null;
                                    break;
                                case DecoratorNode decoratorNode:
                                    decoratorNode.Child = null;
                                    break;
                                case CompositeNode compositeNode:
                                    compositeNode.Children.Remove(nodeView.Node);
                                    break;
                            }
                        }
                    }

                    // 如果节点不是通过 DeleteNode 删除的（比如通过 UI 直接删除），需要销毁节点对象
                    // 但这里我们不销毁，因为 DeleteNode 已经处理了
                }
            }

            // 处理创建的连接
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var parentView = edge.output.node as BTNodeView;
                    var childView = edge.input.node as BTNodeView;

                    switch (parentView?.Node)
                    {
                        case RootNode rootNode when childView != null:
                            rootNode.Child = childView.Node;
                            break;
                        case DecoratorNode decoratorNode when childView != null:
                            decoratorNode.Child = childView.Node;
                            break;
                        case CompositeNode compositeNode when childView != null:
                            compositeNode.Children.Add(childView.Node);
                            break;
                    }
                }
            }

            return graphViewChange;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .ToList()
                .Where(endPort =>
                    endPort.direction != startPort.direction &&
                    endPort.node != startPort.node)
                .ToList();
        }

        #endregion

        #region ContextMenu Methods

        private BTNode copiedNode; // 拷贝缓冲
        private Vector2 mousePosition;

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            mousePosition = evt.localMousePosition;

            switch (evt.target)
            {
                case GraphView:
                    BuildGraphContextMenu(evt);
                    break;
                case BTNodeView nodeView:
                    BuildNodeContextMenu(evt, nodeView);
                    break;
            }
        }

        private void BuildGraphContextMenu(ContextualMenuPopulateEvent evt)
        {
            // 自动生成所有节点类型的菜单
            BuildNodeCreationMenu(evt);

            // Blackboard变量管理
            evt.menu.AppendAction("Blackboard/Add Boolean", _ => AddBlackboardVariable("NewBool", typeof(bool)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add Float", _ => AddBlackboardVariable("NewFloat", typeof(float)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add Vector3", _ => AddBlackboardVariable("NewVector3", typeof(Vector3)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add GameObject",
                _ => AddBlackboardVariable("NewGameObject", typeof(GameObject)), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Add String", _ => AddBlackboardVariable("NewString", typeof(string)),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Blackboard/Refresh", _ => RefreshBlackboardView(), DropdownMenuAction.AlwaysEnabled);

            // 粘贴功能
            evt.menu.AppendAction("Paste", _ => PasteNode(),
                action => copiedNode == null
                    ? DropdownMenuAction.AlwaysDisabled(action)
                    : DropdownMenuAction.AlwaysEnabled(action));
            // 重新载入
            evt.menu.AppendAction("Reload", _ => PopulateView(), DropdownMenuAction.AlwaysEnabled);
        }

        private void BuildNodeContextMenu(ContextualMenuPopulateEvent evt, BTNodeView nodeView)
        {
            // 基本操作
            evt.menu.AppendAction("Copy", _ => CopyNode(nodeView), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Cut", _ => CutNode(nodeView), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Duplicate", _ => DuplicateNode(nodeView), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete", _ => DeleteNode(nodeView), DropdownMenuAction.AlwaysEnabled);
        }

        /// <summary>
        /// 实际地创建一个节点，会同时创建持久化的SO和Editor下的NodeView视图
        /// </summary>
        /// <typeparam name="T">节点类型，需继承BTNode</typeparam>
        public void CreateNode<T>() where T : BTNode
        {
            if (behaviorTree == null)
                return;

            var node = ScriptableObject.CreateInstance<T>();
            node.name = typeof(T).Name;

            AssetDatabase.AddObjectToAsset(node, behaviorTree);

            var nodeView = CreateNodeViewFromNode(node);
            nodeView.SetPosition(new Rect(mousePosition, Vector2.zero));

            EditorUtility.SetDirty(behaviorTree);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 同上创建节点SO+创建节点GraphView视图(无泛型，手动约束)
        /// </summary>
        /// <param name="type">节点类型</param>
        public void CreateNode(Type type)
        {
            if (behaviorTree == null)
                return;

            // 运行时检测
            if (!typeof(BTNode).IsAssignableFrom(type)) return;

            var node = ScriptableObject.CreateInstance(type) as BTNode;
            node!.name = type.Name;

            AssetDatabase.AddObjectToAsset(node, behaviorTree);

            var nodeView = CreateNodeViewFromNode(node);
            nodeView.SetPosition(new Rect(mousePosition, Vector2.zero));

            EditorUtility.SetDirty(behaviorTree);
            AssetDatabase.SaveAssets();
        }

        private void CopyNode(BTNodeView nodeView)
        {
            copiedNode = nodeView.Node;
        }

        private void CutNode(BTNodeView nodeView)
        {
            CopyNode(nodeView);
            DeleteNode(nodeView);
        }

        private void DuplicateNode(BTNodeView nodeView)
        {
            if (behaviorTree == null)
                return;

            var originalNode = nodeView.Node;
            var duplicatedNode = Object.Instantiate(originalNode);
            duplicatedNode.name = originalNode.name + " (Copy)";

            AssetDatabase.AddObjectToAsset(duplicatedNode, behaviorTree);

            var duplicatedView = CreateNodeViewFromNode(duplicatedNode);
            var originalPos = nodeView.GetPosition();
            duplicatedView.SetPosition(new Rect(originalPos.x + 200, originalPos.y, originalPos.width,
                originalPos.height));

            EditorUtility.SetDirty(behaviorTree);
            AssetDatabase.SaveAssets();
        }

        private void DeleteNode(BTNodeView nodeView)
        {
            if (nodeView.Node is RootNode)
            {
                Debug.LogWarning("Cannot delete root node!");
                return;
            }

            // 删除所有连接到该节点的边
            var edgesToRemove = new List<Edge>();
            foreach (var element in graphElements)
            {
                if (element is Edge edge)
                {
                    if (edge.output.node == nodeView || edge.input.node == nodeView)
                    {
                        edgesToRemove.Add(edge);
                    }
                }
            }

            foreach (var edge in edgesToRemove)
            {
                RemoveElement(edge);
            }

            // 从父节点的子节点列表中移除
            var node = nodeView.Node;
            if (node != null)
            {
                // 查找所有可能包含此节点的父节点
                foreach (var (parentNode, parentView) in nodeViews)
                {
                    if (parentNode == null || parentNode.Equals(null))
                        continue;

                    switch (parentNode)
                    {
                        case RootNode root when root.Child == node:
                            root.Child = null;
                            break;
                        case DecoratorNode decorator when decorator.Child == node:
                            decorator.Child = null;
                            break;
                        case CompositeNode composite when composite.Children != null:
                            composite.Children.Remove(node);
                            break;
                    }
                }
            }

            RemoveElement(nodeView);
            nodeViews.Remove(nodeView.Node);

            Object.DestroyImmediate(nodeView.Node, true);

            EditorUtility.SetDirty(behaviorTree);
            AssetDatabase.SaveAssets();
        }

        private void PasteNode()
        {
            if (copiedNode == null || behaviorTree == null)
                return;

            var pastedNode = Object.Instantiate(copiedNode);
            pastedNode.name = copiedNode.name + " (Paste)";

            AssetDatabase.AddObjectToAsset(pastedNode, behaviorTree);

            var pastedView = CreateNodeViewFromNode(pastedNode);
            pastedView.SetPosition(new Rect(mousePosition, Vector2.zero));

            EditorUtility.SetDirty(behaviorTree);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 自动生成节点创建菜单
        /// </summary>
        /// <param name="evt"> 上下文菜单广播事件 </param>
        private void BuildNodeCreationMenu(ContextualMenuPopulateEvent evt)
        {
            var nodeTypes = BTNodeTypeHelper.GetAllNodeTypes();

            foreach (var (category, types) in nodeTypes)
            {
                foreach (var type in types)
                {
                    string displayName = BTNodeTypeHelper.GetNodeDisplayName(type);
                    string menuPath = $"Create Node/{category}/{displayName}";

                    evt.menu.AppendAction(menuPath, _ => CreateNode(type), DropdownMenuAction.AlwaysEnabled);
                }
            }
        }

        #endregion

        #region Blackboard Methods

        public void AddBlackboardVariable(string varName, Type varType)
        {
            if (behaviorTree?.Blackboard == null)
                return;

            // 添加到Runtime Blackboard
            object defaultValue = GetDefaultValue(varType);
            behaviorTree.Blackboard.AddVariable(varName, varType, defaultValue);

            RefreshBlackboardView();

            EditorUtility.SetDirty(behaviorTree.Blackboard);
            AssetDatabase.SaveAssets();
        }


        public void RefreshBlackboardView()
        {
            var blackboard = this.Q<Blackboard>();
            if (blackboard == null || behaviorTree?.Blackboard == null) return;

            blackboard.Clear();

            foreach (var varInfo in behaviorTree.Blackboard.GetVariableInfos())
            {
                if (!varInfo.isExposed) continue;

                var variableRow = CreateVariableRow(varInfo.name, Type.GetType(varInfo.typeName));
                blackboard.Add(variableRow);
            }
        }

        private VisualElement CreateVariableRow(string varName, Type varType)
        {
            var row = new VisualElement();
            row.AddToClassList("blackboard-variable-row");

            // 变量信息行
            var infoRow = new VisualElement();
            infoRow.AddToClassList("blackboard-variable-info");

            var nameField = new TextField
            {
                value = varName
            };
            nameField.AddToClassList("blackboard-variable-name");
            nameField.RegisterCallback<FocusOutEvent>(evt =>
            {
                // 仅在失去焦点时修改黑板变量的名称
                if (evt.target is TextField textField)
                    RenameBlackboardVariable(varName, textField.value);
            });

            var typeLabel = new Label(GetTypeDisplayName(varType));
            typeLabel.AddToClassList("blackboard-variable-type");

            var deleteButton = new Button(() => RemoveBlackboardVariable(varName))
            {
                text = "×"
            };
            deleteButton.AddToClassList("blackboard-delete-button");

            infoRow.Add(nameField);
            infoRow.Add(typeLabel);
            infoRow.Add(deleteButton);

            // 值编辑行
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
            if (varType == typeof(bool))
            {
                var toggle = new Toggle
                {
                    value = behaviorTree.Blackboard.GetBool(varName)
                };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetBool(varName, evt.newValue);
                    EditorUtility.SetDirty(behaviorTree.Blackboard);
                });
                return toggle;
            }

            if (varType == typeof(float))
            {
                var floatField = new FloatField
                {
                    value = behaviorTree.Blackboard.GetFloat(varName)
                };
                floatField.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetFloat(varName, evt.newValue);
                    EditorUtility.SetDirty(behaviorTree.Blackboard);
                });
                return floatField;
            }

            if (varType == typeof(string))
            {
                var textField = new TextField
                {
                    value = behaviorTree.Blackboard.GetString(varName)
                };
                textField.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetString(varName, evt.newValue);
                    EditorUtility.SetDirty(behaviorTree.Blackboard);
                });
                return textField;
            }

            if (varType == typeof(Vector3))
            {
                var vector3Field = new Vector3Field
                {
                    value = behaviorTree.Blackboard.GetVector3(varName),
                };
                vector3Field.RegisterValueChangedCallback(evt =>
                {
                    behaviorTree.Blackboard.SetVector3(varName, evt.newValue);
                    EditorUtility.SetDirty(behaviorTree.Blackboard);
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
                    EditorUtility.SetDirty(behaviorTree.Blackboard);
                });
                return objectField;
            }

            return null;
        }

        public void RemoveBlackboardVariable(string varName)
        {
            if (behaviorTree?.Blackboard == null) return;

            behaviorTree.Blackboard.RemoveVariable(varName);
            RefreshBlackboardView();

            EditorUtility.SetDirty(behaviorTree.Blackboard);
            AssetDatabase.SaveAssets();
        }

        public void RenameBlackboardVariable(string oldName, string newName)
        {
            if (behaviorTree?.Blackboard == null)
                return;

            if (string.IsNullOrEmpty(newName) || oldName == newName)
                return;

            // 检查新名称是否已存在
            if (behaviorTree.Blackboard.HasKey(newName))
            {
                Debug.LogWarning($"Variable '{newName}' already exists!");
                RefreshBlackboardView(); // 刷新以恢复原始名称
                return;
            }

            behaviorTree.Blackboard.RenameVariable(oldName, newName);
            RefreshBlackboardView();

            EditorUtility.SetDirty(behaviorTree.Blackboard);
            AssetDatabase.SaveAssets();
        }

        private static readonly Dictionary<Type, (object defaultValue, string displayName)> TypeInfo =
            new Dictionary<Type, (object, string)>
            {
                { typeof(bool), (false, "bool") },
                { typeof(float), (0f, "float") },
                { typeof(Vector3), (Vector3.zero, "Vector3") },
                { typeof(GameObject), (null, "GameObject") },
                { typeof(string), ("", "string") }
            };

        private static object GetDefaultValue(Type type) =>
            TypeInfo.TryGetValue(type, out var info) ? info.defaultValue : null;

        private static string GetTypeDisplayName(Type type) =>
            TypeInfo.TryGetValue(type, out var info) ? info.displayName : type.Name;

        #endregion

        #region Inspector Methods

        private BTNode lastSelectedNode;

        // TODO:从100ms的轮询优化到其他方式
        private void CheckSelection()
        {
            var selectedNodes = selection.OfType<BTNodeView>().ToList();
            var currentSelectedNode = selectedNodes.Count == 1 ? selectedNodes[0].Node : null;

            // 检查选择是否发生变化
            if (currentSelectedNode != lastSelectedNode)
            {
                lastSelectedNode = currentSelectedNode;

                if (currentSelectedNode != null)
                {
                    BTEditorEventBus.PublishNodeSelected(currentSelectedNode);
                }
                else
                {
                    BTEditorEventBus.PublishNodeDeselected();
                }
            }
        }

        public void HandlePropertyChanged(BTNode node, string propertyName)
        {
            if (nodeViews.TryGetValue(node, out var nodeView))
            {
                nodeView.RefreshContent(propertyName);
            }
        }

        #endregion
    }
}