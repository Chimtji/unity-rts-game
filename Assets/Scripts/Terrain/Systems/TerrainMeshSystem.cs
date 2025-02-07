using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial struct TerrainMeshSystem : ISystem
{
    public const int WORLD_SIZE = 1;
    public const int MESH_SIZE = 100;
    public const int SCALE = 1;
    public const int SIDE_SIZE = 2;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        NativeHashMap<float2, TerrainChunkData> chunkMap = new NativeHashMap<float2, TerrainChunkData>(WORLD_SIZE * WORLD_SIZE, Allocator.Domain);

        for (int x = 0; x < WORLD_SIZE; x++)
        {
            for (int z = 0; z < WORLD_SIZE; z++)
            {
                int sizeWithSide = MESH_SIZE + SIDE_SIZE;

                NativeArray<float3> vertices = CreateVertices(sizeWithSide, SCALE);
                NativeArray<float2> uvs = CreateUVs(vertices, sizeWithSide, SCALE);
                NativeArray<int> triangles = CreateTriangles(sizeWithSide);
                int index = CalculateIndex(x, z, WORLD_SIZE);

                TerrainChunkData chunkData = new TerrainChunkData
                {
                    width = MESH_SIZE - 1,
                    height = MESH_SIZE - 1,
                    scale = SCALE,
                    index = index,
                    vertices = vertices,
                    uvs = uvs,
                    triangles = triangles,
                    worldPosition = CalculateWorldPosition(new float2(x, z)),
                    chunkPosition = new TerrainChunkPositionData
                    {
                        x = x,
                        y = z,
                    },
                };
                chunkMap[index] = chunkData;
                Entity chunkEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<TerrainChunkData>(chunkEntity);
                state.EntityManager.SetComponentData(chunkEntity, chunkData);

            }
        }

        state.EntityManager.AddComponent<TerrainMeshSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle, new TerrainMeshSystemData
        {
            scale = SCALE,
            size = MESH_SIZE * WORLD_SIZE,
            chunkMap = chunkMap,
        });

        // CreateSideColliders(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        RefRW<TerrainMeshSystemData> terrainMeshSystemData = SystemAPI.GetComponentRW<TerrainMeshSystemData>(state.SystemHandle);

        for (int i = 0; i < terrainMeshSystemData.ValueRW.chunkMap.Count; i++)
        {
            TerrainChunkData chunkMap = terrainMeshSystemData.ValueRW.chunkMap[i];
            chunkMap.uvs.Dispose();
            chunkMap.triangles.Dispose();
            chunkMap.vertices.Dispose();
        }

        terrainMeshSystemData.ValueRW.chunkMap.Dispose();
    }

    private void CreateSideColliders(ref SystemState state)
    {
        for (int i = 0; i < 4; i++)
        {
            int sideSize = MESH_SIZE * WORLD_SIZE;

            float3 colliderSize = float3.zero;
            float3 colliderPosition = float3.zero;

            // Bottom side
            if (i == 0)
            {
                colliderSize = new float3(sideSize, 1, 1);
                colliderPosition = new float3(sideSize / 2f, colliderSize.y / 2f, -(colliderSize.z / 2f));
                // Rubberduck.DrawBox(colliderPosition, colliderSize);
            }
            // Top side
            else if (i == 1)
            {
                colliderSize = new float3(sideSize, 1, 1);
                colliderPosition = new float3(sideSize / 2f, colliderSize.y / 2f, sideSize + (colliderSize.z / 2f));
                // Rubberduck.DrawBox(colliderPosition, colliderSize);
            }
            // Left side
            else if (i == 2)
            {
                colliderSize = new float3(1, 1, sideSize);
                colliderPosition = new float3(-(colliderSize.x / 2f), colliderSize.y / 2f, sideSize / 2f);
                // Rubberduck.DrawBox(colliderPosition, colliderSize);
            }
            // Right side
            else if (i == 3)
            {
                colliderSize = new float3(1, 1, sideSize);
                colliderPosition = new float3(sideSize + (colliderSize.x / 2f), colliderSize.y / 2f, sideSize / 2f);
                // Rubberduck.DrawBox(colliderPosition, colliderSize);
            }

            Entity edgeColliderPrefab = state.EntityManager.CreateEntity();
            AddPhysicsCollider(edgeColliderPrefab, state.EntityManager, colliderPosition, colliderSize);
            state.EntityManager.AddComponentData(edgeColliderPrefab, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1f
            });
        }
    }

    private void AddPhysicsCollider(Entity entity, EntityManager entityManager, float3 center, float3 size)
    {
        // Create a BoxCollider BlobAssetReference
        BlobAssetReference<Collider> boxCollider = BoxCollider.Create(
            new BoxGeometry
            {
                Center = center,
                Size = size,
                Orientation = quaternion.identity
            },
            new CollisionFilter
            {
                BelongsTo = ~0u,   // Default collision layer
                CollidesWith = ~0u // Collides with everything
            }
        );

        // Add the PhysicsCollider component to the entity
        entityManager.AddComponentData(entity, new PhysicsCollider
        {
            Value = boxCollider
        });
    }

    private NativeArray<float2> CreateUVs(NativeArray<float3> vertices, int size, float scale)
    {
        NativeArray<float2> uvs = new NativeArray<float2>(vertices.Length, Allocator.Domain);
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new float2(vertices[i].x / (size * scale), vertices[i].z / (size * scale));
        }

        return uvs;
    }

    private NativeArray<int> CreateTriangles(int size)
    {
        NativeArray<int> triangles = new NativeArray<int>(size * size * 6, Allocator.Domain);
        int index = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int vertexIndex = y * (size + 1) + x;

                // First triangle
                triangles[index++] = vertexIndex;
                triangles[index++] = vertexIndex + size + 1;
                triangles[index++] = vertexIndex + 1;

                // Second triangle
                triangles[index++] = vertexIndex + 1;
                triangles[index++] = vertexIndex + size + 1;
                triangles[index++] = vertexIndex + size + 2;
            }
        }

        return triangles;
    }

    private NativeArray<float3> CreateVertices(int size, float scale)
    {
        NativeArray<float3> vertices = new NativeArray<float3>((size + 1) * (size + 1), Allocator.Domain);
        int index = 0;
        for (int y = 0; y <= size; y++)
        {
            for (int x = 0; x <= size; x++)
            {
                float3 position = new float3(x, 0, y);
                position = IncludeSidesCalculation(position, size);

                vertices[index++] = new float3(position.x * scale, position.y, position.z * scale);
            }
        }

        return vertices;

    }

    private float3 IncludeSidesCalculation(float3 position, int size)
    {
        float xPos = position.x;
        float yPos = position.y;
        float zPos = position.z;

        if (position.x == 0)
        {
            yPos = -1;
            xPos = 1;
        }
        if (position.z == 0)
        {
            yPos = -1;
            zPos = 1;
        }
        if (position.x == size)
        {
            yPos = -1;
            xPos = size - 1;
        }
        if (position.z == size)
        {
            yPos = -1;
            zPos = size - 1;
        }

        return new float3(xPos, yPos, zPos);
    }

    private static int CalculateIndex(int x, int y, int width)
    {
        return x + y * width;
    }

    private float3 CalculateWorldPosition(float2 coordinate)
    {
        float2 pos = coordinate * MESH_SIZE / SCALE;
        return new float3(pos.x - (SIDE_SIZE / 2), 0f, pos.y - (SIDE_SIZE / 2));
    }

}