using System;
using UnityEngine;

namespace MonsterBT.Runtime.Actions.Animation
{
    /// <summary>
    /// 设置 Animator 参数节点：设置 Animator 控制器的参数值
    /// </summary>
    [CreateAssetMenu(fileName = "SetAnimatorParameterAction",
        menuName = "MonsterBTNode/Actions/Animation/SetAnimatorParameterAction")]
    public class SetAnimatorParameterAction : ActionNode
    {
        [SerializeField] [Tooltip("参数名称")] private string parameterName = "Speed";
        [SerializeField] [Tooltip("参数类型")] private ParameterType parameterType = ParameterType.Float;

        [Header("参数值")] [SerializeField] private bool boolValue = true;
        [SerializeField] private int intValue = 0;
        [SerializeField] private float floatValue = 0f;

        private Animator animator;
        private int parameterHash;

        private enum ParameterType
        {
            Bool,
            Int,
            Float,
            Trigger
        }

        protected override void OnStart()
        {
            var owner = blackboard.GetGameObject("Owner");
            if (owner == null)
            {
                Debug.LogError("[SetAnimatorParameterAction] Owner GameObject not found in blackboard");
                return;
            }

            animator = owner.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("[SetAnimatorParameterAction] Animator component not found on Owner");
                return;
            }

            parameterHash = Animator.StringToHash(parameterName);
        }

        protected override BTNodeState OnUpdate()
        {
            if (animator == null)
                return BTNodeState.Failure;

            switch (parameterType)
            {
                case ParameterType.Bool:
                    animator.SetBool(parameterHash, boolValue);
                    break;
                case ParameterType.Int:
                    animator.SetInteger(parameterHash, intValue);
                    break;
                case ParameterType.Float:
                    animator.SetFloat(parameterHash, floatValue);
                    break;
                case ParameterType.Trigger:
                    animator.SetTrigger(parameterHash);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return BTNodeState.Success;
        }
    }
}