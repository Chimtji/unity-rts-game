using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public static class Utils
{
    /// <summary>
    /// Removes all children of the passed gameobject
    /// </summary>
    /// <param name="container">The gameobject which holds the objects to remove</param>
    public static void ClearChildren(GameObject container)
    {
        int childCount = container.transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = container.transform.GetChild(i).gameObject;
            RemoveObject(child);
        }
    }

    /// <summary>
    /// Finds the object with the passed name and removes it from the game
    /// </summary>
    public static void RemoveObjectByName(string name)
    {
        GameObject obj = GameObject.Find(name);
        RemoveObject(obj);
    }



    /// <summary>
    /// Removes an object from the game
    /// </summary>
    public static void RemoveObject(GameObject obj)
    {

        if (Application.isPlaying)
        {
            GameObject.Destroy(obj);
        }
        else
        {
            GameObject.DestroyImmediate(obj);
        }
    }

    public static Vector3[] ToVector3Array(NativeArray<float3> nativeArray)
    {
        // Create a new managed array to hold the converted data
        Vector3[] resultArray = new Vector3[nativeArray.Length];

        // Convert each element from float3 to Vector3
        for (int i = 0; i < nativeArray.Length; i++)
        {
            resultArray[i] = (Vector3)nativeArray[i];
        }

        return resultArray;
    }

    public static Vector2[] ToVector2Array(NativeArray<float2> nativeArray)
    {
        // Create a new managed array to hold the converted data
        Vector2[] resultArray = new Vector2[nativeArray.Length];

        // Convert each element from float3 to Vector3
        for (int i = 0; i < nativeArray.Length; i++)
        {
            resultArray[i] = (Vector2)nativeArray[i];
        }

        return resultArray;
    }

    public static int[] ToIntArray(NativeArray<int> nativeArray)
    {
        // Create a new managed array to hold the converted data
        int[] resultArray = new int[nativeArray.Length];

        // Convert each element from float3 to Vector3
        for (int i = 0; i < nativeArray.Length; i++)
        {
            resultArray[i] = (int)nativeArray[i];
        }

        return resultArray;
    }
}

