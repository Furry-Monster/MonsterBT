using UnityEngine;
using UnityEngine.AI;

namespace MonsterBT.Runtime.Actions.Navigation
{
    /// <summary>
    /// 使用 NavMesh 移动到指定位置节点
    /// </summary>
    [CreateAssetMenu(fileName = "MoveToPositionNavMeshAction",
        menuName = "MonsterBTNode/Actions/Navigation/MoveToPositionNavMeshAction")]
    public class MoveToPositionNavMeshAction : ActionNode
    {
        [SerializeField] [Tooltip("目标位置（Vector3）的黑板键名")]
        private string targetPositionKey = "TargetPosition";

        [SerializeField] [Tooltip("移动速度（如果为0则使用 NavMeshAgent 的默认速度）")]
        private float speed = 0f;

        [SerializeField] [Tooltip("停止距离")] private float stoppingDistance = 0.5f;

        [SerializeField] [Tooltip("路径计算超时时间（秒）")]
        private float pathTimeout = 1f;

        private NavMeshAgent navMeshAgent;
        private Vector3 targetPosition;
        private float originalSpeed;
        private float pathCalculationStartTime;
        private bool pathCalculated;

        protected override void OnStart()
        {
            var owner = blackboard.GetGameObject("Owner");
            if (owner == null)
            {
                Debug.LogError("[MoveToPositionNavMeshAction] Owner GameObject not found in blackboard");
                return;
            }

            navMeshAgent = owner.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogError("[MoveToPositionNavMeshAction] NavMeshAgent component not found on Owner");
                return;
            }

            targetPosition = blackboard.GetVector3(targetPositionKey);
            if (targetPosition == Vector3.zero && !blackboard.HasKey(targetPositionKey))
            {
                Debug.LogError(
                    $"[MoveToPositionNavMeshAction] Target position key '{targetPositionKey}' not found in blackboard");
                return;
            }

            if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[MoveToPositionNavMeshAction] Target position {targetPosition} is not on NavMesh");
                return;
            }

            targetPosition = hit.position;

            originalSpeed = navMeshAgent.speed;
            if (speed > 0f)
            {
                navMeshAgent.speed = speed;
            }

            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.SetDestination(targetPosition);
            pathCalculated = false;
            pathCalculationStartTime = Time.time;
        }

        protected override BTNodeState OnUpdate()
        {
            if (navMeshAgent == null)
                return BTNodeState.Failure;

            if (!pathCalculated)
            {
                if (Time.time - pathCalculationStartTime > pathTimeout)
                {
                    Debug.LogWarning("[MoveToPositionNavMeshAction] Path calculation timeout");
                    return BTNodeState.Failure;
                }

                if (navMeshAgent.pathPending)
                    return BTNodeState.Running;

                if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    Debug.LogWarning("[MoveToPositionNavMeshAction] Invalid path to target");
                    return BTNodeState.Failure;
                }

                pathCalculated = true;
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