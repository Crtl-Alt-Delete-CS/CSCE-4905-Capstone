using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrganelleInfoUI : MonoBehaviour
{
    [Serializable]
    public class OrganelleAudio
    {
        public string organelleName;
        public AudioClip[] pageAudioClips;
    }

    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Text Displays")]
    public TMP_Text organelleTitle;
    public TMP_Text organelleDescription;

    [Header("Controls added for the multi-page panel")]
    public Button nextButton;
    public Button soundButton;
    public Button closeButton;

    private Button singleViewButton;
    private TMP_Text singleViewLabel;
    private Image singleViewImage;

    private AppUITheme theme;
    private Sprite roundedSprite;
    private Texture2D roundedTexture;
    private ScrollRect descriptionScroll;

    [Header("Audio")]
    public AudioSource audioSource;
    public OrganelleAudio[] audioLibrary;

    private string[] currentPages;
    private AudioClip[] currentAudioClips;
    private int currentPageIndex;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (soundButton != null)
            soundButton.onClick.AddListener(OnSoundClicked);

        if (panelRoot == null)
            return;

        theme = AppUITheme.Load();
        if (theme == null)
            throw new System.InvalidOperationException("The AppUITheme asset is missing from Resources.");

        roundedSprite = CreateRoundedSprite();
        ConfigurePanel();
        ConfigureDescription();
        ConfigureCloseButton();
        ConfigureAddedControls();
        CreateSingleViewButton();
        HideAddedControls();
        panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        OrganelleSelectionManager.OnOrganelleSelected += ShowInfo;
        OrganelleSelectionManager.OnSelectionCleared += HideInfo;
        OrganelleSelectionManager.OnSingleViewChanged += RefreshSingleViewButton;
    }

    private void OnDisable()
    {
        OrganelleSelectionManager.OnOrganelleSelected -= ShowInfo;
        OrganelleSelectionManager.OnSelectionCleared -= HideInfo;
        OrganelleSelectionManager.OnSingleViewChanged -= RefreshSingleViewButton;
    }

    private void OnDestroy()
    {
        if (roundedSprite != null)
            Destroy(roundedSprite);

        if (roundedTexture != null)
            Destroy(roundedTexture);
    }

    private void ConfigurePanel()
    {
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.04f, 0.035f);
            panelRect.anchorMax = new Vector2(0.96f, 0.43f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.sprite = roundedSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = theme.Border;
            panelImage.raycastTarget = true;
        }

        GameObject surface = CreateImage("Info Card Surface", panelRoot.transform, theme.CardSurface);
        RectTransform surfaceRect = surface.GetComponent<RectTransform>();
        Stretch(surfaceRect);
        surfaceRect.offsetMin = Vector2.one * 2f;
        surfaceRect.offsetMax = Vector2.one * -2f;
        surface.transform.SetAsFirstSibling();

        GameObject descriptionSurface = CreateImage("Description Surface", panelRoot.transform, theme.MutedSurface);
        RectTransform descriptionSurfaceRect = descriptionSurface.GetComponent<RectTransform>();
        descriptionSurfaceRect.anchorMin = new Vector2(0.055f, 0.09f);
        descriptionSurfaceRect.anchorMax = new Vector2(0.945f, 0.63f);
        descriptionSurfaceRect.offsetMin = Vector2.zero;
        descriptionSurfaceRect.offsetMax = Vector2.zero;
        descriptionSurface.transform.SetSiblingIndex(1);

        TMP_Text sectionLabel = CreateText("Organelle Label", panelRoot.transform);
        sectionLabel.text = "CELL PART";
        sectionLabel.fontSize = 18f;
        sectionLabel.fontStyle = FontStyles.Bold;
        sectionLabel.characterSpacing = 2f;
        sectionLabel.color = theme.Primary;
        sectionLabel.alignment = TextAlignmentOptions.Left;
        SetAnchors(sectionLabel.rectTransform, new Vector2(0.07f, 0.84f), new Vector2(0.82f, 0.96f));

        if (organelleTitle != null)
        {
            SetAnchors(organelleTitle.rectTransform, new Vector2(0.07f, 0.66f), new Vector2(0.82f, 0.85f));
            organelleTitle.rectTransform.offsetMin = new Vector2(0f, 0f);
            organelleTitle.rectTransform.offsetMax = Vector2.zero;
            organelleTitle.fontSize = 40f;
            organelleTitle.fontSizeMin = 28f;
            organelleTitle.fontSizeMax = 40f;
            organelleTitle.enableAutoSizing = true;
            organelleTitle.textWrappingMode = TextWrappingModes.NoWrap;
            organelleTitle.overflowMode = TextOverflowModes.Ellipsis;
            organelleTitle.fontStyle = FontStyles.Bold;
            organelleTitle.color = theme.MainText;
            organelleTitle.alignment = TextAlignmentOptions.MidlineLeft;
            organelleTitle.raycastTarget = false;
        }
    }

    private void ConfigureDescription()
    {
        if (organelleDescription == null)
            return;

        GameObject viewportObject = new GameObject("Description Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewportObject.transform.SetParent(panelRoot.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        SetAnchors(viewport, new Vector2(0.075f, 0.11f), new Vector2(0.925f, 0.61f));

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        organelleDescription.transform.SetParent(viewport, false);
        RectTransform descriptionRect = organelleDescription.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0.5f, 1f);
        descriptionRect.anchoredPosition = Vector2.zero;
        descriptionRect.sizeDelta = Vector2.zero;

        organelleDescription.fontSize = 29f;
        organelleDescription.fontSizeMin = 25f;
        organelleDescription.fontSizeMax = 29f;
        organelleDescription.enableAutoSizing = false;
        organelleDescription.textWrappingMode = TextWrappingModes.Normal;
        organelleDescription.overflowMode = TextOverflowModes.Overflow;
        organelleDescription.lineSpacing = 4f;
        organelleDescription.color = theme.MainText;
        organelleDescription.alignment = TextAlignmentOptions.TopLeft;
        organelleDescription.margin = new Vector4(2f, 8f, 2f, 8f);
        organelleDescription.raycastTarget = false;

        ContentSizeFitter contentSize = organelleDescription.GetComponent<ContentSizeFitter>();
        if (contentSize == null)
            contentSize = organelleDescription.gameObject.AddComponent<ContentSizeFitter>();
        contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        descriptionScroll = viewportObject.GetComponent<ScrollRect>();
        descriptionScroll.horizontal = false;
        descriptionScroll.vertical = true;
        descriptionScroll.movementType = ScrollRect.MovementType.Clamped;
        descriptionScroll.scrollSensitivity = 32f;
        descriptionScroll.viewport = viewport;
        descriptionScroll.content = descriptionRect;
        descriptionScroll.verticalScrollbar = null;
        descriptionScroll.inertia = true;

        viewportObject.transform.SetAsLastSibling();
    }

    private void ConfigureCloseButton()
    {
        if (closeButton == null)
            return;

        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.88f, 0.79f);
        closeRect.anchorMax = new Vector2(0.98f, 0.97f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        closeButton.transition = Selectable.Transition.None;

        Image closeImage = closeButton.GetComponent<Image>();
        if (closeImage != null)
        {
            closeImage.sprite = roundedSprite;
            closeImage.type = Image.Type.Sliced;
            closeImage.color = theme.SecondarySurface;
        }

        TMP_Text closeText = closeButton.GetComponentInChildren<TMP_Text>(true);
        if (closeText != null)
        {
            closeText.text = "×";
            closeText.fontSize = 38f;
            closeText.fontStyle = FontStyles.Bold;
            closeText.color = theme.MainText;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.raycastTarget = false;
            Stretch(closeText.rectTransform);
        }

        closeButton.onClick.RemoveListener(ClearSelection);
        closeButton.onClick.AddListener(ClearSelection);
        closeButton.gameObject.SetActive(true);
    }

    private void ConfigureAddedControls()
    {
        ConfigureAddedButton(nextButton, "NEXT", new Vector2(0.53f, 0.02f), new Vector2(0.74f, 0.10f));
        ConfigureAddedButton(soundButton, "SOUND", new Vector2(0.76f, 0.02f), new Vector2(0.97f, 0.10f));
    }

    private void CreateSingleViewButton()
    {
        if (panelRoot == null)
            return;

        GameObject buttonObject = new GameObject("Single View Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panelRoot.transform, false);

        singleViewButton = buttonObject.GetComponent<Button>();
        singleViewImage = buttonObject.GetComponent<Image>();
        singleViewImage.sprite = roundedSprite;
        singleViewImage.type = Image.Type.Sliced;
        singleViewImage.color = theme.SecondarySurface;

        SetAnchors(singleViewButton.GetComponent<RectTransform>(), new Vector2(0.055f, 0.02f), new Vector2(0.50f, 0.10f));
        singleViewButton.transition = Selectable.Transition.None;
        singleViewButton.targetGraphic = singleViewImage;
        singleViewButton.onClick.AddListener(OnSingleViewClicked);

        singleViewLabel = CreateText("Label", buttonObject.transform);
        singleViewLabel.fontSize = 20f;
        singleViewLabel.fontStyle = FontStyles.Bold;
        singleViewLabel.color = theme.MainText;
        singleViewLabel.alignment = TextAlignmentOptions.Center;
        Stretch(singleViewLabel.rectTransform);
        RefreshSingleViewButton(false);

        buttonObject.transform.SetAsLastSibling();
    }

    private void OnSingleViewClicked()
    {
        OrganelleSelectionManager.Instance.ToggleSingleView();
    }

    private void RefreshSingleViewButton(bool isolated)
    {
        if (singleViewLabel != null)
            singleViewLabel.text = isolated ? "FULL CELL" : "SINGLE VIEW";

        if (singleViewLabel != null)
            singleViewLabel.color = isolated ? theme.AppBackground : theme.MainText;

        if (singleViewImage != null)
            singleViewImage.color = isolated ? theme.Primary : theme.SecondarySurface;
    }

    private void ConfigureAddedButton(Button button, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (button == null)
            return;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        SetAnchors(buttonRect, anchorMin, anchorMax);
        button.transition = Selectable.Transition.None;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.sprite = roundedSprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = theme.SecondarySurface;
        }

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.fontSize = 20f;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.color = theme.MainText;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.raycastTarget = false;
            Stretch(buttonText.rectTransform);
        }
    }

    private void ShowInfo(OrganelleTarget organelle)
    {
        if (organelle == null)
            return;

        DisplayOrganelle(organelle.OrganelleName, organelle.Pages);
    }

    public void DisplayOrganelle(string organelleName, string[] pages)
    {
        DisplayOrganelle(organelleName, pages, FindAudioClips(organelleName));
    }

    public void DisplayOrganelle(OrganelleData data)
    {
        if (data == null)
            return;

        string displayName = string.IsNullOrEmpty(data.organelleName)
            ? data.gameObject.name
            : data.organelleName;
        AudioClip[] pageClips = data.pageAudioClips;
        if (pageClips == null || pageClips.Length == 0)
            pageClips = FindAudioClips(displayName);

        DisplayOrganelle(displayName, data.pages, pageClips);
    }

    private void DisplayOrganelle(string organelleName, string[] pages, AudioClip[] pageClips)
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        if (organelleTitle != null)
            organelleTitle.text = organelleName;

        currentPages = pages;
        currentAudioClips = pageClips;
        currentPageIndex = 0;
        UpdatePageDisplay();
        RefreshSingleViewButton(OrganelleSelectionManager.Instance.IsSingleView);
    }

    private AudioClip[] FindAudioClips(string organelleName)
    {
        if (audioLibrary == null || string.IsNullOrWhiteSpace(organelleName))
            return null;

        foreach (OrganelleAudio entry in audioLibrary)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.organelleName) &&
                entry.organelleName.Trim().Equals(organelleName.Trim(), StringComparison.OrdinalIgnoreCase))
                return entry.pageAudioClips;
        }

        return null;
    }

    private void UpdatePageDisplay()
    {
        if (organelleDescription != null)
            organelleDescription.text = currentPages != null && currentPages.Length > 0
                ? currentPages[Mathf.Clamp(currentPageIndex, 0, currentPages.Length - 1)]
                : "No description available";

        if (descriptionScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            if (organelleDescription != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(organelleDescription.rectTransform);
            descriptionScroll.verticalNormalizedPosition = 1f;
        }

        if (nextButton != null)
            nextButton.gameObject.SetActive(currentPages != null && currentPages.Length > 1);

        if (soundButton != null)
            soundButton.gameObject.SetActive(audioSource != null &&
                currentAudioClips != null &&
                currentPageIndex < currentAudioClips.Length &&
                currentAudioClips[currentPageIndex] != null);
    }

    private void HideInfo()
    {
        HidePanel();
    }

    private void ClearSelection()
    {
        OrganelleSelectionManager.Instance.ClearSelection();
    }

    private void HideAddedControls()
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (soundButton != null)
            soundButton.gameObject.SetActive(false);
    }

    private void OnNextClicked()
    {
        if (currentPages == null || currentPages.Length <= 1)
            return;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        currentPageIndex = (currentPageIndex + 1) % currentPages.Length;
        UpdatePageDisplay();
    }

    private void OnSoundClicked()
    {
        if (audioSource == null || currentAudioClips == null ||
            currentPageIndex >= currentAudioClips.Length ||
            currentAudioClips[currentPageIndex] == null)
            return;

        audioSource.Stop();
        audioSource.clip = currentAudioClips[currentPageIndex];
        audioSource.Play();
    }

    public void HidePanel()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (panelRoot != null)
            panelRoot.SetActive(false);
        HideAddedControls();
    }

    private Sprite CreateRoundedSprite()
    {
        const int size = 64;
        const int samplesPerAxis = 4;
        const float radius = 14f;

        roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        roundedTexture.name = "Organelle Info Rounded UI";
        roundedTexture.hideFlags = HideFlags.HideAndDontSave;
        roundedTexture.filterMode = FilterMode.Bilinear;
        roundedTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int coveredSamples = 0;
                for (int sy = 0; sy < samplesPerAxis; sy++)
                {
                    for (int sx = 0; sx < samplesPerAxis; sx++)
                    {
                        Vector2 point = new Vector2(x + (sx + 0.5f) / samplesPerAxis, y + (sy + 0.5f) / samplesPerAxis);
                        float dx = Mathf.Max(Mathf.Abs(point.x - center.x) - (size * 0.5f - radius), 0f);
                        float dy = Mathf.Max(Mathf.Abs(point.y - center.y) - (size * 0.5f - radius), 0f);
                        if (dx * dx + dy * dy <= radius * radius)
                            coveredSamples++;
                    }
                }

                float coverage = coveredSamples / (float)(samplesPerAxis * samplesPerAxis);
                pixels[y * size + x] = new Color(1f, 1f, 1f, coverage);
            }
        }

        roundedTexture.SetPixels(pixels);
        roundedTexture.Apply();
        return Sprite.Create(roundedTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
    }

    private GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private TMP_Text CreateText(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = objectName;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
