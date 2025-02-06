using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct TerrainMeshSystemData : IComponentData
{
    public int scale;
    public int size;
    public NativeHashMap<float2, TerrainChunkData> chunkMap;
}