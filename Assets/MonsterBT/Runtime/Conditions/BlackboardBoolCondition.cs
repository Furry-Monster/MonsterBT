using UnityEngine;

namespace MonsterBT.Runtime.Conditions
{
    [CreateAssetMenu(fileName = "BlackboardBoolCondition",
        menuName = "MonsterBTNode/Conditions/BlackboardBoolCondition")]
    public class BlackboardBoolCondition : ActionNode
    {
        [SerializeField] private string keyName = "boolKey";
        [SerializeField][Tooltip("若为此值,则节点返回执行成功,否则失败")] private bool expectedValue = true;

        protected override BTNodeState OnUpdate()
        {
            if (!blackboard.HasKey(keyName))
                return BTNodeState.Failure;

            bool currentValue = blackboard.GetBool(keyName);
            bool result = currentValue == expectedValue;

            return result ? BTNodeState.Success : BTNodeState.Failure;
        }
    }
}