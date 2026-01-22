using UnityEngine;

namespace MonsterBT.Runtime
{
    /// <summary>
    /// 根节点
    /// </summary>
    [CreateAssetMenu(fileName = "RootNode", menuName = "MonsterBTNode/RootNode")]
    public class RootNode : BTNode
    {
        [SerializeField] private BTNode child;

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
            RootNode node = Instantiate(this);
            if (child != null)
            {
                node.child = child.Clone();
            }
            return node;
        }

        protected override BTNodeState OnUpdate()
        {
            if (child == null)
                return BTNodeState.Failure;

            return child.Update();
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