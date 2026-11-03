using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrganelleInfoUI : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Text Displays")]
    public TMP_Text organelleTitle;
    public TMP_Text organelleDescription;

    [Header("Controls added for the multi-page panel")]
    public Button nextButton;
    public Button soundButton;
    public Button closeButton;

    private AppUITheme theme;
    private Sprite roundedSprite;
    private Texture2D roundedTexture;
    private ScrollRect descriptionScroll;

    private void Awake()
    {
        if (panelRoot == null)
            return;

        theme = AppUITheme.Load();
        if (theme == null)
            throw new System.InvalidOperationException("The AppUITheme asset is missing from Resources.");

        roundedSprite = CreateRoundedSprite();
        ConfigurePanel();
        ConfigureDescription();
        ConfigureCloseButton();
        HideAddedControls();
        panelRoot.SetActive(false);
    }

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

    private void ShowInfo(OrganelleTarget organelle)
    {
        if (organelle == null)
            return;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        if (organelleTitle != null)
            organelleTitle.text = organelle.OrganelleName;

        if (organelleDescription != null)
            organelleDescription.text = organelle.description;

        if (descriptionScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(organelleDescription.rectTransform);
            descriptionScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void HideInfo()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
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
