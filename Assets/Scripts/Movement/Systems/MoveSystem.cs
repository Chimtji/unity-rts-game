using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct MoveSystem : ISystem
{
    public const float DESTINATION_THRESHOLD = 1f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MoveableTag>();
        state.RequireForUpdate<Mover>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ControlsSystemData controlsSystemData = SystemAPI.GetSingleton<ControlsSystemData>();

        if (controlsSystemData.rightMouseButtonWasReleasedThisFrame)
        {
            MoverTargetJob moverTargetJob = new MoverTargetJob
            {
                target = controlsSystemData.clickedWorldPosition,
            };
            moverTargetJob.ScheduleParallel();
        }

        MoverJob moverJob = new MoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        };
        moverJob.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct MoverTargetJob : IJobEntity
    {
        public float3 target;
        public void Execute(ref Mover mover, in SelectedTag selected, in MoveableTag moveable)
        {
            mover.target = target;
        }


    }

    [BurstCompile]
    public partial struct MoverJob : IJobEntity
    {
        public float deltaTime;
        public void Execute(ref Mover mover, ref PhysicsVelocity physicsVelocity, ref LocalTransform localTransform, in MoveableTag movable)
        {
            float3 targetPosition = mover.target;
            float3 entityPosition = localTransform.Position;

            if (targetPosition.Equals(float3.zero))
            {
                targetPosition = entityPosition;
            }

            float3 moveDirection = targetPosition - entityPosition;

            if (math.lengthsq(moveDirection) < DESTINATION_THRESHOLD)
            {
                physicsVelocity.Linear = float3.zero;
                physicsVelocity.Angular = float3.zero;
                mover.isMoving = false;
                return;
            }

            mover.isMoving = true;
            moveDirection = math.normalize(moveDirection);

            localTransform.Rotation =
                math.slerp(localTransform.Rotation,
                            quaternion.LookRotation(moveDirection, math.up()),
                            deltaTime * mover.rotationSpeed);

            physicsVelocity.Angular = float3.zero;
            physicsVelocity.Linear = moveDirection * mover.speed;
        }
    }
}
