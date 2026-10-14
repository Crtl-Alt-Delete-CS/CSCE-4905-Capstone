using NUnit.Framework;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class OrganelleInfoUI : MonoBehaviour
{
    [Serializable]
    public  class OrganelleAudio
    {
        public string organelleName;
        public AudioClip[] pageAudioClips;
    }

    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Text Displays")]
    public TMP_Text organelleTitle;
    public TMP_Text organelleDescription;

    [Header("Buttons")]
    public Button nextButton;
    public Button soundButton;
    public Button closeButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public OrganelleAudio[] audioLibrary;

    // Internal data storage for the currently active organelle
    private string[] currentPages;
    private AudioClip[] currentAudioClips;
    private int currentPageIndex = 0;

    void Awake()
    {
        //Ensure the panel starts hidden
        if (panelRoot != null)
            panelRoot.SetActive(false);

        //Set up button click listeners
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        
        if (soundButton != null)
            soundButton.onClick.AddListener(OnSoundClicked);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
        
    }

    public void DisplayOrganelle(string organelleName, string[] pages)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        organelleTitle.text = organelleName;
        currentPages = pages;
        currentPageIndex = 0;

        currentAudioClips = null;
        if (audioLibrary != null)
        {
            OrganelleAudio entry = Array.Find(audioLibrary, a =>
                a != null && !string.IsNullOrEmpty(a.organelleName) &&
                a.organelleName.Trim().Equals(organelleName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (entry != null)
                currentAudioClips = entry.pageAudioClips;
        }
        

        UpdatePageDisplay();
    }

    public void HidePanel()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnNextClicked()
    {
        if (currentPages == null || currentPages.Length <= 1) return;

        //Move to the next page
        currentPageIndex = (currentPageIndex + 1) % currentPages.Length;
        UpdatePageDisplay();

        //Stop previous audio when moving to the next page
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void OnSoundClicked()
    {
        if (audioSource == null)
        {
            return;
        }

        if (currentAudioClips == null || currentAudioClips.Length == 0)
        {
            return;
        }

        if (currentPageIndex < currentAudioClips.Length && currentAudioClips[currentPageIndex] != null)
        {
            audioSource.Stop();
            audioSource.clip = currentAudioClips[currentPageIndex];
            audioSource.Play();
        }
    }


    private void UpdatePageDisplay()
    {
        if (currentPages != null && currentPages.Length > 0)
        {
            organelleDescription.text = currentPages[currentPageIndex];
        }
        else
        {
            organelleDescription.text = "";
        }
    }

    //[SerializeField] private GameObject panel;
    //[SerializeField] private TextMeshProUGUI titleText;
    //[SerializeField] private TextMeshProUGUI descriptionText;

    //private void OnEnable()
    //{
    //    OrganelleSelectionManager.OnOrganelleSelected += ShowInfo;
    //    OrganelleSelectionManager.OnSelectionCleared += HideInfo;
    //}

    //private void OnDisable()
    //{
    //    OrganelleSelectionManager.OnOrganelleSelected -= ShowInfo;
    //    OrganelleSelectionManager.OnSelectionCleared -= HideInfo;
    //}

    //private void ShowInfo(OrganelleTarget organelle)
    //{
    //    panel.SetActive(true);
    //    titleText.text = organelle.OrganelleName;
    //    if (descriptionText != null)
    //        descriptionText.text = organelle.description;
    //}

    //private void HideInfo()
    //{
    //    panel.SetActive(false);
    //}
}