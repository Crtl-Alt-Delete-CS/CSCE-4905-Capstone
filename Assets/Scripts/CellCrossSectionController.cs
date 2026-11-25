using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Drives a real-time 3D cross-section clipping plane across the active cell
/// (or the isolated organelle while in single-organelle view).
/// </summary>
public class CellCrossSectionController : MonoBehaviour
{
    private const string CrossSectionShaderPath = "Shaders/CrossSectionLit";
    private const float ActiveThreshold = 0.001f;

    private static readonly int CrossSectionEnabledId = Shader.PropertyToID("_CrossSectionEnabled");
    private static readonly int CrossSectionPlaneId = Shader.PropertyToID("_CrossSectionPlane");
    private static readonly int CrossSectionEdgeWidthId = Shader.PropertyToID("_CrossSectionEdgeWidth");
    private static readonly int CullPropertyId = Shader.PropertyToID("_Cull");

    private static bool globalClipEnabled;
    private static Vector3 globalPlaneNormal;
    private static float globalPlaneDistance;

    public event Action<float> OnSliceAmountChanged;

    [SerializeField] private ARCellInteraction cellInteraction;

    private Shader crossSectionShader;
    private GameObject boundCell;
    private Transform boundsRoot;
    private float minLocalY;
    private float maxLocalY;
    private bool hasBounds;
    private float sliceAmount;
    private bool materialsSwapped;

    private readonly List<RendererEntry> rendererEntries = new List<RendererEntry>();
    private readonly Dictionary<Material, Material> materialCache = new Dictionary<Material, Material>();

    private struct RendererEntry
    {
        public Renderer Renderer;
        public Material[] OriginalMaterials;
        public Material[] CrossSectionMaterials;
    }

    public float SliceAmount => sliceAmount;

    /// <summary>Returns true when a world-space point lies on the clipped-away side of the cross-section plane.</summary>
    public static bool IsPointClipped(Vector3 worldPoint)
    {
        if (!globalClipEnabled)
            return false;

        return Vector3.Dot(worldPoint, globalPlaneNormal) + globalPlaneDistance > 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        ResetGlobalShaderState();
    }

    private void Awake()
    {
        if (cellInteraction == null)
            cellInteraction = FindFirstObjectByType<ARCellInteraction>();

        crossSectionShader = Resources.Load<Shader>(CrossSectionShaderPath);
        if (crossSectionShader == null)
            Debug.LogWarning($"CrossSectionLit shader not found at Resources/{CrossSectionShaderPath}.");

        ResetGlobalShaderState();
    }

    private void OnEnable()
    {
        if (cellInteraction == null)
            cellInteraction = FindFirstObjectByType<ARCellInteraction>();

        if (cellInteraction != null)
        {
            cellInteraction.OnActiveCellChanged += HandleActiveCellChanged;
            cellInteraction.OnCellReset += HandleCellReset;
            HandleActiveCellChanged(cellInteraction.CurrentCell);
        }

        OrganelleSelectionManager.OnSingleViewChanged += HandleSingleViewChanged;
        OrganelleSelectionManager.OnOrganelleSelected += HandleOrganelleSelected;
    }

    private void OnDisable()
    {
        if (cellInteraction != null)
        {
            cellInteraction.OnActiveCellChanged -= HandleActiveCellChanged;
            cellInteraction.OnCellReset -= HandleCellReset;
        }

        OrganelleSelectionManager.OnSingleViewChanged -= HandleSingleViewChanged;
        OrganelleSelectionManager.OnOrganelleSelected -= HandleOrganelleSelected;

        RestoreOriginalMaterials();
        ResetGlobalShaderState();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterials();
        ClearMaterialCache();
        ResetGlobalShaderState();
    }

    private void LateUpdate()
    {
        if (sliceAmount <= ActiveThreshold || boundCell == null)
            return;

        if (!boundCell.activeInHierarchy)
        {
            ResetGlobalShaderState();
            return;
        }

        UpdateGlobalPlane();
    }

