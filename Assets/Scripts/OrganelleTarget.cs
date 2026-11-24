using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Marks a GameObject (and everything under it) as one selectable organelle.
/// Draws a screen-space outline while selected, except during single-organelle view.
/// </summary>
public class OrganelleTarget : MonoBehaviour
{
    [SerializeField] private string organelleName = "Organelle";

    [Header("Outline")]
    [SerializeField] private Color outlineColor = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField] private float outlinePixels = 3f;

    public string OrganelleName => organelleName;
    public bool IsSelected { get; private set; }
    public string[] Pages { get; private set; }
    public string description => (Pages != null && Pages.Length > 0) ? Pages[0] : string.Empty;

    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlinePixelsId = Shader.PropertyToID("_OutlinePixels");
    private static Shader outlineShader;

    private readonly List<GameObject> outlineObjects = new List<GameObject>();
    private Material outlineMaterial;
    private bool outlineApplied;

    public void Initialize(string displayName, string[] pages)
    {
        organelleName = displayName;
        Pages = pages;
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        RefreshOutline();
    }

    /// <summary>Shows or hides the outline based on selection and single-organelle view.</summary>
    public void RefreshOutline()
    {
        bool show = IsSelected && !OrganelleSelectionManager.Instance.IsSingleView;
        if (show)
            ApplyOutline();
        else
            RemoveOutline();
    }

    private void ApplyOutline()
    {
        if (outlineApplied)
            return;

        Shader shader = GetOutlineShader();
        if (shader == null)
            return;

        outlineMaterial = new Material(shader);
        outlineMaterial.name = "Organelle Outline";
        outlineMaterial.SetColor(OutlineColorId, outlineColor);
        outlineMaterial.SetFloat(OutlinePixelsId, outlinePixels);

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer meshRenderer = renderers[i] as MeshRenderer;
            if (meshRenderer == null)
                continue;

            MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            CreateOutlineShell(meshRenderer.transform, filter.sharedMesh);
        }

        outlineApplied = true;
    }

    private void CreateOutlineShell(Transform source, Mesh mesh)
    {
        GameObject shell = new GameObject("Organelle Outline");
        shell.transform.SetParent(source, false);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localRotation = Quaternion.identity;
        shell.transform.localScale = Vector3.one;

        MeshFilter shellFilter = shell.AddComponent<MeshFilter>();
        shellFilter.sharedMesh = mesh;

        MeshRenderer shellRenderer = shell.AddComponent<MeshRenderer>();
        Material[] materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++)
            materials[i] = outlineMaterial;

        shellRenderer.sharedMaterials = materials;
        shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
        shellRenderer.receiveShadows = false;
        shellRenderer.lightProbeUsage = LightProbeUsage.Off;
        shellRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        shellRenderer.allowOcclusionWhenDynamic = false;

        outlineObjects.Add(shell);
    }

    private void RemoveOutline()
    {
        for (int i = 0; i < outlineObjects.Count; i++)
        {
            if (outlineObjects[i] != null)
                Destroy(outlineObjects[i]);
        }

        outlineObjects.Clear();

        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
            outlineMaterial = null;
        }

        outlineApplied = false;
    }

    private static Shader GetOutlineShader()
    {
        if (outlineShader == null)
            outlineShader = Resources.Load<Shader>("Shaders/OrganelleOutline");

        if (outlineShader == null)
            Debug.LogWarning("Organelle outline shader is missing from Resources/Shaders.");

        return outlineShader;
    }

    private void OnDestroy()
    {
        RemoveOutline();
    }
}
