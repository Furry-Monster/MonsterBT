using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterBT.Runtime.Composite
{
    /// <summary>
    /// 并行节点完成模式
    /// </summary>
    public enum ParallelFinishMode
    {
        /// <summary>
        /// 立即模式：任意子节点完成时立即结束
        /// </summary>
        Immediate,

        /// <summary>
        /// 延迟模式：等待所有子节点都完成
        /// </summary>
        Delayed,
    }

    /// <summary>
    /// 参考虚幻引擎UE5中"简单并行"的实现
    /// 这里限于Composition的设计，没有使用主任务的概念
    /// </summary>
    [CreateAssetMenu(fileName = "Parallel", menuName = "MonsterBTNode/Composite/Parallel")]
    public class Parallel : CompositeNode
    {
        [SerializeField]
        [Tooltip("完成模式：立即完成或等待所有子节点")]
        private ParallelFinishMode finishMode = ParallelFinishMode.Immediate;

        [SerializeField]
        [Tooltip("成功需要的子节点数量（0表示至少一个）")]
        private int successCount = 1;

        [SerializeField]
        [Tooltip("失败需要的子节点数量（0表示至少一个）")]
        private int failureCount = 1;

        /// 子节点状态表
        private readonly Dictionary<BTNode, BTNodeState> childStates = new Dictionary<BTNode, BTNodeState>();

        protected override void OnStart()
        {
            if (Children == null)
                return;

            childStates.Clear();

            foreach (var child in Children)
            {
                if (child != null)
                {
                    childStates[child] = BTNodeState.Running;
                }
            }
        }

        protected override BTNodeState OnUpdate()
        {
            if (Children == null || Children.Count == 0)
                return BTNodeState.Failure;

            int successNodes = 0;
            int failureNodes = 0;
            int runningNodes = 0;

            // 更新所有还在运行的子节点
            foreach (var child in Children)
            {
                if (child == null)
                    continue;

                var currentState = childStates.GetValueOrDefault(child, BTNodeState.Running);

                if (currentState == BTNodeState.Running)
                {
                    currentState = child.Update();
                    childStates[child] = currentState;
                }

                // 统计各状态的节点数量
                switch (currentState)
                {
                    case BTNodeState.Success:
                        successNodes++;
                        break;
                    case BTNodeState.Failure:
                        failureNodes++;
                        break;
                    case BTNodeState.Running:
                        runningNodes++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // 检查完成条件
            return CheckCompletion(successNodes, failureNodes, runningNodes);
        }

        private BTNodeState CheckCompletion(int successNodes, int failureNodes, int runningNodes)
        {
            int actualSuccessThreshold = successCount > 0 ? successCount : 1;
            int actualFailureThreshold = failureCount > 0 ? failureCount : 1;

            switch (finishMode)
            {
                case ParallelFinishMode.Immediate:
                    if (successNodes >= actualSuccessThreshold)
                    {
                        AbortRemainingChildren();
                        return BTNodeState.Success;
                    }
                    if (failureNodes >= actualFailureThreshold)
                    {
                        AbortRemainingChildren();
                        return BTNodeState.Failure;
                    }
                    return BTNodeState.Running;

                case ParallelFinishMode.Delayed:
                    if (runningNodes > 0)
                    {
                        return BTNodeState.Running;
                    }
                    // 所有节点都完成了，判断最终结果
                    if (successNodes >= actualSuccessThreshold)
                    {
                        return BTNodeState.Success;
                    }
                    return BTNodeState.Failure;

                default:
                    return BTNodeState.Failure;
            }
        }

        protected override void OnStop()
        {
            AbortRemainingChildren();
            childStates.Clear();
        }

        private void AbortRemainingChildren()
        {
            if (Children == null)
                return;

            foreach (var child in Children)
            {
                if (child != null && childStates.ContainsKey(child) && childStates[child] == BTNodeState.Running)
                {
                    child.Abort();
                    childStates[child] = BTNodeState.Failure;
                }
            }
        }

    }
}