    /// <summary>Sets the normalized cross-section depth in [0, 1], where 0 shows the intact cell.</summary>
    public void SetSliceAmount(float value)
    {
        float clamped = Mathf.Clamp01(value);
        bool changed = !Mathf.Approximately(clamped, sliceAmount);
        sliceAmount = clamped;

        ApplyCurrentSliceState();

        if (changed)
            OnSliceAmountChanged?.Invoke(sliceAmount);
    }

    public void ResetSlice()
    {
        SetSliceAmount(0f);
    }

    private void HandleActiveCellChanged(GameObject cell)
    {
        if (boundCell == cell)
        {
            RefreshBounds();
            ApplyCurrentSliceState();
            return;
        }

        RestoreOriginalMaterials();
        ClearMaterialCache();
        rendererEntries.Clear();
        boundCell = cell;
        hasBounds = false;

        if (boundCell != null)
        {
            CaptureRenderers(boundCell);
            RefreshBounds();
        }

        ApplyCurrentSliceState();
    }

    private void HandleCellReset()
    {
        ResetSlice();
    }

    private void HandleSingleViewChanged(bool _)
    {
        RefreshBounds();
        ApplyCurrentSliceState();
    }

    private void HandleOrganelleSelected(OrganelleTarget _)
    {
        if (OrganelleSelectionManager.Instance != null && OrganelleSelectionManager.Instance.IsSingleView)
        {
            RefreshBounds();
            ApplyCurrentSliceState();
        }
    }

    private void ApplyCurrentSliceState()
    {
        if (sliceAmount <= ActiveThreshold || boundCell == null || !boundCell.activeInHierarchy)
        {
            RestoreOriginalMaterials();
            ResetGlobalShaderState();
            return;
        }

        ApplyCrossSectionMaterials();
        if (!hasBounds)
            RefreshBounds();

        if (hasBounds)
            UpdateGlobalPlane();
        else
            ResetGlobalShaderState();
    }

