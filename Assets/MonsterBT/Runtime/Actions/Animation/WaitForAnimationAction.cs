using UnityEngine;

namespace MonsterBT.Runtime.Actions.Animation
{
    /// <summary>
    /// 等待动画完成节点：等待指定的动画状态播放完成
    /// </summary>
    [CreateAssetMenu(fileName = "WaitForAnimationAction",
        menuName = "MonsterBTNode/Actions/Animation/WaitForAnimationAction")]
    public class WaitForAnimationAction : ActionNode
    {
        [SerializeField] [Tooltip("要等待的动画状态名称（为空则等待当前动画）")]
        private string animationStateName = "";

        [SerializeField] [Tooltip("动画层索引")] private int layerIndex = 0;

        [SerializeField] [Tooltip("等待时间（秒），如果动画未完成则超时返回成功")]
        private float timeout = 10f;

        private Animator animator;
        private int animationStateHash;
        private float startTime;
        private bool hasSpecificAnimation;

        protected override void OnStart()
        {
            var owner = blackboard.GetGameObject("Owner");
            if (owner == null)
            {
                Debug.LogError("[WaitForAnimationAction] Owner GameObject not found in blackboard");
                return;
            }

            animator = owner.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("[WaitForAnimationAction] Animator component not found on Owner");
                return;
            }

            hasSpecificAnimation = !string.IsNullOrEmpty(animationStateName);
            if (hasSpecificAnimation)
            {
                animationStateHash = Animator.StringToHash(animationStateName);
                if (!animator.HasState(layerIndex, animationStateHash))
                {
                    Debug.LogWarning(
                        $"[WaitForAnimationAction] Animation state '{animationStateName}' not found in Animator");
                }
            }

            startTime = Time.time;
        }

        protected override BTNodeState OnUpdate()
        {
            if (animator == null)
                return BTNodeState.Failure;

            if (Time.time - startTime >= timeout)
            {
                Debug.LogWarning($"[WaitForAnimationAction] Timeout waiting for animation '{animationStateName}'");
                return BTNodeState.Success;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);

            if (hasSpecificAnimation)
            {
                if (stateInfo.shortNameHash == animationStateHash && stateInfo.normalizedTime >= 1.0f)
                {
                    return BTNodeState.Success;
                }
            }
            else
            {
                if (stateInfo.normalizedTime >= 1.0f)
                {
                    return BTNodeState.Success;
                }
            }

            return BTNodeState.Running;
        }
    }
}