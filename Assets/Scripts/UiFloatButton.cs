using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiFloatButton : MonoBehaviour
{
    [SerializeField, Min(0f)] private float delay = 0f;
    [SerializeField, Min(0.01f)] private float enterDuration = 0.42f;
    [SerializeField] private float enterOffsetY = -72f;
    [SerializeField, Min(0f)] private float idleAmplitude = 4f;
    [SerializeField, Min(0f)] private float idleSpeed = 1.8f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 targetPosition;
    private float startTime;
    private bool entering;

    public void SetDelay(float value)
    {
        delay = Mathf.Max(0f, value);
        startTime = Time.unscaledTime;
        entering = true;
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        targetPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        startTime = Time.unscaledTime;
        entering = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = targetPosition + new Vector2(0f, enterOffsetY);
        }
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = targetPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        float elapsed = Time.unscaledTime - startTime - delay;
        if (elapsed < 0f)
        {
            rectTransform.anchoredPosition = targetPosition + new Vector2(0f, enterOffsetY);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            return;
        }

        if (entering)
        {
            float t = Mathf.Clamp01(elapsed / enterDuration);
            float eased = EaseOutBack(t);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(targetPosition + new Vector2(0f, enterOffsetY), targetPosition, eased);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            }

            if (t >= 1f)
            {
                entering = false;
            }

            return;
        }

        float idle = Mathf.Sin((Time.unscaledTime + delay) * idleSpeed) * idleAmplitude;
        rectTransform.anchoredPosition = targetPosition + new Vector2(0f, idle);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
