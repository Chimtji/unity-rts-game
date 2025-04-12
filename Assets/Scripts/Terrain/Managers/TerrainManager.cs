using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    /// <summary>
    /// Temporary Material Assignment
    /// This needs to come from somewhere else, so this terrain is more dynamic
    /// </summary>
    public Material TEMP_MATERIAL_REF;

    public static TerrainManager Instance { get; private set; }

    public List<GameObject> chunksGameobjectsList = new List<GameObject>();

    public void Awake()
    {
        Instance = this;
    }

    public void SetOverlay(GameObject chunkObject, Material material, NativeArray<float2> uvs)
    {
        MeshRenderer chunkMeshRenderer = chunkObject.GetComponent<MeshRenderer>();
        chunkMeshRenderer.sharedMaterial = material;

        MeshFilter chunkMeshFilter = chunkObject.GetComponent<MeshFilter>();
        Mesh chunkMesh = chunkMeshFilter.mesh;

        chunkMesh.uv = Utils.ToVector2Array(uvs);
        chunkMesh.RecalculateNormals();
        chunkMesh.RecalculateBounds();
    }

    public void ResetOverlay()
    {
        // EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        // NativeArray<Entity> chunkEntities = GetChunks();

        // for (int i = 0; i < chunksGameobjectsList.Count; i++)
        // {
        //     // Reset Material
        //     GameObject chunkObject = chunksGameobjectsList[i];
        //     MeshRenderer chunkMeshRenderer = chunkObject.GetComponent<MeshRenderer>();
        //     chunkMeshRenderer.sharedMaterial = TEMP_MATERIAL_REF;

        //     // Reset UVS
        //     Entity chunkEntity = chunkEntities[i];
        //     TerrainChunkData terrainChunkData = entityManager.GetComponentData<TerrainChunkData>(chunkEntity);

        //     MeshFilter chunkMeshFilter = chunkObject.GetComponent<MeshFilter>();
        //     Mesh chunkMesh = chunkMeshFilter.mesh;
        //     chunkMesh.uv = Utils.ToVector2Array(terrainChunkData.uvs);

        //     chunkMesh.RecalculateNormals();
        //     chunkMesh.RecalculateBounds();
        // }

        // chunkEntities.Dispose();
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

    private NativeArray<Entity> GetChunks()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery chunkEntitiesQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<TerrainChunkData>().Build(entityManager);
        NativeArray<Entity> chunkEntities = chunkEntitiesQuery.ToEntityArray(Allocator.Temp);

        chunkEntitiesQuery.Dispose();

        return chunkEntities;
    }
}