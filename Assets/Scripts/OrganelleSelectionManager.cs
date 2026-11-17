using System;
using System.Collections.Generic;
using UnityEngine;

public class OrganelleSelectionManager : MonoBehaviour
{
    public static OrganelleSelectionManager Instance { get; private set; }

    public static event Action<OrganelleTarget> OnOrganelleSelected;
    public static event Action OnSelectionCleared;
    public static event Action<bool> OnIsolationStateChanged;

    [Header("References")]
    public OrganelleInfoUI infoUI;
    public Camera arCamera;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private float maxRayDistance = 25f;

    [Header("Isolation Settings")]
    [Tooltip("Moves the isolated organelle to the center of the cell so rotation pivots cleanly.")]
    public bool centerWhenIsolated = true;

    [Tooltip("Scale multiplier applied to the isolated organelle for closer inspection.")]
    public float isolatedScaleMultiplier = 1.5f;

    public OrganelleTarget Current { get; private set; }
    public bool IsIsolated { get; private set; }

    private OrganelleTarget _currentSelection;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

    private Transform _isolatedTransform;
    private Vector3 _savedLocalPos;
    private Quaternion _savedLocalRot;
    private Vector3 _savedLocalScale;
    private readonly Dictionary<Renderer, bool> _cachedRenderers = new Dictionary<Renderer, bool>();
    private readonly Dictionary<Collider, bool> _cachedColliders = new Dictionary<Collider, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (infoUI == null)
        {
            infoUI = FindAnyObjectByType<OrganelleInfoUI>();
        }
    }

    public bool TrySelectAtScreenPoint(Vector2 screenPoint, Camera cam = null)
    {
        Camera activeCam = cam != null ? cam : (arCamera != null ? arCamera : Camera.main);
        if (activeCam == null)
            return false;

        Ray ray = activeCam.ScreenPointToRay(screenPoint);
        int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, maxRayDistance, selectableLayers, QueryTriggerInteraction.Ignore);

        if (hitCount > 0)
        {
            float closestDist = float.MaxValue;
            OrganelleTarget bestTarget = null;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                OrganelleTarget target = hit.collider.GetComponentInParent<OrganelleTarget>();

                if (target != null && hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    bestTarget = target;
                }
            }

            if (bestTarget != null)
            {
                Select(bestTarget);
                return true;
            }
        }

        // Keep the organelle isolated when tapping empty space so the user can rotate/zoom freely
        if (IsIsolated)
            return true;

        ClearSelection();
        return false;
    }

    public void Select(OrganelleTarget target)
    {
        if (target == null)
        {
            ClearSelection();
            return;
        }

        if (_currentSelection == target)
            return;

        if (IsIsolated)
        {
            RestoreFullCell();
        }

        if (_currentSelection != null)
        {
            _currentSelection.SetSelected(false);
        }

        _currentSelection = target;
        Current = target;
        _currentSelection.SetSelected(true);

        if (infoUI != null)
        {
            infoUI.DisplayOrganelle(target.OrganelleName, target.DescriptionPages);
        }

        OnOrganelleSelected?.Invoke(_currentSelection);
    }

    public void ClearSelection()
    {
        if (IsIsolated)
        {
            RestoreFullCell();
        }

        if (_currentSelection != null)
        {
            _currentSelection.SetSelected(false);
            _currentSelection = null;
        }

        Current = null;

        if (infoUI != null)
        {
            infoUI.HidePanel();
        }

        OnSelectionCleared?.Invoke();
    }

    public void ToggleIsolateCurrent()
    {
        if (IsIsolated)
        {
            RestoreFullCell();
        }
        else
        {
            IsolateCurrentOrganelle();
        }
    }

    public void IsolateCurrentOrganelle()
    {
        if (Current == null || IsIsolated)
            return;

        Transform cellRoot = GetActiveCellRoot(Current.transform);
        if (cellRoot == null)
            return;

        _isolatedTransform = Current.transform;

        _savedLocalPos = _isolatedTransform.localPosition;
        _savedLocalRot = _isolatedTransform.localRotation;
        _savedLocalScale = _isolatedTransform.localScale;

        _cachedRenderers.Clear();
        _cachedColliders.Clear();

        Renderer[] allRenderers = cellRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in allRenderers)
        {
            _cachedRenderers[rend] = rend.enabled;
            bool belongsToSelected = (rend.transform == _isolatedTransform) || rend.transform.IsChildOf(_isolatedTransform);
            rend.enabled = belongsToSelected;
        }

        Collider[] allColliders = cellRoot.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in allColliders)
        {
            _cachedColliders[col] = col.enabled;
            bool belongsToSelected = (col.transform == _isolatedTransform) || col.transform.IsChildOf(_isolatedTransform);
            col.enabled = belongsToSelected;
        }

        Current.SetSelected(false);

        if (centerWhenIsolated)
        {
            _isolatedTransform.position = cellRoot.position;
            _isolatedTransform.localScale = _savedLocalScale * isolatedScaleMultiplier;
        }

        IsIsolated = true;
        OnIsolationStateChanged?.Invoke(true);
    }

    public void RestoreFullCell()
    {
        if (!IsIsolated)
            return;

        if (_isolatedTransform != null)
        {
            _isolatedTransform.localPosition = _savedLocalPos;
            _isolatedTransform.localRotation = _savedLocalRot;
            _isolatedTransform.localScale = _savedLocalScale;
        }

        foreach (KeyValuePair<Renderer, bool> kvp in _cachedRenderers)
        {
            if (kvp.Key != null)
                kvp.Key.enabled = kvp.Value;
        }

        foreach (KeyValuePair<Collider, bool> kvp in _cachedColliders)
        {
            if (kvp.Key != null)
                kvp.Key.enabled = kvp.Value;
        }

        _cachedRenderers.Clear();
        _cachedColliders.Clear();

        if (Current != null)
        {
            Current.SetSelected(true);
        }

        _isolatedTransform = null;
        IsIsolated = false;
        OnIsolationStateChanged?.Invoke(false);
    }

    private Transform GetActiveCellRoot(Transform organelleTransform)
    {
        ARCellInteraction interaction = FindAnyObjectByType<ARCellInteraction>();
        if (interaction != null && interaction.SpawnedObject != null)
        {
            return interaction.SpawnedObject.transform;
        }

        CellOrganelleSetup setup = organelleTransform.GetComponentInParent<CellOrganelleSetup>();
        if (setup != null)
        {
            return setup.transform;
        }

        return organelleTransform.root;
    }
}