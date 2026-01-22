using UnityEngine;

namespace MonsterBT.Runtime.Decorator
{
    /// <summary>
    /// 重复装饰器
    /// 可以设置重复次数，-1表示无限重复
    /// </summary>
    [CreateAssetMenu(fileName = "Repeater", menuName = "MonsterBTNode/Decorator/Repeater")]
    public class Repeater : DecoratorNode
    {
        [SerializeField]
        [Tooltip("-1 表示无限重复")]
        private int repeatTimes = 1;

        [SerializeField]
        [Tooltip("子节点失败时是否重置计数")]
        private bool resetOnFailure = true;

        private int repeatCount;

        protected override void OnStart()
        {
            repeatCount = 0;
        }

        protected override BTNodeState OnUpdate()
        {
            if (Child == null)
                return BTNodeState.Failure;

            var childState = Child.Update();

            switch (childState)
            {
                case BTNodeState.Running:
                    return BTNodeState.Running;

                case BTNodeState.Success:
                    repeatCount++;
                    if (repeatTimes > 0 && repeatCount >= repeatTimes)
                    {
                        return BTNodeState.Success;
                    }
                    Child.Abort();
                    return BTNodeState.Running;

                case BTNodeState.Failure:
                    if (resetOnFailure)
                    {
                        // 重置计数并继续重复
                        repeatCount = 0;
                        Child.Abort();
                        return BTNodeState.Running;
                    }
                    return BTNodeState.Failure;

                default:
                    return BTNodeState.Failure;
            }
        }

        protected override void OnStop()
        {
            Child?.Abort();
        }
    }
}