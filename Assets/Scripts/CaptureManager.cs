using System;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Captures camera frames and ARKit poses to create an image set for on-device photogrammetry.
/// </summary>
public class CaptureManager : MonoBehaviour
{
    public ARCameraManager arCameraManager;
    public Text hudCounter;
    public Slider progressBar;
    public Text speedText;
    public Text statusText;

    public const int TARGET_PHOTOS = 150;
    public const int MIN_PHOTOS_FOR_RECONSTRUCTION = 20;
    public const long MIN_PHOTO_INTERVAL_MS = 350;

    public event Action<string> ScanCaptured;

    private int currentPhotos;
    private long lastCaptureTime;
    private bool isCapturing;
    private string scanFolder;
    private Vector3 lastPosition;
    private Vector3 lastCapturedPosition;
    private Quaternion lastCapturedRotation;
    private bool hasCapturedPose;

    [Serializable]
    private class PoseRecord
    {
        public string image;
        public string capturedAtUtc;
        public float[] position;
        public float[] rotation;
    }

    private void Start()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = TARGET_PHOTOS;
            progressBar.value = 0;
        }

        SetHud(false);
        SetStatus("Ready to scan. Move slowly around a textured object.");
    }

    public void Start150PhotoMode()
    {
        if (arCameraManager == null)
        {
            SetStatus("Camera is not ready.");
            return;
        }

        currentPhotos = 0;
        lastCaptureTime = 0;
        lastPosition = arCameraManager.transform.position;
        lastCapturedPosition = lastPosition;
        lastCapturedRotation = arCameraManager.transform.rotation;
        hasCapturedPose = false;
        string root = Path.Combine(Application.persistentDataPath, "MOAD3D", "Captures");
        scanFolder = Path.Combine(root, "scan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(scanFolder);
        File.WriteAllText(Path.Combine(scanFolder, "camera_poses.jsonl"), "");
        isCapturing = true;
        SetHud(true);
        SetStatus("Capturing photos. Keep moving slowly and cover every side.");
        UpdateCounter();
    }

    public void CancelCapture()
    {
        if (!isCapturing)
            return;

        isCapturing = false;
        SetHud(false);
        SetStatus($"Capture stopped. {currentPhotos} photos saved.");
        if (currentPhotos >= MIN_PHOTOS_FOR_RECONSTRUCTION)
            ScanCaptured?.Invoke(scanFolder);
        else if (currentPhotos > 0)
            SetStatus($"Saved {currentPhotos} photos. Capture at least {MIN_PHOTOS_FOR_RECONSTRUCTION} to build a 3D model.");
    }

    private void Update()
    {
        if (!isCapturing)
            return;

        if (ARSession.state != ARSessionState.SessionTracking)
        {
            if (speedText != null)
                speedText.text = "Finding camera tracking… hold the iPhone steady";
            return;
        }

        UpdateSpeedGuidance();

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - lastCaptureTime < MIN_PHOTO_INTERVAL_MS)
            return;

        Transform cameraTransform = arCameraManager.transform;
        if (hasCapturedPose
            && Vector3.Distance(cameraTransform.position, lastCapturedPosition) < 0.02f
            && Quaternion.Angle(cameraTransform.rotation, lastCapturedRotation) < 3f)
        {
            if (speedText != null)
                speedText.text = "Turn or move slightly to capture a new view";
            return;
        }

        if (TryCapturePhoto())
        {
            lastCaptureTime = now;
            lastCapturedPosition = cameraTransform.position;
            lastCapturedRotation = cameraTransform.rotation;
            hasCapturedPose = true;
            currentPhotos++;
            UpdateCounter();
            if (currentPhotos >= TARGET_PHOTOS)
                FinishCapture();
        }
    }

    private bool TryCapturePhoto()
    {
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return false;

        try
        {
            var parameters = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorY
            };

            int dataSize = image.GetConvertedDataSize(parameters);
            var pixelData = new NativeArray<byte>(dataSize, Allocator.Temp);
            try
            {
                image.Convert(parameters, pixelData);
                var texture = new Texture2D(parameters.outputDimensions.x, parameters.outputDimensions.y,
                    TextureFormat.RGBA32, false);
                texture.LoadRawTextureData(pixelData);
                texture.Apply(false, false);
                byte[] jpeg = texture.EncodeToJPG(88);
                Destroy(texture);

                string fileName = $"frame_{currentPhotos + 1:D4}.jpg";
                string path = Path.Combine(scanFolder, fileName);
                File.WriteAllBytes(path, jpeg);
                SavePose(fileName);
                return true;
            }
            finally
            {
                if (pixelData.IsCreated)
                    pixelData.Dispose();
            }
        }
        catch (Exception exception)
        {
            SetStatus("Could not save a camera frame: " + exception.Message);
            return false;
        }
        finally
        {
            image.Dispose();
        }
    }

    private void SavePose(string imageName)
    {
        Transform cameraTransform = arCameraManager.transform;
        PoseRecord record = new PoseRecord
        {
            image = imageName,
            capturedAtUtc = DateTime.UtcNow.ToString("o"),
            position = new[] { cameraTransform.position.x, cameraTransform.position.y, cameraTransform.position.z },
            rotation = new[] { cameraTransform.rotation.x, cameraTransform.rotation.y,
                cameraTransform.rotation.z, cameraTransform.rotation.w }
        };
        File.AppendAllText(Path.Combine(scanFolder, "camera_poses.jsonl"), JsonUtility.ToJson(record) + "\n");
    }

    private void UpdateSpeedGuidance()
    {
        if (speedText == null)
            return;

        Vector3 position = arCameraManager.transform.position;
        float speed = Vector3.Distance(position, lastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = position;

        if (speed > 0.45f)
            speedText.text = $"Move slower · {speed:F2} m/s";
        else if (speed < 0.03f)
            speedText.text = "Move around the object";
        else
            speedText.text = $"Good pace · {speed:F2} m/s";
    }

    private void UpdateCounter()
    {
        if (hudCounter != null)
            hudCounter.text = $"Photos {currentPhotos} / {TARGET_PHOTOS}";
        if (progressBar != null)
            progressBar.value = currentPhotos;
    }

    private void FinishCapture()
    {
        isCapturing = false;
        SetHud(false);
        SetStatus($"Saved {currentPhotos} photos. Build your 3D model when ready.");
        ScanCaptured?.Invoke(scanFolder);
    }

    private void SetHud(bool visible)
    {
        if (hudCounter != null) hudCounter.gameObject.SetActive(visible);
        if (progressBar != null) progressBar.gameObject.SetActive(visible);
        if (speedText != null) speedText.gameObject.SetActive(visible);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log("MOAD 3D Scan: " + message);
    }
}
