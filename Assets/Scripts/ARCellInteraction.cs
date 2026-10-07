using System.Collections.Generic;
using UnityEngine;
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

    [Header("Scale Boundaries (Meters)")]
    [SerializeField] private float initialScale = 0.1f;
    [SerializeField] private float minScale = 0.03f;
    [SerializeField] private float maxScale = 0.5f;

    [Header("Elevation Offset")]
    [SerializeField] private float verticalOffset = 0.0f;

    private GameObject spawnedObject;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isDragging = false;

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();
        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();
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

            if (touch.phase == TouchPhase.Began && spawnedObject == null)
            {
                PlaceOrMove(touch.screenPosition, isInitialSpawn: true);
            }
            else if (touch.phase == TouchPhase.Moved && spawnedObject != null)
            {
                PlaceOrMove(touch.screenPosition, isInitialSpawn: false);
            }
        }
        // --- MOBILE 2-FINGER TOUCH: PINCH & ROTATE ---
        else if (activeTouches.Count == 2 && spawnedObject != null)
        {
            var touch0 = activeTouches[0];
            var touch1 = activeTouches[1];

            Vector2 prevPos0 = touch0.screenPosition - touch0.delta;
            Vector2 prevPos1 = touch1.screenPosition - touch1.delta;

            // Pinch-to-scale
            float prevDist = (prevPos0 - prevPos1).magnitude;
            float curDist = (touch0.screenPosition - touch1.screenPosition).magnitude;
            float factor = curDist / (prevDist <= 0.001f ? 1f : prevDist);

            Vector3 targetScale = spawnedObject.transform.localScale * factor;
            float clampedScale = Mathf.Clamp(targetScale.x, minScale, maxScale);
            spawnedObject.transform.localScale = Vector3.one * clampedScale;

            // Twist-to-rotate
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

            // 1. ROTATE: Hold 'R' + Left-Click Drag OR Middle-Mouse Drag
            if (spawnedObject != null && (isMiddleMouseDragging || (isRotateKeyPressed && Mouse.current.leftButton.isPressed)))
            {
                float rotationSpeed = 0.4f;
                spawnedObject.transform.Rotate(Vector3.up, -mouseDelta.x * rotationSpeed, Space.World);
            }
            // 2. REPOSITION / SPAWN: Normal Left-Click Drag
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (spawnedObject == null)
                {
                    PlaceOrMove(mousePos, isInitialSpawn: true);
                }
                else
                {
                    isDragging = true;
                }
            }
            else if (Mouse.current.leftButton.isPressed && isDragging && spawnedObject != null)
            {
                PlaceOrMove(mousePos, isInitialSpawn: false);
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }

            // 3. ZOOM: Mouse Scroll Wheel
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0 && spawnedObject != null)
            {
                float newScale = Mathf.Clamp(spawnedObject.transform.localScale.x + (scroll * 0.0005f), minScale, maxScale);
                spawnedObject.transform.localScale = Vector3.one * newScale;
            }
        }
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
            }
            else
            {
                spawnedObject.transform.position = targetPos;
            }
        }
    }
}