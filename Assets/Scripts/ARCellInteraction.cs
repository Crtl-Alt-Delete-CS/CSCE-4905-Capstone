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
    [SerializeField] private float verticalOffset = 0.02f;

    [Header("Placement Control")]
    // Set by ARPlacementManager: true only in PlanePlacement mode
    public bool canPlace = false;

    private GameObject spawnedObject;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Drag vs. Tap tracking
    private bool isDragging = false;
    private Vector2 touchStartPos;
    private const float TapMovementThreshold = 15f;
    private bool suppressTap = false;

    public bool isTrackedImage = false;

    public void SetSpawnedObject(GameObject obj)
    {
        // If switching to a QR cell while a tap-placed cell is still active, destroy the tap cell first
        if (spawnedObject != null && !isTrackedImage && spawnedObject != obj)
        {
            Destroy(spawnedObject);
        }

        spawnedObject = obj;
        isTrackedImage = (obj != null);
    }

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

        // --- MOBILE 1-FINGER TOUCH ---
        if (activeTouches.Count == 1)
        {
            var touch = activeTouches[0];

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.screenPosition;
                isDragging = false;
                suppressTap = false;

                // Only allow spawning if we are explicitly in PlanePlacement mode
                if (canPlace && spawnedObject == null && !IsOverUI(touch.touchId))
                {
                    PlaceOrMove(touch.screenPosition, isInitialSpawn: true);
                    suppressTap = true;
                }
            }
            else if (touch.phase == TouchPhase.Moved && spawnedObject != null)
            {
                if (!isDragging && (touch.screenPosition - touchStartPos).magnitude > TapMovementThreshold)
                {
                    isDragging = true;
                }

                if (isDragging && !isTrackedImage)
                {
                    PlaceOrMove(touch.screenPosition, isInitialSpawn: false);
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
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

            float prevDist = (prevPos0 - prevPos1).magnitude;
            float curDist = (touch0.screenPosition - touch1.screenPosition).magnitude;
            float factor = curDist / (prevDist <= 0.001f ? 1f : prevDist);

            Vector3 targetScale = spawnedObject.transform.localScale * factor;
            float clampedScale = Mathf.Clamp(targetScale.x, minScale, maxScale);
            spawnedObject.transform.localScale = Vector3.one * clampedScale;

            Vector2 prevDir = prevPos1 - prevPos0;
            Vector2 curDir = touch1.screenPosition - touch0.screenPosition;
            float angleDelta = Vector2.SignedAngle(prevDir, curDir);

            spawnedObject.transform.Rotate(Vector3.up, -angleDelta, Space.World);
        }
        // --- EDITOR / PC MOUSE INPUT (XR SIMULATION) ---
        else if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            bool isRotateKeyPressed = Keyboard.current != null && Keyboard.current.rKey.isPressed;
            bool isMiddleMouseDragging = Mouse.current.middleButton.isPressed;

            if (spawnedObject != null && (isMiddleMouseDragging || (isRotateKeyPressed && Mouse.current.leftButton.isPressed)))
            {
                suppressTap = true;
                float rotationSpeed = 0.4f;
                spawnedObject.transform.Rotate(Vector3.up, -mouseDelta.x * rotationSpeed, Space.World);
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                touchStartPos = mousePos;
                isDragging = false;
                suppressTap = false;

                // Only allow spawning if we are explicitly in PlanePlacement mode
                if (canPlace && spawnedObject == null && !IsOverUI(-1))
                {
                    PlaceOrMove(mousePos, isInitialSpawn: true);
                    suppressTap = true;
                }
            }
            else if (Mouse.current.leftButton.isPressed && spawnedObject != null)
            {
                if (!isDragging && (mousePos - touchStartPos).magnitude > TapMovementThreshold)
                {
                    isDragging = true;
                }

                if (isDragging && !isTrackedImage)
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

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0 && spawnedObject != null)
            {
                float newScale = Mathf.Clamp(spawnedObject.transform.localScale.x + (scroll * 0.005f), minScale, maxScale);
                spawnedObject.transform.localScale = Vector3.one * newScale;
            }
        }
    }

    private static bool IsOverUI(int pointerId)
    {
        if (EventSystem.current == null) return false;
        if (pointerId >= 0 && EventSystem.current.IsPointerOverGameObject(pointerId)) return true;
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void ClearSpawned()
    {
        if (spawnedObject != null)
        {
            OrganelleSelectionManager.Instance.ClearSelection();

            // ONLY destroy objects instantiated via tap-to-place; never destroy tracked prefab children
            if (!isTrackedImage)
            {
                Destroy(spawnedObject);
            }
        }

        spawnedObject = null;
        isTrackedImage = false;
        isDragging = false;
        suppressTap = false;
    }

    private void HandleScreenTap(Vector2 screenPosition, int pointerId)
    {
        if (IsOverUI(pointerId)) return;
        OrganelleSelectionManager.Instance.TrySelectAtScreenPoint(screenPosition, arCamera);
    }

    private void PlaceOrMove(Vector2 screenPosition, bool isInitialSpawn)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Vector3 targetPos = hitPose.position + (Vector3.up * verticalOffset);

            if (isInitialSpawn)
            {
                spawnedObject = Instantiate(cellPrefab, targetPos, hitPose.rotation);
                spawnedObject.transform.localScale = Vector3.one * initialScale;
                isTrackedImage = false;

                if (spawnedObject.GetComponent<CellOrganelleSetup>() == null)
                    spawnedObject.AddComponent<CellOrganelleSetup>();
            }
            else
            {
                spawnedObject.transform.position = targetPos;
            }
        }
    }
}