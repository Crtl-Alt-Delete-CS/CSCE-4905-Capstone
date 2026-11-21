using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadingBar : MonoBehaviour
{
    [SerializeField] private Image trackImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform shineTransform;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Color trackColor = new Color32(22, 32, 53, 255);
    [SerializeField] private Color fillColor = new Color32(0, 212, 170, 255);

    private float shimmerPhase;

    private void Awake()
    {
        Configure();
    }

    private void Configure()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillImage.color = fillColor;
        }

        if (trackImage != null)
            trackImage.color = trackColor;
    }

    private void Update()
    {
        if (shineTransform == null || trackImage == null)
            return;

        RectTransform trackRect = trackImage.rectTransform;
        float fillAmount = fillImage != null ? fillImage.fillAmount : 0f;
        shimmerPhase = (shimmerPhase + Time.unscaledDeltaTime * 0.75f) % 1f;
        float targetX = Mathf.Lerp(-trackRect.rect.width * 0.5f, trackRect.rect.width * 0.5f, fillAmount);
        shineTransform.anchoredPosition = new Vector2(
            targetX + Mathf.Sin(shimmerPhase * Mathf.PI * 2f) * 3f,
            shineTransform.anchoredPosition.y);

        Canvas.ForceUpdateCanvases();
    }

    public void SetProgress(float progress, string status)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(progress);

        if (statusLabel != null)
            statusLabel.text = status;
    }
}
