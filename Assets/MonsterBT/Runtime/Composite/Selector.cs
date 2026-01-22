using UnityEngine;

namespace MonsterBT.Runtime.Composite
{
    /// <summary>
    /// 按顺序执行子节点，直到其中一个成功
    /// </summary>
    [CreateAssetMenu(fileName = "Selector", menuName = "MonsterBTNode/Composite/Selector")]
    public class Selector : CompositeNode
    {
        private int currentChildIndex;

        protected override void OnStart()
        {
            currentChildIndex = 0;
        }

        protected override BTNodeState OnUpdate()
        {
            if (children == null || children.Count == 0)
                return BTNodeState.Failure;

            while (currentChildIndex < children.Count)
            {
                var child = children[currentChildIndex];
                var childState = child.Update();

                switch (childState)
                {
                    case BTNodeState.Running:
                        return BTNodeState.Running;
                    case BTNodeState.Success:
                        return BTNodeState.Success;
                    case BTNodeState.Failure:
                        currentChildIndex++;
                        break;
                }
            }

            return BTNodeState.Failure;
        }

        protected override void OnStop()
        {
            base.OnStop();
            currentChildIndex = 0;
        }
    }
}