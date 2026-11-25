using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ARCellInteraction : MonoBehaviour
{
    [Header("Prefabs & AR")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;

    [Header("Scale Boundaries")]
    [SerializeField] private float initialScale = 0.1f;
    [SerializeField] private float minScale = 0.03f;
    [SerializeField] private float maxScale = 0.5f;

    [Header("Elevation Offset")]
    [SerializeField] private float verticalOffset = 0.0f;

    [Header("Cell Choices")]
    [SerializeField] private GameObject animalCellPrefab;
    [SerializeField] private GameObject plantCellPrefab;
    [SerializeField] private GameObject bacterialCellPrefab;
    [SerializeField] private GameObject fungalCellPrefab;

    [Header("Placement Control")]
    public bool canPlace = false;

    private GameObject spawnedObject;

    private static readonly List<ARRaycastHit> hits =
        new List<ARRaycastHit>();

    private readonly List<RaycastResult> uiHits =
        new List<RaycastResult>();

    private Quaternion resetRotation;
    private Vector3 resetScale;

    private bool blockGestureUntilRelease;
    private bool isDragging;
    private bool suppressTap;

    private Vector2 touchStartPos;

    private const float TapMovementThreshold = 15f;

    public bool isTrackedImage = false;

    public GameObject CurrentCell => spawnedObject;
    public event System.Action<GameObject> OnActiveCellChanged;
    public event System.Action OnCellReset;

    public string StatusMessage { get; private set; } =
        "Choose a cell, then tap a detected surface.";

    // =====================================================
    // SETUP
    // =====================================================

    private void Awake()
    {
        if (raycastManager == null)
        {
            raycastManager =
                GetComponent<ARRaycastManager>();
        }

        if (raycastManager == null)
        {
            raycastManager =
                FindFirstObjectByType<ARRaycastManager>();
        }

        if (planeManager == null)
        {
            planeManager =
                GetComponent<ARPlaneManager>();
        }

        if (planeManager == null)
        {
            planeManager =
                FindFirstObjectByType<ARPlaneManager>();
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        // Default to Animal if no starting prefab was assigned.
        if (cellPrefab == null &&
            animalCellPrefab != null)
        {
            cellPrefab = animalCellPrefab;
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // =====================================================
    // QR TRACKING SUPPORT
    // =====================================================

    public void SetSpawnedObject(GameObject obj)
    {
        // If a plane-placed cell is active and QR mode takes over,
        // remove the plane cell first.
        if (spawnedObject != null &&
            !isTrackedImage &&
            spawnedObject != obj)
        {
            spawnedObject.SetActive(false);
            Destroy(spawnedObject);
        }

        spawnedObject = obj;
        isTrackedImage = obj != null;

        if (spawnedObject != null)
        {
            resetRotation =
                spawnedObject.transform.rotation;

            resetScale =
                spawnedObject.transform.localScale;
        }

        OnActiveCellChanged?.Invoke(spawnedObject);
    }

    public void ClearSpawned()
    {
        if (spawnedObject != null)
        {
            if (OrganelleSelectionManager.Instance != null)
            {
                OrganelleSelectionManager.Instance
                    .ClearSelection();
            }

            // QR cells are children of the tracked-image prefab,
            // so do not destroy them here.
            if (!isTrackedImage)
            {
                spawnedObject.SetActive(false);
                Destroy(spawnedObject);
            }
        }

        spawnedObject = null;

        isTrackedImage = false;
        isDragging = false;
        suppressTap = false;
        blockGestureUntilRelease = false;

        OnCellReset?.Invoke();
        OnActiveCellChanged?.Invoke(null);
    }

    // =====================================================
    // INPUT
    // =====================================================

    private void Update()
    {
        var activeTouches = Touch.activeTouches;

        bool pointerOverUI = false;

        foreach (var touch in activeTouches)
        {
            if (IsOverUI(touch.screenPosition))
            {
                pointerOverUI = true;
                break;
            }
        }

        bool mouseHeld =
            Mouse.current != null &&
            (
                Mouse.current.leftButton.isPressed ||
                Mouse.current.middleButton.isPressed
            );

        if (activeTouches.Count == 0 &&
            Mouse.current != null)
        {
            pointerOverUI =
                IsOverUI(
                    Mouse.current.position.ReadValue());
        }

        // If a UI button started the gesture,
        // do not let that same gesture reach the AR world.
        if (pointerOverUI &&
            (activeTouches.Count > 0 || mouseHeld))
        {
            blockGestureUntilRelease = true;
        }

        if (blockGestureUntilRelease)
        {
            isDragging = false;
            suppressTap = true;

            if (activeTouches.Count == 0 &&
                !mouseHeld)
            {
                blockGestureUntilRelease = false;
            }

            return;
        }

        if (pointerOverUI)
        {
            return;
        }

        // =================================================
        // MOBILE - ONE FINGER
        // =================================================

        if (activeTouches.Count == 1)
        {
            var touch = activeTouches[0];

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos =
                    touch.screenPosition;

                isDragging = false;
                suppressTap = false;

                if (canPlace &&
                    spawnedObject == null &&
                    !IsOverUI(touch.screenPosition))
                {
                    PlaceOrMove(
                        touch.screenPosition,
                        true);

                    suppressTap = true;
                }
            }

            else if (
                touch.phase == TouchPhase.Moved &&
                spawnedObject != null)
            {
                float movement =
                    (
                        touch.screenPosition -
                        touchStartPos
                    ).magnitude;

                if (!isDragging &&
                    movement > TapMovementThreshold)
                {
                    isDragging = true;
                }

                if (isDragging &&
                    !isTrackedImage)
                {
                    PlaceOrMove(
                        touch.screenPosition,
                        false);
                }
            }

            else if (
                touch.phase == TouchPhase.Ended)
            {
                if (!isDragging &&
                    !suppressTap &&
                    spawnedObject != null)
                {
                    HandleScreenTap(
                        touch.screenPosition,
                        touch.touchId);
                }

                isDragging = false;
                suppressTap = false;
            }
        }

        // =================================================
        // MOBILE - TWO FINGERS
        // =================================================

        else if (
            activeTouches.Count == 2 &&
            spawnedObject != null)
        {
            var touch0 = activeTouches[0];
            var touch1 = activeTouches[1];

            suppressTap = true;

            Vector2 previousPosition0 =
                touch0.screenPosition -
                touch0.delta;

            Vector2 previousPosition1 =
                touch1.screenPosition -
                touch1.delta;

            float previousDistance =
                (
                    previousPosition0 -
                    previousPosition1
                ).magnitude;

            float currentDistance =
                (
                    touch0.screenPosition -
                    touch1.screenPosition
                ).magnitude;

            float factor =
                currentDistance /
                (
                    previousDistance <= 0.001f
                        ? 1f
                        : previousDistance
                );

            Vector3 targetScale =
                spawnedObject.transform.localScale *
                factor;

            float clampedScale =
                Mathf.Clamp(
                    targetScale.x,
                    minScale,
                    maxScale);

            spawnedObject.transform.localScale =
                Vector3.one * clampedScale;

            Vector2 previousDirection =
                previousPosition1 -
                previousPosition0;

            Vector2 currentDirection =
                touch1.screenPosition -
                touch0.screenPosition;

            float angleDelta =
                Vector2.SignedAngle(
                    previousDirection,
                    currentDirection);

            spawnedObject.transform.Rotate(
                Vector3.up,
                -angleDelta,
                Space.World);
        }

        // =================================================
        // EDITOR / PC
        // =================================================

        else if (Mouse.current != null)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            Vector2 mouseDelta =
                Mouse.current.delta.ReadValue();

            bool rotateKeyPressed =
                Keyboard.current != null &&
                Keyboard.current.rKey.isPressed;

            bool middleMouseDragging =
                Mouse.current.middleButton.isPressed;

            // Rotate:
            // Middle mouse OR R + left mouse.
            if (spawnedObject != null &&
                (
                    middleMouseDragging ||
                    (
                        rotateKeyPressed &&
                        Mouse.current.leftButton.isPressed
                    )
                ))
            {
                suppressTap = true;

                const float rotationSpeed = 0.4f;

                spawnedObject.transform.Rotate(
                    Vector3.up,
                    -mouseDelta.x * rotationSpeed,
                    Space.World);
            }

            else if (
                Mouse.current.leftButton
                    .wasPressedThisFrame)
            {
                touchStartPos =
                    mousePosition;

                isDragging = false;
                suppressTap = false;

                if (canPlace &&
                    spawnedObject == null &&
                    !IsOverUI(-1))
                {
                    PlaceOrMove(
                        mousePosition,
                        true);

                    suppressTap = true;
                }
            }

            else if (
                Mouse.current.leftButton.isPressed &&
                spawnedObject != null)
            {
                float movement =
                    (
                        mousePosition -
                        touchStartPos
                    ).magnitude;

                if (!isDragging &&
                    movement >
                    TapMovementThreshold)
                {
                    isDragging = true;
                }

                if (isDragging &&
                    !isTrackedImage)
                {
                    PlaceOrMove(
                        mousePosition,
                        false);
                }
            }

            else if (
                Mouse.current.leftButton
                    .wasReleasedThisFrame)
            {
                if (!isDragging &&
                    !suppressTap &&
                    spawnedObject != null)
                {
                    HandleScreenTap(
                        mousePosition,
                        -1);
                }

                isDragging = false;
                suppressTap = false;
            }

            float scroll =
                Mouse.current.scroll.ReadValue().y;

            if (scroll != 0 &&
                spawnedObject != null)
            {
                float newScale =
                    Mathf.Clamp(
                        spawnedObject
                            .transform
                            .localScale.x +
                        (scroll * 0.005f),
                        minScale,
                        maxScale);

                spawnedObject.transform.localScale =
                    Vector3.one * newScale;
            }
        }
    }

    // =====================================================
    // CELL SELECTION
    // =====================================================

    public void SelectAnimalCell()
    {
        SelectCell(animalCellPrefab);
    }

    public void SelectPlantCell()
    {
        SelectCell(plantCellPrefab);
    }

    public void SelectBacterialCell()
    {
        SelectCell(bacterialCellPrefab);
    }

    public void SelectFungalCell()
    {
        SelectCell(fungalCellPrefab);
    }

    public void SelectCell(
        GameObject selectedPrefab)
    {
        BlockCurrentGesture();

        if (selectedPrefab == null)
        {
            StatusMessage =
                "Selected cell prefab is missing.";

            Debug.LogError(
                StatusMessage,
                this);

            return;
        }

        Debug.Log(
            "Selected cell: " +
            selectedPrefab.name,
            this);

        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance
                .ClearSelection();
        }

        // This becomes the prefab used by plane placement.
        cellPrefab = selectedPrefab;

        // If nothing is placed, simply remember the choice.
        if (spawnedObject == null)
        {
            StatusMessage =
                "Selected " +
                selectedPrefab.name +
                ". Tap a surface to place it.";

            return;
        }

        // Sidebar selection should happen in Plane Placement.
        // If a QR cell is still registered, release our
        // reference to it rather than destroying its child.
        if (isTrackedImage)
        {
            spawnedObject = null;
            isTrackedImage = false;

            OnActiveCellChanged?.Invoke(null);

            StatusMessage =
                "Selected " +
                selectedPrefab.name +
                ". Tap a surface to place it.";

            return;
        }

        // Replace the existing plane cell immediately.
        Vector3 position =
            spawnedObject.transform.position;

        Quaternion rotation =
            spawnedObject.transform.rotation;

        spawnedObject.SetActive(false);
        Destroy(spawnedObject);

        spawnedObject = null;

        SpawnPlaneCell(
            position,
            rotation);

        StatusMessage =
            "Switched to " +
            selectedPrefab.name +
            ".";
    }

    // =====================================================
    // PLANE PLACEMENT
    // =====================================================

    private void SpawnPlaneCell(
        Vector3 position,
        Quaternion rotation)
    {
        if (cellPrefab == null)
        {
            StatusMessage =
                "Choose a cell before placing it.";

            Debug.LogError(
                "Cannot spawn cell because cellPrefab is null.",
                this);

            return;
        }

        spawnedObject =
            Instantiate(
                cellPrefab,
                position,
                rotation);

        isTrackedImage = false;

        // Every plane-placed cell begins at the configured AR scale.
        spawnedObject.transform.localScale =
            Vector3.one * initialScale;

        // Save this cell's original transform for Reset.
        resetRotation =
            spawnedObject.transform.rotation;

        resetScale =
            spawnedObject.transform.localScale;

        if (spawnedObject
                .GetComponent<CellOrganelleSetup>()
            == null)
        {
            spawnedObject
                .AddComponent<CellOrganelleSetup>();
        }

        OnActiveCellChanged?.Invoke(spawnedObject);

        Debug.Log(
            "Spawned " +
            spawnedObject.name +
            " at " +
            position,
            this);
    }

    private void PlaceOrMove(
        Vector2 screenPosition,
        bool isInitialSpawn)
    {
        if (raycastManager == null)
        {
            Debug.LogError(
                "ARRaycastManager is missing.",
                this);

            return;
        }

        if (!raycastManager.Raycast(
            screenPosition,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            return;
        }

        Pose hitPose =
            hits[0].pose;

        Vector3 targetPosition =
            hitPose.position +
            Vector3.up * verticalOffset;

        if (isInitialSpawn)
        {
            SpawnPlaneCell(
                targetPosition,
                hitPose.rotation);

            if (spawnedObject != null)
            {
                StatusMessage =
                    "Cell placed. Drag, zoom, or rotate to explore.";
            }
        }

        else if (
            spawnedObject != null &&
            !isTrackedImage)
        {
            spawnedObject.transform.position =
                targetPosition;
        }
    }

    // =====================================================
    // RESET
    // =====================================================

    public void ResetCell()
    {
        BlockCurrentGesture();

        if (spawnedObject == null)
        {
            StatusMessage =
                "Place a cell first.";

            return;
        }

        if (isTrackedImage)
        {
            StatusMessage =
                "Reset is only available in plane placement mode.";

            return;
        }

        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance
                .ClearSelection();
        }

        spawnedObject.transform.rotation =
            resetRotation;

        spawnedObject.transform.localScale =
            resetScale;

        OnCellReset?.Invoke();

        StatusMessage =
            "Cell size and rotation reset.";

        Debug.Log(
            "Reset " +
            spawnedObject.name,
            this);
    }

    // =====================================================
    // RECENTER
    // =====================================================

    public void RecenterCell()
    {
        BlockCurrentGesture();

        if (spawnedObject == null)
        {
            StatusMessage =
                "Place a cell first.";

            return;
        }

        if (isTrackedImage)
        {
            StatusMessage =
                "Recenter is only available in plane placement mode.";

            return;
        }

        if (arCamera == null ||
            raycastManager == null)
        {
            StatusMessage =
                "AR camera or raycast manager is missing.";

            Debug.LogWarning(
                StatusMessage,
                this);

            return;
        }

        if (!raycastManager.Raycast(
            arCamera.pixelRect.center,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            StatusMessage =
                "Point the camera at a detected surface and retry.";

            Debug.Log(
                StatusMessage,
                this);

            return;
        }

        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance
                .ClearSelection();
        }

        spawnedObject.transform.position =
            hits[0].pose.position +
            Vector3.up * verticalOffset;

        StatusMessage =
            "Cell re-centered.";
    }

    // =====================================================
    // CLEAR
    // =====================================================

    public void ClearCell()
    {
        BlockCurrentGesture();

        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance
                .ClearSelection();
        }

        if (spawnedObject != null)
        {
            // QR cells belong to the tracked image object.
            if (!isTrackedImage)
            {
                spawnedObject.SetActive(false);
                Destroy(spawnedObject);
            }

            spawnedObject = null;
        }

        isTrackedImage = false;
        isDragging = false;
        suppressTap = false;

        OnCellReset?.Invoke();
        OnActiveCellChanged?.Invoke(null);

        StatusMessage =
            "Cell cleared. Tap a detected surface to place it again.";
    }

    // =====================================================
    // ORGANELLE TAP
    // =====================================================

    private void HandleScreenTap(
        Vector2 screenPosition,
        int pointerId)
    {
        if (IsOverUI(pointerId))
        {
            return;
        }

        if (OrganelleSelectionManager.Instance == null)
        {
            return;
        }

        OrganelleSelectionManager.Instance
            .TrySelectAtScreenPoint(
                screenPosition,
                arCamera);
    }

    // =====================================================
    // UI DETECTION
    // =====================================================

    private static bool IsOverUI(
        int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0 &&
            EventSystem.current
                .IsPointerOverGameObject(pointerId))
        {
            return true;
        }

        return EventSystem.current
            .IsPointerOverGameObject();
    }

    private bool IsOverUI(
        Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointer =
            new PointerEventData(
                EventSystem.current)
            {
                position = screenPosition
            };

        uiHits.Clear();

        EventSystem.current.RaycastAll(
            pointer,
            uiHits);

        foreach (RaycastResult hit in uiHits)
        {
            if (hit.module is
                UnityEngine.UI.GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }

    private void BlockCurrentGesture()
    {
        blockGestureUntilRelease = true;
        isDragging = false;
        suppressTap = true;
    }
}