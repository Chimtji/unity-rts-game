using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class MouseWorld : MonoBehaviour
{
    public static MouseWorld Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // Raycasts to normal gameobjects and does not hit entities
    // public Vector3 GetPosition()
    // {
    //     Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

    //     if (Physics.Raycast(mouseCameraRay, out RaycastHit raycastHit))
    //     {
    //         return raycastHit.point;
    //     }
    //     else
    //     {
    //         Debug.Log("did not hit anything");
    //         return Vector3.zero;
    //     }
    // }

    /// <summary>
    /// Raycasts from Cameray to mouse position and hits all entities
    /// Beware: This does not hit GameObjects
    /// </summary>
    public (float3, Entity) ReadClick()
    {
        UnityEngine.Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        PhysicsWorldSingleton physicsWorldSingleton = entityQuery.GetSingleton<PhysicsWorldSingleton>();

        RaycastInput input = new RaycastInput
        {
            Start = mouseCameraRay.origin,
            End = mouseCameraRay.origin + mouseCameraRay.direction * 99999f, // or however far you want to cast the ray
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            }
        };

        if (physicsWorldSingleton.CastRay(input, out Unity.Physics.RaycastHit hit))
        {
            return (hit.Position, hit.Entity);
        }
        else
        {
            Debug.Log("did not hit anything");
            return (float3.zero, Entity.Null);
        }

    }
}
