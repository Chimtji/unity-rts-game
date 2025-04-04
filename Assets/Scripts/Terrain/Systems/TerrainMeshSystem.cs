using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct TerrainMeshSystem : ISystem
{
    public const int WORLD_SIZE = 1;
    public const int MESH_SIZE = 100;
    public const int SCALE = 1;
    public const int SIDE_SIZE = 0;

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


    public static Vector2[] SetSharedUVOnMeshTile(int2 gridPosition, float2 uv, Vector2[] uvs)
    {
        int tileIndex = gridPosition.y * MESH_SIZE + gridPosition.x;
        int vertexIndex = tileIndex * 6;

        // Update UVs for the first triangle
        uvs[vertexIndex] = uv;
        uvs[vertexIndex + 1] = uv;
        uvs[vertexIndex + 2] = uv;

        // Update UVs for the second triangle
        uvs[vertexIndex + 3] = uv;
        uvs[vertexIndex + 4] = uv;
        uvs[vertexIndex + 5] = uv;

        return uvs;
    }

    public static Vector2[] SetPartOfTextureOnMeshTile(int2 gridPosition, int textureX, int textureY, Vector2[] uvs, int width, int height, int textureWidth, int textureHeight)
    {
        int tileIndex = gridPosition.y * MESH_SIZE + gridPosition.x;
        int vertexIndex = tileIndex * 6;

        // Bottom Left Triangle
        // --- bottom left
        uvs[vertexIndex + 0] = ConvertPixelsToUVCoordinate(textureX, textureY, textureWidth, textureHeight);
        // --- top left
        uvs[vertexIndex + 1] = ConvertPixelsToUVCoordinate(textureX, textureY + height, textureWidth, textureHeight);
        // --- bottom right
        uvs[vertexIndex + 2] = ConvertPixelsToUVCoordinate(textureX + width, textureY, textureWidth, textureHeight);

        // Top Right Triangle
        // --- bottom right
        uvs[vertexIndex + 3] = ConvertPixelsToUVCoordinate(textureX + width, textureY, textureWidth, textureHeight);
        // --- top left
        uvs[vertexIndex + 4] = ConvertPixelsToUVCoordinate(textureX, textureY + height, textureWidth, textureHeight);
        // --- top right
        uvs[vertexIndex + 5] = ConvertPixelsToUVCoordinate(textureX + width, textureY + height, textureWidth, textureHeight);

        return uvs;
    }

    public static float2 ConvertPixelsToUVCoordinate(int x, int y, int textureWidth, int textureHeight)
    {
        return new float2((float)x / textureWidth, (float)y / textureHeight);
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
                int vertexIndex = (y * size + x) * 6;

                // First triangle
                triangles[index++] = vertexIndex;
                triangles[index++] = vertexIndex + 1;
                triangles[index++] = vertexIndex + 2;

                // Second triangle
                triangles[index++] = vertexIndex + 3;
                triangles[index++] = vertexIndex + 4;
                triangles[index++] = vertexIndex + 5;
            }
        }

        return triangles;
    }

    private NativeArray<float3> CreateVertices(int size, float scale)
    {
        NativeArray<float3> vertices = new NativeArray<float3>(size * size * 6, Allocator.Domain);
        int index = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float3 v0 = new float3(x * scale, 0, y * scale);
                float3 v1 = new float3((x + 1) * scale, 0, y * scale);
                float3 v2 = new float3(x * scale, 0, (y + 1) * scale);
                float3 v3 = new float3((x + 1) * scale, 0, (y + 1) * scale);

                // First triangle
                vertices[index++] = v0;
                vertices[index++] = v2;
                vertices[index++] = v1;

                // Second triangle
                vertices[index++] = v1;
                vertices[index++] = v2;
                vertices[index++] = v3;
            }
        }

        return vertices;
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
