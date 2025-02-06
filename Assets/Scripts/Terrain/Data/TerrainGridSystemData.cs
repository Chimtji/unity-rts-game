using Unity.Entities;

public struct TerrainGridSystemData : IComponentData
{
    public int width;
    public int height;
    public float gridTileSize;
    public TerrainGridMap gridMap;
}