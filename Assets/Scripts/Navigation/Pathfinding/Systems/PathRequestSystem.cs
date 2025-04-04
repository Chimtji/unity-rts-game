using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

partial struct PathRequestSystem : ISystem
{
    public NativeQueue<PathRequestQueueItem> queue;
    public ComponentLookup<PathRequest> pathRequestLookup;

    public struct PathRequestQueueItem
    {
        public float3 target;
        public Entity entity;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MoveableTag>();
        state.RequireForUpdate<Mover>();
        state.RequireForUpdate<CostMapSystemData>();

        queue = new NativeQueue<PathRequestQueueItem>(Allocator.Persistent);

        pathRequestLookup = SystemAPI.GetComponentLookup<PathRequest>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        pathRequestLookup.Update(ref state);

        ControlsSystemData controlsSystemData = SystemAPI.GetSingleton<ControlsSystemData>();

        if (controlsSystemData.rightMouseButtonWasReleasedThisFrame)
        {
            NativeQueue<PathRequestQueueItem>.ParallelWriter queueWriter = queue.AsParallelWriter();
            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            PathRequestJob pathRequestJob = new PathRequestJob
            {
                physicsWorldSingleton = physicsWorldSingleton,
                targetPosition = controlsSystemData.clickedWorldPosition,
                queue = queueWriter,
            };

            pathRequestJob.ScheduleParallel();

            state.Dependency = new PathApplyRequestJob
            {
                queue = queue,
                pathRequestLookup = pathRequestLookup,
            }.Schedule(state.Dependency);
        }

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (queue.IsCreated)
        {
            queue.Dispose();
        }
    }

    [BurstCompile]
    [WithPresent(typeof(PathRequest))]
    partial struct PathRequestJob : IJobEntity
    {
        [ReadOnly] public float3 targetPosition;
        [ReadOnly] public PhysicsWorldSingleton physicsWorldSingleton;
        public NativeQueue<PathRequestQueueItem>.ParallelWriter queue;

        public void Execute(in SelectedTag selected,
                            in MoveableTag moveable,
                            in LocalTransform localTransform,
                            in Entity entity)
        {
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

            RaycastInput raycastInput = new RaycastInput
            {
                Start = new float3(localTransform.Position.x, 1f, localTransform.Position.z),
                End = new float3(targetPosition.x, 1f, targetPosition.z),
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.PATHFINDING_WALLS_LAYER,
                    GroupIndex = 0,
                }
            };

            // We only want to use pathfinding if there is something in the way of the target position
            if (collisionWorld.CastRay(raycastInput))
            {
                queue.Enqueue(new PathRequestQueueItem { entity = entity, target = targetPosition });
            }
        }
    }
    [BurstCompile]
    public struct PathApplyRequestJob : IJob
    {
        public NativeQueue<PathRequestQueueItem> queue;
        public ComponentLookup<PathRequest> pathRequestLookup;

        public void Execute()
        {
            while (queue.TryDequeue(out PathRequestQueueItem item))
            {
                pathRequestLookup.SetComponentEnabled(item.entity, true);

                PathRequest pathRequest = pathRequestLookup[item.entity];
                pathRequest.target = item.target;
                pathRequestLookup[item.entity] = pathRequest;
            }
        }
    }
}
