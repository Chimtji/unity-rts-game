using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct TerrainChunkData : IComponentData
{
    public int scale;
    public int width;
    public int height;
    public int index;
    public NativeArray<float3> vertices;
    public NativeArray<float2> uvs;
    public NativeArray<int> triangles;
    public TerrainChunkPositionData chunkPosition;
    public float3 worldPosition;
}