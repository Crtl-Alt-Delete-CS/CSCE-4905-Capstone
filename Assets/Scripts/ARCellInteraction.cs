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

    [Header("Scale Boundaries (Meters)")]
    [SerializeField] private float initialScale = 0.1f;
    [SerializeField] private float minScale = 0.03f;
    [SerializeField] private float maxScale = 0.5f;

    [Header("Elevation Offset")]
    [SerializeField] private float verticalOffset = 0.0f;

    // Added: prefabs used by the cell-selection buttons.
    [Header("Cell Choices")]
    [SerializeField] private GameObject animalCellPrefab;
    [SerializeField] private GameObject plantCellPrefab;
    [SerializeField] private GameObject bacterialCellPrefab;
    [SerializeField] private GameObject fungalCellPrefab;

    private GameObject spawnedObject;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Added: reset state and protection against UI gestures reaching AR controls.
    private Quaternion resetRotation;
    private bool blockGestureUntilRelease;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    public string StatusMessage { get; private set; } =
        "Choose a cell, then tap a detected surface.";

    // Drag vs. Tap tracking
    private bool isDragging = false;
    private Vector2 touchStartPos;
    private const float TapMovementThreshold = 15f;
    private bool suppressTap = false;

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();

        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();

        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        var activeTouches = Touch.activeTouches;

        // Added: ignore UI interactions through the end of the gesture.
        bool pointerOverUI = false;

        foreach (var touch in activeTouches)
        {
            if (IsOverUI(touch.screenPosition))
            {
                pointerOverUI = true;
                break;
            }
        }

        bool mouseHeld = Mouse.current != null &&
            (Mouse.current.leftButton.isPressed ||
             Mouse.current.middleButton.isPressed);

        if (activeTouches.Count == 0 && Mouse.current != null)
        {
            pointerOverUI = IsOverUI(
                Mouse.current.position.ReadValue());
        }

        if (pointerOverUI && (activeTouches.Count > 0 || mouseHeld))
            blockGestureUntilRelease = true;

        if (blockGestureUntilRelease)
        {
            isDragging = false;
            suppressTap = true;

            if (activeTouches.Count == 0 && !mouseHeld)
                blockGestureUntilRelease = false;

            return;
        }

        // Also prevent mouse-wheel zoom while hovering over UI.
        if (pointerOverUI)
            return;

        // --- MOBILE 1-FINGER TOUCH ---
        if (activeTouches.Count == 1)
        {
            var touch = activeTouches[0];

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.screenPosition;
                isDragging = false;
                suppressTap = false;

                // Initial placement should not also select an organelle.
                if (spawnedObject == null)
                {
                    PlaceOrMove(touch.screenPosition, isInitialSpawn: true);
                    suppressTap = true;
                }
            }
            else if (touch.phase == TouchPhase.Moved && spawnedObject != null)
            {
                // Only begin dragging beyond the tap threshold.
                if (!isDragging &&
                    (touch.screenPosition - touchStartPos).magnitude >
                    TapMovementThreshold)
                {
                    isDragging = true;
                }

                if (isDragging)
                {
                    PlaceOrMove(touch.screenPosition, isInitialSpawn: false);
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                // A stationary gesture counts as a tap.
                if (!isDragging && !suppressTap && spawnedObject != null)
                {
                    HandleScreenTap(touch.screenPosition, touch.touchId);
                }

                isDragging = false;
                suppressTap = false;
            }
        }
        // --- MOBILE 2-FINGER TOUCH: PINCH & ROTATE ---
        else if (activeTouches.Count == 2 && spawnedObject != null)
        {
            var touch0 = activeTouches[0];
            var touch1 = activeTouches[1];
            suppressTap = true;

            Vector2 prevPos0 = touch0.screenPosition - touch0.delta;
            Vector2 prevPos1 = touch1.screenPosition - touch1.delta;

            // Pinch-to-scale
            float prevDist = (prevPos0 - prevPos1).magnitude;
            float curDist =
                (touch0.screenPosition - touch1.screenPosition).magnitude;
            float factor = curDist / (prevDist <= 0.001f ? 1f : prevDist);

            Vector3 targetScale = spawnedObject.transform.localScale * factor;
            float clampedScale = Mathf.Clamp(targetScale.x, minScale, maxScale);
            spawnedObject.transform.localScale = Vector3.one * clampedScale;

            // Twist-to-rotate
            Vector2 prevDir = prevPos1 - prevPos0;
            Vector2 curDir = touch1.screenPosition - touch0.screenPosition;
            float angleDelta = Vector2.SignedAngle(prevDir, curDir);

            spawnedObject.transform.Rotate(
                Vector3.up, -angleDelta, Space.World);
        }
        // --- EDITOR / PC MOUSE INPUT (XR SIMULATION) ---
        else if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            bool isRotateKeyPressed =
                Keyboard.current != null && Keyboard.current.rKey.isPressed;

            bool isMiddleMouseDragging =
                Mouse.current.middleButton.isPressed;

            // 1. ROTATE: Hold R + Left-Click Drag OR Middle-Mouse Drag
            if (spawnedObject != null &&
                (isMiddleMouseDragging ||
                 (isRotateKeyPressed && Mouse.current.leftButton.isPressed)))
            {
                suppressTap = true;
                float rotationSpeed = 0.4f;

                spawnedObject.transform.Rotate(
                    Vector3.up, -mouseDelta.x * rotationSpeed, Space.World);
            }
            // 2. REPOSITION / SPAWN / TAP: Left-Click
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                touchStartPos = mousePos;
                isDragging = false;
                suppressTap = false;

                if (spawnedObject == null)
                {
                    PlaceOrMove(mousePos, isInitialSpawn: true);
                    suppressTap = true;
                }
            }
            else if (Mouse.current.leftButton.isPressed && spawnedObject != null)
            {
                if (!isDragging &&
                    (mousePos - touchStartPos).magnitude > TapMovementThreshold)
                {
                    isDragging = true;
                }

                if (isDragging)
                {
                    PlaceOrMove(mousePos, isInitialSpawn: false);
                }
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (!isDragging && !suppressTap && spawnedObject != null)
                {
                    HandleScreenTap(mousePos, -1);
                }

                isDragging = false;
                suppressTap = false;
            }

            // 3. ZOOM: Mouse Scroll Wheel
            float scroll = Mouse.current.scroll.ReadValue().y;

            if (scroll != 0 && spawnedObject != null)
            {
                float newScale = Mathf.Clamp(
                    spawnedObject.transform.localScale.x + (scroll * 0.005f),
                    minScale,
                    maxScale);

                spawnedObject.transform.localScale = Vector3.one * newScale;
            }
        }
    }

    private void HandleScreenTap(Vector2 screenPosition, int pointerId)
    {
        // Taps on UI shouldn't change the selection.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            return;
        }

        // Select the organelle under the tap; empty space clears selection.
        OrganelleSelectionManager.Instance.TrySelectAtScreenPoint(
            screenPosition, arCamera);
    }

    private void PlaceOrMove(Vector2 screenPosition, bool isInitialSpawn)
    {
        if (raycastManager.Raycast(
            screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Vector3 targetPos =
                hitPose.position + (Vector3.up * verticalOffset);

            if (isInitialSpawn)
            {
                spawnedObject = Instantiate(
                    cellPrefab, targetPos, hitPose.rotation);

                spawnedObject.transform.localScale = Vector3.one * initialScale;

                // Added: remember the original orientation for Reset.
                resetRotation = spawnedObject.transform.rotation;
                StatusMessage = "Cell placed. Drag, zoom, or rotate to explore.";

                // Make the organelles tappable.
                if (spawnedObject.GetComponent<CellOrganelleSetup>() == null)
                    spawnedObject.AddComponent<CellOrganelleSetup>();
            }
            else
            {
                spawnedObject.transform.position = targetPos;
            }
        }
    }

    // Added: onscreen cell-selection methods.
    public void SelectAnimalCell() => SelectCell(animalCellPrefab);
    public void SelectPlantCell() => SelectCell(plantCellPrefab);
    public void SelectBacterialCell() => SelectCell(bacterialCellPrefab);
    public void SelectFungalCell() => SelectCell(fungalCellPrefab);

    public void SelectCell(GameObject selectedPrefab)
    {
        BlockCurrentGesture();

        if (selectedPrefab == null)
        {
            StatusMessage = "Assign this cell's prefab in the Inspector.";
            Debug.LogWarning(StatusMessage, this);
            return;
        }

        OrganelleSelectionManager.Instance.ClearSelection();
        cellPrefab = selectedPrefab;

        if (spawnedObject == null)
        {
            StatusMessage = "Cell selected. Tap a detected surface to place it.";
            return;
        }

        Vector3 position = spawnedObject.transform.position;
        Quaternion rotation = spawnedObject.transform.rotation;
        Vector3 scale = spawnedObject.transform.localScale;

        spawnedObject.SetActive(false);
        Destroy(spawnedObject);

        spawnedObject = Instantiate(cellPrefab, position, rotation);
        spawnedObject.transform.localScale = scale;

        if (spawnedObject.GetComponent<CellOrganelleSetup>() == null)
            spawnedObject.AddComponent<CellOrganelleSetup>();

        StatusMessage = "Cell switched.";
    }

    // Added: reset size and rotation at the current position.
    public void ResetCell()
    {
        BlockCurrentGesture();

        if (spawnedObject == null)
        {
            StatusMessage = "Place a cell first.";
            return;
        }

        OrganelleSelectionManager.Instance.ClearSelection();
        spawnedObject.transform.rotation = resetRotation;
        spawnedObject.transform.localScale = Vector3.one * initialScale;

        StatusMessage = "Cell size and rotation reset.";
    }

    // Added: move onto a detected surface at the center of the camera view.
    public void RecenterCell()
    {
        BlockCurrentGesture();

        if (spawnedObject == null)
        {
            StatusMessage = "Place a cell first.";
            return;
        }

        if (arCamera == null || raycastManager == null)
        {
            StatusMessage = "AR camera or raycast manager is missing.";
            Debug.LogWarning(StatusMessage, this);
            return;
        }

        if (!raycastManager.Raycast(
            arCamera.pixelRect.center,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            StatusMessage = "Point the camera at a detected surface and retry.";
            Debug.Log(StatusMessage, this);
            return;
        }

        OrganelleSelectionManager.Instance.ClearSelection();

        spawnedObject.transform.position =
            hits[0].pose.position + Vector3.up * verticalOffset;

        StatusMessage = "Cell re-centered.";
    }

    // Added: remove the cell so another can be placed.
    public void ClearCell()
    {
        BlockCurrentGesture();
        OrganelleSelectionManager.Instance.ClearSelection();

        if (spawnedObject != null)
        {
            spawnedObject.SetActive(false);
            Destroy(spawnedObject);
            spawnedObject = null;
        }

        StatusMessage = "Cell cleared. Tap a detected surface to place it again.";
    }

    // Added: detect Canvas UI under the pointer.
    private bool IsOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);

        foreach (var hit in uiHits)
        {
            if (hit.module is UnityEngine.UI.GraphicRaycaster)
                return true;
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