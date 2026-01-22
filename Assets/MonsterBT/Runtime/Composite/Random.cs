using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonsterBT.Runtime.Composite
{
    /// <summary>
    /// 支持权重配置，可以控制每个子节点被选中的概率
    /// </summary>
    [CreateAssetMenu(fileName = "Random", menuName = "MonsterBTNode/Composite/Random")]
    public class Random : CompositeNode
    {
        [SerializeField] [Tooltip("是否使用权重选择")] private bool isAverage;

        [SerializeField] [Tooltip("子节点权重列表（仅在useWeights为true时生效）")]
        private List<float> weights = new List<float>();

        [SerializeField] [Tooltip("是否每次重新选择（false表示选中后执行完毕才重新选择）")]
        private bool reselectOnComplete = true;

        private BTNode selectedChild;
        private int selectedIndex;

        protected override void OnStart()
        {
            selectedChild = null;
            selectedIndex = -1;
        }

        protected override BTNodeState OnUpdate()
        {
            if (Children == null || Children.Count == 0)
                return BTNodeState.Failure;

            // 如果没有选中的子节点,则进行选择
            if (selectedChild == null)
            {
                SelectRandomChild();
            }

            if (selectedChild == null)
                return BTNodeState.Failure;

            var childState = selectedChild.Update();
            if (childState != BTNodeState.Running && reselectOnComplete)
            {
                // 如果需要重新选择，直接设置为null，下一次会自动选取
                selectedChild = null;
            }

            return childState;
        }

        protected override void OnStop()
        {
            selectedChild?.Abort();
            selectedChild = null;
            selectedIndex = -1;
        }

        private void SelectRandomChild()
        {
            if (Children == null || Children.Count == 0)
                return;

            // 过滤掉null的子节点
            var validIndices = new List<int>();
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] == null)
                    continue;

                validIndices.Add(i);
            }

            if (validIndices.Count == 0)
                return;

            // 获得随机索引
            if (!isAverage && weights.Count >= validIndices.Count)
                // 按权重选择
                selectedIndex = SelectWeightedRandom(validIndices);
            else
                // 如果权重不足或者设置平均选取，则简单随机选择
                selectedIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];

            selectedChild = Children[selectedIndex];

            // 重置选中子节点的状态（如果之前执行过）
            if (selectedChild.State != BTNodeState.Running)
            {
                selectedChild.Abort();
            }
        }

        private int SelectWeightedRandom(List<int> validIndices)
        {
            float totalWeight = 0f;

            // 计算有效子节点的总权重
            foreach (int index in validIndices)
            {
                float weight = index < weights.Count ? weights[index] : 1f;
                totalWeight += Mathf.Max(weight, 0f); // 确保权重非负
            }
            if (totalWeight <= 0f)
            {
                // 如果总权重为0，回退到简单随机选择
                return validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            }

            // 利用随机值随机选取
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (int index in validIndices)
            {
                float weight = index < weights.Count ? weights[index] : 1f;
                weight = Mathf.Max(weight, 0f);

                currentWeight += weight;
                if (randomValue <= currentWeight)
                {
                    return index;
                }
            }

            // 备选方案,选择最后一个（理论上不应该到达这里）
            return validIndices[^1];
        }

        public override BTNode Clone()
        {
            var cloned = Instantiate(this);
            if (cloned == null || Children == null)
                return cloned;

            cloned.Children.Clear();
            foreach (var child in Children.Where(child => child != null))
            {
                cloned.Children.Add(child.Clone());
            }

            // 克隆的同时,复制权重列表
            cloned.weights = new List<float>(weights);
            return cloned;
        }

    }
}