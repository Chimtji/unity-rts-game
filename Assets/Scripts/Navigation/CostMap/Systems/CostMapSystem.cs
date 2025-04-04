using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[CreateAfter(typeof(TerrainGridSystem))]
public partial struct CostMapSystem : ISystem
{
    public const int WALL_COST = byte.MaxValue;
    public const int MAPS_COUNT = 10;

    public struct CostMapBlob
    {
        public BlobArray<CostMapNodeData> nodes;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        TerrainGridSystemData terrainGridSystemData = SystemAPI.GetSingleton<TerrainGridSystemData>();

        CostMapSystemData costMapSystemData = new CostMapSystemData
        {
            width = terrainGridSystemData.width,
            height = terrainGridSystemData.height,
            nextMapId = 0,
            debugMapId = -1,
            nodeSize = terrainGridSystemData.gridTileSize,
            nodesPerMap = terrainGridSystemData.width * terrainGridSystemData.height,
            allMapsNodes = CreateCachedMaps(ref state, terrainGridSystemData.gridMap.entities, terrainGridSystemData.width, terrainGridSystemData.width * terrainGridSystemData.height),
        };

        state.EntityManager.AddComponent<CostMapSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle,
          costMapSystemData
        );
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        CostMapSystemData costMapSystemData = SystemAPI.GetSingleton<CostMapSystemData>();
        costMapSystemData.allMapsNodes.Dispose();
    }


    private NativeList<Entity> CreateCachedMaps(ref SystemState state, NativeArray<Entity> terrainGridEntities, int terrainGridWidth, int nodesPerMap)
    {
        int size = terrainGridEntities.Length * MAPS_COUNT;
        NativeList<Entity> nodes = new NativeList<Entity>(size, Allocator.Domain);

        for (int i = 0; i < MAPS_COUNT; i++)
        {
            foreach (Entity terrainGridEntity in terrainGridEntities)
            {
                TerrainGridTileData terrainTileData = SystemAPI.GetComponent<TerrainGridTileData>(terrainGridEntity);

                Entity nodeEntityPrefab = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<CostMapNodeData>(nodeEntityPrefab);

                Entity nodeEntity = state.EntityManager.Instantiate(nodeEntityPrefab);

                CostMapNodeData nodeData = new CostMapNodeData
                {
                    index = CalculateNodeIndex(terrainTileData.gridPosition, i, terrainGridWidth, nodesPerMap),
                    mapId = i,
                    terrainGridPosition = terrainTileData.gridPosition,
                    cost = 1,
                    bestCost = byte.MaxValue,
                    vector = new float2(0, 1),
                    enabled = false,
                };

                SystemAPI.SetComponent(nodeEntity, nodeData);

                nodes.Add(nodeEntity);
            }
        }

        return nodes;
    }

    public static NativeList<CostMapNodeData> GetNodeDatasOfMap(NativeSlice<Entity> nodesOfMap, EntityManager entityManager)
    {
        NativeList<CostMapNodeData> nodeDatas = new NativeList<CostMapNodeData>(Allocator.Persistent);
        for (int i = 0; i < nodesOfMap.Length; i++)
        {
            CostMapNodeData nodeData = entityManager.GetComponentData<CostMapNodeData>(nodesOfMap[i]);
            nodeDatas.Add(nodeData);
        }

        return nodeDatas;
    }

    public static NativeSlice<Entity> GetNodesOfMap(int mapId, int nodesPerMap, NativeList<Entity> allMapsNodes)
    {
        int indexStart = CalculateMapIdStartIndex(mapId, nodesPerMap);
        NativeSlice<Entity> nodesOfMap = new NativeSlice<Entity>(allMapsNodes.AsArray(), indexStart, nodesPerMap);
        return nodesOfMap;
    }
    public static void IncreaseNextMapId(RefRW<CostMapSystemData> costMapSystemData)
    {
        int nextId = costMapSystemData.ValueRO.nextMapId + 1;

        // If we get higher than the precreated (cached) maps, then we start from the beginning again
        // We suspect we don't need to have alot of cached maps because they get outdated very quickly
        if (nextId >= MAPS_COUNT)
        {
            costMapSystemData.ValueRW.nextMapId = 0;
        }
        else
        {
            costMapSystemData.ValueRW.nextMapId = nextId;
        }
    }

    public static int CalculateNodeIndex(int2 terrainGridPosition, int mapId, int gridWidth, int nodesPerMap)
    {
        int gridIndex = TerrainGridSystem.CalculateIndex(terrainGridPosition, gridWidth);
        int mapStartIndex = CalculateMapIdStartIndex(mapId, nodesPerMap);
        int result = mapStartIndex + gridIndex;

        return result;
    }

    public static int CalculateMapIdEndIndex(int mapId, int startIndex, int nodesPerMap)
    {
        if (mapId == 0)
        {
            return nodesPerMap - 1;
        }
        else
        {
            return startIndex + nodesPerMap - 1;
        }
    }

    public static int CalculateMapIdStartIndex(int mapId, int nodesPerMap)
    {
        return mapId * nodesPerMap;
    }

    public static float3 GetNodeWorldCenterPosition(int2 position)
    {
        return TerrainGridSystem.GetWorldCenterPosition(position.x, position.y);
    }
    public static float3 GetNodeWorldCenterPosition(int x, int y)
    {
        return TerrainGridSystem.GetWorldCenterPosition(x, y);
    }

    public static int CalculateSingleMapNodeIndex(int2 terrainGridPosition, int width)
    {
        return TerrainGridSystem.CalculateIndex(terrainGridPosition, width);
    }

    public static int2 GetSingleMapNodeGridPositionFromIndex(int index, int width)
    {
        int y = (int)math.floor(index / width);
        int x = index % width;
        return new int2(x, y);
    }

    public static int2 GetNodeGridPosition(float3 worldPosition, float gridTileSize)
    {
        return TerrainGridSystem.GetGridPosition(worldPosition, gridTileSize);
    }
    public static float3 GetWorldMovementVector(float2 vector)
    {
        return new float3(vector.x, 0, vector.y);
    }

    public static bool IsWall(CostMapNodeData gridTileData)
    {
        return gridTileData.cost == WALL_COST;
    }
    public static bool IsValidGridPosition(int2 gridPosition, int width, int height)
    {
        return TerrainGridSystem.IsValidGridPosition(gridPosition, width, height);
    }

    public static int GetNumberOfAvailableMaps(NativeList<Entity> nodes, int nodesPerMap)
    {
        return nodes.Length / nodesPerMap;
    }
}