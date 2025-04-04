using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class PathRequestAuthoring : MonoBehaviour
{
    public class Baker : Baker<PathRequestAuthoring>
    {
        public override void Bake(PathRequestAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PathRequest());
            SetComponentEnabled<PathRequest>(entity, false);
        }
    }
}

public struct PathRequest : IComponentData, IEnableableComponent
{
    public float3 target;
}

