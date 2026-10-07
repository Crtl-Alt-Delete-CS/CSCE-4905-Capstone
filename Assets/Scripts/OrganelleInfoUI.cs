using UnityEngine;
using TMPro;

public class OrganelleInfoUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private void OnEnable()
    {
        OrganelleSelectionManager.OnOrganelleSelected += ShowInfo;
        OrganelleSelectionManager.OnSelectionCleared += HideInfo;
    }

    private void OnDisable()
    {
        OrganelleSelectionManager.OnOrganelleSelected -= ShowInfo;
        OrganelleSelectionManager.OnSelectionCleared -= HideInfo;
    }

    private void ShowInfo(OrganelleTarget organelle)
    {
        panel.SetActive(true);
        titleText.text = organelle.OrganelleName;
        if (descriptionText != null)
            descriptionText.text = organelle.description;
    }

    private void HideInfo()
    {
        panel.SetActive(false);
    }
}