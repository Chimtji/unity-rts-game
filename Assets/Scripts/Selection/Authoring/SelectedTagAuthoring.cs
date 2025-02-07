using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class SelectedTagAuthoring : MonoBehaviour
{
    public class Baker : Baker<SelectedTagAuthoring>
    {
        public override void Bake(SelectedTagAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SelectedTag());
            SetComponentEnabled<SelectedTag>(entity, false);
        }
    }
}

public struct SelectedTag : IComponentData, IEnableableComponent
{
}