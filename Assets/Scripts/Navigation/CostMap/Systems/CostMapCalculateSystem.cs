using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

[CreateAfter(typeof(CostMapSystem))]
public partial struct CostMapCalculateSystem : ISystem
{
    public const int WALL_COST = byte.MaxValue;
    public ComponentLookup<CostMapNodeData> costMapNodeDataLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CostMapSystemData>();
        state.RequireForUpdate<CostMapSystemData>();

        costMapNodeDataLookup = SystemAPI.GetComponentLookup<CostMapNodeData>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        foreach ((
            RefRO<PathRequest> pathRequest,
            RefRW<PathUser> pathUser,
            EnabledRefRW<PathRequest> pathRequestEnabler,
            EnabledRefRW<PathUser> pathUserEnabler)
            in SystemAPI.Query<
                RefRO<PathRequest>,
                RefRW<PathUser>,
                EnabledRefRW<PathRequest>,
                EnabledRefRW<PathUser>>()
                    .WithPresent<PathUser>())
        {
            RefRW<CostMapSystemData> costMapSystemData = SystemAPI.GetSingletonRW<CostMapSystemData>();
            int mapId = costMapSystemData.ValueRO.nextMapId;
            int2 targetGridPosition = CostMapSystem.GetNodeGridPosition(pathRequest.ValueRO.target, costMapSystemData.ValueRO.nodeSize);
            (bool isCached, int cachedMapId, NativeSlice<Entity> cachedMapNodes) = CheckForCachedMap(ref state, costMapSystemData.ValueRO.nodesPerMap, costMapSystemData.ValueRO.allMapsNodes, targetGridPosition);

            if (isCached)
            {
                // Debug.Log("found cached map");
                pathUser.ValueRW.costMapId = cachedMapId;
                pathUser.ValueRW.target = pathRequest.ValueRO.target;
                pathUser.ValueRW.CostMapBlob = ConvertNodesToBlob(ref state, costMapSystemData.ValueRO.nodesPerMap, cachedMapNodes);
                pathUserEnabler.ValueRW = true;
            }
            else
            {
                NativeSlice<Entity> nodesOfMap = CostMapSystem.GetNodesOfMap(mapId, costMapSystemData.ValueRO.nodesPerMap, costMapSystemData.ValueRO.allMapsNodes);

                ResetCostMap(ref state, targetGridPosition, nodesOfMap);
                UpdateBlockedNodes(ref state, nodesOfMap, costMapSystemData.ValueRO.nodeSize);
                CalculateCostMap(ref state, targetGridPosition, nodesOfMap, costMapSystemData.ValueRO);

                costMapSystemData = SystemAPI.GetSingletonRW<CostMapSystemData>();
                CostMapSystem.IncreaseNextMapId(costMapSystemData);

                pathUser.ValueRW.costMapId = mapId;
                pathUser.ValueRW.target = pathRequest.ValueRO.target;
                pathUser.ValueRW.CostMapBlob = ConvertNodesToBlob(ref state, costMapSystemData.ValueRO.nodesPerMap, nodesOfMap);
                pathUserEnabler.ValueRW = true;
            }

            pathRequestEnabler.ValueRW = false;
        }
    }

    [BurstCompile]
    private BlobAssetReference<CostMapSystem.CostMapBlob> ConvertNodesToBlob(ref SystemState state, int nodesPerMap, NativeSlice<Entity> nodesOfMap)
    {
        EntityManager entityManager = state.EntityManager;
        NativeList<CostMapNodeData> nodeDatas = CostMapSystem.GetNodeDatasOfMap(nodesOfMap, entityManager);
        BlobBuilder builder = new BlobBuilder(Allocator.Temp);
        ref CostMapSystem.CostMapBlob costMapBlob = ref builder.ConstructRoot<CostMapSystem.CostMapBlob>();

        BlobBuilderArray<CostMapNodeData> arrayBuilder = builder.Allocate(
            ref costMapBlob.nodes,
            nodesPerMap
        );

        for (int i = 0; i < nodesPerMap; i++)
        {
            arrayBuilder[i] = nodeDatas[i];
        }

        var result = builder.CreateBlobAssetReference<CostMapSystem.CostMapBlob>(Allocator.Persistent);

        builder.Dispose();
        nodeDatas.Dispose();

        return result;


    }

    [BurstCompile]
    private void ResetCostMap(ref SystemState state, int2 targetGridPosition, NativeSlice<Entity> nodes)
    {
        costMapNodeDataLookup.Update(ref state);

        ResetCostMapJob resetCostMapJob = new ResetCostMapJob
        {
            costMapNodeDataLookup = costMapNodeDataLookup,
            nodes = nodes,
            targetGridPosition = targetGridPosition,
        };

        state.Dependency = resetCostMapJob.ScheduleParallel(nodes.Length, 50, state.Dependency);
    }

    [BurstCompile]
    private void UpdateBlockedNodes(ref SystemState state, NativeSlice<Entity> nodes, float nodeSize)
    {
        costMapNodeDataLookup.Update(ref state);
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        UpdateBlockedNodesJob updateBlockedNodesJob = new UpdateBlockedNodesJob
        {
            costMapNodeDataLookup = costMapNodeDataLookup,
            nodes = nodes,
            collisionWorld = collisionWorld,
            nodeSize = nodeSize,
        };

        JobHandle updateBlockedNodesJobHandle = updateBlockedNodesJob.ScheduleParallel(nodes.Length, 50, state.Dependency);
        updateBlockedNodesJobHandle.Complete();
    }

    [BurstCompile]
    private void CalculateCostMap(ref SystemState state, int2 targetGridPosition, NativeSlice<Entity> nodesOfMap, CostMapSystemData costMapSystemData)
    {
        Entity targetEntity = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(targetGridPosition, costMapSystemData.width)];
        RefRW<CostMapNodeData> targetNodeData = SystemAPI.GetComponentRW<CostMapNodeData>(targetEntity);

        NativeQueue<RefRW<CostMapNodeData>> costMapNodesQueue = new NativeQueue<RefRW<CostMapNodeData>>(Allocator.Temp);
        costMapNodesQueue.Enqueue(targetNodeData);

        // I don't think this safety value is correct
        int safety = nodesOfMap.Length * 10;
        while (costMapNodesQueue.Count > 0)
        {
            // This safety is important. If it's not there it leaks and stalls
            safety--;
            if (safety < 0)
            {
                Debug.Log("Hit Safety in Cost Map Calculation");
                break;
            }

            RefRW<CostMapNodeData> currentCostMapNode = costMapNodesQueue.Dequeue();
            NativeList<RefRW<CostMapNodeData>> neighbourNodes = GetNeighbourNodes(ref state, currentCostMapNode.ValueRO, nodesOfMap, costMapSystemData.width, costMapSystemData.height);

            foreach (RefRW<CostMapNodeData> neighbourNode in neighbourNodes)
            {
                if (neighbourNode.ValueRO.cost == WALL_COST) continue;

                int newBestCost = currentCostMapNode.ValueRO.bestCost + neighbourNode.ValueRO.cost;
                if (newBestCost < neighbourNode.ValueRO.bestCost)
                {
                    float2 vector;
                    CalculateCostVector(
                        neighbourNode.ValueRO.terrainGridPosition.x, neighbourNode.ValueRO.terrainGridPosition.y,
                        currentCostMapNode.ValueRO.terrainGridPosition.x, currentCostMapNode.ValueRO.terrainGridPosition.y,
                        out vector
                    );

                    neighbourNode.ValueRW.enabled = true;
                    neighbourNode.ValueRW.bestCost = newBestCost;
                    neighbourNode.ValueRW.vector = vector;

                    costMapNodesQueue.Enqueue(neighbourNode);
                }
            }

            neighbourNodes.Dispose();
        }

        costMapNodesQueue.Dispose();
    }

    [BurstCompile]
    private NativeList<RefRW<CostMapNodeData>> GetNeighbourNodes(
            ref SystemState state,
            CostMapNodeData currentNode,
            NativeSlice<Entity> nodesOfMap,
            int width,
            int height)
    {
        NativeList<RefRW<CostMapNodeData>> neighbourNodes = new NativeList<RefRW<CostMapNodeData>>(Allocator.TempJob);

        int nodeX = currentNode.terrainGridPosition.x;
        int nodeY = currentNode.terrainGridPosition.y;

        int2 positionLeft = new int2(nodeX - 1, nodeY + 0);
        int2 positionRight = new int2(nodeX + 1, nodeY + 0);
        int2 positionUp = new int2(nodeX + 0, nodeY + 1);
        int2 positionDown = new int2(nodeX + 0, nodeY - 1);

        int2 positionLowerLeft = new int2(nodeX - 1, nodeY - 1);
        int2 positionLowerRight = new int2(nodeX + 1, nodeY - 1);
        int2 positionUpperLeft = new int2(nodeX - 1, nodeY + 1);
        int2 positionUpperRight = new int2(nodeX + 1, nodeY + 1);

        if (CostMapSystem.IsValidGridPosition(positionLeft, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionLeft, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }
        if (CostMapSystem.IsValidGridPosition(positionRight, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionRight, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }
        if (CostMapSystem.IsValidGridPosition(positionUp, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionUp, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }
        if (CostMapSystem.IsValidGridPosition(positionDown, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionDown, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }

        if (CostMapSystem.IsValidGridPosition(positionLowerLeft, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionLowerLeft, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }
        if (CostMapSystem.IsValidGridPosition(positionLowerRight, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionLowerRight, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }
        if (CostMapSystem.IsValidGridPosition(positionUpperLeft, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionUpperLeft, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }
        if (CostMapSystem.IsValidGridPosition(positionUpperRight, width, height))
        {
            Entity node = nodesOfMap[CostMapSystem.CalculateSingleMapNodeIndex(positionUpperRight, width)];
            RefRW<CostMapNodeData> nodeData = SystemAPI.GetComponentRW<CostMapNodeData>(node);
            neighbourNodes.Add(nodeData);
        }

        return neighbourNodes;
    }

    [BurstCompile]
    private static void CalculateCostVector(int fromX, int fromY, int toX, int toY, out float2 result)
    {
        result = new float2(toX, toY) - new float2(fromX, fromY);
    }

    [BurstCompile]
    private (bool isCached, int mapId, NativeSlice<Entity> nodesOfMap) CheckForCachedMap(ref SystemState state, int nodesPerMap, NativeList<Entity> allNodes, int2 targetGridPosition)
    {
        int availableMaps = CostMapSystem.GetNumberOfAvailableMaps(allNodes, nodesPerMap);
        // First we check if we can use an already calculated gridMap
        for (int i = 0; i < availableMaps; i++)
        {
            int mapNodeIndex = CostMapSystem.CalculateMapIdStartIndex(i, nodesPerMap);
            Entity mapNode = allNodes[mapNodeIndex];
            CostMapNodeData mapNodeData = SystemAPI.GetComponent<CostMapNodeData>(mapNode);

            if (!mapNodeData.enabled)
            {
                continue;
            }

            if (mapNodeData.target.Equals(targetGridPosition))
            {
                NativeSlice<Entity> nodesOfMap = CostMapSystem.GetNodesOfMap(i, nodesPerMap, allNodes);
                return (true, mapNodeData.mapId, nodesOfMap);
            }
        }

        return (false, 1, new NativeSlice<Entity>());
    }

    [BurstCompile]
    private partial struct ResetCostMapJob : IJobFor
    {
        [NativeDisableParallelForRestriction] public ComponentLookup<CostMapNodeData> costMapNodeDataLookup;
        [NativeDisableParallelForRestriction] public NativeSlice<Entity> nodes;
        [ReadOnly] public int2 targetGridPosition;
        public void Execute(int index)
        {

            // Maybe we should only do this if the map is not enabled
            // and then enable the map, when the cost has been calculated in the other job?

            Entity entity = nodes[index];
            CostMapNodeData costMapNodeData = costMapNodeDataLookup[entity];

            byte cost = 1;
            byte bestCost = byte.MaxValue;

            if (targetGridPosition.x == costMapNodeData.terrainGridPosition.x && targetGridPosition.y == costMapNodeData.terrainGridPosition.y)
            {
                cost = 0;
                bestCost = 0;
            }

            if (costMapNodeData.cost == WALL_COST)
            {
                cost = costMapNodeData.cost;
            }

            costMapNodeData.vector = new float2(0, -1);
            costMapNodeData.cost = cost;
            costMapNodeData.bestCost = bestCost;
            costMapNodeData.target = targetGridPosition;
            costMapNodeData.enabled = false;

            costMapNodeDataLookup[entity] = costMapNodeData;
        }
    }

    [BurstCompile]
    private partial struct UpdateBlockedNodesJob : IJobFor
    {
        [NativeDisableParallelForRestriction] public ComponentLookup<CostMapNodeData> costMapNodeDataLookup;
        [NativeDisableParallelForRestriction] public NativeSlice<Entity> nodes;
        [ReadOnly] public CollisionWorld collisionWorld;
        [ReadOnly] public float nodeSize;
        public void Execute(int index)
        {
            Entity entity = nodes[index];
            CostMapNodeData costMapNodeData = costMapNodeDataLookup[entity];

            float3 nodeWorldCenterPosition = CostMapSystem.GetNodeWorldCenterPosition(
                costMapNodeData.terrainGridPosition.x,
                costMapNodeData.terrainGridPosition.y
            );

            Aabb aabb = new Aabb
            {
                Min = nodeWorldCenterPosition - new float3(nodeSize / 4f),
                Max = nodeWorldCenterPosition + new float3(nodeSize / 4f),
            };

            NativeList<int> hitIndices = new NativeList<int>(Allocator.Temp);

            OverlapAabbInput input = new OverlapAabbInput
            {
                Aabb = aabb,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.PATHFINDING_WALLS_LAYER,
                    GroupIndex = 0,
                }
            };

            collisionWorld.OverlapAabb(input, ref hitIndices);

            if (hitIndices.Length > 0)
            {
                costMapNodeData.cost = WALL_COST;
                costMapNodeData.enabled = true;
            }

            hitIndices.Dispose();

            costMapNodeDataLookup[entity] = costMapNodeData;
        }

    }
}