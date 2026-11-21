using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ControlsPanelLayout : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private float panelWidth = 300f;
    [SerializeField] private float panelHeight = 560f;
    [SerializeField] private float rightMargin = 20f;
    [SerializeField] private float topMargin = 20f;

    [Header("Buttons")]
    [SerializeField] private float buttonHeight = 60f;
    [SerializeField] private float spacing = 15f;
    [SerializeField] private int padding = 12;

    private void Awake()
    {
        ApplyLayout();
    }

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        RectTransform rect = GetComponent<RectTransform>();

        // Keep panel in upper-right corner
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);

        rect.sizeDelta = new Vector2(panelWidth, panelHeight);
        rect.anchoredPosition = new Vector2(-rightMargin, -topMargin);

        // Style panel background
        Image panelImage = GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color =
                new Color(0.035f, 0.12f, 0.105f, 0.85f);
        }

        // Configure vertical layout
        VerticalLayoutGroup layout =
            GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout =
                gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding =
            new RectOffset(padding, padding, padding, padding);

        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;

        layout.childControlWidth = true;
        layout.childControlHeight = true;

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        layout.childScaleWidth = false;
        layout.childScaleHeight = false;

        // Configure every button
        foreach (Transform child in transform)
        {
            Button button = child.GetComponent<Button>();

            if (button == null)
                continue;

            LayoutElement element =
                child.GetComponent<LayoutElement>();

            if (element == null)
            {
                element =
                    child.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = buttonHeight;
            element.preferredHeight = buttonHeight;
            element.flexibleHeight = 0;

            FixButtonText(child.gameObject);
        }

        // Keep controls above other UI
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
        }
    }

    private void FixButtonText(GameObject buttonObject)
    {
        string label = GetButtonLabel(buttonObject.name);

        // Try existing TextMeshPro label first
        TMP_Text tmpText =
            buttonObject.GetComponentInChildren<TMP_Text>(true);

        if (tmpText != null)
        {
            tmpText.text = label;
            tmpText.color = Color.white;
            tmpText.fontSize = 28;
            tmpText.alignment =
                TextAlignmentOptions.Center;

            if (tmpText.font == null &&
                TMP_Settings.defaultFontAsset != null)
            {
                tmpText.font =
                    TMP_Settings.defaultFontAsset;
            }

            RectTransform textRect =
                tmpText.GetComponent<RectTransform>();

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            tmpText.gameObject.SetActive(true);

            return;
        }

        // Try legacy UI Text
        Text normalText =
            buttonObject.GetComponentInChildren<Text>(true);

        if (normalText != null)
        {
            ConfigureLegacyText(normalText, label);
            return;
        }

        // No text exists, so create one
        GameObject labelObject =
            new GameObject(
                "ButtonLabel",
                typeof(RectTransform),
                typeof(Text)
            );

        labelObject.transform.SetParent(
            buttonObject.transform,
            false
        );

        Text newText =
            labelObject.GetComponent<Text>();

        ConfigureLegacyText(newText, label);
    }

    private void ConfigureLegacyText(
        Text textComponent,
        string label)
    {
        textComponent.text = label;

        textComponent.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        textComponent.fontSize = 28;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.color = Color.white;
        textComponent.alignment =
            TextAnchor.MiddleCenter;

        textComponent.raycastTarget = false;

        RectTransform rect =
            textComponent.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private string GetButtonLabel(string objectName)
    {
        switch (objectName)
        {
            case "AnimalButton":
                return "Animal";

            case "PlantButton":
                return "Plant";

            case "BacterialButton":
                return "Bacteria";

            case "FungalButton":
                return "Fungal";

            case "ResetButton":
                return "Reset";

            case "RecenterButton":
                return "Re-Center";

            case "ClearButton":
                return "Clear";

            default:
                return objectName.Replace("Button", "");
        }
    }
}