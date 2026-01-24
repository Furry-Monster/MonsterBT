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
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);

            PopulateView();
        }

        ~BTNodeGraphView()
        {
            graphViewChanged -= OnGraphViewChanged;
            UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
            UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            var commandName = evt.commandName;

            switch (commandName)
            {
                case "Copy":
                    CopySelectedNodes();
                    evt.StopPropagation();
                    break;

                case "Paste":
                    PasteNode();
                    evt.StopPropagation();
                    break;

                case "Cut":
                    CutSelectedNodes();
                    evt.StopPropagation();
                    break;

                case "Delete":
                case "SoftDelete":
                    DeleteSelectedNodes();
                    evt.StopPropagation();
                    break;

                case "Duplicate":
                    DuplicateSelectedNodes();
                    evt.StopPropagation();
                    break;
            }
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            var commandName = evt.commandName;

            switch (commandName)
            {
                case "Paste":
                    // 只有在有复制节点时才允许粘贴
                    if (copiedNode != null)
                        evt.StopPropagation();
                    break;

                case "Copy":
                    // 只有在有选中节点且可以复制时才允许
                    var copyableNodes = selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanCopyNode(nv.Node, behaviorTree))
                        .ToList();
                    if (copyableNodes.Count > 0)
                        evt.StopPropagation();
                    break;

                case "Cut":
                    // 只有在有选中节点且可以剪切时才允许
                    var cuttableNodes = selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanCutNode(nv.Node, behaviorTree))
                        .ToList();
                    if (cuttableNodes.Count > 0)
                        evt.StopPropagation();
                    break;

                case "Delete":
                case "SoftDelete":
                    // 只有在有选中节点且可以删除时才允许
                    var deletableNodes = selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanDeleteNode(nv.Node))
                        .ToList();
                    if (deletableNodes.Count > 0)
                        evt.StopPropagation();
                    break;

                case "Duplicate":
                    // 只有在有选中节点且可以重复时才允许
                    var duplicatableNodes = selection.OfType<BTNodeView>()
                        .Where(nv => BTNodeEditorService.CanDuplicateNode(nv.Node, behaviorTree))
                        .ToList();
                    if (duplicatableNodes.Count > 0)
                        evt.StopPropagation();
                    break;
            }
        }

        public void SetBehaviorTree(BehaviorTree tree)
        {
            behaviorTree = tree;

            if (tree != null)
            {
                BTAssetService.AutoFixBehaviourTree(tree);
            }

            PopulateView();
            RefreshBlackboardView();
        }

        /// <summary>
        /// 广播视图更新，此方法将从头递归地、重新加载整个行为树视图，不应频繁调用
        /// </summary>
        public void PopulateView()
        {
            // 清除现有内容
            graphViewChanged -= OnGraphViewChanged;

            DeleteElements(graphElements);
            nodeViews.Clear();

            graphViewChanged += OnGraphViewChanged;

            // 填充视图
            if (behaviorTree?.RootNode != null && !behaviorTree.RootNode.Equals(null))
            {
                var rootView = CreateNodeViewFromNode(behaviorTree.RootNode);
                if (rootView == null)
                    return;

                CreateNodeViewsRecursive(behaviorTree.RootNode);
                CreateIsolatedNodes();
                CreateConnections();
            }
        }

        private void CreateNodeViewsRecursive(BTNode node)
        {
            if (node == null || node.Equals(null))
                return;

            foreach (var child in BTNodeEditorService.GetChildren(node))
            {
                if (child == null || child.Equals(null))
                    continue;

                CreateNodeViewFromNode(child);
                CreateNodeViewsRecursive(child);
            }
        }

        /// <summary>
        /// 加载所有存储在BehaviorTreeAsset中但尚未连接的节点
        /// </summary>
        private void CreateIsolatedNodes()
        {
            if (behaviorTree == null)
                return;

            var isolatedNodes = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(behaviorTree))
                .OfType<BTNode>()
                .Where(node => node != null && !node.Equals(null)) // 过滤已销毁的节点
                .Where(node => !nodeViews.ContainsKey(node)) // 过滤已创建的节点
                .ToList();

            foreach (var node in isolatedNodes)
                CreateNodeViewFromNode(node);
        }

        private BTNodeView CreateNodeViewFromNode(BTNode node)
        {
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
            catch (Exception ex)
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

                // 连接到所有子节点
                foreach (var child in BTNodeEditorService.GetChildren(node))
                {
                    if (child == null || child.Equals(null))
                        continue;

                    if (nodeViews.TryGetValue(child, out var childView) && childView != null)
                        ConnectNodes(nodeView, childView);
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

                        if (parentView?.Node != null && !parentView.Node.Equals(null) &&
                            childView?.Node != null && !childView.Node.Equals(null))
                        {
                            BTNodeEditorService.RemoveChild(parentView.Node, childView.Node);
                            if (behaviorTree != null)
                            {
                                BTEditorAssetService.MarkDirty(behaviorTree);
                            }
                        }
                    }
                }

                // 对于删除的节点，执行完整的删除逻辑
                foreach (var nodeView in nodesToRemove)
                {
                    if (nodeView?.Node == null || nodeView.Node.Equals(null))
                        continue;

                    // RootNode 不能删除
                    if (!BTNodeEditorService.CanDeleteNode(nodeView.Node))
                    {
                        Debug.LogWarning("Cannot delete root node!");
                        AddElement(nodeView);
                        continue;
                    }

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
                    var node = nodeView.Node;
                    foreach (var edge in edgesToRemove)
                    {
                        var parentView = edge.output.node as BTNodeView;
                        var childView = edge.input.node as BTNodeView;

                        if (parentView?.Node != null && !parentView.Node.Equals(null) &&
                            childView == nodeView && node != null)
                        {
                            BTNodeEditorService.RemoveChild(parentView.Node, node);
                        }
                    }

                    // 查找所有可能包含此节点的父节点并移除
                    foreach (var (parentNode, parentView) in nodeViews)
                    {
                        if (parentNode == null || parentNode.Equals(null))
                            continue;

                        BTNodeEditorService.RemoveChild(parentNode, node);
                    }

                    // 销毁节点对象
                    try
                    {
                        Object.DestroyImmediate(node, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to destroy node: {ex.Message}");
                    }
                }

                if (nodesToRemove.Count > 0 && behaviorTree != null)
                {
                    BTAssetService.AutoFixBehaviourTree(behaviorTree);
                    BTEditorAssetService.MarkDirty(behaviorTree);
                }
            }

            // 处理创建的连接
            if (graphViewChange.edgesToCreate != null)
            {
                var connectionChanged = false;
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var parentView = edge.output.node as BTNodeView;
                    var childView = edge.input.node as BTNodeView;

                    if (parentView?.Node != null && !parentView.Node.Equals(null) &&
                        childView?.Node != null && !childView.Node.Equals(null))
                    {
                        // 检查是否已存在连接，避免重复
                        var existingChildren = BTNodeEditorService.GetChildren(parentView.Node);
                        if (!existingChildren.Contains(childView.Node))
                        {
                            BTNodeEditorService.SetChild(parentView.Node, childView.Node);
                            connectionChanged = true;
                        }
                    }
                }

                if (connectionChanged && behaviorTree != null)
                {
                    BTEditorAssetService.MarkDirty(behaviorTree);
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

        #region HotKey Methods

        private void CopySelectedNodes()
        {
            var selectedNodes = selection.OfType<BTNodeView>()
                .Where(nv => BTNodeEditorService.CanCopyNode(nv.Node, behaviorTree))
                .ToList();

            if (selectedNodes.Count > 0)
            {
                // 复制第一个选中的节点（保持简单，后续可以扩展为多选）
                copiedNode = selectedNodes[0].Node;
            }
        }

        private void CutSelectedNodes()
        {
            CopySelectedNodes();
            DeleteSelectedNodes();
        }

        private void DeleteSelectedNodes()
        {
            var selectedNodes = selection.OfType<BTNodeView>()
                .Where(nv => BTNodeEditorService.CanDeleteNode(nv.Node))
                .ToList();

            foreach (var nodeView in selectedNodes)
            {
                DeleteNode(nodeView);
            }
        }

        private void DuplicateSelectedNodes()
        {
            var selectedNodes = selection.OfType<BTNodeView>()
                .Where(nv => BTNodeEditorService.CanDuplicateNode(nv.Node, behaviorTree))
                .ToList();

            foreach (var nodeView in selectedNodes)
            {
                DuplicateNode(nodeView);
            }
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
            var node = nodeView.Node;
            var canCopy = BTNodeEditorService.CanCopyNode(node, behaviorTree);
            var canCut = BTNodeEditorService.CanCutNode(node, behaviorTree);
            var canDuplicate = BTNodeEditorService.CanDuplicateNode(node, behaviorTree);
            var canDelete = BTNodeEditorService.CanDeleteNode(node);

            // 基本操作
            evt.menu.AppendAction("Copy", _ => CopyNode(nodeView),
                canCopy ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
            evt.menu.AppendAction("Cut", _ => CutNode(nodeView),
                canCut ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
            evt.menu.AppendAction("Duplicate", _ => DuplicateNode(nodeView),
                canDuplicate ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
            evt.menu.AppendAction("Delete", _ => DeleteNode(nodeView),
                canDelete ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
        }

        public void CreateNode<T>() where T : BTNode
        {
            if (behaviorTree == null)
                return;

            var node = BTEditorAssetService.CreateNodeInAsset(behaviorTree, typeof(T));
            if (node == null)
                return;

            var nodeView = CreateNodeViewFromNode(node);
            nodeView.SetPosition(new Rect(mousePosition, Vector2.zero));
        }

        public void CreateNode(Type type)
        {
            if (behaviorTree == null)
                return;

            var node = BTEditorAssetService.CreateNodeInAsset(behaviorTree, type);
            if (node == null)
                return;

            var nodeView = CreateNodeViewFromNode(node);
            nodeView.SetPosition(new Rect(mousePosition, Vector2.zero));
        }

        private void CopyNode(BTNodeView nodeView)
        {
            if (!BTNodeEditorService.CanCopyNode(nodeView.Node, behaviorTree))
            {
                Debug.LogWarning("Cannot copy root node!");
                return;
            }

            copiedNode = nodeView.Node;
        }


        private void CutNode(BTNodeView nodeView)
        {
            if (!BTNodeEditorService.CanCutNode(nodeView.Node, behaviorTree))
            {
                Debug.LogWarning("Cannot cut root node!");
                return;
            }

            CopyNode(nodeView);
            DeleteNode(nodeView);
        }

        private void DuplicateNode(BTNodeView nodeView)
        {
            if (behaviorTree == null || nodeView?.Node == null || nodeView.Node.Equals(null))
                return;

            if (!BTNodeEditorService.CanDuplicateNode(nodeView.Node, behaviorTree))
            {
                Debug.LogWarning("Cannot duplicate root node!");
                return;
            }

            try
            {
                var originalNode = nodeView.Node;
                var duplicatedNode = Object.Instantiate(originalNode);
                duplicatedNode.name = originalNode.name + " (Copy)";

                // 清除子节点引用，避免复制时包含子节点
                foreach (var child in BTNodeEditorService.GetChildren(duplicatedNode).ToList())
                {
                    BTNodeEditorService.RemoveChild(duplicatedNode, child);
                }

                AssetDatabase.AddObjectToAsset(duplicatedNode, behaviorTree);
                BTEditorAssetService.MarkDirty(behaviorTree);

                var duplicatedView = CreateNodeViewFromNode(duplicatedNode);
                if (duplicatedView != null)
                {
                    var originalPos = nodeView.GetPosition();
                    duplicatedView.SetPosition(new Rect(originalPos.x + 200, originalPos.y, originalPos.width,
                        originalPos.height));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to duplicate node: {ex.Message}");
            }
        }

        private void DeleteNode(BTNodeView nodeView)
        {
            if (!BTNodeEditorService.CanDeleteNode(nodeView.Node))
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

                    BTNodeEditorService.RemoveChild(parentNode, node);
                }
            }

            RemoveElement(nodeView);
            nodeViews.Remove(nodeView.Node);

            try
            {
                Object.DestroyImmediate(nodeView.Node, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to destroy node: {ex.Message}");
            }

            BTAssetService.AutoFixBehaviourTree(behaviorTree);
            BTEditorAssetService.MarkDirty(behaviorTree);
        }

        private void PasteNode()
        {
            if (copiedNode == null || behaviorTree == null)
                return;

            if (copiedNode.Equals(null))
            {
                Debug.LogWarning("Copied node has been destroyed.");
                copiedNode = null;
                return;
            }

            if (!BTNodeEditorService.CanCopyNode(copiedNode, behaviorTree))
            {
                Debug.LogWarning("Cannot paste root node!");
                copiedNode = null;
                return;
            }

            try
            {
                var pastedNode = Object.Instantiate(copiedNode);
                pastedNode.name = copiedNode.name + " (Paste)";

                // 清除子节点引用，避免粘贴时包含子节点
                foreach (var child in BTNodeEditorService.GetChildren(pastedNode).ToList())
                {
                    BTNodeEditorService.RemoveChild(pastedNode, child);
                }

                AssetDatabase.AddObjectToAsset(pastedNode, behaviorTree);
                BTEditorAssetService.MarkDirty(behaviorTree);

                var pastedView = CreateNodeViewFromNode(pastedNode);
                if (pastedView != null)
                {
                    pastedView.SetPosition(new Rect(mousePosition, Vector2.zero));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to paste node: {ex.Message}");
            }
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
                    var displayName = BTNodeTypeHelper.GetNodeDisplayName(type);
                    var menuPath = $"Create Node/{category}/{displayName}";

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
            var defaultValue = GetDefaultValue(varType);
            behaviorTree.Blackboard.AddVariable(varName, varType, defaultValue);

            RefreshBlackboardView();
            BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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
                    behaviorTree.Blackboard.SetGameObject(varName, evt.newValue as GameObject);
                    BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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
            BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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
            BTEditorAssetService.MarkDirty(behaviorTree.Blackboard);
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

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            UpdateSelection();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            UpdateSelection();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            UpdateSelection();
        }

        /// <summary>
        /// 更新选择状态并发布事件
        /// </summary>
        private void UpdateSelection()
        {
            var selectedNodes = selection.OfType<BTNodeView>().ToList();
            var currentSelectedNode = selectedNodes.Count == 1 ? selectedNodes[0].Node : null;

            if (currentSelectedNode != null)
                BTEditorEventBus.PublishNodeSelected(currentSelectedNode);
            else
                BTEditorEventBus.PublishNodeDeselected();
        }

        public void UpdateNodeContent(BTNode node, string propertyName)
        {
            if (nodeViews.TryGetValue(node, out var nodeView))
            {
                nodeView.RefreshContent(propertyName);
            }
        }

        #endregion
    }
}