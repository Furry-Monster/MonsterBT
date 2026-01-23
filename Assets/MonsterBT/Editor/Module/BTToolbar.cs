using System;
using MonsterBT.Runtime;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MonsterBT.Editor
{
    public class BTToolbar : VisualElement
    {
        private readonly ObjectField behaviorTreeField;

        public event Action<BehaviorTree> OnBehaviorTreeChanged;
        public event Action OnCreateNewRequested;
        public event Action OnSaveRequested;
        public event Action OnAutoLayoutRequested;
        public event Action OnPlayToggleRequested;
        public event Action OnDebugToggleRequested;

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
            createBtn.clicked += () => OnCreateNewRequested?.Invoke();
            toolbar.Add(createBtn);

            var saveBtn = new Button { name = "save-button", text = "Save" };
            saveBtn.AddToClassList("toolbar-button");
            saveBtn.clicked += () => OnSaveRequested?.Invoke();
            toolbar.Add(saveBtn);

            var autoLayoutBtn = new Button { name = "auto-layout-button", text = "Auto Layout" };
            autoLayoutBtn.AddToClassList("toolbar-button");
            autoLayoutBtn.clicked += () => OnAutoLayoutRequested?.Invoke();
            toolbar.Add(autoLayoutBtn);

            var playBtn = new Button { name = "play-button", text = "▶ Play" };
            playBtn.AddToClassList("toolbar-button");
            playBtn.clicked += () => OnPlayToggleRequested?.Invoke();
            toolbar.Add(playBtn);

            var debugBtn = new Button { name = "debug-button", text = "# Debug" };
            debugBtn.AddToClassList("toolbar-button");
            debugBtn.clicked += () => OnDebugToggleRequested?.Invoke();
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
            OnBehaviorTreeChanged?.Invoke(changeEvent.newValue as BehaviorTree);
        }
    }
}