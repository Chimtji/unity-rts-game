using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

/// <summary>
/// This System handles generation of the Terrain Mesh and handles all changes to it
/// 
/// -- Logic Approach --
/// This system should be the only one knowing about chunks and how the mesh is split up for performance optimiziation.
/// Consumers of this terrain should not think about which chunk they want to affect etc. 
/// This system should handle all translations from world points to local chunk positions
///
/// </summary>
public partial struct TerrainShapeSystem : ISystem
{
    public const int WORLD_SIZE = 2;
    public const int MESH_SIZE_IN_TILES = 10;
    public const int MESH_SIZE_IN_POINTS = MESH_SIZE_IN_TILES + 1;
    public const int SCALE = 1;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        NativeHashMap<float2, TerrainChunkData> chunkMap = new NativeHashMap<float2, TerrainChunkData>(WORLD_SIZE * WORLD_SIZE, Allocator.Domain);

        for (int x = 0; x < WORLD_SIZE; x++)
        {
            for (int z = 0; z < WORLD_SIZE; z++)
            {
                float2 chunkPosition = new float2(x, z);

                // We create the points and uvs used to make up the mesh. These points are just a flat plane
                // The arrays are persistent, so we can later access them in other systems
                NativeArray<float3> vertices = MeshUtils.CreateVertices(MESH_SIZE_IN_TILES, SCALE);
                NativeArray<float2> uvs = MeshUtils.CreateUVs(vertices, MESH_SIZE_IN_TILES, SCALE);
                NativeArray<int> triangles = MeshUtils.CreateTriangles(MESH_SIZE_IN_TILES);

                // We go through all vertices and add height to each vertex based on noise
                // We split up the logic like this to keep things simple, maintainable and readable.
                vertices = AddHeightToVertices(vertices, MESH_SIZE_IN_POINTS, SCALE, chunkPosition);

                // We create the mesh in entity world and instantiate it
                Entity meshEntity = MeshUtils.CreateMeshEntity(
                    ref state,
                    vertices,
                    uvs,
                    triangles,
                    false,
                    new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit")),
                    CalculateChunkWorldPosition(new float2(x, z))
                );

                // We create a chunkData component and create a chunk entity to hold all chunk related data
                TerrainChunkData chunkData = new TerrainChunkData
                {
                    meshEntity = meshEntity,
                    width = MESH_SIZE_IN_TILES,
                    height = MESH_SIZE_IN_TILES,
                    scale = SCALE,
                    index = CalculateChunkIndex(x, z, WORLD_SIZE),
                    worldPosition = CalculateChunkWorldPosition(chunkPosition),
                    chunkPosition = chunkPosition
                };

                Entity chunkEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<TerrainChunkData>(chunkEntity);
                state.EntityManager.SetComponentData(chunkEntity, chunkData);

                // We add the chunk to the chunk map
                chunkMap[chunkData.chunkPosition] = chunkData;
            }
        }

        state.EntityManager.AddComponent<TerrainMeshSystemData>(state.SystemHandle);
        state.EntityManager.SetComponentData(state.SystemHandle, new TerrainMeshSystemData
        {
            scale = SCALE,
            totalSize = MESH_SIZE_IN_TILES * WORLD_SIZE,
            chunkMap = chunkMap,
        });
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ControlsSystemData controlsSystemData = SystemAPI.GetSingleton<ControlsSystemData>();

        if (controlsSystemData.leftMouseButtonWasReleasedThisFrame)
        {

            ChangeHeightOnPoint(ref state, controlsSystemData.clickedWorldPosition);
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        RefRW<TerrainMeshSystemData> terrainMeshSystemData = SystemAPI.GetComponentRW<TerrainMeshSystemData>(state.SystemHandle);

        // for (int i = 0; i < terrainMeshSystemData.ValueRW.chunkMap.Count; i++)
        // {
        //     TerrainChunkData chunkMap = terrainMeshSystemData.ValueRW.chunkMap[i];
        //     chunkMap.uvs.Dispose();
        //     chunkMap.triangles.Dispose();
        //     chunkMap.vertices.Dispose();
        // }

        terrainMeshSystemData.ValueRW.chunkMap.Dispose();
    }

