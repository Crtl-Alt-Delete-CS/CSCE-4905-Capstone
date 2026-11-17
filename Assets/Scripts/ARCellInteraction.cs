using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class ARCellInteraction : MonoBehaviour
{
    [Header("AR References")]
    public GameObject cellPrefab;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;

    [Header("Placement & Scale Settings")]
    public float initialScale = 0.25f;
    public float minScale = 0.05f;
    public float maxScale = 1.0f;
    public float verticalOffset = 0.05f;

    [Header("Mode State")]
    public bool canPlace = true;
    public bool isTrackedImage = false;

    private GameObject spawnedObject;
    public GameObject SpawnedObject => spawnedObject;

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isDragging = false;
    private Vector2 touchStartPos;
    private const float TapMovementThreshold = 20f;
    private bool suppressTap = false;

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();
        if (raycastManager == null)
            raycastManager = FindAnyObjectByType<ARRaycastManager>();

        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();
        if (planeManager == null)
            planeManager = FindAnyObjectByType<ARPlaneManager>();

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
        bool inPlaneMode = IsInPlaneMode();

        // Automatically destroy the AR plane-placed cell when switching out of Plane mode
        if (!inPlaneMode && spawnedObject != null && !isTrackedImage)
        {
            ClearSpawned();
        }
        // Automatically clear the QR-tracked reference and UI popup when switching into Plane mode
        else if (inPlaneMode && isTrackedImage)
        {
            ClearSpawned();
        }

        var activeTouches = Touch.activeTouches;

        // 1. Two-finger gestures: Pinch to Scale + Twist to Rotate
        if (activeTouches.Count >= 2 && spawnedObject != null)
        {
            suppressTap = true;
            isDragging = false;

            Touch t0 = activeTouches[0];
            Touch t1 = activeTouches[1];

            if (IsOverUI(t0.screenPosition) || IsOverUI(t1.screenPosition))
                return;

            Vector2 prevPos0 = t0.screenPosition - t0.delta;
            Vector2 prevPos1 = t1.screenPosition - t1.delta;

            float prevDist = Vector2.Distance(prevPos0, prevPos1);
            float currDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);

            if (prevDist > 0.01f)
            {
                float scaleFactor = currDist / prevDist;
                Vector3 newScale = spawnedObject.transform.localScale * scaleFactor;
                float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);
                spawnedObject.transform.localScale = Vector3.one * clamped;
            }

            Vector2 prevDir = prevPos1 - prevPos0;
            Vector2 currDir = t1.screenPosition - t0.screenPosition;
            float angleDelta = Vector2.SignedAngle(prevDir, currDir);
            spawnedObject.transform.Rotate(Vector3.up, -angleDelta, Space.World);
            return;
        }

        // 2. Single-finger touch: Tap to Place / Select Organelle / Drag to Move
        if (activeTouches.Count == 1)
        {
            Touch touch = activeTouches[0];

            if (touch.phase == TouchPhase.Began)
            {
                if (IsOverUI(touch.screenPosition))
                {
                    suppressTap = true;
                    return;
                }

                touchStartPos = touch.screenPosition;
                isDragging = false;
                suppressTap = false;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                if (suppressTap)
                    return;

                float moveDist = Vector2.Distance(touch.screenPosition, touchStartPos);
                if (moveDist > TapMovementThreshold)
                {
                    isDragging = true;

                    bool isIsolated = OrganelleSelectionManager.Instance != null && OrganelleSelectionManager.Instance.IsIsolated;

                    // When an organelle is isolated or QR-tracked, dragging rotates the model in place
                    if (isIsolated || isTrackedImage)
                    {
                        if (spawnedObject != null && arCamera != null)
                        {
                            float rotSpeed = 0.25f;
                            spawnedObject.transform.Rotate(Vector3.up, -touch.delta.x * rotSpeed, Space.World);
                            spawnedObject.transform.Rotate(arCamera.transform.right, touch.delta.y * rotSpeed, Space.World);
                        }
                    }
                    else if (spawnedObject != null && inPlaneMode)
                    {
                        PlaceOrMove(touch.screenPosition, isInitialSpawn: false);
                    }
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (!suppressTap && !isDragging)
                {
                    HandleScreenTap(touch.screenPosition);
                }
                isDragging = false;
                suppressTap = false;
            }
            return;
        }

        // 3. Editor / PC Mouse fallback
#if UNITY_EDITOR
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame && !IsOverUI(mousePos))
            {
                HandleScreenTap(mousePos);
            }
            else if (Mouse.current.rightButton.isPressed && spawnedObject != null && arCamera != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                spawnedObject.transform.Rotate(Vector3.up, -delta.x * 0.3f, Space.World);
                spawnedObject.transform.Rotate(arCamera.transform.right, delta.y * 0.3f, Space.World);
            }
        }
