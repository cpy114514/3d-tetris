using UnityEngine;

[DisallowMultipleComponent]
public sealed class UiPanelAnimator : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.22f;
    [SerializeField] private Vector3 startScale = new Vector3(0.94f, 0.94f, 1f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 targetScale;
    private float startTime;

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

        targetScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        startTime = Time.unscaledTime;

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.Scale(targetScale, startScale);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = targetScale == Vector3.zero ? Vector3.one : targetScale;
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

        float t = Mathf.Clamp01((Time.unscaledTime - startTime) / duration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        rectTransform.localScale = Vector3.Lerp(Vector3.Scale(targetScale, startScale), targetScale, eased);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = eased;
        }
    }
}
