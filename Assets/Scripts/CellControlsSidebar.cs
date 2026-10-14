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

    private GameObject sidebar;
    private TMP_Text toggleLabel;
    private bool isOpen;

    private void Start()
    {
        Button[] buttons =
        {
            animalButton, plantButton, bacterialButton, fungalButton,
            resetButton, recenterButton, clearButton
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

        // This script belongs on the existing Canvas.
        RectTransform canvasRect = GetComponent<RectTransform>();

        if (GetComponent<Canvas>() == null || canvasRect == null)
        {
            Debug.LogError("Attach CellControlsSidebar to the Canvas.", this);
            return;
        }

        var sampleText = animalButton.GetComponentInChildren<TMP_Text>(true);

        // Sidebar occupies the upper-right area, above the information card.
        sidebar = new GameObject(
            "ControlsSidebar",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup));

        RectTransform panelRect = sidebar.GetComponent<RectTransform>();
        panelRect.SetParent(transform, false);
        SetAnchors(panelRect, new Vector2(0.73f, 0.36f),
            new Vector2(0.98f, 0.86f));

        Image panelImage = sidebar.GetComponent<Image>();
        panelImage.color = new Color32(17, 25, 38, 245);
        panelImage.raycastTarget = true;

        VerticalLayoutGroup layout = sidebar.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            button.transform.SetParent(panelRect, false);
            button.transform.localScale = Vector3.one;
            button.transform.localRotation = Quaternion.identity;

            RectTransform rect = button.GetComponent<RectTransform>();
            Vector3 position = rect.localPosition;
            position.z = 0;
            rect.localPosition = position;

            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element == null)
                element = button.gameObject.AddComponent<LayoutElement>();

            element.ignoreLayout = false;
            element.minHeight = 0;
            element.preferredHeight = 0;
            element.flexibleHeight = 1;

            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                background.color = i < 4
                    ? new Color32(36, 52, 71, 255)
                    : new Color32(48, 65, 82, 255);

                button.targetGraphic = background;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = Color.white;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16;
                label.fontSizeMax = 36;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            // Collapse after choosing a cell. Keep the original callbacks.
            if (i < 4)
                button.onClick.AddListener(CloseSidebar);
        }

        // Separate toggle remains visible when the sidebar is closed.
        GameObject toggleObject = new GameObject(
            "ControlsToggle",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));

        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.SetParent(transform, false);
        SetAnchors(toggleRect, new Vector2(0.73f, 0.88f),
            new Vector2(0.98f, 0.97f));

        Image toggleImage = toggleObject.GetComponent<Image>();

// Reuse the rounded background from the Animal button.
Image referenceImage = animalButton.GetComponent<Image>();
if (referenceImage != null)
{
    toggleImage.sprite = referenceImage.sprite;
    toggleImage.type = referenceImage.type;
    toggleImage.pixelsPerUnitMultiplier =
        referenceImage.pixelsPerUnitMultiplier;
}

        toggleImage.color = new Color32(36, 52, 71, 255);
        panelImage.color = Color.clear;

        // Make the toggle shorter.
        SetAnchors(toggleRect,
            new Vector2(0.73f, 0.89f),
            new Vector2(0.98f, 0.95f));

        Button toggleButton = toggleObject.GetComponent<Button>();
        toggleButton.targetGraphic = toggleImage;
        toggleButton.transition = Selectable.Transition.ColorTint;

        // Subtle feedback when hovering or pressing.
        ColorBlock colors = toggleButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration = 0.12f;
        toggleButton.colors = colors;

        toggleButton.onClick.AddListener(ToggleSidebar);

        GameObject textObject = new GameObject(
            "Label", typeof(RectTransform), typeof(TextMeshProUGUI));

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(toggleRect, false);
        SetAnchors(textRect, Vector2.zero, Vector2.one);
        textRect.offsetMin = new Vector2(8, 4);
        textRect.offsetMax = new Vector2(-8, -4);

        toggleLabel = textObject.GetComponent<TextMeshProUGUI>();
        if (sampleText != null)
            toggleLabel.font = sampleText.font;

        toggleLabel.color = Color.white;
        toggleLabel.enableAutoSizing = true;
        toggleLabel.fontSizeMin = 16;
        toggleLabel.fontSizeMax = 36;
        toggleLabel.alignment = TextAlignmentOptions.Center;
        toggleLabel.raycastTarget = false;

        CloseSidebar();
    }

    private static void SetAnchors(
        RectTransform rect, Vector2 minimum, Vector2 maximum)
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
            sidebar.SetActive(open);

        if (toggleLabel != null)
            toggleLabel.text = open ? "Close" : "Controls";
    }
}