using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    /// <summary>
    /// Temporary Material Assignment
    /// </summary>
    public Material material;

    public static TerrainManager Instance { get; private set; }

    private List<GameObject> chunksGameobjectsList = new List<GameObject>();

    public void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        Generate();
    }

    public void Update()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery chunkEntitiesQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<TerrainChunkData>().Build(entityManager);
        NativeArray<Entity> chunkEntities = chunkEntitiesQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < chunkEntities.Length; i++)
        {
            Entity chunkEntity = chunkEntities[i];
            TerrainChunkData terrainChunkData = entityManager.GetComponentData<TerrainChunkData>(chunkEntity);

            Mesh mesh = AssembleMesh(terrainChunkData.vertices, terrainChunkData.uvs, terrainChunkData.triangles);
            MeshCollider chunkCollider = chunksGameobjectsList[i].GetComponent<MeshCollider>();
            chunkCollider.sharedMesh = mesh;

        }
    }

    public void Generate()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery chunkEntitiesQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<TerrainChunkData>().Build(entityManager);
        NativeArray<Entity> chunkEntities = chunkEntitiesQuery.ToEntityArray(Allocator.Temp);

        for (int i = 0; i < chunkEntities.Length; i++)
        {
            Entity chunkEntity = chunkEntities[i];
            TerrainChunkData terrainChunkData = entityManager.GetComponentData<TerrainChunkData>(chunkEntity);

            GameObject gameObject = new GameObject("Terrain Chunk");
            gameObject.transform.position = terrainChunkData.worldPosition;
            gameObject.transform.parent = transform;

            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            Mesh mesh = AssembleMesh(terrainChunkData.vertices, terrainChunkData.uvs, terrainChunkData.triangles);

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();

            chunksGameobjectsList.Add(gameObject);
        }
    }

    private Mesh AssembleMesh(NativeArray<float3> vertices, NativeArray<float2> uvs, NativeArray<int> triangles)
    {
        Mesh mesh = new Mesh
        {
            vertices = Utils.ToVector3Array(vertices),
            triangles = Utils.ToIntArray(triangles),
            uv = Utils.ToVector2Array(uvs),
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}