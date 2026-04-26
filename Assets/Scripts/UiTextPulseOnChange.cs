using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiTextPulseOnChange : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [SerializeField, Min(1f)] private float pulseScale = 1.12f;
    [SerializeField] private Color flashColor = new Color(0.88f, 0.86f, 0.58f, 1f);

    private Text uiText;
    private RectTransform rectTransform;
    private string lastText;
    private Vector3 baseScale;
    private Color baseColor;
    private float pulseStartTime;
    private bool pulsing;

    private void Awake()
    {
        uiText = GetComponent<Text>();
        rectTransform = transform as RectTransform;
    }

    private void OnEnable()
    {
        if (uiText == null)
        {
            uiText = GetComponent<Text>();
        }

        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        baseColor = uiText != null ? uiText.color : Color.white;
        lastText = uiText != null ? uiText.text : string.Empty;
        pulsing = false;
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
        }

        if (uiText != null)
        {
            uiText.color = baseColor;
        }
    }

    private void Update()
    {
        if (uiText == null || rectTransform == null)
        {
            return;
        }

        if (uiText.text != lastText)
        {
            lastText = uiText.text;
            baseScale = rectTransform.localScale;
            baseColor = uiText.color;
            pulseStartTime = Time.unscaledTime;
            pulsing = true;
        }

        if (!pulsing)
        {
            return;
        }

        float t = Mathf.Clamp01((Time.unscaledTime - pulseStartTime) / duration);
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        float scale = Mathf.Lerp(pulseScale, 1f, ease);
        rectTransform.localScale = baseScale * scale;
        uiText.color = Color.Lerp(flashColor, baseColor, ease);

        if (t >= 1f)
        {
            pulsing = false;
            rectTransform.localScale = baseScale;
            uiText.color = baseColor;
        }
    }
}
