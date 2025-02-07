using Unity.Entities;
using Unity.Mathematics;

public struct ControlsSystemData : IComponentData
{
    public bool rightMouseButtonWasReleasedThisFrame;
    public bool leftMouseButtonWasReleasedThisFrame;
    public bool rightMouseButtonPressed;
    public bool leftMouseButtonPressed;
    public float3 clickedWorldPosition;
}