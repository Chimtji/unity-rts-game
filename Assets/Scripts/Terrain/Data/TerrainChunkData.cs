using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public struct TerrainChunkData : IComponentData
{
    public int meshId;
    public Entity meshEntity;
    public int scale;
    public int width;
    public int height;
    public int index;
    public float2 chunkPosition;
    public float3 worldPosition;
}