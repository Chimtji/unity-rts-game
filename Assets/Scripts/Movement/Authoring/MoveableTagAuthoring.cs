using UnityEngine;
using Unity.Entities;

public class MoveableTagAuthoring : MonoBehaviour
{
    public class Baker : Baker<MoveableTagAuthoring>
    {
        public override void Bake(MoveableTagAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MoveableTag());
        }
    }
}

public struct MoveableTag : IComponentData, IEnableableComponent
{
}

