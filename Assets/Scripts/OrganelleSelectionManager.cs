using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class OrganelleSelectionManager : MonoBehaviour
{
    public static event Action<OrganelleTarget> OnOrganelleSelected;
    public static event Action OnSelectionCleared;

    [SerializeField] private Camera arCamera;
    private OrganelleTarget _currentSelection;

    private void Awake()
    {
        if (arCamera == null) arCamera = Camera.main;
    }

    private void Update()
    {
        // Mobile single-tap detection
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            HandleRaycast(touchPos);
        }
        // Editor / mouse fallback
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            HandleRaycast(mousePos);
        }
    }

    private void HandleRaycast(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            OrganelleTarget target = hit.collider.GetComponentInParent<OrganelleTarget>();
            if (target != null)
            {
                SelectOrganelle(target);
                return;
            }
        }

        // Deselect when tapping off an organelle
        ClearSelection();
    }

    private void SelectOrganelle(OrganelleTarget target)
    {
        if (_currentSelection != null)
            _currentSelection.SetSelected(false);

        _currentSelection = target;
        _currentSelection.SetSelected(true);
        OnOrganelleSelected?.Invoke(target);
    }

    public void ClearSelection()
    {
        if (_currentSelection != null)
        {
            _currentSelection.SetSelected(false);
            _currentSelection = null;
            OnSelectionCleared?.Invoke();
        }
    }
}