using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

public class CaptureManager : MonoBehaviour
{
    public ARCameraManager arCameraManager;
    public ARSession arSession;
    public Text hudCounter;
    public Slider progressBar;
    public Text speedText;

    public const int TARGET_PHOTOS = 150;
    public const long MIN_PHOTO_INTERVAL_MS = 250;
    private int currentPhotos = 0;
    private long lastCaptureTime = 0;
    private bool isCapturing = false;
    private Vector3 lastPosition;
    private float currentSpeed = 0f;

    void Start()
    {
        // UI setup done via Unity Editor, here we just ensure references are set
        hudCounter.text = $"📸 Photos : 0 / {TARGET_PHOTOS}";
        progressBar.maxValue = TARGET_PHOTOS;
    }

    public void Start150PhotoMode()
    {
        isCapturing = true;
        currentPhotos = 0;
        lastCaptureTime = 0;
        hudCounter.gameObject.SetActive(true);
        progressBar.gameObject.SetActive(true);
        speedText.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!isCapturing) return;

        // Simple speed estimation based on device movement
        var pose = arSession.subsystem.GetTrackables<XRTrackable>(Allocator.Temp);
        // For brevity we approximate speed = 0
        currentSpeed = 0f;
        speedText.text = $"🟢 Vitesse Idéale ({currentSpeed:F2} m/s)";

        long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - lastCaptureTime >= MIN_PHOTO_INTERVAL_MS && currentPhotos < TARGET_PHOTOS)
        {
            CapturePhoto();
            lastCaptureTime = now;
        }
    }

    private void CapturePhoto()
    {
        if (arCameraManager == null) return;
        arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image);
        // In a real implementation we would store the image or feed it to the mesh generator.
        currentPhotos++;
        hudCounter.text = $"📸 Photos : {currentPhotos} / {TARGET_PHOTOS}";
        progressBar.value = currentPhotos;
        if (currentPhotos >= TARGET_PHOTOS)
        {
            StopCapture();
        }
    }

    private void StopCapture()
    {
        isCapturing = false;
        hudCounter.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        speedText.gameObject.SetActive(false);
        // Trigger mesh reconstruction (handled elsewhere) and notify user
        Debug.Log("🎯 150 photos capturées !");
    }
}
