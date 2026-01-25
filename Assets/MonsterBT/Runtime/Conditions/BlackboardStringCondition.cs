using UnityEngine;

namespace MonsterBT.Runtime.Conditions
{
    public enum StringComparison
    {
        Equals,
        Contains,
        StartsWith,
        EndsWith,
        IsEmpty,
        IsNotEmpty
    }

    [CreateAssetMenu(fileName = "BlackboardStringCondition",
        menuName = "MonsterBTNode/Conditions/BlackboardStringCondition")]
    public class BlackboardStringCondition : ActionNode
    {
        [SerializeField] private string keyName = "stringKey";
        [SerializeField] [Tooltip("字符串规则")] private StringComparison comparisonType = StringComparison.Equals;
        [SerializeField] private string expectedValue = "";
        [SerializeField] [Tooltip("是否大小写敏感")] private bool caseSensitive = true;

        protected override BTNodeState OnUpdate()
        {
            if (!blackboard.HasKey(keyName))
                return BTNodeState.Failure;

            var currentValue = blackboard.GetString(keyName);

            var result = comparisonType switch
            {
                StringComparison.Equals => caseSensitive
                    ? currentValue == expectedValue
                    : string.Equals(currentValue, expectedValue, System.StringComparison.OrdinalIgnoreCase),
                StringComparison.Contains => caseSensitive
                    ? currentValue.Contains(expectedValue)
                    : currentValue.ToLower().Contains(expectedValue.ToLower()),
                StringComparison.StartsWith => caseSensitive
                    ? currentValue.StartsWith(expectedValue)
                    : currentValue.ToLower().StartsWith(expectedValue.ToLower()),
                StringComparison.EndsWith => caseSensitive
                    ? currentValue.EndsWith(expectedValue)
                    : currentValue.ToLower().EndsWith(expectedValue.ToLower()),
                StringComparison.IsEmpty => string.IsNullOrEmpty(currentValue),
                StringComparison.IsNotEmpty => !string.IsNullOrEmpty(currentValue),
                _ => false
            };

            return result ? BTNodeState.Success : BTNodeState.Failure;
        }
    }
}