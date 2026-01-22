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

                if (debugMode)
                    Debug.Log($"[BehaviorTreeRunner] Tree state: {state}");

                // 根据需要处理树的完成状态
                if (state is BTNodeState.Success or BTNodeState.Failure)
                {
                    if (!loop)
                    {
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

                // 设置一些默认的黑板值
                if (runtimeTree.Blackboard != null)
                {
                    runtimeTree.Blackboard.SetGameObject("Owner", gameObject);
                    runtimeTree.Blackboard.SetTransform("OwnerTransform", transform);
                    runtimeTree.Blackboard.SetGameObject("MainCamera", Camera.main?.gameObject);
                }

                isRunning = true;

                if (debugMode)
                    Debug.Log($"[BehaviorTreeRunner] Started behavior tree: {behaviorTreeAsset.name}");
            }
        }

        public void StopTree()
        {
            if (runtimeTree == null)
                return;

            runtimeTree.Abort();
            isRunning = false;

            if (debugMode)
                Debug.Log("[BehaviorTreeRunner] Stopped behavior tree");
        }

        public void RestartTree()
        {
            StopTree();
            StartTree();
        }

        private void OnDisable()
        {
            StopTree();
        }
    }
}