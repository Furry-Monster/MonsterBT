using UnityEngine;
using UnityEngine.AI;

namespace MonsterBT.Runtime.Actions.Navigation
{
    /// <summary>
    /// 设置 NavMesh 目标节点：设置 NavMeshAgent 的目标位置，但不等待到达
    /// </summary>
    [CreateAssetMenu(fileName = "SetNavMeshDestinationAction",
        menuName = "MonsterBTNode/Actions/Navigation/SetNavMeshDestinationAction")]
    public class SetNavMeshDestinationAction : ActionNode
    {
        [SerializeField] [Tooltip("目标位置（Vector3）的黑板键名，如果为空则使用目标 GameObject 的位置")]
        private string targetPositionKey = "";

        [SerializeField] [Tooltip("目标 GameObject 的黑板键名，如果设置了则优先使用")]
        private string targetKey = "Target";

        [SerializeField] [Tooltip("是否使用目标 GameObject")]
        private bool useTargetGameObject = false;

        private NavMeshAgent navMeshAgent;

        protected override void OnStart()
        {
            var owner = blackboard.GetGameObject("Owner");
            if (owner == null)
            {
                Debug.LogError("[SetNavMeshDestinationAction] Owner GameObject not found in blackboard");
                return;
            }

            navMeshAgent = owner.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogError("[SetNavMeshDestinationAction] NavMeshAgent component not found on Owner");
                return;
            }
        }

        protected override BTNodeState OnUpdate()
        {
            if (navMeshAgent == null)
                return BTNodeState.Failure;

            Vector3 destination = Vector3.zero;
            bool hasDestination = false;

            if (useTargetGameObject && !string.IsNullOrEmpty(targetKey))
            {
                var targetObject = blackboard.GetGameObject(targetKey);
                if (targetObject != null)
                {
                    destination = targetObject.transform.position;
                    hasDestination = true;
                }
            }
            else if (!string.IsNullOrEmpty(targetPositionKey))
            {
                destination = blackboard.GetVector3(targetPositionKey);
                if (blackboard.HasKey(targetPositionKey))
                {
                    hasDestination = true;
                }
            }

            if (!hasDestination)
            {
                Debug.LogError("[SetNavMeshDestinationAction] No valid destination found");
                return BTNodeState.Failure;
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
                return BTNodeState.Success;
            }
            else
            {
                Debug.LogWarning($"[SetNavMeshDestinationAction] Destination {destination} is not on NavMesh");
                return BTNodeState.Failure;
            }
        }
    }
}