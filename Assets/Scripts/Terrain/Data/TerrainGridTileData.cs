using Unity.Entities;
using Unity.Mathematics;

public struct TerrainGridTileData : IComponentData
{
    /// <summary>
    /// The Index of this tile in the Grid Map
    /// </summary>
    public int index;

    /// <summary>
    /// The grid coordinate of this tile in the grid map
    /// </summary>
    public int2 gridPosition;

    /// <summary>
    /// The bottom left corner of this tile in world space
    /// </summary>
    public float3 worldPosition;

    /// <summary>
    /// The center of the the tile in world space
    /// </summary>
    public float3 worldCenterPosition;

    /// <summary>
    /// The steepness of the tile in degrees
    /// </summary>
    public float steepness;

    /// <summary>
    /// The Y position at the center of this tile
    /// </summary>
    public float centerHeight;

    /// <summary>
    /// The World Position of the lower left corner of this tile
    /// </summary>
    public float3 lowerLeftCornerPosition;

    /// <summary>
    /// The World Position of the lower right corner of this tile
    /// </summary>
    public float3 lowerRightCornerPosition;

    /// <summary>
    /// The World Position of the upper left corner of this tile
    /// </summary>
    public float3 upperLeftCornerPosition;

    /// <summary>
    /// The World Position of the upper right corner of this tile
    /// </summary>
    public float3 upperRightCornerPosition;
}