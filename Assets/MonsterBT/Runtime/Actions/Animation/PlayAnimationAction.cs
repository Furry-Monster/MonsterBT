using UnityEngine;

namespace MonsterBT.Runtime.Actions.Animation
{
    /// <summary>
    /// 播放动画节点：播放指定的动画状态
    /// </summary>
    [CreateAssetMenu(fileName = "PlayAnimationAction", menuName = "MonsterBTNode/Actions/Animation/PlayAnimationAction")]
    public class PlayAnimationAction : ActionNode
    {
        [SerializeField][Tooltip("动画状态名称")] private string animationStateName = "Idle";
        [SerializeField][Tooltip("动画层索引")] private int layerIndex = 0;
        [SerializeField][Tooltip("是否等待动画完成")] private bool waitForCompletion = false;
        [SerializeField][Tooltip("动画过渡时间")] private float transitionDuration = 0.25f;

        private Animator animator;
        private bool animationStarted;
        private float animationStartTime;
        private int animationStateHash;

        protected override void OnStart()
        {
            var owner = blackboard.GetGameObject("Owner");
            if (owner == null)
            {
                Debug.LogError("[PlayAnimationAction] Owner GameObject not found in blackboard");
                return;
            }

            animator = owner.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("[PlayAnimationAction] Animator component not found on Owner");
                return;
            }

            animationStateHash = Animator.StringToHash(animationStateName);
            animationStarted = false;
        }

        protected override BTNodeState OnUpdate()
        {
            if (animator == null)
                return BTNodeState.Failure;

            if (!animationStarted)
            {
                if (!animator.HasState(layerIndex, animationStateHash))
                {
                    Debug.LogWarning($"[PlayAnimationAction] Animation state '{animationStateName}' not found in Animator");
                    return BTNodeState.Failure;
                }

                animator.CrossFade(animationStateHash, transitionDuration, layerIndex);
                animationStarted = true;
                animationStartTime = Time.time;

                if (!waitForCompletion)
                {
                    return BTNodeState.Success;
                }
            }

            if (waitForCompletion)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                if (stateInfo.shortNameHash == animationStateHash && stateInfo.normalizedTime >= 1.0f)
                {
                    return BTNodeState.Success;
                }

                return BTNodeState.Running;
            }

            return BTNodeState.Success;
        }

        protected override void OnStop()
        {
            animationStarted = false;
        }
    }
}
