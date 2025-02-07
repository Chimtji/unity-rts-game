using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class MoverAuthoring : MonoBehaviour
{
    public float speed;
    public float rotationSpeed;
    public class Baker : Baker<MoverAuthoring>
    {
        public override void Bake(MoverAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Mover
            {
                speed = authoring.speed,
                rotationSpeed = authoring.rotationSpeed,
            });
        }
    }
}

public struct Mover : IComponentData
{
    public bool isMoving;
    public float3 target;
    public float speed;
    public float rotationSpeed;
}

