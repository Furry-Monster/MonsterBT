using UnityEngine;

namespace MonsterBT.Runtime
{
    /// <summary>
    /// 行为树执行器，挂载到对应的GO上即可
    /// </summary>
    public class BehaviorTreeRunner : MonoBehaviour
    {
        [SerializeField] private BehaviorTree behaviorTreeAsset;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool debugMode;

        private BehaviorTree runtimeTree;
        private bool isRunning;
        private BTNodeState lastState = BTNodeState.Running;

        public BehaviorTree RuntimeTree => runtimeTree;
        public bool IsRunning => isRunning;
        public bool DebugMode => debugMode;

        private void Start()
        {
            if (runOnStart)
            {
                StartTree();
            }
        }

        private void Update()
        {
            if (isRunning && runtimeTree != null)
            {
                var state = runtimeTree.Update();

                if (debugMode && state != lastState)
                {
                    Debug.Log($"[BT] [{gameObject.name}] State: {lastState} -> {state} | Tree: {behaviorTreeAsset?.name}");
                    lastState = state;
                }

                if (state is BTNodeState.Success or BTNodeState.Failure)
                {
                    if (!loop)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"[BT] [{gameObject.name}] Tree completed: {state} | Tree: {behaviorTreeAsset?.name}");
                        }
                        StopTree();
                    }
                }
            }
        }

        public void StartTree()
        {
            if (behaviorTreeAsset != null)
            {
                runtimeTree = behaviorTreeAsset.Clone();
                runtimeTree.Initialize();

                if (runtimeTree.Blackboard != null)
                {
                    runtimeTree.Blackboard.SetGameObject("Owner", gameObject);
                    runtimeTree.Blackboard.SetTransform("OwnerTransform", transform);
                    runtimeTree.Blackboard.SetGameObject("MainCamera", Camera.main?.gameObject);
                    runtimeTree.Blackboard.SetBool("DebugMode", debugMode);
                }

                isRunning = true;
                lastState = BTNodeState.Running;

                if (debugMode)
                {
                    Debug.Log($"[BT] [{gameObject.name}] Started tree: {behaviorTreeAsset.name}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"[BT] [{gameObject.name}] No behavior tree asset assigned");
            }
        }

        public void StopTree()
        {
            if (runtimeTree == null)
                return;

            runtimeTree.Abort();
            isRunning = false;

            if (debugMode)
            {
                Debug.Log($"[BT] [{gameObject.name}] Stopped tree: {behaviorTreeAsset?.name}");
            }
        }

        public void RestartTree()
        {
            if (debugMode)
            {
                Debug.Log($"[BT] [{gameObject.name}] Restarting tree: {behaviorTreeAsset?.name}");
            }
            StopTree();
            StartTree();
        }

        private void OnDisable()
        {
            StopTree();
        }
    }
}