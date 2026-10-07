using System;
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

    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask selectableLayers = ~0;
    [SerializeField] private float maxDistance = 100f;

    private static OrganelleSelectionManager _instance;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[64];

    public OrganelleTarget Current { get; private set; }

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

        // Show the selected organelle's name even if no info panel has been built in the scene.
        if (FindFirstObjectByType<OrganelleInfoUI>() == null && GetComponent<OrganelleSelectionLabel>() == null)
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

        if (Current != null) Current.SetSelected(false);

        Current = target;
        Current.SetSelected(true);
        OnOrganelleSelected?.Invoke(target);
    }

    public void ClearSelection()
    {
        if (Current == null) return;

        Current.SetSelected(false);
        Current = null;
        OnSelectionCleared?.Invoke();
    }
}
