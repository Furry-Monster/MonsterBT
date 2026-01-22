using UnityEngine;

namespace MonsterBT.Runtime
{
    /// <summary>
    /// 装饰节点基类
    /// </summary>
    public abstract class DecoratorNode : BTNode
    {
        [SerializeField] protected BTNode child;

        public BTNode Child
        {
            get => child;
            set => child = value;
        }

        public override void Initialize(Blackboard blackboard)
        {
            base.Initialize(blackboard);
            child?.Initialize(blackboard);
        }

        public override BTNode Clone()
        {
            DecoratorNode node = Instantiate(this);
            if (child != null)
            {
                node.child = child.Clone();
            }
            return node;
        }

        protected override void OnStop()
        {
            if (child?.State == BTNodeState.Running)
            {
                child.Abort();
            }
        }
    }
}