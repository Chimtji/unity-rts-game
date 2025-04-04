using Unity.Collections;
using Unity.Entities;

public struct CostMapSystemData : IComponentData
{
    public int width;
    public int height;
    public float nodeSize;
    public int nextMapId;
    public NativeList<Entity> allMapsNodes;
    public NativeArray<Entity> templateNodes;
    public int nodesPerMap;
    public int debugMapId;
}