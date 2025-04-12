using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

/// <summary>
/// The purpose of this system is to make the mouse accessible in the ECS world
/// </summary>
public partial struct ControlsSystem : ISystem
{
    /// <summary>
    /// We create Controls ComponentData and add it to this system entity
    /// This entity is available to all entities
    /// </summary>
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.AddComponent<ControlsSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle, new ControlsSystemData
        {
            leftMouseButtonPressed = false,
            rightMouseButtonPressed = false,
            rightMouseButtonWasReleasedThisFrame = false,
            leftMouseButtonWasReleasedThisFrame = false,
            clickedWorldPosition = float3.zero,
        });
    }

    /// <summary>
    /// Records the Mouse Input and passes it to the Entity World
    /// FYI: Mouse is Managed, so we can't burst this code, but consumers of this data can still be bursted
    /// </summary>
    public void OnUpdate(ref SystemState state)
    {
        ControlsSystemData controlsData = SystemAPI.GetSingleton<ControlsSystemData>();

        // Persist
        float3 clickedPosition = controlsData.clickedWorldPosition;
        Entity clickedEntity = controlsData.clickedEntity;
        bool rightButtonPressed = controlsData.rightMouseButtonPressed;
        bool leftButtonPressed = controlsData.leftMouseButtonPressed;

        // New Value for each frame
        bool rightMouseButtonWasReleasedThisFrame = false;
        bool leftMouseButtonWasReleasedThisFrame = false;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            (float3 position, Entity entity) = MouseWorld.Instance.ReadClick();
            rightButtonPressed = true;
            clickedPosition = position;
            clickedEntity = entity;
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            rightButtonPressed = false;
            rightMouseButtonWasReleasedThisFrame = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            (float3 position, Entity entity) = MouseWorld.Instance.ReadClick();
            leftButtonPressed = true;
            clickedPosition = position;
            clickedEntity = entity;
        }
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            leftButtonPressed = false;
            leftMouseButtonWasReleasedThisFrame = true;
        }

        SystemAPI.SetComponent(state.SystemHandle, new ControlsSystemData
        {
            rightMouseButtonPressed = rightButtonPressed,
            leftMouseButtonPressed = leftButtonPressed,
            leftMouseButtonWasReleasedThisFrame = leftMouseButtonWasReleasedThisFrame,
            rightMouseButtonWasReleasedThisFrame = rightMouseButtonWasReleasedThisFrame,
            clickedWorldPosition = clickedPosition,
            clickedEntity = clickedEntity,
        });
    }
}