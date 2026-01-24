using UnityEngine;

namespace MonsterBT.Runtime.Decorator
{
    /// <summary>
    /// Until装饰器
    /// 持续执行子节点直到达到指定状态
    /// </summary>
    [CreateAssetMenu(fileName = "Until", menuName = "MonsterBTNode/Decorator/Until")]
    public class Until : DecoratorNode
    {
        [SerializeField] [Tooltip("重复执行直到子节点返回此状态")]
        private BTNodeState endState = BTNodeState.Success;

        protected override BTNodeState OnUpdate()
        {
            if (Child == null)
                return BTNodeState.Failure;

            var childState = Child.Update();

            if (childState == endState)
            {
                return BTNodeState.Success;
            }

            // 如果子节点已经完成但未达到目标状态，重新执行
            if (childState is BTNodeState.Success or BTNodeState.Failure)
            {
                Child.Abort();
            }

            return BTNodeState.Running;
        }

        protected override void OnStop()
        {
            Child?.Abort();
        }
    }
}