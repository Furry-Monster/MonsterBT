using MonsterBT.Editor.Services;
using MonsterBT.Runtime;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MonsterBT.Editor
{
    public class BTToolbar : VisualElement
    {
        private readonly ObjectField behaviorTreeField;

        public BTToolbar()
        {
            var styleSheet = BTEditorResources.LoadStyleSheet("BTEditorStyle.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);

            var toolbar = new Toolbar { name = "main-toolbar" };
            toolbar.AddToClassList("editor-toolbar");

            behaviorTreeField = new ObjectField("Behavior Tree Asset") { name = "behavior-tree-field" };
            behaviorTreeField.AddToClassList("toolbar-field");
            behaviorTreeField.objectType = typeof(BehaviorTree);
            behaviorTreeField.RegisterValueChangedCallback(OnBehaviorTreeChangedInternal);
            toolbar.Add(behaviorTreeField);

            var createBtn = new Button { name = "create-button", text = "Create New" };
            createBtn.AddToClassList("toolbar-button");
            createBtn.clicked += () => BTEditorEventBus.PublishCreateNewRequested();
            toolbar.Add(createBtn);

            var saveBtn = new Button { name = "save-button", text = "Save" };
            saveBtn.AddToClassList("toolbar-button");
            saveBtn.clicked += () => BTEditorEventBus.PublishSaveRequested();
            toolbar.Add(saveBtn);

            var autoLayoutBtn = new Button { name = "auto-layout-button", text = "Auto Layout" };
            autoLayoutBtn.AddToClassList("toolbar-button");
            autoLayoutBtn.clicked += () => BTEditorEventBus.PublishAutoLayoutRequested();
            toolbar.Add(autoLayoutBtn);

            var playBtn = new Button { name = "play-button", text = "▶ Play" };
            playBtn.AddToClassList("toolbar-button");
            playBtn.clicked += () => BTEditorEventBus.PublishPlayToggleRequested();
            toolbar.Add(playBtn);

            var debugBtn = new Button { name = "debug-button", text = "# Debug" };
            debugBtn.AddToClassList("toolbar-button");
            debugBtn.clicked += () => BTEditorEventBus.PublishDebugToggleRequested();
            toolbar.Add(debugBtn);

            Add(toolbar);
        }

        public void SetBehaviorTree(BehaviorTree behaviorTree)
        {
            if (behaviorTreeField != null)
                behaviorTreeField.value = behaviorTree;
        }

        private void OnBehaviorTreeChangedInternal(ChangeEvent<Object> changeEvent)
        {
            BTEditorEventBus.PublishBehaviorTreeChanged(changeEvent.newValue as BehaviorTree);
        }
    }
}