    private void CaptureRenderers(GameObject cell)
    {
        if (cell == null)
            return;

        Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;

            string objName = r.gameObject.name;
            if (objName == "Organelle Outline" || objName.StartsWith("_"))
                continue;

            Material[] originals = r.sharedMaterials;
            if (originals == null || originals.Length == 0)
                continue;

            Material[] clones = new Material[originals.Length];
            for (int m = 0; m < originals.Length; m++)
                clones[m] = GetOrCreateCrossSectionMaterial(originals[m]);

            rendererEntries.Add(new RendererEntry
            {
                Renderer = r,
                OriginalMaterials = originals,
                CrossSectionMaterials = clones
            });
        }
    }

    private Material GetOrCreateCrossSectionMaterial(Material source)
    {
        if (source == null || crossSectionShader == null)
            return source;

        if (materialCache.TryGetValue(source, out Material cached) && cached != null)
            return cached;

        Material clone = new Material(source);
        clone.name = source.name + " (CrossSection)";
        clone.shader = crossSectionShader;
        clone.renderQueue = source.renderQueue;

        // Opaque shells need double-sided rendering so their hollow interior is visible when sliced.
        if (source.renderQueue < 3000)
            clone.SetInt(CullPropertyId, (int)CullMode.Off);

        materialCache[source] = clone;
        return clone;
    }

    private void ApplyCrossSectionMaterials()
    {
        if (materialsSwapped || crossSectionShader == null)
            return;

        for (int i = 0; i < rendererEntries.Count; i++)
        {
            RendererEntry entry = rendererEntries[i];
            if (entry.Renderer != null && entry.CrossSectionMaterials != null)
                entry.Renderer.sharedMaterials = entry.CrossSectionMaterials;
        }

        materialsSwapped = true;
    }

    private void RestoreOriginalMaterials()
    {
        if (!materialsSwapped)
            return;

        for (int i = 0; i < rendererEntries.Count; i++)
        {
            RendererEntry entry = rendererEntries[i];
            if (entry.Renderer != null && entry.OriginalMaterials != null)
                entry.Renderer.sharedMaterials = entry.OriginalMaterials;
        }

        materialsSwapped = false;
    }

    private void ClearMaterialCache()
    {
        foreach (Material mat in materialCache.Values)
        {
            if (mat != null)
                Destroy(mat);
        }

        materialCache.Clear();
    }

    private void RefreshBounds()
    {
        hasBounds = false;
        boundsRoot = ResolveBoundsRoot();
        if (boundsRoot == null)
            return;

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        bool found = false;

        Matrix4x4 rootWorldToLocal = boundsRoot.worldToLocalMatrix;
        Renderer[] renderers = boundsRoot.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                continue;

            string objName = r.gameObject.name;
            if (objName == "Organelle Outline" || objName.StartsWith("_"))
                continue;

            Bounds localBounds;
            if (r is MeshRenderer)
            {
                MeshFilter filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;
                localBounds = filter.sharedMesh.bounds;
            }
            else if (r is SkinnedMeshRenderer skinned)
            {
                localBounds = skinned.localBounds;
            }
            else
            {
                continue;
            }

            Matrix4x4 toRootLocal = rootWorldToLocal * r.transform.localToWorldMatrix;
            EncapsulateProjectedY(localBounds, toRootLocal, ref minY, ref maxY);
            found = true;
        }

        if (found && maxY > minY)
        {
            minLocalY = minY;
            maxLocalY = maxY;
            hasBounds = true;
        }
    }

    private Transform ResolveBoundsRoot()
    {
        if (boundCell == null)
            return null;

        OrganelleSelectionManager selection = OrganelleSelectionManager.Instance;
        if (selection != null && selection.IsSingleView && selection.Current != null &&
            selection.Current.transform.IsChildOf(boundCell.transform))
        {
            return selection.Current.transform;
        }

        return boundCell.transform;
    }

    private static void EncapsulateProjectedY(Bounds localBounds, Matrix4x4 matrix, ref float minY, ref float maxY)
    {
        Vector3 c = localBounds.center;
        Vector3 e = localBounds.extents;

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 corner = new Vector3(c.x + sx * e.x, c.y + sy * e.y, c.z + sz * e.z);
                    float y = matrix.MultiplyPoint3x4(corner).y;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }
    }

    private void UpdateGlobalPlane()
    {
        if (boundsRoot == null || !hasBounds)
        {
            ResetGlobalShaderState();
            return;
        }

        float maxCutY = maxLocalY + 0.005f;
        float minCutY = Mathf.Lerp(maxLocalY, minLocalY, 0.9f);
        float cutLocalY = Mathf.Lerp(maxCutY, minCutY, sliceAmount);

        Vector3 planePointWS = boundsRoot.TransformPoint(new Vector3(0f, cutLocalY, 0f));
        Vector3 planeNormalWS = boundsRoot.up.normalized;
        float planeDistance = -Vector3.Dot(planeNormalWS, planePointWS);

        float worldHeight = Mathf.Abs(maxLocalY - minLocalY) * boundsRoot.lossyScale.y;
        float edgeWidth = Mathf.Clamp(worldHeight * 0.006f, 0.0015f, 0.015f);

        globalClipEnabled = true;
        globalPlaneNormal = planeNormalWS;
        globalPlaneDistance = planeDistance;

        Shader.SetGlobalFloat(CrossSectionEnabledId, 1f);
        Shader.SetGlobalVector(CrossSectionPlaneId, new Vector4(planeNormalWS.x, planeNormalWS.y, planeNormalWS.z, planeDistance));
        Shader.SetGlobalFloat(CrossSectionEdgeWidthId, edgeWidth);
    }

    private static void ResetGlobalShaderState()
    {
        globalClipEnabled = false;
        globalPlaneNormal = Vector3.up;
        globalPlaneDistance = 0f;
        Shader.SetGlobalFloat(CrossSectionEnabledId, 0f);
    }
}
