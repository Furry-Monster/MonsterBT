using UnityEngine;

namespace MonsterBT.Runtime.Decorator
{
    /// <summary>
    /// ForceReturn装饰器 
    /// 无论子节点实际返回什么状态，都会被替换为指定状态
    /// </summary>
    [CreateAssetMenu(fileName = "ForceReturn", menuName = "MonsterBTNode/Decorator/ForceReturn")]
    public class ForceReturn : DecoratorNode
    {
        [SerializeField] [Tooltip("强制返回的状态")] private BTNodeState forceState = BTNodeState.Success;

        protected override BTNodeState OnUpdate()
        {
            if (Child == null)
                return BTNodeState.Failure;

            var childState = Child.Update();

            if (childState == BTNodeState.Running)
            {
                return BTNodeState.Running;
            }

            return forceState;
        }

        protected override void OnStop()
        {
            Child?.Abort();
        }
    }
}