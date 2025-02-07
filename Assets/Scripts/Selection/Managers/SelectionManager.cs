using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The sole purpose of this manager is to listen for screen interactions 
/// in relation to selecting and enable/disable selected tags on matching entities.
/// This manager is therefore a bridge between gameobject and entities world.
/// A system will handle what the 'selected' means.
/// </summary>

public class SelectionManager : MonoBehaviour
{
    /// <summary>
    /// The camera to calculate the selection from
    /// IMPORTANT: For some reason the collision script breaks with Far Clip Plane value at 1000. It works as intended at 100.
    /// </summary>
    [SerializeField] private Camera selectionCamera;

    /// <summary>
    /// The Selection Layers that should be selectable
    /// </summary>
    [SerializeField] private PhysicsCategoryTags collidesWith;

    /// <summary>
    /// The Start of the Selection GUI Rect
    /// </summary>
    private Vector2 mouseStartPosition;

    /// <summary>
    /// If mouse is pressed and more than x from start position this is true
    /// </summary>
    private bool isDragging;

    private void OnEnable()
    {
        selectionCamera = selectionCamera == null ? Camera.main : selectionCamera;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseStartPosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.isPressed && !isDragging)
        {
            if (math.distance(mouseStartPosition, Mouse.current.position.ReadValue()) > 5)
            {
                isDragging = true;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (!isDragging)
            {
                SelectSingle(Keyboard.current.leftShiftKey.isPressed);
            }
            else
            {
                SelectArea(Keyboard.current.leftShiftKey.isPressed);
            }
            isDragging = false;
        }
    }

    private void OnGUI()
    {
        if (isDragging)
        {
            // For some reaseon we can't use the same rect for GUI as for the area selection.
            // This is obviously a logic mistake somewhere, but can't seem to find it.
            // This is of course error prone, so if we experience errors with the selection being off, this should get fixed.
            Rect selectionRect = SelectionGUI.GetScreenRect(mouseStartPosition, Mouse.current.position.ReadValue());
            SelectionGUI.DrawScreenRect(selectionRect, new Color(0.8f, 0.8f, 0.95f, 0.1f));
            SelectionGUI.DrawScreenRectBorder(selectionRect, 1, Color.blue);
        }
    }

    private void SelectSingle(bool additive = false)
    {
        // We get access to the Entity World
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        if (!additive)
        {
            DeselectAll(entityManager);
        }

        // We Create a ray from Selection Camera to clicked location
        var ray = selectionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // We Create Raycast data to define what to look for
        // If stuff is not getting selected:
        // - make sure SelectionManager has chosen the correct layers and the stuff is on those layers
        // - The stuff has the selectableTag
        RaycastInput raycastData = new RaycastInput
        {
            Start = ray.origin,
            End = ray.GetPoint(selectionCamera.farClipPlane),
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNITS_LAYER,
                GroupIndex = 0,
            }
        };

        // We cast a ray and get the hit
        if (collisionWorld.CastRay(raycastData, out Unity.Physics.RaycastHit raycastHit))
        {
            if (entityManager.HasComponent<SelectableTag>(raycastHit.Entity))
            {
                entityManager.SetComponentEnabled<SelectedTag>(raycastHit.Entity, true);
            }
        }
        ;
    }

    private void SelectArea(bool additive = false)
    {

        // We get access to the Entity World
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, SelectableTag>().Build(entityManager);
        NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.Temp);
        NativeArray<LocalTransform> transforms = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        if (!additive)
        {
            DeselectAll(entityManager);
        }

        // We go through all the transforms of the selectable entities and check if 
        // is inside the screen rect. their position (converted to screen position)
        // IMPORTANT: This means that we only select based on the root position (1 point) and not the mesh of the selectable
        for (int i = 0; i < transforms.Length; i++)
        {
            LocalTransform unitLocalTransform = transforms[i];
            Vector2 unitScreenPosition = Camera.main.WorldToScreenPoint(unitLocalTransform.Position);

            if (GetSelectionAreaRect().Contains(unitScreenPosition))
            {
                entityManager.SetComponentEnabled<SelectedTag>(entityArray[i], true);
            }
        }

        transforms.Dispose();
        entityArray.Dispose();
        entityQuery.Dispose();
    }

    private void DeselectAll(EntityManager entityManager)
    {
        // We get access to the Entity World
        EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<SelectedTag>().Build(entityManager);
        NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.Temp);

        // For each entity we flip the enabled on selectedTag
        for (int i = 0; i < entityArray.Length; i++)
        {
            entityManager.SetComponentEnabled<SelectedTag>(entityArray[i], false);
        }

        entityArray.Dispose();
        entityQuery.Dispose();
    }

    private Rect GetSelectionAreaRect()
    {
        Vector2 selectionEndMousePosition = Mouse.current.position.ReadValue();

        Vector2 lowerLeftCorner = new Vector2(
            Mathf.Min(mouseStartPosition.x, selectionEndMousePosition.x),
            Mathf.Min(mouseStartPosition.y, selectionEndMousePosition.y)
            );

        Vector2 upperRightCorner = new Vector2(
            Mathf.Max(mouseStartPosition.x, selectionEndMousePosition.x),
            Mathf.Max(mouseStartPosition.y, selectionEndMousePosition.y)
            );


        return new Rect(
            lowerLeftCorner.x, lowerLeftCorner.y,
            upperRightCorner.x - lowerLeftCorner.x,
            upperRightCorner.y - lowerLeftCorner.y
            );
    }
}
