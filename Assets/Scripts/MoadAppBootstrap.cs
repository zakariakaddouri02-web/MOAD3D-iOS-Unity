using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>Creates the starter camera and capture UI for a clean Unity project.</summary>
public class MoadAppBootstrap : MonoBehaviour
{
    private void Awake()
    {
        ARCameraManager cameraManager = CreateARCamera();
        Canvas canvas = CreateCanvas();
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = new GameObject("Event System").AddComponent<EventSystem>();
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        Text status = CreateText(canvas.transform, "Status", 25, TextAnchor.MiddleCenter,
            new Vector2(0.05f, 0.83f), new Vector2(0.64f, 0.95f));
        Text counter = CreateText(canvas.transform, "Photo Counter", 30, TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.8f));
        Text speed = CreateText(canvas.transform, "Movement Guidance", 22, TextAnchor.MiddleCenter,
            new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.71f));

        Slider progress = CreateProgress(canvas.transform);
        CaptureManager capture = gameObject.AddComponent<CaptureManager>();
        capture.arCameraManager = cameraManager;
        capture.hudCounter = counter;
        capture.progressBar = progress;
        capture.speedText = speed;
        capture.statusText = status;

        GameObject reconstructionObject = new GameObject("Reconstruction Controller");
        ScanReconstructionController reconstruction = reconstructionObject.AddComponent<ScanReconstructionController>();
        Button reconstructButton = CreateButton(canvas.transform, "Build 3D Model", new Vector2(0.29f, 0.20f),
            new Vector2(0.71f, 0.29f), reconstruction.StartReconstruction);

        CreateButton(canvas.transform, "Start 150-photo scan", new Vector2(0.08f, 0.08f),
            new Vector2(0.48f, 0.17f), () =>
            {
                reconstruction.ClearInputFolder();
                capture.Start150PhotoMode();
            });
        CreateButton(canvas.transform, "Stop", new Vector2(0.52f, 0.08f),
            new Vector2(0.72f, 0.17f), capture.CancelCapture);
        capture.ScanCaptured += reconstruction.SetInputFolder;

        GameObject viewerPanel = CreateModelViewer(canvas.transform, out ObjModelViewer modelViewer);
        reconstruction.Configure(status, reconstructButton, modelViewer);
        CreateButton(canvas.transform, "3D Model", new Vector2(0.68f, 0.86f),
            new Vector2(0.94f, 0.94f), () =>
            {
                viewerPanel.SetActive(true);
                modelViewer.SetViewerVisible(true);
                modelViewer.LoadModel();
            });

        FlashToggle flash = gameObject.AddComponent<FlashToggle>();
        CreateButton(canvas.transform, "Flash", new Vector2(0.76f, 0.08f),
            new Vector2(0.92f, 0.17f), flash.Toggle);
    }

    private static GameObject CreateModelViewer(Transform parent, out ObjModelViewer modelViewer)
    {
        GameObject panel = new GameObject("3D Model Viewer");
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.22f);
        panelRect.anchorMax = new Vector2(0.95f, 0.82f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.025f, 0.04f, 0.055f, 0.97f);

        Text title = CreateText(panel.transform, "Viewer Title", 26, TextAnchor.MiddleLeft,
            new Vector2(0.05f, 0.88f), new Vector2(0.72f, 0.98f));
        title.text = "3D Model Preview";

        GameObject previewObject = new GameObject("Model Preview Surface");
        previewObject.transform.SetParent(panel.transform, false);
        RectTransform previewRect = previewObject.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.05f, 0.19f);
        previewRect.anchorMax = new Vector2(0.95f, 0.86f);
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;
        RawImage previewImage = previewObject.AddComponent<RawImage>();
        previewImage.color = Color.white;
        modelViewer = previewObject.AddComponent<ObjModelViewer>();

        Text help = CreateText(panel.transform, "Viewer Message", 17, TextAnchor.MiddleCenter,
            new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.17f));
        help.text = "Import an OBJ mesh to inspect it in 3D";
        modelViewer.Configure(previewImage, CreateViewerCamera(), help);

        CreateButton(panel.transform, "Import OBJ", new Vector2(0.47f, 0.88f),
            new Vector2(0.68f, 0.98f), modelViewer.ImportModel);
        CreateButton(panel.transform, "Reload", new Vector2(0.69f, 0.88f),
            new Vector2(0.82f, 0.98f), modelViewer.LoadModel);
        CreateButton(panel.transform, "Close", new Vector2(0.83f, 0.88f),
            new Vector2(0.96f, 0.98f), () =>
            {
                modelViewer.SetViewerVisible(false);
                panel.SetActive(false);
            });
        panel.SetActive(false);
        return panel;
    }

    private static Camera CreateViewerCamera()
    {
        GameObject lightObject = new GameObject("3D Preview Light");
        lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        Light previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.3f;

        GameObject cameraObject = new GameObject("3D Preview Camera");
        cameraObject.transform.position = new Vector3(0f, 0f, -3f);
        cameraObject.transform.rotation = Quaternion.identity;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 1f);
        camera.fieldOfView = 40f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.enabled = false;
        return camera;
    }

    public void OnOBJImportResult(string result)
    {
        ObjModelViewer viewer = FindObjectOfType<ObjModelViewer>();
        if (viewer != null)
            viewer.OnOBJImportResult(result);
    }

    private static ARCameraManager CreateARCamera()
    {
        if (FindObjectOfType<ARSession>() == null)
            new GameObject("AR Session").AddComponent<ARSession>();

        GameObject originObject = new GameObject("AR Session Origin");
        ARSessionOrigin origin = originObject.AddComponent<ARSessionOrigin>();
        GameObject cameraObject = new GameObject("AR Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(originObject.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.AddComponent<ARCameraBackground>();
        ARCameraManager cameraManager = cameraObject.AddComponent<ARCameraManager>();
        cameraObject.AddComponent<ARPoseDriver>();
        origin.camera = camera;
        return cameraManager;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("MOAD Capture UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static Text CreateText(Transform parent, string name, int size, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Slider CreateProgress(Transform parent)
    {
        GameObject sliderObject = new GameObject("Photo Progress");
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.59f);
        rect.anchorMax = new Vector2(0.9f, 0.63f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = CaptureManager.TARGET_PHOTOS;
        slider.value = 0;
        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(1, 1, 1, 0.2f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color(0.26f, 0.79f, 0.56f, 1);
        slider.fillRect = fillRect;
        slider.targetGraphic = background;
        return slider;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchorMin,
        Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.07f, 0.09f, 0.12f, 0.85f);
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        Text text = CreateText(buttonObject.transform, "Label", 20, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one);
        text.text = label;
        return button;
    }
}
