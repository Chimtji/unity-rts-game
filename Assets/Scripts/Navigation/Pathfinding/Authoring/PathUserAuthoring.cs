using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class PathUserAuthoring : MonoBehaviour
{
    public class Baker : Baker<PathUserAuthoring>
    {
        public override void Bake(PathUserAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PathUser());
            SetComponentEnabled<PathUser>(entity, true);
        }
    }
}

public struct PathUser : IComponentData, IEnableableComponent
{
    public float3 target;
    public float3 lastMoveVector;
    public int costMapId;
    public BlobAssetReference<CostMapSystem.CostMapBlob> CostMapBlob;
}

