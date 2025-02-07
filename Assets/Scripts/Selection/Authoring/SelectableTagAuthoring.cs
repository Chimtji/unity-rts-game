using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class SelectableTagAuthoring : MonoBehaviour
{
    public class Baker : Baker<SelectableTagAuthoring>
    {
        public override void Bake(SelectableTagAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SelectableTag());
        }
    }
}

public struct SelectableTag : IComponentData, IEnableableComponent
{
}