using System;
using System.Collections.Generic;
using System.Linq;
using MonsterBT.Editor.Services;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.Experimental.GraphView.Blackboard;

namespace MonsterBT.Editor
{
    public class BTNodeGraphView : GraphView
    {
        private BehaviorTree behaviorTree;
        private readonly Dictionary<BTNode, BTNodeView> nodeViews;
        private BTBlackboardViewManager blackboardManager;
        private BTNodeOperationService nodeOperationService;
        private BTGraphCommandHandler commandHandler;
        private BTGraphContextMenuBuilder contextMenuBuilder;

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

            var blackboardView = new Blackboard { name = "blackboard", title = "Variables" };
            blackboardView.AddToClassList("blackboard");
            blackboardView.addItemRequested += _ => Debug.Log("I don't know how to make this invisible.");
            Add(blackboardView);

            blackboardManager = new BTBlackboardViewManager(null, blackboardView);

            // 添加grid
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            name = "node-graph";
            AddToClassList("node-graph-view");

            graphViewChanged += OnGraphViewChanged;

            commandHandler = new BTGraphCommandHandler(this, null);
            RegisterCallback<ExecuteCommandEvent>(commandHandler.HandleExecuteCommand);
            RegisterCallback<ValidateCommandEvent>(commandHandler.HandleValidateCommand);

            contextMenuBuilder = new BTGraphContextMenuBuilder(this, blackboardManager, null);

            PopulateView();
        }

        ~BTNodeGraphView()
        {
            graphViewChanged -= OnGraphViewChanged;
            if (commandHandler != null)
            {
                UnregisterCallback<ExecuteCommandEvent>(commandHandler.HandleExecuteCommand);
                UnregisterCallback<ValidateCommandEvent>(commandHandler.HandleValidateCommand);
            }
        }

        public void SetBehaviorTree(BehaviorTree tree)
        {
            behaviorTree = tree;

            if (tree != null)
            {
                BTAssetService.AutoFixBehaviourTree(tree);
            }

            if (blackboardManager != null)
            {
                var blackboardView = this.Q<Blackboard>();
                blackboardManager = new BTBlackboardViewManager(tree, blackboardView);
            }

            if (commandHandler != null)
            {
                commandHandler = new BTGraphCommandHandler(this, tree);
            }

            if (contextMenuBuilder != null)
            {
                contextMenuBuilder = new BTGraphContextMenuBuilder(this, blackboardManager, tree);
            }

            nodeOperationService = new BTNodeOperationService(tree, nodeViews, this);

            PopulateView();
            RefreshBlackboardView();
        }

        public void PopulateView()
        {
            // 清除现有内容
            graphViewChanged -= OnGraphViewChanged;

            DeleteElements(graphElements);
            nodeViews.Clear();

            graphViewChanged += OnGraphViewChanged;

            if (behaviorTree?.RootNode != null && !behaviorTree.RootNode.Equals(null))
            {
                var rootView = CreateViewForNode(behaviorTree.RootNode);
                if (rootView == null)
                    return;

                CreateNodeViewsRecursive(behaviorTree.RootNode);
                LoadOrphanedNodes();
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

                CreateViewForNode(child);
                CreateNodeViewsRecursive(child);
            }
        }

        private void LoadOrphanedNodes()
        {
            if (behaviorTree == null)
                return;

            var orphanedNodes = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(behaviorTree))
                .OfType<BTNode>()
                .Where(node => node != null && !node.Equals(null))
                .Where(node => !nodeViews.ContainsKey(node))
                .ToList();

            foreach (var node in orphanedNodes)
                CreateViewForNode(node);
        }

        private BTNodeView CreateViewForNode(BTNode node)
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

                foreach (var nodeView in nodesToRemove)
                {
                    if (nodeView?.Node == null || nodeView.Node.Equals(null))
                        continue;

                    if (!BTNodeEditorService.CanDeleteNode(nodeView.Node))
                    {
                        Debug.LogWarning("Cannot delete root node!");
                        AddElement(nodeView);
                        continue;
                    }

                    if (nodeOperationService != null)
                    {
                        nodeOperationService.RemoveNodeFromGraph(nodeView.Node, nodeView);
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

        public void CopySelectedNodes()
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

        public void CutSelectedNodes()
        {
            CopySelectedNodes();
            DeleteSelectedNodes();
        }

        public void DeleteSelectedNodes()
        {
            var selectedNodes = selection.OfType<BTNodeView>()
                .Where(nv => BTNodeEditorService.CanDeleteNode(nv.Node))
                .ToList();

            foreach (var nodeView in selectedNodes)
            {
                DeleteNode(nodeView);
            }
        }

        public void DuplicateSelectedNodes()
        {
            var selectedNodes = selection.OfType<BTNodeView>()
                .Where(nv => BTNodeEditorService.CanDuplicateNode(nv.Node, behaviorTree))
                .ToList();

            foreach (var nodeView in selectedNodes)
            {
                DuplicateNode(nodeView);
            }
        }

        private BTNode copiedNode;
        private Vector2 mousePosition;

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            mousePosition = evt.localMousePosition;
            if (contextMenuBuilder != null)
            {
                contextMenuBuilder.BuildContextualMenu(evt, evt.localMousePosition);
            }
        }

        public void CopyNode(BTNodeView nodeView)
        {
            if (!BTNodeEditorService.CanCopyNode(nodeView.Node, behaviorTree))
            {
                Debug.LogWarning("Cannot copy root node!");
                return;
            }

            copiedNode = nodeView.Node;
        }

        public void CutNode(BTNodeView nodeView)
        {
            if (!BTNodeEditorService.CanCutNode(nodeView.Node, behaviorTree))
            {
                Debug.LogWarning("Cannot cut root node!");
                return;
            }

            CopyNode(nodeView);
            DeleteNode(nodeView);
        }

        public void CreateNode<T>() where T : BTNode
        {
            Create(typeof(T), mousePosition);
        }

        public void CreateNode(Type type)
        {
            Create(type, mousePosition);
        }

        public void Create(Type type, Vector2 position)
        {
            if (nodeOperationService != null)
            {
                nodeOperationService.CreateNode(type, position, CreateViewForNode);
            }
        }


        public void DuplicateNode(BTNodeView nodeView)
        {
            if (nodeOperationService != null)
            {
                nodeOperationService.DuplicateNode(nodeView, new Vector2(200, 0), CreateViewForNode);
            }
        }

        public void DeleteNode(BTNodeView nodeView)
        {
            if (nodeOperationService != null)
            {
                nodeOperationService.DeleteNode(nodeView);
            }
        }


        public void PasteNode()
        {
            if (nodeOperationService != null && copiedNode != null)
            {
                nodeOperationService.PasteNode(copiedNode, mousePosition, CreateViewForNode);
            }
        }

        public bool HasCopiedNode()
        {
            return copiedNode != null && !copiedNode.Equals(null);
        }

        public void AddBlackboardVariable(string varName, Type varType)
        {
            blackboardManager?.AddVariable(varName, varType);
        }

        public void RefreshBlackboardView()
        {
            blackboardManager?.RefreshView();
        }

        public void RemoveBlackboardVariable(string varName)
        {
            blackboardManager?.RemoveVariable(varName);
        }

        public void RenameBlackboardVariable(string oldName, string newName)
        {
            blackboardManager?.RenameVariable(oldName, newName);
        }

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
    }
}