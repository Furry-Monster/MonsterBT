using UnityEngine;

namespace MonsterBT.Runtime.Conditions
{
    public enum ComparisonType
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        GreaterThanOrEqual,
        GreaterThan
    }

    [CreateAssetMenu(fileName = "DistanceCondition", menuName = "MonsterBTNode/Conditions/DistanceCondition")]
    public class DistanceCondition : ActionNode
    {
        [SerializeField] private string targetKey = "Target";
        [SerializeField] private float compareDistance = 5f;
        [SerializeField] private ComparisonType comparison = ComparisonType.LessThan;

        protected override BTNodeState OnUpdate()
        {
            var ownerTransform = blackboard.GetTransform("OwnerTransform");
            var targetObject = blackboard.GetGameObject(targetKey);

            if (ownerTransform == null || targetObject == null)
                return BTNodeState.Failure;

            float currentDistance = Vector3.Distance(ownerTransform.position, targetObject.transform.position);

            bool result = comparison switch
            {
                ComparisonType.LessThan => currentDistance < compareDistance,
                ComparisonType.LessThanOrEqual => currentDistance <= compareDistance,
                ComparisonType.Equal => Mathf.Approximately(currentDistance, compareDistance),
                ComparisonType.GreaterThanOrEqual => currentDistance >= compareDistance,
                ComparisonType.GreaterThan => currentDistance > compareDistance,
                _ => false
            };

            return result ? BTNodeState.Success : BTNodeState.Failure;
        }
    }
}