#endif
    }

    private bool IsInPlaneMode()
    {
        if (!canPlace)
            return false;

        if (planeManager != null && !planeManager.enabled)
            return false;

        if (raycastManager != null && !raycastManager.enabled)
            return false;

        return true;
    }

    private void HandleScreenTap(Vector2 screenPos)
    {
        if (IsOverUI(screenPos))
            return;

        bool hasActiveCell = spawnedObject != null && spawnedObject.activeInHierarchy;

        // If we are in AR Plane mode and no active cell is placed yet, spawn one on the plane
        if (IsInPlaneMode() && (!hasActiveCell || isTrackedImage))
        {
            PlaceOrMove(screenPos, isInitialSpawn: true);
            return;
        }

        // Otherwise, raycast to select an organelle on the active cell (Plane or QR)
        if (OrganelleSelectionManager.Instance != null)
        {
            bool selected = OrganelleSelectionManager.Instance.TrySelectAtScreenPoint(screenPos, arCamera);

            // Re-link spawnedObject if a QR cell was tapped after switching modes
            if (selected && spawnedObject == null && OrganelleSelectionManager.Instance.Current != null)
            {
                CellOrganelleSetup setup = OrganelleSelectionManager.Instance.Current.GetComponentInParent<CellOrganelleSetup>();
                if (setup != null)
                {
                    spawnedObject = setup.gameObject;
                    isTrackedImage = !IsInPlaneMode();
                }
            }
        }
    }

    private void PlaceOrMove(Vector2 screenPos, bool isInitialSpawn)
    {
        if (raycastManager == null)
            return;

        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            Vector3 targetPos = hitPose.position + Vector3.up * verticalOffset;

            if (isInitialSpawn)
            {
                if (cellPrefab == null)
                    return;

                if (spawnedObject != null && !isTrackedImage)
                {
                    Destroy(spawnedObject);
                }

                spawnedObject = Instantiate(cellPrefab, targetPos, hitPose.rotation);
                spawnedObject.transform.localScale = Vector3.one * initialScale;
                isTrackedImage = false;

                if (spawnedObject.GetComponent<CellOrganelleSetup>() == null)
                {
                    spawnedObject.AddComponent<CellOrganelleSetup>();
                }
            }
            else if (spawnedObject != null && !isTrackedImage)
            {
                spawnedObject.transform.position = targetPos;
            }
        }
    }

    public void SetSpawnedObject(GameObject obj)
    {
        SetSpawnedObject(obj, obj != null);
    }

    public void SetSpawnedObject(GameObject obj, bool tracked)
    {
        if (OrganelleSelectionManager.Instance != null && spawnedObject != obj)
        {
            OrganelleSelectionManager.Instance.ClearSelection();
        }

        spawnedObject = obj;
        isTrackedImage = tracked && (obj != null);
    }

    public void ClearSpawned()
    {
        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance.ClearSelection();
        }

        if (spawnedObject != null && !isTrackedImage)
        {
            Destroy(spawnedObject);
        }

        spawnedObject = null;
        isTrackedImage = false;
        isDragging = false;
        suppressTap = false;
    }

    public void ToggleIsolateOrganelle()
    {
        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance.ToggleIsolateCurrent();
        }
    }

    private bool IsOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}