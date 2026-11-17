using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reconstructs a captured photo set on supported iPhones and loads the OBJ result.</summary>
public class ScanReconstructionController : MonoBehaviour
{
    private Text statusText;
    private Button reconstructButton;
    private ObjModelViewer modelViewer;
    private string inputFolder;
    private bool isProcessing;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int MOADPhotogrammetryIsSupported();

    [DllImport("__Internal")]
    private static extern void MOADPhotogrammetryStart(string inputPath, string usdzPath, string objPath);
#endif

    public void Configure(Text status, Button button, ObjModelViewer viewer)
    {
        statusText = status;
        reconstructButton = button;
        modelViewer = viewer;
        if (reconstructButton != null)
            reconstructButton.gameObject.SetActive(false);
    }

    public void ClearInputFolder()
    {
        inputFolder = null;
        if (reconstructButton != null)
            reconstructButton.gameObject.SetActive(false);
    }

    public void SetInputFolder(string folder)
    {
        inputFolder = folder;
        if (reconstructButton != null)
        {
            reconstructButton.gameObject.SetActive(true);
            reconstructButton.interactable = true;
            Text label = reconstructButton.GetComponentInChildren<Text>();
            if (label != null) label.text = "Build 3D Model";
        }
        SetStatus("Photos saved. Tap Build 3D Model to create your scan.");
    }

    public void StartReconstruction()
    {
        if (isProcessing || string.IsNullOrEmpty(inputFolder) || !Directory.Exists(inputFolder))
        {
            SetStatus("Finish capturing at least 20 photos before building the model.");
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        if (MOADPhotogrammetryIsSupported() == 0)
        {
            SetStatus("This iPhone does not support on-device object reconstruction.");
            return;
        }

        string outputDirectory = Path.Combine(Application.persistentDataPath, "MOAD3D", "Models");
        Directory.CreateDirectory(outputDirectory);
        string usdzPath = Path.Combine(outputDirectory,
            "scan_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".usdz");
        string objPath = Path.Combine(outputDirectory, "model.obj");
        isProcessing = true;
        if (reconstructButton != null)
            reconstructButton.interactable = false;
        SetStatus("Building your 3D model on iPhone. Keep MOAD open.");
        MOADPhotogrammetryStart(inputFolder, usdzPath, objPath);
#else
        SetStatus("On-device model building runs on a supported iPhone with iOS 17 or later.");
#endif
    }

    public void OnNativeProgress(string payload)
    {
        string[] fields = (payload ?? "").Split(new[] { '|' }, 2);
        float progress = 0;
        if (fields.Length > 0)
            float.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out progress);

        string message = fields.Length > 1 ? fields[1] : "Reconstruction update";
        if (message.StartsWith("Model ready:", StringComparison.Ordinal))
        {
            isProcessing = false;
            if (reconstructButton != null)
            {
                reconstructButton.interactable = true;
                Text label = reconstructButton.GetComponentInChildren<Text>();
                if (label != null) label.text = "Rebuild 3D";
            }
            if (modelViewer != null) modelViewer.LoadModel();
            SetStatus("3D model ready. Tap 3D Model to view it.");
            return;
        }

        SetStatus(progress > 0 && progress < 1
            ? $"{message} {Mathf.RoundToInt(progress * 100)}%"
            : message);

        if (message.StartsWith("Reconstruction failed:", StringComparison.Ordinal)
            || message.StartsWith("Model created but OBJ export failed:", StringComparison.Ordinal)
            || message.StartsWith("This iPhone cannot export", StringComparison.Ordinal)
            || message.StartsWith("This iPhone does not support", StringComparison.Ordinal)
            || message.StartsWith("Capture at least", StringComparison.Ordinal)
            || message.StartsWith("Could not start reconstruction:", StringComparison.Ordinal)
            || message.StartsWith("Reconstruction was cancelled", StringComparison.Ordinal))
        {
            isProcessing = false;
            if (reconstructButton != null)
                reconstructButton.interactable = true;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log("MOAD 3D Scan: " + message);
    }
}
