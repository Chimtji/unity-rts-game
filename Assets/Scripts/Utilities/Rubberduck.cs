using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// This class is only used for debugging and is called rubberduck, 
/// because the Debug class is taken
/// </summary>
public static class Rubberduck
{
    private static float lifespan = 50f;
    private static Color color = Color.red;
    private static int segments = 32;
    public static void DrawLine(float3 from, float3 to)
    {
        Debug.DrawLine(from, to, color, lifespan);
    }
    public static void DrawSphere(float3 position, float radius)
    {
        // Draw the horizontal circle (XZ plane)
        for (int i = 0; i < segments; i++)
        {
            float theta1 = (2 * math.PI / segments) * i;
            float theta2 = (2 * math.PI / segments) * (i + 1);

            float3 point1 = position + new float3(math.cos(theta1), 0, math.sin(theta1)) * radius;
            float3 point2 = position + new float3(math.cos(theta2), 0, math.sin(theta2)) * radius;

            Debug.DrawLine(point1, point2, color, lifespan);
        }

        // Draw the vertical circles (XY and YZ planes)
        for (int i = 0; i < segments; i++)
        {
            float theta1 = (2 * math.PI / segments) * i;
            float theta2 = (2 * math.PI / segments) * (i + 1);

            // XY plane
            float3 point1XY = position + new float3(math.cos(theta1), math.sin(theta1), 0) * radius;
            float3 point2XY = position + new float3(math.cos(theta2), math.sin(theta2), 0) * radius;
            Debug.DrawLine(point1XY, point2XY, color, lifespan);

            // YZ plane
            float3 point1YZ = position + new float3(0, math.cos(theta1), math.sin(theta1)) * radius;
            float3 point2YZ = position + new float3(0, math.cos(theta2), math.sin(theta2)) * radius;
            Debug.DrawLine(point1YZ, point2YZ, color, lifespan);
        }
    }

    public static void DrawBox(float3 position, float3 size)
    {
        // Calculate the half extents of the box
        float3 halfSize = size * 0.5f;

        // Compute the 8 corners of the box
        float3[] corners = new float3[8];
        corners[0] = position + new float3(-halfSize.x, -halfSize.y, -halfSize.z);
        corners[1] = position + new float3(halfSize.x, -halfSize.y, -halfSize.z);
        corners[2] = position + new float3(halfSize.x, -halfSize.y, halfSize.z);
        corners[3] = position + new float3(-halfSize.x, -halfSize.y, halfSize.z);
        corners[4] = position + new float3(-halfSize.x, halfSize.y, -halfSize.z);
        corners[5] = position + new float3(halfSize.x, halfSize.y, -halfSize.z);
        corners[6] = position + new float3(halfSize.x, halfSize.y, halfSize.z);
        corners[7] = position + new float3(-halfSize.x, halfSize.y, halfSize.z);

        // Draw the bottom face (corners 0-3)
        Debug.DrawLine(corners[0], corners[1], color, lifespan);
        Debug.DrawLine(corners[1], corners[2], color, lifespan);
        Debug.DrawLine(corners[2], corners[3], color, lifespan);
        Debug.DrawLine(corners[3], corners[0], color, lifespan);

        // Draw the top face (corners 4-7)
        Debug.DrawLine(corners[4], corners[5], color, lifespan);
        Debug.DrawLine(corners[5], corners[6], color, lifespan);
        Debug.DrawLine(corners[6], corners[7], color, lifespan);
        Debug.DrawLine(corners[7], corners[4], color, lifespan);

        // Draw the vertical edges connecting top and bottom faces
        Debug.DrawLine(corners[0], corners[4], color, lifespan);
        Debug.DrawLine(corners[1], corners[5], color, lifespan);
        Debug.DrawLine(corners[2], corners[6], color, lifespan);
        Debug.DrawLine(corners[3], corners[7], color, lifespan);
    }
}

