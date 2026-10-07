using UnityEngine;

/// <summary>
/// Marks a GameObject (and everything under it) as one selectable organelle.
/// Handles the visual highlight; selection logic lives in OrganelleSelectionManager.
/// </summary>
public class OrganelleTarget : MonoBehaviour
{
    [SerializeField] private string organelleName = "Organelle";
    [TextArea] public string description;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.1f);
    [SerializeField, Range(0f, 5f)] private float emissionIntensity = 1.5f;

    public string OrganelleName => organelleName;
    public bool IsSelected { get; private set; }

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Renderer[] _renderers;
    private Material[][] _originalMaterials;
    private Material[][] _highlightMaterials;

    public void Initialize(string displayName, string info)
    {
        organelleName = displayName;
        description = info;
    }

    public void SetSelected(bool selected)
    {
        if (selected == IsSelected) return;

        if (selected) ApplyHighlight();
        else RemoveHighlight();

        IsSelected = selected;
    }

    private void ApplyHighlight()
    {
        // Search lazily: the highlighted organelle may be a group (e.g. a mitochondrion with two membranes).
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalMaterials = new Material[_renderers.Length][];
        _highlightMaterials = new Material[_renderers.Length][];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Material[] shared = _renderers[i].sharedMaterials;
            var copies = new Material[shared.Length];

            for (int j = 0; j < shared.Length; j++)
            {
                if (shared[j] == null) continue;

                // Instance copies so the shared cell materials (used by every other organelle) stay untouched.
                var copy = new Material(shared[j]);
                copy.EnableKeyword("_EMISSION");
                copy.SetColor(EmissionColorId, highlightColor * emissionIntensity);
                copies[j] = copy;
            }

            _originalMaterials[i] = shared;
            _highlightMaterials[i] = copies;
            _renderers[i].sharedMaterials = copies;
        }
    }

    private void RemoveHighlight()
    {
        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
                _renderers[i].sharedMaterials = _originalMaterials[i];

            foreach (Material m in _highlightMaterials[i])
                if (m != null) Destroy(m);
        }

        _renderers = null;
        _originalMaterials = null;
        _highlightMaterials = null;
    }

    private void OnDestroy()
    {
        if (IsSelected) RemoveHighlight();
    }
}
