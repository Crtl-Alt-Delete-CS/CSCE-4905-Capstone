using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single place that owns "which organelle is selected". Input is detected by ARCellInteraction
/// (which already separates taps from drags / pinches) and forwarded to TrySelectAtScreenPoint.
/// Created automatically on first use, so it doesn't have to be placed in the scene.
/// </summary>
public class OrganelleSelectionManager : MonoBehaviour
{
    public static event Action<OrganelleTarget> OnOrganelleSelected;
    public static event Action OnSelectionCleared;
    public static event Action<bool> OnSingleViewChanged;

    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private OrganelleInfoUI infoUI;

    private static OrganelleSelectionManager _instance;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[64];
    private readonly List<GameObject> hiddenCellParts = new List<GameObject>();

    public OrganelleTarget Current { get; private set; }
    public bool IsSingleView { get; private set; }

    public static OrganelleSelectionManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<OrganelleSelectionManager>();

            if (_instance == null)
                _instance = new GameObject("OrganelleSelectionManager").AddComponent<OrganelleSelectionManager>();

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (infoUI == null)
            infoUI = FindFirstObjectByType<OrganelleInfoUI>();

        // Show the selected organelle's name even if no info panel has been built in the scene.
        if (infoUI == null && GetComponent<OrganelleSelectionLabel>() == null)
            gameObject.AddComponent<OrganelleSelectionLabel>();
    }

    /// <summary>Selects the organelle under the screen point, or clears the selection if none was hit.</summary>
    public bool TrySelectAtScreenPoint(Vector2 screenPoint, Camera cam = null)
    {
        cam = cam != null ? cam : (arCamera != null ? arCamera : Camera.main);
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(screenPoint);
        int count = Physics.RaycastNonAlloc(ray, HitBuffer, maxDistance, selectableLayers, QueryTriggerInteraction.Ignore);

        // Nearest organelle wins; colliders that don't belong to an organelle (AR planes, etc.) are skipped.
        OrganelleTarget best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (HitBuffer[i].distance >= bestDistance) continue;
            OrganelleTarget t = HitBuffer[i].collider.GetComponentInParent<OrganelleTarget>();
            if (t == null) continue;
            best = t;
            bestDistance = HitBuffer[i].distance;
        }

        if (best != null)
        {
            Select(best);
            return true;
        }

        ClearSelection();
        return false;
    }

    public void Select(OrganelleTarget target)
    {
        if (target == null) { ClearSelection(); return; }
        if (target == Current) return;

        bool keepSingleView = IsSingleView;
        if (keepSingleView)
            RestoreCellParts();

        if (Current != null) Current.SetSelected(false);

        Current = target;
        Current.SetSelected(true);
        OnOrganelleSelected?.Invoke(target);

        if (keepSingleView)
            HideEverythingExcept(Current);

        if (infoUI == null)
            infoUI = FindFirstObjectByType<OrganelleInfoUI>();

        if (infoUI != null)
        {
            OrganelleData data = target.GetComponentInParent<OrganelleData>();
            if (data != null)
                infoUI.DisplayOrganelle(data);
            else
                Debug.LogWarning($"OrganelleData script is missing on {target.gameObject.name}");
        }
    }

    /// <summary>
    /// Hides every other part of the current cell so only the selected organelle is visible.
    /// </summary>
    public void ToggleSingleView()
    {
        if (IsSingleView)
            ExitSingleView();
        else
            EnterSingleView();
    }

    public void EnterSingleView()
    {
        if (Current == null || IsSingleView)
            return;

        HideEverythingExcept(Current);
        IsSingleView = true;
        Current.RefreshOutline();
        OnSingleViewChanged?.Invoke(true);
    }

    public void ExitSingleView()
    {
        if (!IsSingleView && hiddenCellParts.Count == 0)
            return;

        RestoreCellParts();
        IsSingleView = false;
        if (Current != null)
            Current.RefreshOutline();
        OnSingleViewChanged?.Invoke(false);
    }

    public void ClearSelection()
    {
        if (IsSingleView)
            ExitSingleView();

        if (Current == null) return;

        Current.SetSelected(false);
        Current = null;
        OnSelectionCleared?.Invoke();

        if (infoUI != null)
            infoUI.HidePanel();
    }

    private void HideEverythingExcept(OrganelleTarget target)
    {
        RestoreCellParts();
        if (target == null)
            return;

        CellOrganelleSetup cell = target.GetComponentInParent<CellOrganelleSetup>();
        if (cell == null)
            return;

        Transform keep = target.transform;
        Transform node = keep;
        while (node != null && node != cell.transform)
        {
            Transform parent = node.parent;
            if (parent == null)
                break;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == node || child.name.StartsWith("_") || !child.gameObject.activeSelf)
                    continue;

                child.gameObject.SetActive(false);
                hiddenCellParts.Add(child.gameObject);
            }

            node = parent;
        }
    }

    private void RestoreCellParts()
    {
        for (int i = 0; i < hiddenCellParts.Count; i++)
        {
            if (hiddenCellParts[i] != null)
                hiddenCellParts[i].SetActive(true);
        }

        hiddenCellParts.Clear();
    }
}
