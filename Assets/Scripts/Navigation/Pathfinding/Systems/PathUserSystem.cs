using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct PathUserSystem : ISystem
{
    public const float DESTINATION_THRESHOLD = 2f;
    public ComponentLookup<CostMapNodeData> costMapNodeDataLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MoveableTag>();
        state.RequireForUpdate<Mover>();
        state.RequireForUpdate<CostMapSystemData>();

        costMapNodeDataLookup = SystemAPI.GetComponentLookup<CostMapNodeData>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        costMapNodeDataLookup.Update(ref state);

        CostMapSystemData costMapSystemData = SystemAPI.GetSingleton<CostMapSystemData>();
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        PathUserMoveJob pathUserMoveJob = new PathUserMoveJob
        {
            costMapWidth = costMapSystemData.width,
            nodesPerCostMap = costMapSystemData.nodesPerMap,
            costMapNodeSize = costMapSystemData.nodeSize,
            collisionWorld = collisionWorld,
            costMapNodeDataLookup = costMapNodeDataLookup,
            costMapNodes = costMapSystemData.allMapsNodes,
        };

        state.Dependency = pathUserMoveJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct PathUserMoveJob : IJobEntity
    {
        [ReadOnly] public CollisionWorld collisionWorld;
        [ReadOnly] public ComponentLookup<CostMapNodeData> costMapNodeDataLookup;
        [ReadOnly] public float costMapNodeSize;
        [ReadOnly] public int costMapWidth;
        [ReadOnly] public int nodesPerCostMap;
        [ReadOnly] public NativeList<Entity> costMapNodes;

        public void Execute(ref Mover mover, in LocalTransform localTransform, ref PathUser pathUser, EnabledRefRW<PathUser> pathUserEnabler)
        {
            // On user init, target is zero, so ignore this position, so user does not start moving on spawn
            if (float3.zero.Equals(pathUser.target))
            {
                mover.target = localTransform.Position;
                pathUserEnabler.ValueRW = false;
                return;
            }

            // If user is inside target node, then stop
            if (math.distance(localTransform.Position, pathUser.target) < costMapNodeSize)
            {
                mover.target = localTransform.Position;
                pathUserEnabler.ValueRW = false;
                return;
            }

            // Cast ray from user to target to see if there are any obstacles between
            RaycastInput raycastInput = new RaycastInput
            {
                Start = new float3(localTransform.Position.x, 1f, localTransform.Position.z),
                End = new float3(pathUser.target.x, 1f, pathUser.target.z),
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.PATHFINDING_WALLS_LAYER,
                    GroupIndex = 0,
                }
            };

            // If there are no obstacles between user and target, then just go there without using costmap
            if (!collisionWorld.CastRay(raycastInput))
            {
                mover.target = pathUser.target;
                pathUserEnabler.ValueRW = false;
                return;
            }

            if (pathUser.CostMapBlob.IsCreated)
            {
                // Remember this blobarray of nodes is a snapshot of the costMapNodeData at calculation time
                // and NOT realtime data.
                ref BlobArray<CostMapNodeData> nodes = ref pathUser.CostMapBlob.Value.nodes;
                int2 currentNodeGridPosition = CostMapSystem.GetNodeGridPosition(localTransform.Position, costMapNodeSize);
                int index = CostMapSystem.CalculateSingleMapNodeIndex(currentNodeGridPosition, costMapWidth);

                CostMapNodeData costMapNodeData = nodes[index];
                float3 nodeVector = CostMapSystem.GetWorldMovementVector(costMapNodeData.vector);

                // if the current node is a wall, then keep going in the direction user was going.
                // This is happening when a user is going around corners. If the user enters the wall node slightly,
                // Then it will just keep moving around instead of going into wall because wall has up vector.
                if (CostMapSystem.IsWall(costMapNodeData))
                {
                    nodeVector = pathUser.lastMoveVector;
                }
                else
                {
                    pathUser.lastMoveVector = nodeVector;
                }

                mover.target = CostMapSystem.GetNodeWorldCenterPosition(currentNodeGridPosition.x, currentNodeGridPosition.y) + nodeVector * (costMapNodeSize * 2f);
                return;
            }

            // int2 currentNodeGridPosition = CostMapSystem.GetNodeGridPosition(localTransform.Position, costMapNodeSize);
            // int index = CostMapSystem.CalculateNodeIndex(currentNodeGridPosition, pathUser.costMapId, costMapWidth, nodesPerCostMap);

            // Entity currentNodeEntity = costMapNodes[index];

            // if (costMapNodeDataLookup.HasComponent(currentNodeEntity))
            // {
            //     CostMapNodeData currentNodeData = costMapNodeDataLookup[currentNodeEntity];
            //     float3 nodeVector = CostMapSystem.GetWorldMovementVector(currentNodeData.vector);

            //     // if the current node is a wall, then keep going in the direction user was going.
            //     // This is happening when a user is going around corners. If the user enters the wall node slightly,
            //     // Then it will just keep moving around instead of stopping because the wall node has no vector.
            //     if (CostMapSystem.IsWall(currentNodeData))
            //     {
            //         nodeVector = pathUser.lastMoveVector;
            //     }
            //     else
            //     {
            //         pathUser.lastMoveVector = nodeVector;
            //     }

            //     mover.target = CostMapSystem.GetNodeWorldCenterPosition(currentNodeGridPosition.x, currentNodeGridPosition.y) + nodeVector * (costMapNodeSize * 2f);
            // }
        }
    }
}
