using MonsterBT.Editor.Service.Asset;
using MonsterBT.Editor.View.Graph;
using MonsterBT.Runtime;
using UnityEditor.UIElements;
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
            AddToClassList("toolbar-container");
            
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
            if (behaviorTreeField != null)
                behaviorTreeField.value = behaviorTree;
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
                graphView.AutoLayout();
            }
        }

        private void OnBehaviorTreeFieldChanged(ChangeEvent<Object> changeEvent)
        {
            var behaviorTree = changeEvent.newValue as BehaviorTree;
            OnBehaviorTreeChanged(behaviorTree);
        }
    }
}