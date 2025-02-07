using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

/// <summary>
/// The purpose of this system is to make the mouse accessible in the ECS world
/// </summary>
public partial struct ControlsSystem : ISystem
{
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

    public void OnUpdate(ref SystemState state)
    {
        ControlsSystemData controlsData = SystemAPI.GetSingleton<ControlsSystemData>();

        // Persist
        float3 clickedPosition = controlsData.clickedWorldPosition;
        bool rightButtonPressed = controlsData.rightMouseButtonPressed;
        bool leftButtonPressed = controlsData.leftMouseButtonPressed;

        // New Value for each frame
        bool rightMouseButtonWasReleasedThisFrame = false;
        bool leftMouseButtonWasReleasedThisFrame = false;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            rightButtonPressed = true;
            clickedPosition = MouseWorld.Instance.GetPosition();
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            rightButtonPressed = false;
            rightMouseButtonWasReleasedThisFrame = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            leftButtonPressed = true;
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
            clickedWorldPosition = clickedPosition,
            leftMouseButtonWasReleasedThisFrame = leftMouseButtonWasReleasedThisFrame,
            rightMouseButtonWasReleasedThisFrame = rightMouseButtonWasReleasedThisFrame,
        });
    }
}