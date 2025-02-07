using Unity.Entities;
using UnityEngine;

public class SelectedVisualAuthoring : MonoBehaviour
{

    public GameObject visualGameObject;

    public class Baker : Baker<SelectedVisualAuthoring>
    {
        public override void Bake(SelectedVisualAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SelectedVisual
            {
                visualEntity = GetEntity(authoring.visualGameObject, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct SelectedVisual : IComponentData, IEnableableComponent
{
    public Entity visualEntity;
}