    private NativeArray<float3> AddHeightToVertices(NativeArray<float3> vertices, int size, float scale, float2 chunkPosition)
    {
        NativeArray<float> noiseMap = NoiseUtils.GenerateNoiseMap(MESH_SIZE_IN_POINTS, scale, 2, 2f, 1f, chunkPosition);
        for (int y = 0; y < MESH_SIZE_IN_POINTS; y++)
        {
            for (int x = 0; x < MESH_SIZE_IN_POINTS; x++)
            {
                float2 position = new float2(x, y);
                NativeList<int> verticesIndex = MeshUtils.GetVerticesIndexAtPosition(vertices, position);

                int noiseMapIndex = x + y * MESH_SIZE_IN_POINTS;
                float height = noiseMap[noiseMapIndex];

                foreach (int vertexIndex in verticesIndex)
                {
                    float3 vertex = vertices[vertexIndex];
                    vertex.y = height;
                    vertices[vertexIndex] = vertex;
                }

                verticesIndex.Dispose();
            }
        }

        noiseMap.Dispose();
        return vertices;
    }

    private int CalculateChunkIndex(int x, int y, int width)
    {
        return x + y * width;
    }

    private float3 CalculateChunkWorldPosition(float2 chunkPosition)
    {
        float2 pos = chunkPosition * MESH_SIZE_IN_TILES / SCALE;
        return new float3(pos.x, 0f, pos.y);
    }

    private float2 GetChunkPositionOfWorldPoint(float3 point)
    {
        float x = math.floor(point.x / MESH_SIZE_IN_TILES * SCALE);
        float y = math.floor(point.z / MESH_SIZE_IN_TILES * SCALE);

        return new float2(x, y);
    }

    private float3 WorldPointToLocalChunkPoint(float3 worldPoint, float2 chunkPosition)
    {
        float localPointX = worldPoint.x - (MESH_SIZE_IN_TILES * chunkPosition.x * SCALE);
        float localPointZ = worldPoint.z - (MESH_SIZE_IN_TILES * chunkPosition.y * SCALE);

        return new float3(localPointX, worldPoint.y, localPointZ);
    }

    public void ChangeHeightOnPoint(ref SystemState state, float3 clickedPosition)
    {
        TerrainMeshSystemData terrainMeshData = SystemAPI.GetSingleton<TerrainMeshSystemData>();
        NativeHashMap<float2, TerrainChunkData> chunkMap = terrainMeshData.chunkMap;

        float2 clickedChunkPosition = GetChunkPositionOfWorldPoint(clickedPosition);
        Entity chunkMeshEntity = chunkMap[clickedChunkPosition].meshEntity;

        float3 localChunkClickedPosition = WorldPointToLocalChunkPoint(clickedPosition, clickedChunkPosition);
        float2 nearestMeshCoord = MeshUtils.GetNearestMeshCoord(localChunkClickedPosition);
        DynamicBuffer<TerrainVertexBufferData> vertexBuffer = state.EntityManager.GetBuffer<TerrainVertexBufferData>(chunkMeshEntity);

        NativeList<float3> vertices = new NativeList<float3>(Allocator.Temp);
        for (int i = 0; i < vertexBuffer.Length; i++)
        {
            vertices.Add(vertexBuffer[i].vertex);
        }

        NativeList<int> verticesIndexAtMeshPoint = MeshUtils.GetVerticesIndexAtPosition(vertices.AsArray(), nearestMeshCoord);

        foreach (int vertexIndex in verticesIndexAtMeshPoint)
        {
            TerrainVertexBufferData item = vertexBuffer[vertexIndex];
            item.vertex.y = 0f;
            vertexBuffer[vertexIndex] = item;
            vertices[vertexIndex] = item.vertex;
        }

        MeshReferenceData meshReference = state.EntityManager.GetComponentData<MeshReferenceData>(chunkMeshEntity);
        int meshId = meshReference.meshId;

        Mesh mesh = TerrainMeshStorage.meshes[meshId];

        mesh.SetVertices(vertices.AsArray().Reinterpret<Vector3>().ToArray());

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
