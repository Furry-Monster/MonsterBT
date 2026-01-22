using UnityEngine;

namespace MonsterBT.Runtime.Decorator
{
    /// <summary>
    /// 求逆装饰器
    /// </summary>
    [CreateAssetMenu(fileName = "Inverter", menuName = "MonsterBTNode/Decorator/Inverter")]
    public class Inverter : DecoratorNode
    {
        protected override BTNodeState OnUpdate()
        {
            if (child == null)
                return BTNodeState.Failure;

            var childState = child.Update();

            switch (childState)
            {
                case BTNodeState.Running:
                    return BTNodeState.Running;
                case BTNodeState.Success:
                    return BTNodeState.Failure;
                case BTNodeState.Failure:
                    return BTNodeState.Success;
                default:
                    return BTNodeState.Failure;
            }
        }
    }
}