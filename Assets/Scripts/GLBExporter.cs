using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityGLTF;

public class GLBExporter : MonoBehaviour
{
    public static void ExportLatestScanToGlb()
    {
        // Find the ARMeshManager to get the latest generated mesh
        var meshManager = GameObject.FindObjectOfType<ARMeshManager>();
        if (meshManager == null)
        {
            Debug.LogWarning("ARMeshManager not found – no mesh to export.");
            return;
        }

        // Collect all generated meshes into a single GameObject
        var root = new GameObject("ExportRoot");
        foreach (var meshFilter in meshManager.GetComponentInChildren<MeshFilter>())
        {
            var go = new GameObject(meshFilter.name);
            go.transform.parent = root.transform;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = meshFilter.sharedMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = meshFilter.GetComponent<MeshRenderer>()?.sharedMaterials ?? new Material[0];
        }

        // Prepare export path – Documents/MOAD3D/
        string docsPath = Path.Combine(Application.persistentDataPath, "MOAD3D");
        if (!Directory.Exists(docsPath)) Directory.CreateDirectory(docsPath);
        string fileName = $"scan_{System.DateTime.Now:yyyyMMdd_HHmmss}.glb";
        string filePath = Path.Combine(docsPath, fileName);

        // Use UnityGLTF exporter (GLTFSceneExporter) to write GLB
        var exporter = new GLTFSceneExporter(root.transform, new ExportOptions { ExportOnlyVisible = false, ExportFormat = ExportFormat.Glb });
        exporter.SaveGLB(filePath);

        Debug.Log($"GLB exported to {filePath}");
        // Show iOS share sheet (requires Unity's native bridge – simplified here)
        // In a real project you would call a native plugin to invoke UIActivityViewController.
        // For demonstration we just log.
        Debug.Log("Share sheet would be presented here.");

        // Clean up temporary root object
        GameObject.DestroyImmediate(root);
    }
}
