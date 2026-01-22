using UnityEngine;

namespace MonsterBT.Runtime.Actions
{
    [CreateAssetMenu(fileName = "MoveToTargetAction", menuName = "MonsterBTNode/Actions/MoveToTargetAction")]
    public class MoveToTargetAction : ActionNode
    {
        [SerializeField][Tooltip("移动的目标点")] private string targetKey = "Target";
        [SerializeField][Tooltip("移动速度")] private float speed = 5f;
        [SerializeField][Tooltip("停止判定距离")] private float stoppingDistance = 0.1f;

        private Transform ownerTransform;
        private Transform targetTransform;

        protected override void OnStart()
        {
            ownerTransform = blackboard.GetTransform("OwnerTransform");
            var targetObject = blackboard.GetGameObject(targetKey);
            targetTransform = targetObject?.transform;
        }

        protected override BTNodeState OnUpdate()
        {
            if (ownerTransform == null || targetTransform == null)
                return BTNodeState.Failure;

            Vector3 direction = (targetTransform.position - ownerTransform.position).normalized;
            float distance = Vector3.Distance(ownerTransform.position, targetTransform.position);

            if (distance <= stoppingDistance)
                return BTNodeState.Success;

            ownerTransform.position += direction * (speed * Time.deltaTime);

            return BTNodeState.Running;
        }
    }
}