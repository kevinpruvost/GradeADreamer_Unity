using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Dummiesman;


public class ModelAssigner : MonoBehaviour
{

    void Start()
    {
    }

    static public void AssignModel(string modelPath, GameObject targetObject)
    {
        // Sets to relative path of the project
        if (!modelPath.StartsWith("Assets/"))
        {
            // Cut part before "Assets/"
            modelPath = modelPath.Substring(modelPath.IndexOf("Assets/"));
            // Also cut Assets/ from the beginning
            // modelPath = modelPath.Substring(7);
        }

        string objFilePath = $"{modelPath}.obj";
        string mtlFilePath = $"{modelPath}.mtl";
        var loadedObj = new OBJLoader().Load(objFilePath, mtlFilePath);
        var child = loadedObj.transform.GetChild(0);

        Mesh modelMesh = child.GetComponent<MeshFilter>().sharedMesh;
        Material material = child.GetComponent<MeshRenderer>().sharedMaterial;

        if (modelMesh == null)
        {
            Debug.LogError("Model not found or failed to load.");
            return;
        }

        if (material == null)
        {
            Debug.LogError("Material not found or failed to load.");
            return;
        }

        // Get the MeshFilter and MeshRenderer components
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
        {
            Debug.LogError("MeshFilter or MeshRenderer not found on the target object.");
            return;
        }

        // Assign the loaded mesh to the MeshFilter
        meshRenderer.material = material;
        meshFilter.mesh = modelMesh;

        DestroyImmediate(loadedObj);
    }
}
