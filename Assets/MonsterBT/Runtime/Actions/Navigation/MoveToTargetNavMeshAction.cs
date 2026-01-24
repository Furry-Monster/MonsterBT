using UnityEngine;
using UnityEngine.AI;

namespace MonsterBT.Runtime.Actions.Navigation
{
    /// <summary>
    /// 使用 NavMesh 移动到目标节点
    /// </summary>
    [CreateAssetMenu(fileName = "MoveToTargetNavMeshAction", menuName = "MonsterBTNode/Actions/Navigation/MoveToTargetNavMeshAction")]
    public class MoveToTargetNavMeshAction : ActionNode
    {
        [SerializeField][Tooltip("目标 GameObject 的黑板键名")] private string targetKey = "Target";
        [SerializeField][Tooltip("移动速度（如果为0则使用 NavMeshAgent 的默认速度）")] private float speed = 0f;
        [SerializeField][Tooltip("停止距离")] private float stoppingDistance = 0.5f;
        [SerializeField][Tooltip("路径计算超时时间（秒）")] private float pathTimeout = 1f;
        [SerializeField][Tooltip("目标更新间隔（秒），用于动态目标")] private float targetUpdateInterval = 0.5f;

        private NavMeshAgent navMeshAgent;
        private Transform targetTransform;
        private float originalSpeed;
        private float pathCalculationStartTime;
        private float lastTargetUpdateTime;
        private bool pathCalculated;

        protected override void OnStart()
        {
            var owner = blackboard.GetGameObject("Owner");
            if (owner == null)
            {
                Debug.LogError("[MoveToTargetNavMeshAction] Owner GameObject not found in blackboard");
                return;
            }

            navMeshAgent = owner.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogError("[MoveToTargetNavMeshAction] NavMeshAgent component not found on Owner");
                return;
            }

            var targetObject = blackboard.GetGameObject(targetKey);
            if (targetObject == null)
            {
                Debug.LogError($"[MoveToTargetNavMeshAction] Target GameObject key '{targetKey}' not found in blackboard");
                return;
            }

            targetTransform = targetObject.transform;

            originalSpeed = navMeshAgent.speed;
            if (speed > 0f)
            {
                navMeshAgent.speed = speed;
            }

            navMeshAgent.stoppingDistance = stoppingDistance;
            pathCalculated = false;
            pathCalculationStartTime = Time.time;
            lastTargetUpdateTime = Time.time;
        }

        protected override BTNodeState OnUpdate()
        {
            if (navMeshAgent == null || targetTransform == null)
                return BTNodeState.Failure;

            if (targetTransform.gameObject == null || !targetTransform.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[MoveToTargetNavMeshAction] Target is null or inactive");
                return BTNodeState.Failure;
            }

            if (!pathCalculated)
            {
                if (Time.time - pathCalculationStartTime > pathTimeout)
                {
                    Debug.LogWarning("[MoveToTargetNavMeshAction] Path calculation timeout");
                    return BTNodeState.Failure;
                }

                if (navMeshAgent.pathPending)
                    return BTNodeState.Running;

                if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    Debug.LogWarning("[MoveToTargetNavMeshAction] Invalid path to target");
                    return BTNodeState.Failure;
                }

                pathCalculated = true;
            }

            if (Time.time - lastTargetUpdateTime >= targetUpdateInterval)
            {
                var targetPosition = targetTransform.position;
                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(hit.position);
                }
                lastTargetUpdateTime = Time.time;
            }

            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= stoppingDistance)
            {
                return BTNodeState.Success;
            }

            return BTNodeState.Running;
        }

        protected override void OnStop()
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.ResetPath();
                if (speed > 0f)
                {
                    navMeshAgent.speed = originalSpeed;
                }
            }
        }
    }
}
