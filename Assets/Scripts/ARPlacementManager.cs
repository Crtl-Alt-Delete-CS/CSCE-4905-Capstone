using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ARPlacementManager : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARCameraManager arCameraManager;
    
    private HomeLoadingUI homeUI;
    private GameObject arControls;
    private Text arStatusLabel;
    private string currentARStatus;
    private bool arExperienceActive;

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();
        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();
        if (arSession == null)
            arSession = FindFirstObjectByType<ARSession>();
        if (arCameraManager == null)
            arCameraManager = FindFirstObjectByType<ARCameraManager>();

        SetARExperienceActive(false);
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildInterface();
        SetARExperienceActive(false);
    }

    private void Update()
    {
        if (!arExperienceActive)
            return;

        UpdateARStatus();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("Mobile UI Event System");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("Mobile UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        arControls = new GameObject("AR Controls", typeof(RectTransform));
        arControls.transform.SetParent(canvas.transform, false);
        RectTransform controlsRect = arControls.GetComponent<RectTransform>();
        controlsRect.anchorMin = Vector2.zero;
        controlsRect.anchorMax = Vector2.one;
        controlsRect.offsetMin = Vector2.zero;
        controlsRect.offsetMax = Vector2.zero;

        CreateButton(arControls.transform, "HOME", new Vector2(0.06f, 0.91f), new Vector2(0.34f, 0.97f),
            new Color(0.035f, 0.12f, 0.105f, 0.88f), Color.white, ReturnHome);
        arStatusLabel = AddLabel(arControls.transform, "Starting AR...", 30, FontStyle.Normal, Color.white,
            new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.1f), TextAnchor.MiddleCenter);

        homeUI = gameObject.AddComponent<HomeLoadingUI>();
        homeUI.Initialize(EnterAR);
    }

    private Text AddLabel(Transform parent, string text, int fontSize, FontStyle style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        return label;
    }

    private void UpdateARStatus()
    {
        string status;
        if (arSession == null)
        {
            status = "AR Session is missing from the scene.";
        }
        else
        {
            switch (ARSession.state)
            {
                case ARSessionState.SessionTracking:
                    status = "Move phone slowly to scan a surface.";
                    break;
                case ARSessionState.SessionInitializing:
                    status = "Starting camera and tracking...";
                    break;
                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:
                    status = "Install Google Play Services for AR.";
                    break;
                case ARSessionState.Unsupported:
                    status = "ARCore is not supported on this device.";
                    break;
                default:
                    status = "Checking AR availability...";
                    break;
            }
        }

        if (status != currentARStatus)
        {
            currentARStatus = status;
            arStatusLabel.text = status;
        }
    }

    private void CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
        Color background, Color foreground, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = background;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        AddLabel(buttonObject.transform, label, 34, FontStyle.Bold, foreground,
            Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
    }

    private void EnterAR()
    {
        SetARExperienceActive(true);
    }

    private void ReturnHome()
    {
        SetARExperienceActive(false);
    }

    private void SetARExperienceActive(bool active)
    {
        arExperienceActive = active;
        if (arSession != null)
            arSession.enabled = active;
        if (arCameraManager != null)
            arCameraManager.enabled = active;
        if (planeManager != null)
            planeManager.enabled = active;
        if (raycastManager != null)
            raycastManager.enabled = active;
        if (homeUI != null)
            homeUI.SetVisible(!active);
        if (arControls != null)
            arControls.SetActive(active);
    }

}
