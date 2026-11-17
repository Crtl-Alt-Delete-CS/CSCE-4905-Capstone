using UnityEngine;

public class OrganelleTarget : MonoBehaviour
{
    [Header("Organelle Info")]
    [SerializeField] private string organelleName;

    [TextArea(3, 6)]
    public string description;

    public string[] descriptionPages;

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

    public string OrganelleName => !string.IsNullOrEmpty(organelleName) ? organelleName : gameObject.name;
    public bool IsSelected { get; private set; }

    public string[] DescriptionPages
    {
        get
        {
            if (descriptionPages != null && descriptionPages.Length > 0)
                return descriptionPages;

            if (!string.IsNullOrEmpty(description))
                return description.Split(new[] { "\n\n", "|" }, System.StringSplitOptions.RemoveEmptyEntries);

            return new[] { "No description available." };
        }
    }

    private Renderer[] _renderers;
    private Color[] _originalColors;
    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    private void Awake()
    {
        CacheRenderers();
    }

    /// <summary>
    /// Overload accepting a single description string.
    /// </summary>
    public void Initialize(string title, string desc)
    {
        organelleName = title;
        description = desc;
        descriptionPages = !string.IsNullOrEmpty(desc)
            ? desc.Split(new[] { "\n\n", "|" }, System.StringSplitOptions.RemoveEmptyEntries)
            : new string[0];
        CacheRenderers();
    }

    /// <summary>
    /// Overload called by CellOrganelleSetup.cs (lines 109 and 113) accepting string[] pages.
    /// </summary>
    public void Initialize(string title, string[] pages)
    {
        organelleName = title;
        descriptionPages = pages ?? new string[0];
        description = (pages != null && pages.Length > 0) ? string.Join("\n\n", pages) : string.Empty;
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r != null && r.sharedMaterial != null)
            {
                if (r.sharedMaterial.HasProperty(BaseColorProp))
                    _originalColors[i] = r.sharedMaterial.GetColor(BaseColorProp);
                else if (r.sharedMaterial.HasProperty(ColorProp))
                    _originalColors[i] = r.sharedMaterial.GetColor(ColorProp);
                else
                    _originalColors[i] = Color.white;
            }
        }
    }

    public void SetSelected(bool selected)
    {
        if (IsSelected == selected)
            return;

        IsSelected = selected;

        if (selected)
            ApplyHighlight();
        else
            RemoveHighlight();
    }

    private void ApplyHighlight()
    {
        if (_renderers == null)
            CacheRenderers();

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null || r.material == null)
                continue;

            if (r.material.HasProperty(BaseColorProp))
                r.material.SetColor(BaseColorProp, highlightColor);
            else if (r.material.HasProperty(ColorProp))
                r.material.SetColor(ColorProp, highlightColor);
        }
    }

    private void RemoveHighlight()
    {
        if (_renderers == null || _originalColors == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer r = _renderers[i];
            if (r == null || r.material == null)
                continue;

            if (r.material.HasProperty(BaseColorProp))
                r.material.SetColor(BaseColorProp, _originalColors[i]);
            else if (r.material.HasProperty(ColorProp))
                r.material.SetColor(ColorProp, _originalColors[i]);
        }
    }
}