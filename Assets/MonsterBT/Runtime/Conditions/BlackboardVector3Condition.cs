using UnityEngine;

namespace MonsterBT.Runtime.Conditions
{
    public enum Vector3Comparison
    {
        DistanceTo,
        MagnitudeEqual,
        MagnitudeGreater,
        MagnitudeLess,
        XEqual,
        YEqual,
        ZEqual,
        IsZero,
        IsNotZero
    }

    [CreateAssetMenu(fileName = "BlackboardVector3Condition",
        menuName = "MonsterBTNode/Conditions/BlackboardVector3Condition")]
    public class BlackboardVector3Condition : ActionNode
    {
        [SerializeField] private string keyName = "vector3Key";
        [SerializeField] private Vector3Comparison comparisonType = Vector3Comparison.DistanceTo;
        
        [Header("比较值")]
        [SerializeField] private Vector3 expectedVector = Vector3.zero;
        [SerializeField] private float expectedFloat = 0f;
        [SerializeField][Tooltip("浮点数容差")] private float tolerance = 0.1f;

        protected override BTNodeState OnUpdate()
        {
            if (!blackboard.HasKey(keyName))
                return BTNodeState.Failure;

            Vector3 currentValue = blackboard.GetVector3(keyName);
            bool result = false;

            switch (comparisonType)
            {
                case Vector3Comparison.DistanceTo:
                    float distance = Vector3.Distance(currentValue, expectedVector);
                    result = distance <= expectedFloat;
                    break;

                case Vector3Comparison.MagnitudeEqual:
                    result = Mathf.Abs(currentValue.magnitude - expectedFloat) <= tolerance;
                    break;

                case Vector3Comparison.MagnitudeGreater:
                    result = currentValue.magnitude > expectedFloat;
                    break;

                case Vector3Comparison.MagnitudeLess:
                    result = currentValue.magnitude < expectedFloat;
                    break;

                case Vector3Comparison.XEqual:
                    result = Mathf.Abs(currentValue.x - expectedFloat) <= tolerance;
                    break;

                case Vector3Comparison.YEqual:
                    result = Mathf.Abs(currentValue.y - expectedFloat) <= tolerance;
                    break;

                case Vector3Comparison.ZEqual:
                    result = Mathf.Abs(currentValue.z - expectedFloat) <= tolerance;
                    break;

                case Vector3Comparison.IsZero:
                    result = currentValue.magnitude <= tolerance;
                    break;

                case Vector3Comparison.IsNotZero:
                    result = currentValue.magnitude > tolerance;
                    break;
            }

            return result ? BTNodeState.Success : BTNodeState.Failure;
        }
    }
}