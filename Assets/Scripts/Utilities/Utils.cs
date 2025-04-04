using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public static class Utils
{
    public static GameObject GetGameObjectByName(string name)
    {
        GameObject obj = GameObject.Find(name);
        return obj;
    }

    public static TextMesh CreateWorldText(string text, float3 position, string name = "world_text", Transform container = null)
    {
        GameObject gameObject = new GameObject(name, typeof(TextMesh));

        Transform transform = gameObject.transform;
        transform.localPosition = position;
        transform.SetParent(container);

        TextMesh textMesh = gameObject.GetComponent<TextMesh>();
        textMesh.anchor = TextAnchor.UpperLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.text = text;
        textMesh.characterSize = 0.02f;
        textMesh.fontSize = 200;
        textMesh.color = Color.black;

        return textMesh;
    }
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
    /// Removes all children of name in the passed gameobject
    /// </summary>
    /// <param name="container">The gameobject which holds the objects to remove</param>
    /// <param name="name">the name of the children to delete</param>
    public static void ClearChildrenByName(GameObject container, string name)
    {
        int childCount = container.transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            GameObject child = container.transform.GetChild(i).gameObject;
            if (child.name == name)
            {
                RemoveObject(child);
            }
        }
    }

    /// <summary>
    /// Finds the object with the passed name and removes it from the game
    /// </summary>
    public static void RemoveObjectByNameInContainer(string name, GameObject container)
    {

        Transform obj = container.transform.Find(name);

        if (obj)
        {
            RemoveObject(obj.gameObject);
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
    /// Finds all objects with the passed name and removes it from the game
    /// </summary>
    public static void RemoveAllObjectsByName(string name)
    {
        GameObject[] objects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in objects)
        {
            if (obj.name == name)
            {
                RemoveObject(obj);
            }
        }
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

