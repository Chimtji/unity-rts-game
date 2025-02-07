using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct SelectedVisualSystem : ISystem
{
    public ComponentLookup<LocalTransform> localTransformLookup;
    public ComponentLookup<SelectedTag> selectedLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SelectableTag>();

        localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false);
        selectedLookup = SystemAPI.GetComponentLookup<SelectedTag>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        localTransformLookup.Update(ref state);
        selectedLookup.Update(ref state);

        // I have tried doing a queue, so things could maybe go more parallel and 
        // not have this bottleneck, but it seems it's slower in the profiler by quite much.
        HandleSelectedVisualJob handleSelectedVisualJob = new HandleSelectedVisualJob
        {
            selectedLookup = selectedLookup,
            localTransformLookup = localTransformLookup,
        };
        state.Dependency = handleSelectedVisualJob.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct HandleSelectedVisualJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SelectedTag> selectedLookup;
        [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> localTransformLookup;

        public void Execute(in SelectableTag selectable, ref SelectedVisual selectedVisual, Entity entity)
        {
            var visualEntity = selectedVisual.visualEntity;
            if (selectedLookup.HasComponent(entity) && localTransformLookup.HasComponent(visualEntity))
            {
                LocalTransform transform = localTransformLookup[visualEntity];
                bool isSelected = selectedLookup.IsComponentEnabled(entity);

                if (isSelected && transform.Scale != 1f)
                {
                    transform.Scale = 1f;
                    localTransformLookup[visualEntity] = transform;
                    return;
                }

                if (!isSelected && transform.Scale != 0f)
                {
                    transform.Scale = 0f;
                    localTransformLookup[visualEntity] = transform;
                    return;
                }
            }
        }
    }
}

