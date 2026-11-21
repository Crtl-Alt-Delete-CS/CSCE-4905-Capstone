using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class CreateLoadingBarPrefab
{
    [MenuItem("Tools/BioVisionAR/Create Loading Bar Prefab")]
    public static void CreatePrefab()
    {
        const string prefabFolder = "Assets/Resources/Prefabs";
        const string prefabPath = prefabFolder + "/LoadingBar.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        AppUITheme theme = Resources.Load<AppUITheme>("AppUITheme");
        if (theme == null)
        {
            Debug.LogError("Cannot create loading bar: Resources/AppUITheme.asset is missing.");
            return;
        }

        Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Sprite");
        GameObject root = CreateImage("Loading Bar", null, roundedSprite, theme.Border);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(340f, 108f);

        GameObject surface = CreateImage("Surface", root.transform, roundedSprite, theme.CardSurface);
        Stretch(surface.GetComponent<RectTransform>());
        surface.GetComponent<RectTransform>().offsetMin = Vector2.one;
        surface.GetComponent<RectTransform>().offsetMax = Vector2.one * -1f;

        VerticalLayoutGroup layout = surface.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 11f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text heading = CreateText("Loading Heading", surface.transform, "GETTING THINGS READY", 11, theme.Primary);
        heading.fontStyle = FontStyles.Bold;
        heading.characterSpacing = 1.5f;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        GameObject trackObject = CreateImage("Progress Track", surface.transform, roundedSprite, theme.MutedSurface);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.sizeDelta = new Vector2(304f, 8f);
        LayoutElement trackLayout = trackObject.AddComponent<LayoutElement>();
        trackLayout.preferredHeight = 8f;
        trackLayout.minHeight = 8f;

        GameObject fillObject = CreateImage("Progress Fill", trackObject.transform, roundedSprite, theme.Primary);
        Image fill = fillObject.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        Stretch(fill.rectTransform);

        GameObject shineObject = CreateImage("Progress Shine", fillObject.transform, roundedSprite,
            new Color(1f, 1f, 1f, 0.28f));
        RectTransform shineRect = shineObject.GetComponent<RectTransform>();
        shineRect.anchorMin = new Vector2(0.5f, 0f);
        shineRect.anchorMax = new Vector2(0.5f, 1f);
        shineRect.pivot = new Vector2(0.5f, 0.5f);
        shineRect.sizeDelta = new Vector2(18f, 0f);

        TMP_Text status = CreateText("Loading Status", surface.transform,
            "Checking AR support…  0%", 13, theme.MutedText);
        status.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        LoadingBar bar = root.AddComponent<LoadingBar>();
        SerializedObject serializedBar = new SerializedObject(bar);
        serializedBar.FindProperty("trackImage").objectReferenceValue = trackObject.GetComponent<Image>();
        serializedBar.FindProperty("fillImage").objectReferenceValue = fill;
        serializedBar.FindProperty("shineTransform").objectReferenceValue = shineRect;
        serializedBar.FindProperty("statusLabel").objectReferenceValue = status;
        serializedBar.FindProperty("trackColor").colorValue = theme.MutedSurface;
        serializedBar.FindProperty("fillColor").colorValue = theme.Primary;
        serializedBar.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created " + prefabPath);
    }

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject objectInstance = new GameObject(name, typeof(RectTransform), typeof(Image));
        if (parent != null)
            objectInstance.transform.SetParent(parent, false);
        Image image = objectInstance.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return objectInstance;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color)
    {
        GameObject objectInstance = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        objectInstance.transform.SetParent(parent, false);
        TMP_Text label = objectInstance.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = name == "Loading Status"
            ? TextAlignmentOptions.Left
            : name == "Loading Heading"
                ? TextAlignmentOptions.Left
                : TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
