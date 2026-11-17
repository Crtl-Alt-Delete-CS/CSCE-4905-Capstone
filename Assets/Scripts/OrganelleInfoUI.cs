using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrganelleInfoUI : MonoBehaviour
{
    [Serializable]
    public struct OrganelleAudio
    {
        public string organelleName;
        public AudioClip[] pageClips;
    }

    [Header("UI Panel & Text")]
    public GameObject panelRoot;
    public TMP_Text organelleTitle;
    public TMP_Text organelleDescription;

    [Header("Controls")]
    public Button nextButton;
    public Button soundButton;
    public Button closeButton;
    public Button isolateButton;
    public TMP_Text isolateButtonText;

    [Header("Audio")]
    public AudioSource audioSource;
    public OrganelleAudio[] audioLibrary;

    private string[] _currentPages;
    private int _currentPageIndex;
    private string _currentOrganelleName;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextPageClicked);

        if (soundButton != null)
            soundButton.onClick.AddListener(OnSoundClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (isolateButton != null)
        {
            isolateButton.onClick.AddListener(OnIsolateClicked);
            if (isolateButtonText == null)
            {
                isolateButtonText = isolateButton.GetComponentInChildren<TMP_Text>();
            }
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        OrganelleSelectionManager.OnIsolationStateChanged += UpdateIsolateButtonVisual;
    }

    private void OnDisable()
    {
        OrganelleSelectionManager.OnIsolationStateChanged -= UpdateIsolateButtonVisual;
    }

    public void DisplayOrganelle(string title, string[] pages)
    {
        _currentOrganelleName = title;
        _currentPages = (pages != null && pages.Length > 0) ? pages : new[] { "" };
        _currentPageIndex = 0;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (organelleTitle != null)
            organelleTitle.text = title;

        if (nextButton != null)
            nextButton.gameObject.SetActive(_currentPages.Length > 1);

        bool isCurrentlyIsolated = OrganelleSelectionManager.Instance != null && OrganelleSelectionManager.Instance.IsIsolated;
        UpdateIsolateButtonVisual(isCurrentlyIsolated);

        UpdatePageContent();
    }

    public void DisplayOrganelle(string title, string singleDescription)
    {
        string[] pages = !string.IsNullOrEmpty(singleDescription)
            ? singleDescription.Split(new[] { "\n\n", "|" }, StringSplitOptions.RemoveEmptyEntries)
            : new[] { "" };
        DisplayOrganelle(title, pages);
    }

    public void HidePanel()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnNextPageClicked()
    {
        if (_currentPages == null || _currentPages.Length <= 1)
            return;

        _currentPageIndex = (_currentPageIndex + 1) % _currentPages.Length;
        UpdatePageContent();
    }

    private void UpdatePageContent()
    {
        if (organelleDescription != null && _currentPages != null && _currentPages.Length > 0)
        {
            organelleDescription.text = _currentPages[_currentPageIndex].Trim();
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void OnSoundClicked()
    {
        if (audioSource == null || audioLibrary == null)
            return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            return;
        }

        AudioClip clip = GetClipForCurrentPage(_currentOrganelleName, _currentPageIndex);
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private AudioClip GetClipForCurrentPage(string organelleName, int pageIndex)
    {
        if (string.IsNullOrEmpty(organelleName) || audioLibrary == null)
            return null;

        foreach (OrganelleAudio entry in audioLibrary)
        {
            if (string.Equals(entry.organelleName, organelleName, StringComparison.OrdinalIgnoreCase))
            {
                if (entry.pageClips != null && entry.pageClips.Length > 0)
                {
                    int clampedIndex = Mathf.Clamp(pageIndex, 0, entry.pageClips.Length - 1);
                    return entry.pageClips[clampedIndex];
                }
            }
        }
        return null;
    }

    private void OnIsolateClicked()
    {
        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance.ToggleIsolateCurrent();
        }
    }

    private void UpdateIsolateButtonVisual(bool isIsolated)
    {
        if (isolateButtonText != null)
        {
            isolateButtonText.text = isIsolated ? "Show Full Cell" : "Isolate Organelle";
        }
    }

    private void OnCloseClicked()
    {
        if (OrganelleSelectionManager.Instance != null)
        {
            OrganelleSelectionManager.Instance.ClearSelection();
        }
        else
        {
            HidePanel();
        }
    }
}