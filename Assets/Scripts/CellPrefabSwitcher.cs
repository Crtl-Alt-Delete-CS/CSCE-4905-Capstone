using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImage))]
public class CellPrefabSwitcher : MonoBehaviour
{
    [SerializeField] GameObject animalCell;
    [SerializeField] GameObject bacterialCell;
    [SerializeField] GameObject fungalCell;
    [SerializeField] GameObject plantCell;

    ARTrackedImage trackedImage;
    GameObject active;

    void Awake() => trackedImage = GetComponent<ARTrackedImage>();

    void OnEnable() => Refresh();

    void Update()
    {
        // referenceImage can be populated after spawn, so keep it in sync
        if (active == null) Refresh();
        if (active != null)
            active.SetActive(trackedImage.trackingState == TrackingState.Tracking);
    }

    void Refresh()
    {
        string n = trackedImage.referenceImage.name;
        if (string.IsNullOrEmpty(n)) return;
        n = n.ToLowerInvariant();

        foreach (var c in new[] { animalCell, bacterialCell, fungalCell, plantCell })
            if (c) c.SetActive(false);

        active = n.Contains("animal")    ? animalCell
               : n.Contains("bacterial") ? bacterialCell
               : n.Contains("fungal")    ? fungalCell
               : n.Contains("plant")     ? plantCell
               : null;

        if (active == null)
            Debug.LogWarning($"No cell mapped for image '{trackedImage.referenceImage.name}'");
    }
}