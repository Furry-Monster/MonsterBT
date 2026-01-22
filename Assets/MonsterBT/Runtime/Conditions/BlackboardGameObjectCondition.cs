using UnityEngine;

namespace MonsterBT.Runtime.Conditions
{
    public enum GameObjectComparison
    {
        IsNull,
        IsNotNull,
        IsActive,
        IsInactive,
        HasTag,
        HasComponent,
        EqualsReference
    }

    [CreateAssetMenu(fileName = "BlackboardGameObjectCondition",
        menuName = "MonsterBTNode/Conditions/BlackboardGameObjectCondition")]
    public class BlackboardGameObjectCondition : ActionNode
    {
        [SerializeField] private string keyName = "gameObjectKey";
        [SerializeField] private GameObjectComparison comparisonType = GameObjectComparison.IsNotNull;
        
        [Header("比较参数")]
        [SerializeField] private string expectedTag = "Player";
        [SerializeField] private string componentTypeName = "Rigidbody";
        [SerializeField] private GameObject referenceObject;

        protected override BTNodeState OnUpdate()
        {
            if (!blackboard.HasKey(keyName))
                return BTNodeState.Failure;

            GameObject currentObject = blackboard.GetGameObject(keyName);
            bool result = false;

            switch (comparisonType)
            {
                case GameObjectComparison.IsNull:
                    result = currentObject == null;
                    break;

                case GameObjectComparison.IsNotNull:
                    result = currentObject != null;
                    break;

                case GameObjectComparison.IsActive:
                    result = currentObject != null && currentObject.activeInHierarchy;
                    break;

                case GameObjectComparison.IsInactive:
                    result = currentObject == null || !currentObject.activeInHierarchy;
                    break;

                case GameObjectComparison.HasTag:
                    result = currentObject != null && currentObject.CompareTag(expectedTag);
                    break;

                case GameObjectComparison.HasComponent:
                    if (currentObject != null)
                    {
                        System.Type componentType = System.Type.GetType(componentTypeName);
                        if (componentType != null)
                        {
                            result = currentObject.GetComponent(componentType) != null;
                        }
                        else
                        {
                            Debug.LogWarning($"Component type '{componentTypeName}' not found");
                            return BTNodeState.Failure;
                        }
                    }
                    break;

                case GameObjectComparison.EqualsReference:
                    result = currentObject == referenceObject;
                    break;
            }

            return result ? BTNodeState.Success : BTNodeState.Failure;
        }
    }
}