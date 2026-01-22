using MonsterBT.Runtime;
using MonsterBT.Runtime.Actions;
using MonsterBT.Runtime.Composite;
using MonsterBT.Runtime.Conditions;
using MonsterBT.Runtime.Decorator;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public class BTEditorWindow : EditorWindow
    {
        private BTNodeGraphView graphView;
        private BTPropInspector inspector;
        private ObjectField behaviorTreeField;

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
            
            if (behaviorTreeField != null)
                behaviorTreeField.value = behaviorTree;
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
            var toolbar = new Toolbar { name = "main-toolbar" };
            toolbar.AddToClassList("editor-toolbar");

            behaviorTreeField = new ObjectField("Behavior Tree Asset") { name = "behavior-tree-field" };
            behaviorTreeField.AddToClassList("toolbar-field");
            behaviorTreeField.objectType = typeof(BehaviorTree);
            toolbar.Add(behaviorTreeField);

            var createBtn = new Button { name = "create-button", text = "Create New" };
            createBtn.AddToClassList("toolbar-button");
            toolbar.Add(createBtn);

            var saveBtn = new Button { name = "save-button", text = "Save" };
            saveBtn.AddToClassList("toolbar-button");
            toolbar.Add(saveBtn);

            var autoLayoutBtn = new Button { name = "auto-layout-button", text = "Auto Layout" };
            autoLayoutBtn.AddToClassList("toolbar-button");
            toolbar.Add(autoLayoutBtn);

            var playBtn = new Button { name = "play-button", text = "▶ Play" };
            playBtn.AddToClassList("toolbar-button");
            toolbar.Add(playBtn);

            var debugBtn = new Button { name = "debug-button", text = "# Debug" };
            debugBtn.AddToClassList("toolbar-button");
            toolbar.Add(debugBtn);

            parent.Add(toolbar);
        }

        private void CreateMainContent(VisualElement parent)
        {
            var mainContent = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal)
                { name = "main-content" };
            mainContent.style.flexGrow = 1;

            mainContent.Add(CreateNodeLibraryPanel());

            var rightContent = new TwoPaneSplitView(1, 300, TwoPaneSplitViewOrientation.Horizontal);
            var graphContainer = new VisualElement { name = "graph-container" };
            graphContainer.AddToClassList("graph-container");
            graphContainer.style.flexGrow = 1;
            rightContent.Add(graphContainer);
            rightContent.Add(CreateInspectorPanel());

            mainContent.Add(rightContent);
            parent.Add(mainContent);
        }

        private VisualElement CreateNodeLibraryPanel()
        {
            var panel = new VisualElement { name = "node-library-panel" };
            panel.AddToClassList("sidebar");

            var title = new Label("Node Library") { name = "node-library-title" };
            title.AddToClassList("sidebar-title");
            panel.Add(title);

            var compositeSection = new VisualElement { name = "composite-nodes" };
            compositeSection.AddToClassList("sidebar-section");
            var compositeTitle = new Label("Composite Nodes");
            compositeTitle.AddToClassList("sidebar-title");
            compositeSection.Add(compositeTitle);
            compositeSection.Add(CreateNodeListItem("selector-item", "Selector", "Composite"));
            compositeSection.Add(CreateNodeListItem("sequence-item", "Sequence", "Composite"));
            panel.Add(compositeSection);

            var decoratorSection = new VisualElement { name = "decorator-nodes" };
            decoratorSection.AddToClassList("sidebar-section");
            var decoratorTitle = new Label("Decorator Nodes");
            decoratorTitle.AddToClassList("sidebar-title");
            decoratorSection.Add(decoratorTitle);
            decoratorSection.Add(CreateNodeListItem("inverter-item", "Inverter", "Decorator"));
            panel.Add(decoratorSection);

            var actionSection = new VisualElement { name = "action-nodes" };
            actionSection.AddToClassList("sidebar-section");
            var actionTitle = new Label("Action Nodes");
            actionTitle.AddToClassList("sidebar-title");
            actionSection.Add(actionTitle);
            actionSection.Add(CreateNodeListItem("debug-log-item", "Debug Log", "Action"));
            actionSection.Add(CreateNodeListItem("wait-item", "Wait", "Action"));
            actionSection.Add(CreateNodeListItem("move-to-target-item", "Move To Target", "Action"));
            panel.Add(actionSection);

            return panel;
        }

        private VisualElement CreateNodeListItem(string name, string labelText, string typeText)
        {
            var item = new VisualElement { name = name };
            item.AddToClassList("node-list-item");
            item.Add(new Label(labelText));
            var typeLabel = new Label(typeText) { name = "blackboard-type" };
            typeLabel.AddToClassList("blackboard-type");
            item.Add(typeLabel);
            return item;
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

            var statusText = new Label("Ready") { name = "status-text" };
            statusText.style.color = new Color(0.78f, 0.78f, 0.78f);
            statusText.style.fontSize = 11;
            statusBar.Add(statusText);

            var nodeCount = new Label("Nodes: 0") { name = "node-count" };
            nodeCount.style.color = new Color(0.59f, 0.59f, 0.59f);
            nodeCount.style.fontSize = 10;
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

            behaviorTreeField = rootContainer.Q<ObjectField>("behavior-tree-field");
            if (behaviorTreeField != null)
                behaviorTreeField.RegisterValueChangedCallback(OnBehaviorTreeChanged);

            rootContainer.Q<Button>("create-button")?.RegisterCallback<ClickEvent>(OnCreateNewBehaviorTree);
            rootContainer.Q<Button>("save-button")?.RegisterCallback<ClickEvent>(OnSaveBehaviorTree);
            rootContainer.Q<Button>("auto-layout-button")?.RegisterCallback<ClickEvent>(OnAutoLayoutNodes);
            rootContainer.Q<Button>("play-button")?.RegisterCallback<ClickEvent>(OnTogglePlayMode);
            rootContainer.Q<Button>("debug-button")?.RegisterCallback<ClickEvent>(OnToggleDebugMode);

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

            if (graphView != null && inspector != null)
            {
                graphView.OnNodeSelected += inspector.SetSelectedNode;
                graphView.OnNodeDeselected += inspector.ClearSelection;
                inspector.OnPropertyChanged += graphView.HandlePropertyChanged;
            }

            rootContainer.Query<VisualElement>(className: "node-list-item").ForEach(item =>
            {
                item.RegisterCallback<ClickEvent>(OnClickSpawnNode);
                item.RegisterCallback<MouseDownEvent>(OnDragSpawnNode);
            });

            if (currentBehaviorTree != null)
                SetBehaviorTree(currentBehaviorTree);
        }

        #endregion

        #region Toolbar Callbacks

        private void OnBehaviorTreeChanged(ChangeEvent<Object> changeEvent)
        {
            currentBehaviorTree = changeEvent.newValue as BehaviorTree;
            graphView.SetBehaviorTree(currentBehaviorTree);
        }

        private void OnAutoLayoutNodes(ClickEvent evt)
        {
            if (graphView != null && currentBehaviorTree != null)
            {
                // TODO:实现自动布局逻辑
                Debug.Log("执行自动布局");
            }
        }

        private void OnTogglePlayMode(ClickEvent evt)
        {
            // TODO:实现播放/停止行为树的功能
            Debug.Log("切换播放模式");
        }

        private void OnToggleDebugMode(ClickEvent evt)
        {
            // TODO:实现调试模式切换
            Debug.Log("切换调试模式");
        }

        private void OnCreateNewBehaviorTree(ClickEvent evt)
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

            string path = EditorUtility.SaveFilePanelInProject("Save Behavior Tree", "NewBehaviorTree", "asset", "");
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(tree, path);
            AssetDatabase.AddObjectToAsset(rootNode, tree);
            AssetDatabase.AddObjectToAsset(blackboard, tree);
            AssetDatabase.SaveAssets();

            behaviorTreeField.value = tree;
            currentBehaviorTree = tree;
            graphView.SetBehaviorTree(currentBehaviorTree);
        }

        private void OnSaveBehaviorTree(ClickEvent evt)
        {
            if (currentBehaviorTree == null)
                return;

            EditorUtility.SetDirty(currentBehaviorTree);
            AssetDatabase.SaveAssets();
            Debug.Log("Behavior tree saved.");
        }

        #endregion

        #region Library Callbacks

        private void OnClickSpawnNode(ClickEvent evt)
        {
            if (graphView == null || currentBehaviorTree == null) return;

            var targetElement = evt.currentTarget as VisualElement;
            if (targetElement == null) return;

            var nodeType = targetElement.name switch
            {
                "selector-item" => typeof(Selector),
                "sequence-item" => typeof(Sequence),
                "inverter-item" => typeof(Inverter),
                "debug-log-item" => typeof(DebugLogAction),
                "wait-item" => typeof(WaitAction),
                "move-to-target-item" => typeof(MoveToTargetAction),
                "distance-condition-item" => typeof(DistanceCondition),
                _ => null
            };

            if (nodeType != null)
                graphView.CreateNode(nodeType);
        }

        private void OnDragSpawnNode(MouseDownEvent evt)
        {
            //TODO: 实现拖动到指定位置创建节点
        }

        #endregion
    }
}