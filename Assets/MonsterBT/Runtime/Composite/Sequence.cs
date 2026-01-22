using UnityEngine;

namespace MonsterBT.Runtime.Composite
{
    /// <summary>
    /// 顺序执行所有节点，任何一个执行失败都算作失败
    /// </summary>
    [CreateAssetMenu(fileName = "Sequence", menuName = "MonsterBTNode/Composite/Sequence")]
    public class Sequence : CompositeNode
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
                    case BTNodeState.Failure:
                        return BTNodeState.Failure;
                    case BTNodeState.Success:
                        currentChildIndex++;
                        break;
                }
            }

            return BTNodeState.Success;
        }

        protected override void OnStop()
        {
            base.OnStop();
            currentChildIndex = 0;
        }
    }
}