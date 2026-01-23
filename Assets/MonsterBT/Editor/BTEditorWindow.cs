using System;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MonsterBT.Editor
{
    public class BTEditorWindow : EditorWindow
    {
        private BTNodeGraphView graphView;
        private BTPropInspector inspector;
        private BTNodeLibrary nodeLibrary;
        private BTToolbar toolbar;

        private BehaviorTree currentBehaviorTree;

        #region UI Methods

        [MenuItem("Window/MonsterBT/BehaviorTree")]
        public static void ShowWindow()
        {
            var window = GetWindow<BTEditorWindow>();
            window.titleContent = new GUIContent("Monster BehaviorTree");
            window.minSize = new Vector2(800, 600);
        }

        public static void OpenBehaviorTree(BehaviorTree behaviorTree)
        {
            if (behaviorTree == null)
                return;

            var window = GetWindow<BTEditorWindow>();
            window.titleContent = new GUIContent("Monster BehaviorTree");
            window.minSize = new Vector2(800, 600);
            window.SetBehaviorTree(behaviorTree);
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is BehaviorTree behaviorTree)
            {
                OpenBehaviorTree(behaviorTree);
                return true;
            }

            return false;
        }

        public void SetBehaviorTree(BehaviorTree behaviorTree)
        {
            currentBehaviorTree = behaviorTree;

            if (behaviorTree != null && behaviorTree.Blackboard == null)
            {
                var blackboard = CreateInstance<Blackboard>();
                blackboard.name = "Blackboard";
                behaviorTree.Blackboard = blackboard;

                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(behaviorTree)))
                {
                    AssetDatabase.AddObjectToAsset(blackboard, behaviorTree);
                    EditorUtility.SetDirty(behaviorTree);
                    AssetDatabase.SaveAssets();
                }
            }

            toolbar?.SetBehaviorTree(behaviorTree);
            graphView?.SetBehaviorTree(behaviorTree);
        }

        public void CreateGUI()
        {
            var styleSheet = BTEditorResources.LoadStyleSheet("BTEditorStyle.uss");
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            var rootContainer = new VisualElement { name = "behavior-tree-editor-root" };
            rootContainer.AddToClassList("behavior-tree-editor");
            rootVisualElement.Add(rootContainer);

            CreateToolbar(rootContainer);
            CreateMainContent(rootContainer);
            CreateStatusBar(rootContainer);

            SetupUIElements();

            if (currentBehaviorTree != null && graphView != null)
                graphView.SetBehaviorTree(currentBehaviorTree);
        }

        private void CreateToolbar(VisualElement parent)
        {
            toolbar = new BTToolbar();
            parent.Add(toolbar);
        }

        private void CreateMainContent(VisualElement parent)
        {
            var mainContent = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal)
            {
                name = "main-content",
                style =
                {
                    flexGrow = 1
                }
            };

            nodeLibrary = new BTNodeLibrary();
            mainContent.Add(nodeLibrary);

            var rightContent = new TwoPaneSplitView(1, 300, TwoPaneSplitViewOrientation.Horizontal);
            var graphContainer = new VisualElement { name = "graph-container" };
            graphContainer.AddToClassList("graph-container");
            graphContainer.style.flexGrow = 1;
            rightContent.Add(graphContainer);
            rightContent.Add(CreateInspectorPanel());

            mainContent.Add(rightContent);
            parent.Add(mainContent);
        }


        private VisualElement CreateInspectorPanel()
        {
            var panel = new VisualElement { name = "inspector-panel" };
            panel.AddToClassList("sidebar");

            var inspectorTitle = new Label("Inspector");
            inspectorTitle.AddToClassList("sidebar-title");
            panel.Add(inspectorTitle);

            var propertiesSection = new VisualElement { name = "node-properties" };
            propertiesSection.AddToClassList("sidebar-section");
            var propertiesTitle = new Label("Node Properties");
            propertiesTitle.AddToClassList("sidebar-title");
            propertiesSection.Add(propertiesTitle);
            propertiesSection.Add(new VisualElement { name = "property-container" });

            panel.Add(propertiesSection);
            return panel;
        }

        private void CreateStatusBar(VisualElement parent)
        {
            var statusBar = new VisualElement { name = "status-bar" };
            statusBar.AddToClassList("status-bar");

            var statusText = new Label("Ready")
            {
                name = "status-text",
                style =
                {
                    color = new Color(0.78f, 0.78f, 0.78f),
                    fontSize = 11
                }
            };
            statusBar.Add(statusText);

            var nodeCount = new Label("Nodes: 0")
            {
                name = "node-count",
                style =
                {
                    color = new Color(0.59f, 0.59f, 0.59f),
                    fontSize = 10
                }
            };
            statusBar.Add(nodeCount);

            parent.Add(statusBar);
        }

        public void OnDestroy()
        {
            if (graphView == null || inspector == null)
                return;

            graphView.OnNodeSelected -= inspector.SetSelectedNode;
            graphView.OnNodeDeselected -= inspector.ClearSelection;
            inspector.OnPropertyChanged -= graphView.HandlePropertyChanged;
        }

        private void SetupUIElements()
        {
            var rootContainer = rootVisualElement.Q<VisualElement>("behavior-tree-editor-root");
            if (rootContainer == null) return;

            var graphContainer = rootContainer.Q<VisualElement>("graph-container");
            if (graphContainer != null)
            {
                graphView = new BTNodeGraphView();
                graphContainer.Add(graphView);
            }

            var propContainer = rootContainer.Q<VisualElement>("property-container");
            if (propContainer != null)
            {
                inspector = new BTPropInspector();
                propContainer.Add(inspector);
            }

            if (toolbar != null)
            {
                toolbar.OnBehaviorTreeChanged += OnBehaviorTreeChanged;
                toolbar.OnCreateNewRequested += OnCreateNewBehaviorTree;
                toolbar.OnSaveRequested += OnSaveBehaviorTree;
                toolbar.OnAutoLayoutRequested += OnAutoLayoutNodes;
                toolbar.OnPlayToggleRequested += OnTogglePlayMode;
                toolbar.OnDebugToggleRequested += OnToggleDebugMode;
            }

            if (graphView != null && inspector != null)
            {
                graphView.OnNodeSelected += inspector.SetSelectedNode;
                graphView.OnNodeDeselected += inspector.ClearSelection;
                inspector.OnPropertyChanged += graphView.HandlePropertyChanged;
            }

            if (nodeLibrary != null && graphView != null)
            {
                nodeLibrary.OnNodeRequested += graphView.CreateNode;
            }

            if (currentBehaviorTree != null)
                SetBehaviorTree(currentBehaviorTree);
        }

        #endregion

        #region Toolbar Callbacks

        private void OnBehaviorTreeChanged(BehaviorTree behaviorTree)
        {
            currentBehaviorTree = behaviorTree;
            graphView?.SetBehaviorTree(currentBehaviorTree);
        }

        private void OnAutoLayoutNodes()
        {
            if (graphView != null && currentBehaviorTree != null)
            {
                // TODO:实现自动布局逻辑
                Debug.Log("执行自动布局");
            }
        }

        private void OnTogglePlayMode()
        {
            // TODO:实现播放/停止行为树的功能
            Debug.Log("切换播放模式");
        }

        private void OnToggleDebugMode()
        {
            // TODO:实现调试模式切换
            Debug.Log("切换调试模式");
        }

        private void OnCreateNewBehaviorTree()
        {
            var tree = CreateInstance<BehaviorTree>();
            var rootNode = CreateInstance<RootNode>();
            var blackboard = CreateInstance<Blackboard>();

            rootNode.name = "Root";
            rootNode.Position = new Vector2(400, 100);
            blackboard.name = "Blackboard";

            tree.RootNode = rootNode;
            tree.Blackboard = blackboard;
            tree.name = "New BehaviorTree";

            var path = EditorUtility.SaveFilePanelInProject("Save Behavior Tree", "NewBehaviorTree", "asset", "");
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(tree, path);
            AssetDatabase.AddObjectToAsset(rootNode, tree);
            AssetDatabase.AddObjectToAsset(blackboard, tree);
            AssetDatabase.SaveAssets();

            SetBehaviorTree(tree);
        }

        private void OnSaveBehaviorTree()
        {
            if (currentBehaviorTree == null)
                return;

            EditorUtility.SetDirty(currentBehaviorTree);
            AssetDatabase.SaveAssets();
            Debug.Log("Behavior tree saved.");
        }

        #endregion
    }
}