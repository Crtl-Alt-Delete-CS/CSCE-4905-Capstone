using UnityEngine;

public class OrganelleTarget : MonoBehaviour
{
    [field: SerializeField] public string OrganelleName { get; private set; } = "Organelle";
    [TextArea] public string description;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void SetSelected(bool isSelected)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_propBlock);
        if (isSelected)
        {
            _propBlock.SetColor(EmissionColor, Color.yellow * 0.8f);
            _renderer.material.EnableKeyword("_EMISSION");
        }
        else
        {
            _propBlock.SetColor(EmissionColor, Color.black);
        }
        _renderer.SetPropertyBlock(_propBlock);
    }
}