using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Lives on the TrackedCell_Parent prefab (next to ARTrackedImage).
/// Shows exactly one cell child based on which reference image was detected,
/// and hides it while the image is not being tracked.
/// Matching is by substring (case-insensitive), so "AnimalCell" and "qr_Animal" both work.
/// </summary>
[RequireComponent(typeof(ARTrackedImage))]
public class CellPrefabSwitcher : MonoBehaviour
{
    [Header("Cell Child Objects")]
    [SerializeField] private GameObject animalCell;
    [SerializeField] private GameObject bacterialCell;
    [SerializeField] private GameObject fungalCell;
    [SerializeField] private GameObject plantCell;

    private ARTrackedImage trackedImage;
    private GameObject activeCell;
    private bool warnedUnmapped;
    private bool diagLogged;

    private static bool warnedAboutAssets;

    private void Awake()
    {
        trackedImage = GetComponent<ARTrackedImage>();

        animalCell    = ResolveChild(animalCell,    "AnimalCell");
        bacterialCell = ResolveChild(bacterialCell, "BacterialCell");
        fungalCell    = ResolveChild(fungalCell,    "FungalCell");
        plantCell     = ResolveChild(plantCell,     "PlantCell");
    }

    /// <summary>
    /// The fields above must reference the child objects INSIDE this prefab. If they reference the
    /// prefab assets from the Project window instead, SetActive() edits the asset files themselves and
    /// never affects the spawned cell. Anything that is not a child of this object is replaced by the
    /// child with the given name.
    /// </summary>
    private GameObject ResolveChild(GameObject assigned, string childName)
    {
        if (assigned != null && assigned.scene.IsValid() && assigned.transform.IsChildOf(transform))
            return assigned;

        if (assigned != null && !warnedAboutAssets)
        {
            warnedAboutAssets = true;
            Debug.LogWarning("CellPrefabSwitcher: a cell field points at a prefab asset instead of a child of " +
                             "TrackedCell_Parent. Using the child objects by name instead; re-assign the fields to " +
                             "the children in the prefab's Hierarchy to remove this warning.", this);
        }

        Transform child = transform.Find(childName); // Find also returns inactive children
        if (child == null)
        {
            Debug.LogError($"CellPrefabSwitcher: no child named '{childName}' under '{name}'.", this);
            return null;
        }
        return child.gameObject;
    }

    private void OnEnable()
    {
        // Also runs again when the ARTrackedImageManager is re-enabled (mode switch).
        activeCell = null;
        HideAll();
        Refresh();
    }

    private void Update()
    {
        // referenceImage can be filled in after the object is spawned, so keep trying until it is.
        if (activeCell == null)
            Refresh();

        if (activeCell != null)
        {
            bool tracked = trackedImage.trackingState == TrackingState.Tracking;
            if (activeCell.activeSelf != tracked)
                activeCell.SetActive(tracked);

            // Log once, a moment after the cell is shown, what every child looks like.
            if (tracked && !diagLogged)
            {
                diagLogged = true;
                LogDiagnostics();
            }
        }
    }

    private void LogDiagnostics()
    {
        string Describe(string label, GameObject go)
        {
            if (go == null) return $"  {label}: <not assigned>";
            string src = "n/a";
#if UNITY_EDITOR
            var original = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
            src = original != null ? UnityEditor.AssetDatabase.GetAssetPath(original) : "(not a prefab instance)";
#endif
            return $"  {label}: object '{go.name}', activeInHierarchy={go.activeInHierarchy}, prefab asset={src}";
        }

        Debug.Log("[CellDiag] image '" + trackedImage.referenceImage.name + "'\n"
            + Describe("animalCell   ", animalCell) + "\n"
            + Describe("bacterialCell", bacterialCell) + "\n"
            + Describe("fungalCell   ", fungalCell) + "\n"
            + Describe("plantCell    ", plantCell), this);
    }

    public void UpdateVisibleCell()
    {
        activeCell = null;
        HideAll();
        Refresh();
    }

    private void Refresh()
    {
        string imageName = trackedImage.referenceImage.name;
        if (string.IsNullOrEmpty(imageName))
            return; // not assigned yet

        string n = imageName.ToLowerInvariant();

        if (n.Contains("animal"))         activeCell = animalCell;
        else if (n.Contains("bacteria"))  activeCell = bacterialCell;
        else if (n.Contains("fungal") || n.Contains("fungi")) activeCell = fungalCell;
        else if (n.Contains("plant"))     activeCell = plantCell;

        if (activeCell != null)
            Debug.Log($"CellPrefabSwitcher: image '{imageName}' (trackable {trackedImage.trackableId}) -> showing '{activeCell.name}'", this);

        if (activeCell == null && !warnedUnmapped)
        {
            warnedUnmapped = true;
            Debug.LogWarning($"CellPrefabSwitcher: no cell mapped for reference image '{imageName}'.", this);
        }
    }

    private void HideAll()
    {
        if (animalCell) animalCell.SetActive(false);
        if (bacterialCell) bacterialCell.SetActive(false);
        if (fungalCell) fungalCell.SetActive(false);
        if (plantCell) plantCell.SetActive(false);
    }
}