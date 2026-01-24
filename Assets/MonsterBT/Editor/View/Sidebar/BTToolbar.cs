using MonsterBT.Editor.Base;
using MonsterBT.Editor.Service.Asset;
using MonsterBT.Editor.View.Graph;
using BTEditorResources = MonsterBT.Editor.Base.BTEditorResources;
using MonsterBT.Runtime;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MonsterBT.Editor.View.Sidebar
{
    public class BTToolbar : VisualElement
    {
        private readonly ObjectField behaviorTreeField;
        private BTNodeGraphView graphView;
        private BehaviorTree currentBehaviorTree;
        private System.Action<BehaviorTree> onBehaviorTreeChangedCallback;

        public BTToolbar()
        {
            var styleSheet = BTEditorResources.LoadStyleSheet("BTEditorStyle.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);

            var toolbar = new Toolbar { name = "main-toolbar" };
            toolbar.AddToClassList("editor-toolbar");

            behaviorTreeField = new ObjectField("Behavior Tree Asset") { name = "behavior-tree-field" };
            behaviorTreeField.AddToClassList("toolbar-field");
            behaviorTreeField.objectType = typeof(BehaviorTree);
            behaviorTreeField.RegisterValueChangedCallback(OnBehaviorTreeFieldChanged);
            toolbar.Add(behaviorTreeField);

            var createBtn = new Button { name = "create-button", text = "Create New" };
            createBtn.AddToClassList("toolbar-button");
            createBtn.clicked += CreateNewBehaviorTree;
            toolbar.Add(createBtn);

            var saveBtn = new Button { name = "save-button", text = "Save" };
            saveBtn.AddToClassList("toolbar-button");
            saveBtn.clicked += SaveBehaviorTree;
            toolbar.Add(saveBtn);

            var autoLayoutBtn = new Button { name = "auto-layout-button", text = "Auto Layout" };
            autoLayoutBtn.AddToClassList("toolbar-button");
            autoLayoutBtn.clicked += AutoLayoutNodes;
            toolbar.Add(autoLayoutBtn);

            var playBtn = new Button { name = "play-button", text = "▶ Play" };
            playBtn.AddToClassList("toolbar-button");
            playBtn.clicked += TogglePlayMode;
            toolbar.Add(playBtn);

            var debugBtn = new Button { name = "debug-button", text = "# Debug" };
            debugBtn.AddToClassList("toolbar-button");
            debugBtn.clicked += ToggleDebugMode;
            toolbar.Add(debugBtn);

            Add(toolbar);
        }

        public void SetBehaviorTree(BehaviorTree behaviorTree)
        {
            currentBehaviorTree = behaviorTree;
            if (behaviorTreeField != null)
                behaviorTreeField.value = behaviorTree;
        }

        public void SetGraphView(BTNodeGraphView graphView)
        {
            this.graphView = graphView;
        }

        public void SetBehaviorTreeChangedCallback(System.Action<BehaviorTree> callback)
        {
            onBehaviorTreeChangedCallback = callback;
        }

        public void OnBehaviorTreeChanged(BehaviorTree behaviorTree)
        {
            currentBehaviorTree = behaviorTree;
            graphView?.SetBehaviorTree(currentBehaviorTree);

            onBehaviorTreeChangedCallback?.Invoke(behaviorTree);
        }

        public void CreateNewBehaviorTree()
        {
            var tree = BTBehaviorTreeService.CreateNewBehaviorTree();
            if (tree != null)
            {
                OnBehaviorTreeChanged(tree);
            }
        }

        public void SaveBehaviorTree()
        {
            if (currentBehaviorTree != null)
            {
                BTBehaviorTreeService.SaveBehaviorTree(currentBehaviorTree);
            }
        }

        public void AutoLayoutNodes()
        {
            if (graphView != null && currentBehaviorTree != null)
            {
                Debug.Log("执行自动布局");
            }
        }

        public void TogglePlayMode()
        {
            Debug.Log("切换播放模式");
        }

        public void ToggleDebugMode()
        {
            Debug.Log("切换调试模式");
        }

        private void OnBehaviorTreeFieldChanged(ChangeEvent<Object> changeEvent)
        {
            var behaviorTree = changeEvent.newValue as BehaviorTree;
            OnBehaviorTreeChanged(behaviorTree);
        }
    }
}
