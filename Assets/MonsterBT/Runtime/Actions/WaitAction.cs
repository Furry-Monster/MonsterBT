using System.Collections;
using UnityEngine;

namespace MonsterBT.Runtime.Actions
{
    [CreateAssetMenu(fileName = "WaitAction", menuName = "MonsterBTNode/Actions/WaitAction")]
    public class WaitAction : ActionNode
    {
        [SerializeField][Tooltip("µÈ´ýÊ±¼ä")] private float waitTime = 1f;

        private float startTime;

        protected override void OnStart()
        {
            startTime = Time.time;
        }

        protected override BTNodeState OnUpdate()
        {
            if (Time.time - startTime >= waitTime)
            {
                return BTNodeState.Success;
            }

            return BTNodeState.Running;
        }
    }
}