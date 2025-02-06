using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

[CreateAfter(typeof(TerrainMeshSystem))]
public partial struct TerrainGridSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        TerrainMeshSystemData terrainMeshData = SystemAPI.GetSingleton<TerrainMeshSystemData>();

        int width = terrainMeshData.size;
        int height = terrainMeshData.size;
        int gridTileSize = terrainMeshData.scale;
        int gridTilesCount = width * height;

        Entity gridTileEntityPrefab = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<TerrainGridTileData>(gridTileEntityPrefab);

        TerrainGridMap gridMap = new TerrainGridMap();
        gridMap.entities = new NativeArray<Entity>(gridTilesCount, Allocator.Persistent);

        state.EntityManager.Instantiate(gridTileEntityPrefab, gridMap.entities);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int index = CalculateIndex(x, y, width);
                float3 worldPosition = GetWorldPosition(x, y);
                float3 worldCenterPosition = new float3(worldPosition.x + (gridTileSize / 2f), worldPosition.y, worldPosition.z + (gridTileSize / 2f));

                // Calculate the world corners of the tile
                float3 lowerLeftCornerPosition = worldPosition;
                float3 lowerRightCornerPosition = new float3(worldPosition.x + gridTileSize, worldPosition.y, worldPosition.z);
                float3 upperLeftCornerPosition = new float3(worldPosition.x, worldPosition.y, worldPosition.z + gridTileSize);
                float3 upperRightCornerPosition = new float3(worldPosition.x + gridTileSize, worldPosition.y, worldPosition.z + gridTileSize);

                // Calculate the height of the tile at the center
                float centerHeight = (lowerLeftCornerPosition.y +
                                    lowerRightCornerPosition.y +
                                    upperLeftCornerPosition.y +
                                    upperRightCornerPosition.y) / 4;

                // Calculate the steepness of the tile
                float3 vector1 = upperLeftCornerPosition - lowerLeftCornerPosition;
                float3 vector2 = lowerRightCornerPosition - lowerLeftCornerPosition;
                float3 normal = math.normalize(math.cross(vector1, vector2));
                float cosTheta = math.dot(normal, new float3(0, 1, 0));
                float steepness = math.degrees(math.acos(math.clamp(cosTheta, -1f, 1f)));

                TerrainGridTileData gridTile = new TerrainGridTileData
                {
                    index = index,
                    gridPosition = new int2(x, y),
                    worldPosition = worldPosition,
                    worldCenterPosition = worldCenterPosition,
                    lowerLeftCornerPosition = lowerLeftCornerPosition,
                    lowerRightCornerPosition = lowerRightCornerPosition,
                    upperLeftCornerPosition = upperLeftCornerPosition,
                    upperRightCornerPosition = upperRightCornerPosition,
                    centerHeight = centerHeight,
                    steepness = steepness
                };

                SystemAPI.SetComponent(gridMap.entities[index], gridTile);
            }
        }

        TerrainGridSystemData terrainGridSystemData = new TerrainGridSystemData
        {
            width = width,
            height = height,
            gridTileSize = gridTileSize,
            gridMap = gridMap,
        };

        state.EntityManager.AddComponent<TerrainGridSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle,
           terrainGridSystemData
        );
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        RefRW<TerrainGridSystemData> gridSystemData = SystemAPI.GetComponentRW<TerrainGridSystemData>(state.SystemHandle);
        gridSystemData.ValueRW.gridMap.entities.Dispose();
    }

    public static int CalculateIndex(int2 gridPosition, int width)
    {
        return CalculateIndex(gridPosition.x, gridPosition.y, width);
    }

    public static int CalculateIndex(int x, int y, int width)
    {
        return x + y * width;
    }

    public static float3 GetWorldPosition(int2 gridPosition)
    {
        return GetWorldPosition(gridPosition.x, gridPosition.y);
    }

    public static float3 GetWorldPosition(int x, int y)
    {
        return new float3(
            x * TerrainMeshSystem.SCALE,
            0f,
            y * TerrainMeshSystem.SCALE
        );
    }
    public static float3 GetWorldCenterPosition(int2 gridPosition)
    {
        return GetWorldCenterPosition(gridPosition.x, gridPosition.y);
    }
    public static float3 GetWorldCenterPosition(int x, int y)
    {
        return new float3(
            x * TerrainMeshSystem.SCALE + (TerrainMeshSystem.SCALE / 2f),
            0f,
            y * TerrainMeshSystem.SCALE + (TerrainMeshSystem.SCALE / 2f)
        );
    }

    public static int2 GetGridPosition(float3 worldPosition, float gridNodeSize)
    {
        return new int2(
            (int)math.floor(worldPosition.x / gridNodeSize),
            (int)math.floor(worldPosition.z / gridNodeSize)
        );
    }

    public static bool IsValidGridPosition(int2 gridPosition, int width, int height)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < width &&
            gridPosition.y < height;
    }
}