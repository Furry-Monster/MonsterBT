using MonsterBT.Runtime.Utils;
using UnityEngine;

namespace MonsterBT.Runtime
{
    /// <summary>
    /// 节点状态
    /// </summary>
    public enum BTNodeState
    {
        Running,
        Success,
        Failure
    }

    /// <summary>
    /// 行为树节点SO基类
    /// </summary>
    public abstract class BTNode : ScriptableObject
    {
        [SerializeField] protected string description;
        [SerializeField] [ReadOnly] protected Vector2 position;

        public string Description
        {
            get => description;
            set => description = value;
        }

        public Vector2 Position
        {
            get => position;
            set => position = value;
        }

        protected BTNodeState state = BTNodeState.Running;
        protected bool started;
        protected Blackboard blackboard;

        public BTNodeState State => state;

        public virtual void Initialize(Blackboard blackboard)
        {
            this.blackboard = blackboard;
        }

        public BTNodeState Update()
        {
            if (!started)
            {
                OnStart();
                started = true;
                if (IsDebugMode())
                {
                    Debug.Log($"[BT] Node started: {GetNodeName()}");
                }
            }

            state = OnUpdate();

            if (state is BTNodeState.Success or BTNodeState.Failure)
            {
                if (IsDebugMode())
                {
                    Debug.Log($"[BT] Node finished: {GetNodeName()} -> {state}");
                }
                OnStop();
                started = false;
            }

            return state;
        }

        protected bool IsDebugMode()
        {
            return blackboard != null && blackboard.GetBool("DebugMode");
        }

        protected string GetNodeName()
        {
            return string.IsNullOrEmpty(name) ? GetType().Name : name;
        }

        public virtual BTNode Clone()
        {
            return Instantiate(this);
        }

        public void Abort()
        {
            OnStop();
            started = false;
            state = BTNodeState.Failure;
        }

        protected virtual void OnStart()
        {
        }

        protected abstract BTNodeState OnUpdate();

        protected virtual void OnStop()
        {
        }
    }
}