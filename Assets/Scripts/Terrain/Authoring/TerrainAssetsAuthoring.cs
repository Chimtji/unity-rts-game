using Unity.Entities;
using UnityEngine;

class TerrainAssetsAuthoring : MonoBehaviour
{
    public GameObject obstaclePrefab;

    public class Baker : Baker<TerrainAssetsAuthoring>
    {
        public override void Bake(TerrainAssetsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TerrainAssets
            {
                obstaclePrefab = GetEntity(authoring.obstaclePrefab, TransformUsageFlags.Dynamic),
            });
        }
    }
}

public struct TerrainAssets : IComponentData
{
    public Entity obstaclePrefab;
}



