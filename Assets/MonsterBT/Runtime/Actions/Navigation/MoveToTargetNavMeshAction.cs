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

            if (!navMeshAgent.enabled)
            {
                Debug.LogError("[MoveToTargetNavMeshAction] NavMeshAgent is disabled");
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
            
            // 确保 NavMeshAgent 可以更新位置和旋转
            if (!navMeshAgent.updatePosition)
            {
                Debug.LogWarning("[MoveToTargetNavMeshAction] NavMeshAgent.updatePosition is false, enabling it");
                navMeshAgent.updatePosition = true;
            }
            if (!navMeshAgent.updateRotation)
            {
                Debug.LogWarning("[MoveToTargetNavMeshAction] NavMeshAgent.updateRotation is false, enabling it");
                navMeshAgent.updateRotation = true;
            }

            pathCalculated = false;
            pathCalculationStartTime = Time.time;
            lastTargetUpdateTime = Time.time;

            // 立即设置目标位置
            var targetPosition = targetTransform.position;
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
                Debug.Log($"[MoveToTargetNavMeshAction] Started moving to target: {targetObject.name} at {hit.position}, " +
                         $"Agent position: {navMeshAgent.transform.position}, Speed: {navMeshAgent.speed}, " +
                         $"Stopping distance: {stoppingDistance}");
            }
            else
            {
                Debug.LogWarning($"[MoveToTargetNavMeshAction] Target position {targetPosition} is not on NavMesh (within 5 units)");
            }
        }

        protected override BTNodeState OnUpdate()
        {
            if (navMeshAgent == null || targetTransform == null)
            {
                Debug.LogError("[MoveToTargetNavMeshAction] NavMeshAgent or Target is null");
                return BTNodeState.Failure;
            }

            if (!navMeshAgent.enabled)
            {
                Debug.LogError("[MoveToTargetNavMeshAction] NavMeshAgent is disabled");
                return BTNodeState.Failure;
            }

            if (targetTransform.gameObject == null || !targetTransform.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[MoveToTargetNavMeshAction] Target is null or inactive");
                return BTNodeState.Failure;
            }

            // 检查路径计算状态
            if (!pathCalculated)
            {
                if (Time.time - pathCalculationStartTime > pathTimeout)
                {
                    Debug.LogWarning($"[MoveToTargetNavMeshAction] Path calculation timeout. Status: {navMeshAgent.pathStatus}, Pending: {navMeshAgent.pathPending}");
                    return BTNodeState.Failure;
                }

                if (navMeshAgent.pathPending)
                {
                    return BTNodeState.Running;
                }

                if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    Debug.LogWarning($"[MoveToTargetNavMeshAction] Invalid path to target. Target: {targetTransform.position}, Agent: {navMeshAgent.transform.position}");
                    return BTNodeState.Failure;
                }

                pathCalculated = true;
                Debug.Log($"[MoveToTargetNavMeshAction] Path calculated. Distance: {navMeshAgent.remainingDistance}, Status: {navMeshAgent.pathStatus}");
            }

            // 更新目标位置（用于动态目标）
            if (Time.time - lastTargetUpdateTime >= targetUpdateInterval)
            {
                var targetPosition = targetTransform.position;
                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(hit.position);
                }
                else
                {
                    Debug.LogWarning($"[MoveToTargetNavMeshAction] Cannot sample target position: {targetPosition}");
                }
                lastTargetUpdateTime = Time.time;
            }

            // 检查是否到达目标
            if (!navMeshAgent.pathPending)
            {
                // remainingDistance 可能是 Infinity（当路径刚计算完成时）或有效距离
                float remainingDistance = navMeshAgent.remainingDistance;
                
                // 如果距离是 Infinity，检查是否有有效路径
                if (float.IsInfinity(remainingDistance))
                {
                    // 如果路径状态是完整的，但距离是 Infinity，可能是刚计算完路径
                    // 或者目标已经在停止距离内，直接检查实际距离
                    if (navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete)
                    {
                        float actualDistance = Vector3.Distance(navMeshAgent.transform.position, targetTransform.position);
                        if (actualDistance <= stoppingDistance)
                        {
                            Debug.Log($"[MoveToTargetNavMeshAction] Reached target. Actual distance: {actualDistance}");
                            return BTNodeState.Success;
                        }
                        // 如果实际距离大于停止距离，但路径距离是 Infinity，继续等待 Agent 开始移动
                        return BTNodeState.Running;
                    }
                    // 如果路径状态不是完整的，继续等待
                    return BTNodeState.Running;
                }

                // 距离是有效值，检查是否到达
                if (remainingDistance <= stoppingDistance)
                {
                    Debug.Log($"[MoveToTargetNavMeshAction] Reached target. Remaining distance: {remainingDistance}");
                    return BTNodeState.Success;
                }

                // 检查 NavMeshAgent 是否实际在移动（只在有有效距离时检查）
                if (navMeshAgent.velocity.magnitude < 0.01f && remainingDistance > stoppingDistance)
                {
                    // 只有在路径计算完成一段时间后仍未移动才警告
                    if (Time.time - pathCalculationStartTime > 0.5f)
                    {
                        Debug.LogWarning($"[MoveToTargetNavMeshAction] Agent not moving. Velocity: {navMeshAgent.velocity.magnitude}, " +
                                       $"Distance: {remainingDistance}, Status: {navMeshAgent.pathStatus}, " +
                                       $"HasPath: {navMeshAgent.hasPath}");
                    }
                }
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
