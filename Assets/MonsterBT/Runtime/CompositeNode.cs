using System.Collections.Generic;
using UnityEngine;

namespace MonsterBT.Runtime
{
    /// <summary>
    /// 组合节点基类
    /// </summary>
    public abstract class CompositeNode : BTNode
    {
        [SerializeField] protected List<BTNode> children = new List<BTNode>();

        public List<BTNode> Children => children;

        public override void Initialize(Blackboard blackboard)
        {
            base.Initialize(blackboard);
            foreach (var child in children)
            {
                child.Initialize(blackboard);
            }
        }

        public override BTNode Clone()
        {
            var node = Instantiate(this);
            node.children = new List<BTNode>();

            foreach (var child in children)
            {
                node.children.Add(child.Clone());
            }

            return node;
        }

        public void AddChild(BTNode child)
        {
            children.Add(child);
        }

        public void RemoveChild(BTNode child)
        {
            children.Remove(child);
        }

        protected override void OnStop()
        {
            foreach (var child in children)
            {
                if (child.State == BTNodeState.Running)
                {
                    child.Abort();
                }
            }
        }
    }
}