using System;
using MonsterBT.Runtime;
using UnityEngine;

namespace MonsterBT.Editor.Services
{
    public static class BTEditorEventBus
    {
        #region Toolbar Events

        public static event Action<BehaviorTree> OnBehaviorTreeChanged;
        public static event Action OnCreateNewRequested;
        public static event Action OnSaveRequested;
        public static event Action OnAutoLayoutRequested;
        public static event Action OnPlayToggleRequested;
        public static event Action OnDebugToggleRequested;

        public static void PublishBehaviorTreeChanged(BehaviorTree tree)
        {
            OnBehaviorTreeChanged?.Invoke(tree);
        }

        public static void PublishCreateNewRequested()
        {
            OnCreateNewRequested?.Invoke();
        }

        public static void PublishSaveRequested()
        {
            OnSaveRequested?.Invoke();
        }

        public static void PublishAutoLayoutRequested()
        {
            OnAutoLayoutRequested?.Invoke();
        }

        public static void PublishPlayToggleRequested()
        {
            OnPlayToggleRequested?.Invoke();
        }

        public static void PublishDebugToggleRequested()
        {
            OnDebugToggleRequested?.Invoke();
        }

        #endregion

        #region Graph View Events

        public static event Action<BTNode> OnNodeSelected;
        public static event Action OnNodeDeselected;

        public static void PublishNodeSelected(BTNode node)
        {
            OnNodeSelected?.Invoke(node);
        }

        public static void PublishNodeDeselected()
        {
            OnNodeDeselected?.Invoke();
        }

        #endregion

        #region Node Library Events

        public static event Action<Type> OnNodeRequested;

        public static void PublishNodeRequested(Type nodeType)
        {
            OnNodeRequested?.Invoke(nodeType);
        }

        #endregion

        #region Inspector Events

        public static event Action<BTNode, string> OnPropertyChanged;

        public static void PublishPropertyChanged(BTNode node, string propertyName)
        {
            OnPropertyChanged?.Invoke(node, propertyName);
        }

        #endregion

        #region Log Events

        public static event Action<string, LogType> OnLogMessage;

        public static void PublishLogMessage(string message, LogType logType)
        {
            OnLogMessage?.Invoke(message, logType);
        }

        #endregion

        #region Cleanup

        public static void ClearAll()
        {
            OnBehaviorTreeChanged = null;
            OnCreateNewRequested = null;
            OnSaveRequested = null;
            OnAutoLayoutRequested = null;
            OnPlayToggleRequested = null;
            OnDebugToggleRequested = null;
            OnNodeSelected = null;
            OnNodeDeselected = null;
            OnNodeRequested = null;
            OnPropertyChanged = null;
            OnLogMessage = null;
        }

        #endregion
    }
}