using UnityEngine;

/// <summary>
/// Minimal on-screen label naming the selected organelle. Needs no Canvas or font assets,
/// so selection is visible out of the box; OrganelleInfoUI can replace it later.
/// </summary>
public class OrganelleSelectionLabel : MonoBehaviour
{
    private OrganelleTarget _selected;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;

    private void OnEnable()
    {
        OrganelleSelectionManager.OnOrganelleSelected += Show;
        OrganelleSelectionManager.OnSelectionCleared += Hide;
    }

    private void OnDisable()
    {
        OrganelleSelectionManager.OnOrganelleSelected -= Show;
        OrganelleSelectionManager.OnSelectionCleared -= Hide;
    }

    private void Show(OrganelleTarget t) => _selected = t;
    private void Hide() => _selected = null;

    private void OnGUI()
    {
        if (_selected == null) return;

        if (_titleStyle == null)
        {
            _titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
            _bodyStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
        }

        // Scale with screen size so it stays readable on phones.
        float unit = Mathf.Min(Screen.width, Screen.height) / 30f;
        _titleStyle.fontSize = Mathf.RoundToInt(unit * 1.2f);
        _bodyStyle.fontSize = Mathf.RoundToInt(unit);

        float width = Screen.width * 0.9f;
        float height = string.IsNullOrEmpty(_selected.description)
            ? unit * 3f
            : unit * 8f;
        var rect = new Rect((Screen.width - width) / 2f, Screen.height - height - unit, width, height);

        Color previousColor = GUI.color;
        GUI.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
        var inner = new Rect(rect.x + unit * 0.5f, rect.y + unit * 0.3f, rect.width - unit, rect.height - unit * 0.6f);
        GUILayout.BeginArea(inner);
        GUILayout.Label(_selected.OrganelleName + " (selected)", _titleStyle);
        if (!string.IsNullOrEmpty(_selected.description))
            GUILayout.Label(_selected.description, _bodyStyle);
        GUILayout.EndArea();
    }
}
