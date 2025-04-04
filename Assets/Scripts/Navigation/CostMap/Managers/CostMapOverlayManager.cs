using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public enum OverlayType { Heatmap, Flowfield }

public class CostMapOverlayManager : MonoBehaviour
{
    public bool enable = false;
    public static CostMapOverlayManager Instance { get; private set; }

    public OverlayType overlayType;
    public Material heatMapMaterial;
    public Material flowFieldMaterial;
    private int showingMapId = 0;
    private bool showingOverlay = false;

    private TerrainManager terrainManager
    {
        get
        {
            return gameObject.GetComponent<TerrainManager>();
        }
    }

    public void Awake()
    {
        Instance = this;
    }

    public void LateUpdate()
    {
        if (!enable)
        {
            if (showingOverlay)
            {
                terrainManager.ResetOverlay();
            }

            return;
        }

        World world = World.DefaultGameObjectInjectionWorld;
        EntityManager entityManager = world.EntityManager;
        SystemHandle costMapSystemHandle = world.GetExistingSystem<CostMapSystem>();
        EntityQuery selectedEntitiesQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, SelectedTag>().Build(entityManager);

        // Do not proceed if available data is not valid
        if (costMapSystemHandle == SystemHandle.Null) return;
        if (!entityManager.HasComponent<CostMapSystemData>(costMapSystemHandle)) return;
        if (terrainManager.chunksGameobjectsList.Count == 0) return;
        if (selectedEntitiesQuery.CalculateEntityCount() <= 0) return;

        NativeArray<Entity> entityArray = selectedEntitiesQuery.ToEntityArray(Allocator.Temp);
        Entity firstSelectedEntity = entityArray[0];

        CostMapSystemData costMapSystemData = entityManager.GetComponentData<CostMapSystemData>(costMapSystemHandle);
        BlobAssetReference<CostMapSystem.CostMapBlob> costMapBlob = entityManager.GetComponentData<PathUser>(firstSelectedEntity).CostMapBlob;

        if (!costMapBlob.IsCreated) return;

        int nextMapId = costMapSystemData.nextMapId;

        if (showingMapId != nextMapId)
        {
            UpdateOverlay(costMapSystemData, ref costMapBlob.Value.nodes);
        }

        entityArray.Dispose();
    }

    private void UpdateOverlay(CostMapSystemData costMapSystemData, ref BlobArray<CostMapNodeData> nodesOfMap)
    {
        Material material = heatMapMaterial;

        foreach (GameObject chunkObject in terrainManager.chunksGameobjectsList)
        {
            bool hasUpdated = false;
            MeshFilter chunkMeshFilter = chunkObject.GetComponent<MeshFilter>();
            Mesh chunkMesh = chunkMeshFilter.mesh;
            Vector2[] uvs = new Vector2[chunkMesh.vertexCount];

            for (int i = 0; i < nodesOfMap.Length; i++)
            {
                CostMapNodeData gridNodeData = nodesOfMap[i];

                if (!gridNodeData.enabled) continue;

                int2 gridPosition = gridNodeData.terrainGridPosition;

                switch (overlayType)
                {
                    case OverlayType.Flowfield:
                        (Vector2[] flowFieldUvs, Material flowFieldMaterial) = CalculateFlowField(gridNodeData, gridPosition, uvs);
                        uvs = flowFieldUvs;
                        material = flowFieldMaterial;
                        break;

                    case OverlayType.Heatmap:
                        (Vector2[] heatMapUvs, Material heatMapMaterial) = CalculateHeatmap(gridNodeData, gridPosition, uvs);
                        uvs = heatMapUvs;
                        material = heatMapMaterial;
                        break;
                }

                hasUpdated = true;
            }

            if (hasUpdated)
            {
                terrainManager.SetOverlay(chunkObject, material, uvs);
                showingMapId = costMapSystemData.nextMapId;
                showingOverlay = true;
            }
        }
    }

    private (Vector2[] uvs, Material material) CalculateHeatmap(CostMapNodeData nodeData, int2 gridPosition, Vector2[] uvs)
    {
        float costNormalized = (float)nodeData.bestCost / 100;
        float2 uv = new float2(costNormalized, 0f);
        Vector2[] calculatedUvs = TerrainMeshSystem.SetSharedUVOnMeshTile(gridPosition, uv, uvs);

        return (calculatedUvs, heatMapMaterial);
    }

    private (Vector2[] uvs, Material material) CalculateFlowField(CostMapNodeData nodeData, int2 gridPosition, Vector2[] uvs)
    {
        int SpritesInMaterial = 11;
        int width = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial;
        int height = TerrainMeshSystem.MESH_SIZE;
        int textureWidth = TerrainMeshSystem.MESH_SIZE;
        int textureHeight = TerrainMeshSystem.MESH_SIZE;
        int x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 1;
        int y = 0;

        // Wall
        if (nodeData.bestCost == CostMapSystem.WALL_COST)
        {
            x = 0;
        }

        // Target
        else if (nodeData.bestCost == 0)
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 1;
        }

        // Up
        else if (nodeData.vector.Equals(new float2(0, 1)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 8;
        }

        // Down
        else if (nodeData.vector.Equals(new float2(0, -1)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 9;
        }

        // Right
        else if (nodeData.vector.Equals(new float2(1, 0)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 6;
        }

        // Left
        else if (nodeData.vector.Equals(new float2(-1, 0)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 7;
        }

        // Top Right
        else if (nodeData.vector.Equals(new float2(1, 1)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 5;
        }
        // Bottom Left
        else if (nodeData.vector.Equals(new float2(-1, -1)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 2;
        }
        // Top Left
        else if (nodeData.vector.Equals(new float2(-1, 1)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 4;
        }
        // Bottom Right
        else if (nodeData.vector.Equals(new float2(1, -1)))
        {
            x = TerrainMeshSystem.MESH_SIZE / SpritesInMaterial * 3;
        }

        Vector2[] calculatedUvs = TerrainMeshSystem.SetPartOfTextureOnMeshTile(gridPosition, x, y, uvs, width, height, textureWidth, textureHeight);
        Material material = flowFieldMaterial;

        return (calculatedUvs, material);
    }
}