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
            {
                if (IsDebugMode())
                {
                    Debug.LogWarning($"[BT] Selector has no children: {GetNodeName()}");
                }
                return BTNodeState.Failure;
            }

            while (currentChildIndex < children.Count)
            {
                var child = children[currentChildIndex];
                var childState = child.Update();

                if (IsDebugMode())
                {
                    var childName = string.IsNullOrEmpty(child.name) ? child.GetType().Name : child.name;
                    Debug.Log($"[BT] Selector [{GetNodeName()}] Child [{currentChildIndex}]: {childName} -> {childState}");
                }

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