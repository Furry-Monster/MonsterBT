using UnityEngine;

namespace MonsterBT.Runtime.Actions
{
    [CreateAssetMenu(fileName = "DebugLogAction", menuName = "MonsterBTNode/Actions/DebugLogAction")]
    public class DebugLogAction : ActionNode
    {
        [SerializeField][Tooltip("输出的消息")] private string message = "Debug Log Action";
        [SerializeField][Tooltip("消息输出的级别")] private LogType logType = LogType.Log;

        protected override BTNodeState OnUpdate()
        {
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log($"[BT] {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[BT] {message}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[BT] {message}");
                    break;
            }

            return BTNodeState.Success;
        }
    }
}