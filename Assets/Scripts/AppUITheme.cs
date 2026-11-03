using UnityEngine;

[CreateAssetMenu(fileName = "AppUITheme", menuName = "AR Science/UI Theme")]
public sealed class AppUITheme : ScriptableObject
{
    [Header("Surfaces")]
    [SerializeField] private Color appBackground = ColorFromHex("0B1120");
    [SerializeField] private Color arFallbackBackground = ColorFromHex("08141F");
    [SerializeField] private Color cardSurface = ColorFromHex("111C30");
    [SerializeField] private Color mutedSurface = ColorFromHex("162035");
    [SerializeField] private Color secondarySurface = ColorFromHex("1A2D4A");

    [Header("Text and accents")]
    [SerializeField] private Color primary = ColorFromHex("00D4AA");
    [SerializeField] private Color accent = ColorFromHex("7C3AED");
    [SerializeField] private Color mainText = ColorFromHex("E8F4F8");
    [SerializeField] private Color mutedText = ColorFromHex("6A9AB8");
    [SerializeField] private Color border = ColorFromHex("1F3350");
    [SerializeField] private Color success = ColorFromHex("22C55E");
    [SerializeField] private Color warning = ColorFromHex("F59E0B");

    public Color AppBackground => appBackground;
    public Color ARFallbackBackground => arFallbackBackground;
    public Color CardSurface => cardSurface;
    public Color MutedSurface => mutedSurface;
    public Color SecondarySurface => secondarySurface;
    public Color Primary => primary;
    public Color Accent => accent;
    public Color MainText => mainText;
    public Color MutedText => mutedText;
    public Color Border => border;
    public Color Success => success;
    public Color Warning => warning;

    public static AppUITheme Load()
    {
        return Resources.Load<AppUITheme>("AppUITheme");
    }

    private static Color ColorFromHex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
}
