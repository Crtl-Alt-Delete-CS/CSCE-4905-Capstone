using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class HomeLoadingUI : MonoBehaviour
{
    [Serializable]
    private sealed class HomeAction
    {
        public string title;
        public string description;
        public string icon;
        public HomeDestination destination;
    }

    private enum HomeDestination
    {
        Scan,
        Library,
        AR
    }

    private readonly HomeAction[] actions =
    {
        new HomeAction
        {
            title = "Scan to Load Cell",
            description = "Point your camera at a printed cell image or QR code",
            icon = "scan",
            destination = HomeDestination.Scan
        },
        new HomeAction
        {
            title = "Browse Cell Library",
            description = "Pick a cell type from our full collection",
            icon = "library",
            destination = HomeDestination.Library
        },
        new HomeAction
        {
            title = "Start AR View",
            description = "Place a cell model in your space right now",
            icon = "ar",
            destination = HomeDestination.AR
        }
    };

    private const float CardHeight = 122f;
    private const float NavigationHeight = 76f;
    private readonly string[] loadingTasks =
    {
        "Checking AR support…",
        "Loading cell library…",
        "Preparing the explorer…"
    };

    private AppUITheme theme;
    private Canvas canvas;
    private RectTransform safeAreaRoot;
    private GameObject splashScreen;
    private GameObject homeScreen;
    private GameObject placeholderOverlay;
    private TMP_Text placeholderTitle;
    private Image progressFill;
    private TMP_Text loadingStatus;
    private Action startAR;
    private Rect lastSafeArea;
    private int screenWidth;
    private int screenHeight;
    private Sprite roundedSprite;
    private Sprite circleSprite;
    private readonly List<Sprite> iconSprites = new List<Sprite>();
    private readonly List<Texture2D> iconTextures = new List<Texture2D>();

    public void Initialize(Action onStartAR)
    {
        startAR = onStartAR;
        theme = AppUITheme.Load();
        if (theme == null)
            throw new InvalidOperationException("The AppUITheme asset is missing from Resources.");

        roundedSprite = CreateSolidSprite(64, 14, false);
        circleSprite = CreateSolidSprite(64, 0, true);
        BuildCanvas();
        BuildSplash();
        BuildHome();
        ApplySafeArea();
        StartCoroutine(PlayStartupLoading());
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null)
            canvas.enabled = visible;
    }

    private void Update()
    {
        if (Screen.width != screenWidth || Screen.height != screenHeight || Screen.safeArea != lastSafeArea)
            ApplySafeArea();
    }

    private void OnDestroy()
    {
        if (roundedSprite != null)
            Destroy(roundedSprite);
        if (circleSprite != null)
            Destroy(circleSprite);
        foreach (Sprite iconSprite in iconSprites)
            Destroy(iconSprite);
        foreach (Texture2D iconTexture in iconTextures)
            Destroy(iconTexture);
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Splash and Home UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390f, 844f);
        scaler.matchWidthOrHeight = 0.5f;

        safeAreaRoot = CreateRect("Safe Area", canvasObject.transform);
        Stretch(safeAreaRoot);
    }

    private void BuildSplash()
    {
        splashScreen = CreateScreen("Splash", theme.AppBackground);
        AddDecorativeCircles(splashScreen.transform);

        RectTransform stack = CreateRect("Loading Content", splashScreen.transform);
        SetAnchors(stack, new Vector2(0.1f, 0.17f), new Vector2(0.9f, 0.83f));
        VerticalLayoutGroup layout = stack.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject logo = CreateImage("Cell Logo", stack, circleSprite, theme.SecondarySurface);
        LayoutElement logoLayout = logo.AddComponent<LayoutElement>();
        logoLayout.preferredWidth = 136f;
        logoLayout.preferredHeight = 136f;
        logoLayout.flexibleWidth = 0f;
        logoLayout.flexibleHeight = 0f;
        CreateLogoMark(logo.transform);

        TMP_Text title = CreateText("AR Science", stack, 42, FontStyles.Bold, theme.MainText, TextAlignmentOptions.Center);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
        TMP_Text subtitle = CreateText("Cell Biology Explorer", stack, 18, FontStyles.Normal, theme.MutedText, TextAlignmentOptions.Center);
        subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        GameObject progressPanel = CreateImage("Loading Progress Card", stack, roundedSprite, theme.Border);
        LayoutElement progressPanelLayout = progressPanel.AddComponent<LayoutElement>();
        progressPanelLayout.preferredHeight = 108f;
        progressPanelLayout.minHeight = 108f;
        progressPanel.GetComponent<Image>().raycastTarget = false;

        GameObject progressSurface = CreateImage("Loading Progress Surface", progressPanel.transform, roundedSprite, theme.CardSurface);
        RectTransform progressSurfaceRect = progressSurface.GetComponent<RectTransform>();
        Stretch(progressSurfaceRect);
        progressSurfaceRect.offsetMin = Vector2.one;
        progressSurfaceRect.offsetMax = Vector2.one * -1f;
        progressSurface.GetComponent<Image>().raycastTarget = false;

        VerticalLayoutGroup progressPanelLayoutGroup = progressSurface.AddComponent<VerticalLayoutGroup>();
        progressPanelLayoutGroup.padding = new RectOffset(18, 18, 14, 14);
        progressPanelLayoutGroup.spacing = 11f;
        progressPanelLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        progressPanelLayoutGroup.childControlWidth = true;
        progressPanelLayoutGroup.childControlHeight = false;
        progressPanelLayoutGroup.childForceExpandWidth = true;
        progressPanelLayoutGroup.childForceExpandHeight = false;

        TMP_Text progressHeading = CreateText("Loading Heading", progressSurface.transform, 11, FontStyles.Bold, theme.Primary, TextAlignmentOptions.Left);
        progressHeading.text = "GETTING THINGS READY";
        progressHeading.characterSpacing = 1.5f;
        progressHeading.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        GameObject progressTrack = CreateImage("Progress Track", progressSurface.transform, roundedSprite, theme.MutedSurface);
        LayoutElement trackLayout = progressTrack.AddComponent<LayoutElement>();
        trackLayout.preferredHeight = 8f;
        trackLayout.minHeight = 8f;
        progressFill = CreateImage("Progress Fill", progressTrack.transform, roundedSprite, theme.Primary).GetComponent<Image>();
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFill.fillAmount = 0f;
        Stretch(progressFill.rectTransform);

        loadingStatus = CreateText(loadingTasks[0] + "  0%", progressSurface.transform, 13, FontStyles.Normal, theme.MutedText, TextAlignmentOptions.Left);
        loadingStatus.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        Button skipButton = CreateTextButton("Skip", stack, 16, theme.MutedText, () => ShowHome());
        skipButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        splashScreen.SetActive(true);
    }

    private void BuildHome()
    {
        homeScreen = CreateScreen("Home", theme.AppBackground);

        RectTransform scrollRect = CreateRect("Home Scroll View", homeScreen.transform);
        SetAnchors(scrollRect, Vector2.zero, Vector2.one);
        scrollRect.offsetMin = new Vector2(0f, NavigationHeight);

        ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;

        GameObject viewportObject = CreateImage("Viewport", scrollRect, null, Color.white);
        viewportObject.GetComponent<Image>().color = Color.clear;
        viewportObject.GetComponent<Image>().raycastTarget = true;
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport);
        viewportObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(22, 22, 26, 22);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        scroll.content = content;

        TMP_Text eyebrow = CreateText("WELCOME BACK", content, 12, FontStyles.Bold, theme.MutedText, TextAlignmentOptions.Left);
        eyebrow.characterSpacing = 2f;
        eyebrow.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        TMP_Text heading = CreateText("Explore Cell Biology", content, 30, FontStyles.Bold, theme.MainText, TextAlignmentOptions.Left);
        heading.enableAutoSizing = true;
        heading.fontSizeMin = 24f;
        heading.fontSizeMax = 30f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

        TMP_Text subtitle = CreateText("Choose how to get started", content, 16, FontStyles.Normal, theme.MutedText, TextAlignmentOptions.Left);
        subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        for (int i = 0; i < actions.Length; i++)
            CreateActionCard(content, actions[i]);

        CreateTipCard(content);
        CreateBottomNavigation(homeScreen.transform);
        BuildPlaceholderOverlay(homeScreen.transform);
        homeScreen.SetActive(false);
    }

    private GameObject CreateScreen(string name, Color background)
    {
        GameObject screen = CreateImage(name, safeAreaRoot, null, background);
        Stretch(screen.GetComponent<RectTransform>());
        return screen;
    }

    private void AddDecorativeCircles(Transform parent)
    {
        CreateCircle(parent, new Vector2(0.08f, 0.84f), 240f, WithAlpha(theme.SecondarySurface, 0.06f));
        CreateCircle(parent, new Vector2(0.92f, 0.18f), 180f, WithAlpha(theme.Accent, 0.035f));
    }

    private void CreateCircle(Transform parent, Vector2 anchor, float size, Color color)
    {
        GameObject circle = CreateImage("Background Circle", parent, circleSprite, color);
        RectTransform rect = circle.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(size, size);
    }

    private void CreateLogoMark(Transform parent)
    {
        Image ring = CreateImage("Logo Ring", parent, circleSprite, theme.Primary).GetComponent<Image>();
        RectTransform ringRect = ring.rectTransform;
        ringRect.anchorMin = new Vector2(0.12f, 0.12f);
        ringRect.anchorMax = new Vector2(0.88f, 0.88f);
        ringRect.offsetMin = Vector2.zero;
        ringRect.offsetMax = Vector2.zero;

        Image logoCore = CreateImage("Logo Core", parent, circleSprite, theme.SecondarySurface).GetComponent<Image>();
        RectTransform coreRect = logoCore.rectTransform;
        coreRect.anchorMin = new Vector2(0.17f, 0.17f);
        coreRect.anchorMax = new Vector2(0.83f, 0.83f);
        coreRect.offsetMin = Vector2.zero;
        coreRect.offsetMax = Vector2.zero;

        TMP_Text ar = CreateText("AR", parent, 40, FontStyles.Bold, theme.MainText, TextAlignmentOptions.Center);
        Stretch(ar.rectTransform);
    }

    private void CreateActionCard(Transform parent, HomeAction action)
    {
        GameObject card = CreateImage(action.title + " Card", parent, roundedSprite, theme.Border);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(0f, CardHeight);
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.preferredHeight = CardHeight;
        layout.minHeight = CardHeight;
        Image cardImage = card.GetComponent<Image>();
        cardImage.raycastTarget = true;
        GameObject cardSurface = CreateImage("Card Surface", card.transform, roundedSprite, theme.CardSurface);
        RectTransform cardSurfaceRect = cardSurface.GetComponent<RectTransform>();
        Stretch(cardSurfaceRect);
        cardSurfaceRect.offsetMin = Vector2.one;
        cardSurfaceRect.offsetMax = Vector2.one * -1f;
        cardSurface.GetComponent<Image>().raycastTarget = false;

        Button button = card.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = cardImage;
        button.onClick.AddListener(() => SelectAction(action.destination));
        card.AddComponent<CardPressFeedback>().Initialize(cardRect, cardImage, theme.Border, theme.Primary);

        GameObject iconBack = CreateImage("Action Icon Background", card.transform, circleSprite, WithAlpha(theme.Primary, 0.13f));
        RectTransform iconRect = iconBack.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(16f, 0f);
        iconRect.sizeDelta = new Vector2(54f, 54f);
        CreateActionIcon(iconBack.transform, action.icon);

        TMP_Text title = CreateText("Action Title", card.transform, 18, FontStyles.Bold, theme.MainText, TextAlignmentOptions.Left);
        title.text = action.title;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.59f);
        titleRect.anchorMax = new Vector2(1f, 0.92f);
        titleRect.offsetMin = new Vector2(80f, 0f);
        titleRect.offsetMax = new Vector2(-42f, 0f);
        title.enableAutoSizing = true;
        title.fontSizeMin = 16f;
        title.fontSizeMax = 18f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text description = CreateText("Action Description", card.transform, 14, FontStyles.Normal, theme.MutedText, TextAlignmentOptions.Left);
        description.text = action.description;
        RectTransform descriptionRect = description.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0.12f);
        descriptionRect.anchorMax = new Vector2(1f, 0.51f);
        descriptionRect.offsetMin = new Vector2(80f, 0f);
        descriptionRect.offsetMax = new Vector2(-42f, 0f);
        description.enableAutoSizing = true;
        description.fontSizeMin = 13f;
        description.fontSizeMax = 14f;
        description.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text chevron = CreateText("Chevron", card.transform, 22, FontStyles.Normal, theme.Primary, TextAlignmentOptions.Center);
        RectTransform chevronRect = chevron.rectTransform;
        chevronRect.anchorMin = new Vector2(1f, 0.5f);
        chevronRect.anchorMax = new Vector2(1f, 0.5f);
        chevronRect.pivot = new Vector2(1f, 0.5f);
        chevronRect.anchoredPosition = new Vector2(-12f, 0f);
        chevronRect.sizeDelta = new Vector2(24f, 40f);
        chevron.text = "›";
    }

    private void CreateActionIcon(Transform parent, string icon)
    {
        GameObject iconObject = CreateImage("Action Icon", parent, CreateIconSprite(icon), theme.Primary);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(36f, 36f);
        iconObject.GetComponent<Image>().preserveAspect = true;
    }

    private void CreateTipCard(Transform parent)
    {
        GameObject tip = CreateImage("Quick Tip Card", parent, roundedSprite, theme.MutedSurface);
        tip.AddComponent<LayoutElement>().preferredHeight = 112f;
        TMP_Text label = CreateText("Quick Tip Label", tip.transform, 12, FontStyles.Bold, theme.Primary, TextAlignmentOptions.Left);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.7f);
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 0f);
        labelRect.offsetMax = new Vector2(-16f, -8f);
        label.text = "QUICK TIP";

        TMP_Text text = CreateText("Quick Tip", tip.transform, 14, FontStyles.Normal, theme.MainText, TextAlignmentOptions.Left);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0.12f);
        textRect.anchorMax = new Vector2(1f, 0.68f);
        textRect.offsetMin = new Vector2(16f, 0f);
        textRect.offsetMax = new Vector2(-16f, 0f);
        text.text = "Point your camera at a flat, well-lit surface to place a 3D cell model. Pinch to zoom and twist to rotate.";
        text.enableAutoSizing = true;
        text.fontSizeMin = 13f;
        text.fontSizeMax = 14f;
    }

    private void CreateBottomNavigation(Transform parent)
    {
        GameObject nav = CreateImage("Bottom Navigation", parent, null, theme.CardSurface);
        RectTransform navRect = nav.GetComponent<RectTransform>();
        navRect.anchorMin = Vector2.zero;
        navRect.anchorMax = new Vector2(1f, 0f);
        navRect.pivot = new Vector2(0.5f, 0f);
        navRect.sizeDelta = new Vector2(0f, NavigationHeight);

        HorizontalLayoutGroup layout = nav.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 8, 8);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        string[] labels = { "Home", "Scan", "Library", "Accessibility" };
        for (int i = 0; i < labels.Length; i++)
        {
            string label = labels[i];
            Color color = i == 0 ? theme.Primary : theme.MutedText;
            Button item = CreateTextButton(label, nav.transform, 17, color, () =>
            {
                if (label == "Home")
                    ShowHome();
                else if (label == "Scan")
                    ShowPlaceholder("Scan to Load Cell");
                else if (label == "Library")
                    ShowPlaceholder("Browse Cell Library");
                else
                    ShowPlaceholder("Accessibility");
            });
            LayoutElement itemLayout = item.gameObject.AddComponent<LayoutElement>();
            itemLayout.minWidth = 52f;
            itemLayout.minHeight = 52f;
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = label;
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
                text.fontSizeMax = 17f;
            }
        }
    }

    private void BuildPlaceholderOverlay(Transform parent)
    {
        placeholderOverlay = CreateImage("Navigation Preview", parent, null, new Color(0f, 0f, 0f, 0.62f));
        placeholderOverlay.GetComponent<Image>().raycastTarget = true;
        Stretch(placeholderOverlay.GetComponent<RectTransform>());
        placeholderOverlay.SetActive(false);

        GameObject panel = CreateImage("Preview Card", placeholderOverlay.transform, roundedSprite, theme.CardSurface);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.34f);
        panelRect.anchorMax = new Vector2(0.92f, 0.66f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        placeholderTitle = CreateText("Preview Title", panel.transform, 24, FontStyles.Bold, theme.MainText, TextAlignmentOptions.Center);
        placeholderTitle.rectTransform.anchorMin = new Vector2(0.06f, 0.68f);
        placeholderTitle.rectTransform.anchorMax = new Vector2(0.94f, 0.9f);
        placeholderTitle.rectTransform.offsetMin = Vector2.zero;
        placeholderTitle.rectTransform.offsetMax = Vector2.zero;
        placeholderTitle.enableAutoSizing = true;
        placeholderTitle.fontSizeMin = 18f;

        TMP_Text message = CreateText("Preview Message", panel.transform, 16, FontStyles.Normal, theme.MutedText, TextAlignmentOptions.Center);
        message.rectTransform.anchorMin = new Vector2(0.08f, 0.36f);
        message.rectTransform.anchorMax = new Vector2(0.92f, 0.62f);
        message.rectTransform.offsetMin = Vector2.zero;
        message.rectTransform.offsetMax = Vector2.zero;
        message.text = "This area is coming soon. You can still start an AR view.";
        message.enableAutoSizing = true;
        message.fontSizeMin = 14f;

        CreateActionButton("Done", panel.transform, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.25f), true, () =>
        {
            placeholderOverlay.SetActive(false);
            ShowHome();
        });

    }

    private void SelectAction(HomeDestination destination)
    {
        if (destination == HomeDestination.AR)
        {
            startAR?.Invoke();
            return;
        }

        ShowPlaceholder(destination == HomeDestination.Scan ? "Scan to Load Cell" : "Browse Cell Library");
    }

    private void ShowPlaceholder(string title)
    {
        if (placeholderOverlay == null)
            return;

        if (placeholderTitle != null)
            placeholderTitle.text = title;
        placeholderOverlay.SetActive(true);
    }

    private void ShowHome()
    {
        if (splashScreen != null)
            splashScreen.SetActive(false);
        if (placeholderOverlay != null)
            placeholderOverlay.SetActive(false);
        if (homeScreen != null)
            homeScreen.SetActive(true);
    }

    private IEnumerator PlayStartupLoading()
    {
        for (int i = 0; i < loadingTasks.Length; i++)
        {
            SetLoadingStatus(loadingTasks[i], Mathf.RoundToInt((float)i / loadingTasks.Length * 100f));
            float elapsed = 0f;
            const float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float taskProgress = Mathf.Clamp01(elapsed / duration);
                float overallProgress = (i + taskProgress) / loadingTasks.Length;
                progressFill.fillAmount = overallProgress;
                SetLoadingStatus(loadingTasks[i], Mathf.RoundToInt(overallProgress * 100f));
                yield return null;
            }
        }

        ShowHome();
    }

    private void CreateActionButton(string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax, bool primary, Action onClick)
    {
        GameObject frame = CreateImage(label + " Button", parent, roundedSprite, primary ? theme.Primary : theme.Border);
        RectTransform rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = frame.GetComponent<Image>();
        image.raycastTarget = true;
        GameObject surface = CreateImage("Button Surface", frame.transform, roundedSprite, primary ? theme.Primary : theme.CardSurface);
        RectTransform surfaceRect = surface.GetComponent<RectTransform>();
        Stretch(surfaceRect);
        surfaceRect.offsetMin = Vector2.one;
        surfaceRect.offsetMax = Vector2.one * -1f;
        surface.GetComponent<Image>().raycastTarget = false;

        Button button = frame.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());
        TMP_Text text = CreateText("Button Label", frame.transform, 16, FontStyles.Bold, primary ? theme.AppBackground : theme.MainText, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(8f, 4f);
        text.rectTransform.offsetMax = new Vector2(-8f, -4f);
        text.text = label;
    }

    private void SetLoadingStatus(string status, int percentage)
    {
        loadingStatus.text = "<mspace=0.55em>" + status + "  " + percentage + "%</mspace>";
    }

    private Button CreateTextButton(string label, Transform parent, float size, Color color, Action onClick)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        Image hitTarget = buttonObject.GetComponent<Image>();
        hitTarget.color = Color.clear;
        hitTarget.raycastTarget = true;
        button.targetGraphic = hitTarget;
        button.onClick.AddListener(() => onClick());

        TMP_Text text = CreateText("Label", buttonObject.transform, size, FontStyles.Bold, color, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.text = label;
        return button;
    }

    private TMP_Text CreateText(string objectName, Transform parent, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = objectName;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private GameObject CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sprite == roundedSprite)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }
        return imageObject;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private void ApplySafeArea()
    {
        if (safeAreaRoot == null)
            return;

        Rect safe = Screen.safeArea;
        safeAreaRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeAreaRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;
        lastSafeArea = safe;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    private Sprite CreateIconSprite(string icon)
    {
        const int size = 128;
        const int samplesPerAxis = 4;
        const float stroke = 0.055f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = icon + " UI Icon";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2[][] paths = GetIconPaths(icon);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int coveredSamples = 0;
                for (int sy = 0; sy < samplesPerAxis; sy++)
                {
                    for (int sx = 0; sx < samplesPerAxis; sx++)
                    {
                        Vector2 point = new Vector2(
                            (x + (sx + 0.5f) / samplesPerAxis) / size,
                            (y + (sy + 0.5f) / samplesPerAxis) / size);
                        if (IsIconPixel(icon, point, paths, stroke))
                            coveredSamples++;
                    }
                }

                float coverage = coveredSamples / (float)(samplesPerAxis * samplesPerAxis);
                pixels[y * size + x] = new Color(1f, 1f, 1f, coverage);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        iconTextures.Add(texture);
        iconSprites.Add(sprite);
        return sprite;
    }

    private Vector2[][] GetIconPaths(string icon)
    {
        if (icon == "scan")
        {
            return new[]
            {
                new[] { new Vector2(0.24f, 0.39f), new Vector2(0.24f, 0.24f), new Vector2(0.39f, 0.24f) },
                new[] { new Vector2(0.61f, 0.24f), new Vector2(0.76f, 0.24f), new Vector2(0.76f, 0.39f) },
                new[] { new Vector2(0.24f, 0.61f), new Vector2(0.24f, 0.76f), new Vector2(0.39f, 0.76f) },
                new[] { new Vector2(0.61f, 0.76f), new Vector2(0.76f, 0.76f), new Vector2(0.76f, 0.61f) }
            };
        }

        if (icon == "library")
        {
            return new[]
            {
                new[] { new Vector2(0.5f, 0.27f), new Vector2(0.25f, 0.32f), new Vector2(0.25f, 0.72f), new Vector2(0.5f, 0.67f) },
                new[] { new Vector2(0.5f, 0.27f), new Vector2(0.75f, 0.32f), new Vector2(0.75f, 0.72f), new Vector2(0.5f, 0.67f) },
                new[] { new Vector2(0.5f, 0.29f), new Vector2(0.5f, 0.65f) },
                new[] { new Vector2(0.32f, 0.43f), new Vector2(0.43f, 0.41f) },
                new[] { new Vector2(0.57f, 0.41f), new Vector2(0.68f, 0.43f) },
                new[] { new Vector2(0.32f, 0.54f), new Vector2(0.43f, 0.52f) },
                new[] { new Vector2(0.57f, 0.52f), new Vector2(0.68f, 0.54f) }
            };
        }

        return new[]
        {
            new[] { new Vector2(0.5f, 0.5f) },
            new[] { new Vector2(0.34f, 0.4f) },
            new[] { new Vector2(0.64f, 0.4f) },
            new[] { new Vector2(0.38f, 0.66f) },
            new[] { new Vector2(0.66f, 0.64f) }
        };
    }

    private bool IsIconPixel(string icon, Vector2 point, Vector2[][] paths, float stroke)
    {
        if (icon == "ar")
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            float radius = Vector2.Distance(point, center);
            if (Mathf.Abs(radius - 0.32f) <= stroke * 0.5f)
                return true;

            for (int i = 0; i < paths.Length; i++)
                if (Vector2.Distance(point, paths[i][0]) <= stroke * 0.75f)
                    return true;
        }

        for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
        {
            Vector2[] path = paths[pathIndex];
            for (int pointIndex = 1; pointIndex < path.Length; pointIndex++)
            {
                if (DistanceToSegment(point, path[pointIndex - 1], path[pointIndex]) <= stroke * 0.5f)
                    return true;
            }
        }

        return false;
    }

    private float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = Vector2.Dot(segment, segment);
        float along = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared) : 0f;
        return Vector2.Distance(point, start + along * segment);
    }

    private Sprite CreateSolidSprite(int size, int border, bool circle)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime UI Shape";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        float radius = circle ? size * 0.5f : size * 0.22f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        const int samplesPerAxis = 4;
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
                        if (IsInsideShape(point, center, size, radius, circle))
                            coveredSamples++;
                    }
                }

                float coverage = coveredSamples / (float)(samplesPerAxis * samplesPerAxis);
                pixels[y * size + x] = new Color(1f, 1f, 1f, coverage);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        Vector4 spriteBorder = new Vector4(border, border, border, border);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, spriteBorder);
    }

    private bool IsInsideShape(Vector2 point, Vector2 center, int size, float radius, bool circle)
    {
        if (circle)
            return Vector2.Distance(point, center) <= radius;

        float dx = Mathf.Max(Mathf.Abs(point.x - center.x) - (size * 0.5f - radius), 0f);
        float dy = Mathf.Max(Mathf.Abs(point.y - center.y) - (size * 0.5f - radius), 0f);
        return dx * dx + dy * dy <= radius * radius;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private sealed class CardPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform target;
        private Image border;
        private Color normalBorder;
        private Color pressedBorder;

        public void Initialize(RectTransform targetTransform, Image borderImage, Color normal, Color pressed)
        {
            target = targetTransform;
            border = borderImage;
            normalBorder = normal;
            pressedBorder = pressed;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            target.localScale = Vector3.one * 0.98f;
            border.color = pressedBorder;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetState();
        }

        private void ResetState()
        {
            if (target != null)
                target.localScale = Vector3.one;
            if (border != null)
                border.color = normalBorder;
        }
    }
}
