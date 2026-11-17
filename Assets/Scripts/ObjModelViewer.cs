using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Loads a local Wavefront OBJ mesh and lets the user inspect it in 3D.</summary>
public class ObjModelViewer : MonoBehaviour, IPointerDownHandler, IDragHandler, IScrollHandler
{
    private const string ModelRelativePath = "MOAD3D/Models/model.obj";
    private GameObject modelRoot;
    private Mesh loadedMesh;
    private Material loadedMaterial;
    private Camera previewCamera;
    private RawImage previewImage;
    private Text message;
    private RenderTexture renderTexture;
    private float yaw = 25f;
    private float pitch = -12f;
    private float distance = 3f;
    private Vector2 previousTouch;
    private float previousPinchDistance;

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void MOADPickOBJ(string destinationFolder);
#endif

    public void Configure(RawImage image, Camera camera, Text status)
    {
        previewImage = image;
        previewCamera = camera;
        message = status;
        renderTexture = new RenderTexture(768, 768, 16, RenderTextureFormat.ARGB32);
        renderTexture.name = "MOAD 3D Model Preview";
        previewImage.texture = renderTexture;
        previewCamera.targetTexture = renderTexture;
        previewCamera.enabled = false;
    }

    public void SetViewerVisible(bool visible)
    {
        if (previewCamera != null) previewCamera.enabled = visible;
    }

    public void LoadModel()
    {
        string path = Path.Combine(Application.persistentDataPath, ModelRelativePath);
        if (!File.Exists(path))
        {
            SetMessage("No model yet. Add an OBJ file at " + path);
            return;
        }

        try
        {
            Mesh mesh = ParseObj(path);
            if (modelRoot != null)
                Destroy(modelRoot);
            if (loadedMesh != null) Destroy(loadedMesh);
            if (loadedMaterial != null) Destroy(loadedMaterial);

            modelRoot = new GameObject("Loaded OBJ Model");
            MeshFilter filter = modelRoot.AddComponent<MeshFilter>();
            loadedMesh = mesh;
            filter.sharedMesh = loadedMesh;
            MeshRenderer renderer = modelRoot.AddComponent<MeshRenderer>();
            loadedMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.25f, 0.78f, 0.60f, 1f)
            };
            renderer.sharedMaterial = loadedMaterial;

            Bounds bounds = mesh.bounds;
            float largestSide = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scale = 1.6f / Mathf.Max(largestSide, 0.0001f);
            modelRoot.transform.localScale = Vector3.one * scale;
            modelRoot.transform.position = -bounds.center * scale;
            yaw = 25f;
            pitch = -12f;
            distance = 3f;
            SetMessage("OBJ model loaded · drag to rotate · pinch to zoom");
        }
        catch (Exception exception)
        {
            SetMessage("Could not load OBJ: " + exception.Message);
        }
    }

    public void ImportModel()
    {
#if UNITY_IOS && !UNITY_EDITOR
        string folder = Path.Combine(Application.persistentDataPath, "MOAD3D/Models");
        Directory.CreateDirectory(folder);
        SetMessage("Choose an OBJ model from Files…");
        MOADPickOBJ(folder);
#else
        SetMessage("OBJ import from Files is available in an iOS device build.");
#endif
    }

    public void OnOBJImportResult(string result)
    {
        if (result == "OK") LoadModel();
        else SetMessage("OBJ import failed: " + result);
    }

    private static Mesh ParseObj(string path)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        foreach (string line in File.ReadLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("v ", StringComparison.Ordinal))
            {
                string[] parts = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                    vertices.Add(new Vector3(Parse(parts[1]), Parse(parts[2]), Parse(parts[3])));
            }
            else if (trimmed.StartsWith("f ", StringComparison.Ordinal))
            {
                string[] parts = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;
                var face = new List<int>();
                for (int i = 1; i < parts.Length; i++)
                {
                    string indexText = parts[i].Split('/')[0];
                    int index = int.Parse(indexText, CultureInfo.InvariantCulture);
                    face.Add(index > 0 ? index - 1 : vertices.Count + index);
                }
                for (int i = 1; i + 1 < face.Count; i++)
                {
                    triangles.Add(face[0]);
                    triangles.Add(face[i]);
                    triangles.Add(face[i + 1]);
                }
            }
        }

        if (vertices.Count == 0 || triangles.Count == 0)
            throw new InvalidDataException("File must contain vertices and faces.");
        var mesh = new Mesh { indexFormat = vertices.Count > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);

    private void LateUpdate()
    {
        if (previewCamera == null || modelRoot == null) return;
        previewCamera.transform.position = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distance);
        previewCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (Input.touchCount == 2)
        {
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            float pinch = Vector2.Distance(first.position, second.position);
            if (previousPinchDistance > 0f)
                distance = Mathf.Clamp(distance * previousPinchDistance / Mathf.Max(pinch, 1f), 1.2f, 8f);
            previousPinchDistance = pinch;
        }
        else previousPinchDistance = 0f;
    }

    public void OnPointerDown(PointerEventData eventData) => previousTouch = eventData.position;

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - previousTouch;
        previousTouch = eventData.position;
        yaw += delta.x * 0.35f;
        pitch = Mathf.Clamp(pitch - delta.y * 0.25f, -80f, 80f);
    }

    public void OnScroll(PointerEventData eventData) => distance = Mathf.Clamp(distance - eventData.scrollDelta.y * 0.2f, 1.2f, 8f);

    private void SetMessage(string value)
    {
        if (message != null) message.text = value;
    }

    private void OnDestroy()
    {
        if (modelRoot != null) Destroy(modelRoot);
        if (loadedMesh != null) Destroy(loadedMesh);
        if (loadedMaterial != null) Destroy(loadedMaterial);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
