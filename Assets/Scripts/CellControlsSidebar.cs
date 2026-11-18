using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CellControlsSidebar : MonoBehaviour
{
    [Header("Drag your existing buttons here")]
    [SerializeField] private Button animalButton;
    [SerializeField] private Button plantButton;
    [SerializeField] private Button bacterialButton;
    [SerializeField] private Button fungalButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button recenterButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private ARCellInteraction cellInteraction;

    [Header("Top Bar Style")]
    [Tooltip("Drag the MODE button here so Controls matches it exactly.")]
    [SerializeField] private Button modeButton;

    private GameObject sidebar;
    private TMP_Text toggleLabel;
    private bool isOpen;

    private void Start()
    {
        Button[] buttons =
        {
            animalButton,
            plantButton,
            bacterialButton,
            fungalButton,
            resetButton,
            recenterButton,
            clearButton
        };

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                Debug.LogError(
                    "Assign all seven buttons to CellControlsSidebar.", this);
                return;
            }
        }

            if (cellInteraction == null)
    {
        cellInteraction = FindFirstObjectByType<ARCellInteraction>();
    }

    if (cellInteraction == null)
    {
        Debug.LogError(
            "ARCellInteraction could not be found.",
            this);
        return;
    }

    // Replace Inspector callbacks with known-good callbacks.

    // Animal
    animalButton.onClick = new Button.ButtonClickedEvent();
    animalButton.onClick.AddListener(
        cellInteraction.SelectAnimalCell);
    animalButton.onClick.AddListener(CloseSidebar);

    // Plant
    plantButton.onClick = new Button.ButtonClickedEvent();
    plantButton.onClick.AddListener(
        cellInteraction.SelectPlantCell);
    plantButton.onClick.AddListener(CloseSidebar);

    // Bacterial
    bacterialButton.onClick = new Button.ButtonClickedEvent();
    bacterialButton.onClick.AddListener(
        cellInteraction.SelectBacterialCell);
    bacterialButton.onClick.AddListener(CloseSidebar);

    // Fungal
    fungalButton.onClick = new Button.ButtonClickedEvent();
    fungalButton.onClick.AddListener(
        cellInteraction.SelectFungalCell);
    fungalButton.onClick.AddListener(CloseSidebar);

    // Reset
    resetButton.onClick = new Button.ButtonClickedEvent();
    resetButton.onClick.AddListener(
        cellInteraction.ResetCell);

    // Recenter
    recenterButton.onClick = new Button.ButtonClickedEvent();
    recenterButton.onClick.AddListener(
        cellInteraction.RecenterCell);

    // Clear
    clearButton.onClick = new Button.ButtonClickedEvent();
    clearButton.onClick.AddListener(
        cellInteraction.ClearCell);
        

        RectTransform canvasRect = GetComponent<RectTransform>();

        if (GetComponent<Canvas>() == null || canvasRect == null)
        {
            Debug.LogError(
                "Attach CellControlsSidebar to the Canvas.", this);
            return;
        }

        TMP_Text sampleText =
            animalButton.GetComponentInChildren<TMP_Text>(true);

        //
        // SIDEBAR
        //

        sidebar = new GameObject(
            "ControlsSidebar",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup));

        RectTransform panelRect =
            sidebar.GetComponent<RectTransform>();

        panelRect.SetParent(transform, false);

        SetAnchors(
            panelRect,
            new Vector2(0.66f, 0.36f),
            new Vector2(0.94f, 0.86f));

        Image panelImage = sidebar.GetComponent<Image>();

        // Transparent so only the buttons are visible.
        panelImage.color = Color.clear;
        panelImage.raycastTarget = true;

        VerticalLayoutGroup layout =
            sidebar.GetComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        //
        // EXISTING SIDEBAR BUTTONS
        //

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            button.transform.SetParent(panelRect, false);
            button.transform.localScale = Vector3.one;
            button.transform.localRotation = Quaternion.identity;

            RectTransform rect =
                button.GetComponent<RectTransform>();

            Vector3 position = rect.localPosition;
            position.z = 0;
            rect.localPosition = position;

            LayoutElement element =
                button.GetComponent<LayoutElement>();

            if (element == null)
            {
                element =
                    button.gameObject.AddComponent<LayoutElement>();
            }

            element.ignoreLayout = false;
            element.minHeight = 0;
            element.preferredHeight = 0;
            element.flexibleHeight = 1;

            Image background =
                button.GetComponent<Image>();

            if (background != null)
            {
                background.color = i < 4
                    ? new Color32(36, 52, 71, 255)
                    : new Color32(48, 65, 82, 255);

                button.targetGraphic = background;
            }

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                label.color = Color.white;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16;
                label.fontSizeMax = 36;
                label.alignment =
                    TextAlignmentOptions.Center;

                label.raycastTarget = false;
            }

        }

        //
        // CONTROLS TOP BUTTON
        //

        GameObject toggleObject = new GameObject(
            "ControlsToggle",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));

        RectTransform toggleRect =
            toggleObject.GetComponent<RectTransform>();

        toggleRect.SetParent(transform, false);

        // HOME | MODE | CONTROLS
        SetAnchors(
            toggleRect,
            new Vector2(0.66f, 0.91f),
            new Vector2(0.94f, 0.97f));

        Image toggleImage =
            toggleObject.GetComponent<Image>();

        Button toggleButton =
            toggleObject.GetComponent<Button>();

        //
        // COPY MODE BUTTON STYLE
        //

        if (modeButton != null)
        {
            Image modeImage =
                modeButton.GetComponent<Image>();

            if (modeImage != null)
            {
                toggleImage.color = modeImage.color;
                toggleImage.sprite = modeImage.sprite;
                toggleImage.type = modeImage.type;
                toggleImage.material = modeImage.material;
                toggleImage.pixelsPerUnitMultiplier =
                    modeImage.pixelsPerUnitMultiplier;
            }

            toggleButton.transition =
                modeButton.transition;

            toggleButton.colors =
                modeButton.colors;

            toggleButton.spriteState =
                modeButton.spriteState;
        }
        else
        {
            // Fallback if MODE was not assigned.
            toggleImage.color =
                new Color(0.035f, 0.12f, 0.105f, 0.88f);

            toggleButton.transition =
                Selectable.Transition.ColorTint;
        }

        toggleButton.targetGraphic = toggleImage;
        toggleButton.onClick.AddListener(ToggleSidebar);

        //
        // CONTROLS TEXT
        //

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.SetParent(toggleRect, false);

        SetAnchors(
            textRect,
            Vector2.zero,
            Vector2.one);

        textRect.offsetMin = new Vector2(8, 4);
        textRect.offsetMax = new Vector2(-8, -4);

        toggleLabel =
            textObject.GetComponent<TextMeshProUGUI>();

        // Copy MODE text style if available.
        if (modeButton != null)
        {
            TMP_Text modeText =
                modeButton.GetComponentInChildren<TMP_Text>(true);

            if (modeText != null)
            {
                toggleLabel.font = modeText.font;
                toggleLabel.fontSize = modeText.fontSize;
                toggleLabel.fontStyle = modeText.fontStyle;
                toggleLabel.color = modeText.color;
            }
        }
        else if (sampleText != null)
        {
            toggleLabel.font = sampleText.font;
            toggleLabel.color = Color.white;
            toggleLabel.fontStyle = FontStyles.Bold;
        }

        toggleLabel.text = "CONTROLS";
        toggleLabel.enableAutoSizing = true;
        toggleLabel.fontSizeMin = 14;
        toggleLabel.fontSizeMax = 36;
        toggleLabel.alignment =
            TextAlignmentOptions.Center;

        toggleLabel.raycastTarget = false;

        CloseSidebar();
    }

    private static void SetAnchors(
        RectTransform rect,
        Vector2 minimum,
        Vector2 maximum)
    {
        rect.anchorMin = minimum;
        rect.anchorMax = maximum;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    public void ToggleSidebar()
    {
        SetOpen(!isOpen);
    }

    public void CloseSidebar()
    {
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (sidebar != null)
        {
            sidebar.SetActive(open);
        }

        if (toggleLabel != null)
        {
            toggleLabel.text =
                open ? "CLOSE" : "CONTROLS";
        }
    }
}