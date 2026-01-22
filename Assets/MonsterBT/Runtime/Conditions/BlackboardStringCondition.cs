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
        [SerializeField][Tooltip("字符串规则")] private StringComparison comparisonType = StringComparison.Equals;
        [SerializeField] private string expectedValue = "";
        [SerializeField][Tooltip("是否大小写敏感")] private bool caseSensitive = true;

        protected override BTNodeState OnUpdate()
        {
            if (!blackboard.HasKey(keyName))
                return BTNodeState.Failure;

            string currentValue = blackboard.GetString(keyName);
            bool result = false;

            switch (comparisonType)
            {
                case StringComparison.Equals:
                    result = caseSensitive
                        ? currentValue == expectedValue
                        : string.Equals(currentValue, expectedValue, System.StringComparison.OrdinalIgnoreCase);
                    break;

                case StringComparison.Contains:
                    result = caseSensitive
                        ? currentValue.Contains(expectedValue)
                        : currentValue.ToLower().Contains(expectedValue.ToLower());
                    break;

                case StringComparison.StartsWith:
                    result = caseSensitive
                        ? currentValue.StartsWith(expectedValue)
                        : currentValue.ToLower().StartsWith(expectedValue.ToLower());
                    break;

                case StringComparison.EndsWith:
                    result = caseSensitive
                        ? currentValue.EndsWith(expectedValue)
                        : currentValue.ToLower().EndsWith(expectedValue.ToLower());
                    break;

                case StringComparison.IsEmpty:
                    result = string.IsNullOrEmpty(currentValue);
                    break;

                case StringComparison.IsNotEmpty:
                    result = !string.IsNullOrEmpty(currentValue);
                    break;
            }

            return result ? BTNodeState.Success : BTNodeState.Failure;
        }
    }
}