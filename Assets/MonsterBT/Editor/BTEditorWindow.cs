using MonsterBT.Editor.Services;
using MonsterBT.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace MonsterBT.Editor
{
    public class BTEditorWindow : EditorWindow
    {
        private BTNodeGraphView graphView;
        private BTPropInspector inspector;
        private BTNodeLibrary nodeLibrary;
        private BTToolbar toolbar;
        private BTStatusBar statusBar;

        private BehaviorTree currentBehaviorTree;

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

            if (behaviorTree != null)
            {
                BTBehaviorTreeService.EnsureBlackboardExists(behaviorTree);
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

            // L：Node Library
            nodeLibrary = new BTNodeLibrary();

            var centerRightSplit = new TwoPaneSplitView(1, 300, TwoPaneSplitViewOrientation.Horizontal);
            // M：Graph View
            CreateGraphView(centerRightSplit);
            // R：Inspector
            CreateInspectorPanel(centerRightSplit);

            mainContent.Add(nodeLibrary);
            mainContent.Add(centerRightSplit);

            parent.Add(mainContent);
        }

        private void CreateGraphView(VisualElement parent)
        {
            graphView = new BTNodeGraphView();
            var graphContainer = new VisualElement
            {
                name = "graph-container"
            };
            graphContainer.AddToClassList("graph-container");
            graphContainer.style.flexGrow = 1;
            graphContainer.Add(graphView);

            parent.Add(graphContainer);
        }

        private void CreateInspectorPanel(VisualElement parent)
        {
            var panel = new VisualElement { name = "inspector-panel" };
            panel.AddToClassList("sidebar");

            var inspectorTitle = new Label("Inspector");
            inspectorTitle.AddToClassList("sidebar-title");
            panel.Add(inspectorTitle);

            inspector = new BTPropInspector();
            panel.Add(inspector);

            parent.Add(panel);
        }

        private void CreateStatusBar(VisualElement parent)
        {
            statusBar = new BTStatusBar();
            parent.Add(statusBar);
        }

        public void OnDestroy()
        {
            // 窗口关闭前验证并保存资源
            if (currentBehaviorTree != null)
            {
                BTAssetService.ValidateAndFixBehaviorTree(currentBehaviorTree);
                BTEditorAssetService.MarkDirtyAndSave(currentBehaviorTree);
            }

            BTEditorEventBus.ClearAll();
        }

        private void SetupUIElements()
        {
            if (toolbar != null)
            {
                if (graphView != null)
                {
                    toolbar.SetGraphView(graphView);
                }

                toolbar.SetBehaviorTreeChangedCallback(OnBehaviorTreeChanged);
            }

            if (inspector != null)
            {
                BTEditorEventBus.OnNodeSelected += inspector.SetSelectedNode;
                BTEditorEventBus.OnNodeDeselected += inspector.ClearSelection;
            }

            if (graphView != null)
            {
                BTEditorEventBus.OnPropertyChanged += graphView.HandlePropertyChanged;
                BTEditorEventBus.OnNodeRequested += graphView.CreateNode;
            }

            if (currentBehaviorTree != null)
                SetBehaviorTree(currentBehaviorTree);
        }

        private void OnBehaviorTreeChanged(BehaviorTree behaviorTree)
        {
            currentBehaviorTree = behaviorTree;
            if (behaviorTree != null)
            {
                BTBehaviorTreeService.EnsureBlackboardExists(behaviorTree);
                // 验证资源完整性
                BTAssetService.ValidateAndFixBehaviorTree(behaviorTree);
            }
        }
    }
}