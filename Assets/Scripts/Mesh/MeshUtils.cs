using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshUtils
{
    public static Entity CreateMeshEntity(
        ref SystemState state,
        NativeArray<float3> vertices,
        NativeArray<float2> uvs,
        NativeArray<int> triangles,
        bool receiveShadows,
        UnityEngine.Material material,
        float3 worldPosition,
        ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off
        )
    {
        // Create Entity to hold mesh
        Entity meshEntity = state.EntityManager.CreateEntity();

        DynamicBuffer<TerrainVertexBufferData> vertexBuffer = state.EntityManager.AddBuffer<TerrainVertexBufferData>(meshEntity);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexBuffer.Add(new TerrainVertexBufferData { vertex = vertices[i] });
        }

        // Create regular mesh (Managed instance)
        Mesh mesh = new Mesh
        {
            vertices = vertices.Reinterpret<Vector3>().ToArray(),
            triangles = triangles.Reinterpret<int>().ToArray(),
            uv = uvs.Reinterpret<Vector2>().ToArray(),
        };

        int meshId = TerrainMeshStorage.meshes.Count;
        TerrainMeshStorage.meshes.Add(mesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Convert mesh to Entities Graphics format
        RenderMeshArray meshArray = new RenderMeshArray(new UnityEngine.Material[] { material }, new Mesh[] { mesh });
        RenderMeshDescription meshDescription = new RenderMeshDescription(
            shadowCastingMode,
            receiveShadows
        );

        // Add components to entity for it to render the mesh
        RenderMeshUtility.AddComponents(
            meshEntity,
            state.EntityManager,
            meshDescription,
            meshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

        // Set world position of entity
        state.EntityManager.AddComponentData(meshEntity, new LocalTransform
        {
            Position = worldPosition,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        // Attach the mesh bounds to the entity rendered mesh
        state.EntityManager.AddComponentData(meshEntity, new RenderBounds
        {
            Value = mesh.bounds.ToAABB()
        });

        // Add Collider to the mesh
        BlobAssetReference<Unity.Physics.Collider> colliderBlob = Unity.Physics.MeshCollider.Create(vertices, ToInt3Triangles(triangles));
        state.EntityManager.AddComponentData(meshEntity, new PhysicsCollider
        {
            Value = colliderBlob
        });

        // Add the entity to the physics world (without this any physics interaction wont work)
        state.EntityManager.AddComponent<PhysicsWorldIndex>(meshEntity);

        state.EntityManager.AddComponentData(meshEntity, new MeshReferenceData
        {
            meshId = meshId,
        });

        state.EntityManager.Instantiate(meshEntity);

        return meshEntity;
    }

    /// <summary>
    /// Converts a flat array of triangles into an int3 array of triangles
    /// Triangles as an in3 representation is used in Unity.Physics, whereas flat triangles array are used in UnityEngine
    /// </summary>
    /// <param name="flatTriangles">The flat array of triangles for a mesh</param>
    public static NativeArray<int3> ToInt3Triangles(NativeArray<int> flatTriangles)
    {
        // Triangles in format of a flat int array
        NativeArray<int> triangleIndices = flatTriangles;
        int triangleCount = triangleIndices.Length / 3;

        // Triangles in a format of triplet indices
        NativeArray<int3> tripletTriangles = new NativeArray<int3>(triangleCount, Allocator.Temp);

        // Convert flat array to int3s
        for (int i = 0; i < triangleCount; i++)
        {
            tripletTriangles[i] = new int3(
                triangleIndices[i * 3 + 0],
                triangleIndices[i * 3 + 1],
                triangleIndices[i * 3 + 2]
            );
        }

        return tripletTriangles;
    }

    /// <summary>
    /// Creates a persistent array of UVS based on vertices of a mesh
    /// </summary>
    /// <param name="vertices">The vertices created for the mesh</param>
    /// <param name="size">The size of the mesh</param>
    /// <param name="scale">The scale of the mesh</param>
    public static NativeArray<float2> CreateUVs(NativeArray<float3> vertices, int size, float scale)
    {
        NativeArray<float2> uvs = new NativeArray<float2>(vertices.Length, Allocator.Persistent);
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new float2(vertices[i].x / (size * scale), vertices[i].z / (size * scale));
        }

        return uvs;
    }

    /// <summary>
    /// Creates a persistent array of triangles for a mesh
    /// </summary>
    /// <param name="size">The size of the mesh</param>
    public static NativeArray<int> CreateTriangles(int size)
    {
        NativeArray<int> triangles = new NativeArray<int>(size * size * 6, Allocator.Persistent);
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

    /// <summary>
    /// Creates the vertices for a mesh based on size and scale
    /// </summary>
    /// <param name="size">The size of the mesh in tiles</param>
    /// <param name="scale">The scale of the mesh</param>
    /// <returns></returns>
    public static NativeArray<float3> CreateVertices(int size, float scale)
    {
        NativeArray<float3> vertices = new NativeArray<float3>(size * size * 6, Allocator.Persistent);

        int index = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float height = 0;
                float3 bottomLeft = new float3(x * scale, height, y * scale);
                float3 bottomRight = new float3((x + 1) * scale, height, y * scale);
                float3 topLeft = new float3(x * scale, height, (y + 1) * scale);
                float3 topRight = new float3((x + 1) * scale, height, (y + 1) * scale);

                // First triangle
                vertices[index++] = bottomLeft;
                vertices[index++] = topLeft;
                vertices[index++] = bottomRight;

                // Second triangle
                vertices[index++] = bottomRight;
                vertices[index++] = topLeft;
                vertices[index++] = topRight;
            }
        }

        return vertices;
    }

    public static NativeArray<float2> SetSharedUVOnMeshTile(int2 gridPosition, float2 uv, NativeArray<float2> uvs, int meshTileSize)
    {
        int tileIndex = gridPosition.y * meshTileSize + gridPosition.x;
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

    public static NativeArray<float2> SetPartOfTextureOnMeshTile(
        int2 gridPosition,
        int textureX,
        int textureY,
        NativeArray<float2> uvs,
        int width,
        int height,
        int textureWidth,
        int textureHeight,
        int meshTileSize)
    {
        int tileIndex = gridPosition.y * meshTileSize + gridPosition.x;
        int vertexIndex = tileIndex * 6;

        int firstBottomLeft = vertexIndex + 0;
        int firstTopLeft = vertexIndex + 1;
        int firstBottomRight = vertexIndex + 2;
        int secondBottomRight = vertexIndex + 3;
        int secondTopLeft = vertexIndex + 4;
        int secondTopRight = vertexIndex + 5;

        // Bottom Left Triangle
        uvs[firstBottomLeft] = ConvertPixelsToUVCoordinate(textureX, textureY, textureWidth, textureHeight);
        uvs[firstTopLeft] = ConvertPixelsToUVCoordinate(textureX, textureY + height, textureWidth, textureHeight);
        uvs[firstBottomRight] = ConvertPixelsToUVCoordinate(textureX + width, textureY, textureWidth, textureHeight);

        // Top Right Triangle
        uvs[secondBottomRight] = ConvertPixelsToUVCoordinate(textureX + width, textureY, textureWidth, textureHeight);
        uvs[secondTopLeft] = ConvertPixelsToUVCoordinate(textureX, textureY + height, textureWidth, textureHeight);
        uvs[secondTopRight] = ConvertPixelsToUVCoordinate(textureX + width, textureY + height, textureWidth, textureHeight);

        return uvs;
    }

    public static float2 ConvertPixelsToUVCoordinate(int x, int y, int textureWidth, int textureHeight)
    {
        return new float2((float)x / textureWidth, (float)y / textureHeight);
    }

    /// <summary>
    /// Gives a list of vertices sharing the same mesh coordinate (x,z). 
    /// This only makes sense for meshes with unique vertex positions.
    /// Don't use if passed vertices share vertex positions. 
    /// </summary>
    /// <param name="vertices">The array of vertices to look through</param>
    /// <param name="position">The coordinate to check for</param>
    /// <param name="tolerance">The margin to check for if its the same position. Default 0.0001f</param>
    /// <returns>A list of vertex index's at the given mesh coordinate </returns>
    public static NativeList<int> GetVerticesIndexAtPosition(NativeArray<float3> vertices, float2 position, float tolerance = 0.0001f)
    {
        NativeList<int> indices = new NativeList<int>(Allocator.Temp);
        for (int i = 0; i < vertices.Length; i++)
        {
            float3 v = vertices[i];
            if (math.abs(v.x - position.x) < tolerance && math.abs(v.z - position.y) < tolerance)
            {
                indices.Add(i);
            }
        }
        return indices;
    }

    /// <summary>
    /// Rounds the point to nearest mesh coordinate (x,y) - does not take height into account.
    /// This only works when mesh scale is 1.
    /// </summary>
    /// <param name="point">the point in local position</param>
    /// <returns>Mesh coordinate in x,y without height</returns>
    public static float2 GetNearestMeshCoord(float3 point)
    {
        return new float2(math.round(point.x), math.round(point.z));
    }
}