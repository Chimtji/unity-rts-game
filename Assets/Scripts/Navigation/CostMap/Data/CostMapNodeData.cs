using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct CostMapNodeData : IComponentData
{
    public int index;
    public int generateIndex;
    public int mapId;
    public int2 terrainGridPosition;
    public byte cost;
    public int bestCost;
    public float2 vector;
    public int2 target;
    public float3 lifespan;
    public bool enabled;
}