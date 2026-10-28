using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARTrackedImage))]
public class CellPrefabSwitcher : MonoBehaviour
{
    [Header("Cell Child Objects")]
    [SerializeField] private GameObject animalCell;
    [SerializeField] private GameObject bacterialCell;
    [SerializeField] private GameObject fungalCell;
    [SerializeField] private GameObject plantCell;

    private ARTrackedImage trackedImage;

    private void Awake()
    {
        trackedImage = GetComponent<ARTrackedImage>();
    }

    private void Start()
    {
        UpdateVisibleCell();
    }

    public void UpdateVisibleCell()
    {
        // Hide all models first
        if (animalCell) animalCell.SetActive(false);
        if (bacterialCell) bacterialCell.SetActive(false);
        if (fungalCell) fungalCell.SetActive(false);
        if (plantCell) plantCell.SetActive(false);

        // Match detected image name from Reference Image Library
        string imageName = trackedImage.referenceImage.name;

        switch (imageName)
        {
            case "AnimalCell":
                if (animalCell) animalCell.SetActive(true);
                break;
            case "BacterialCell":
                if (bacterialCell) bacterialCell.SetActive(true);
                break;
            case "FungalCell":
                if (fungalCell) fungalCell.SetActive(true);
                break;
            case "PlantCell":
                if (plantCell) plantCell.SetActive(true);
                break;
        }
    }
}