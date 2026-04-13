using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const float UiColorGrayMix = 0.34f;

    [Header("UI")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public Button backButton;

    private void Start()
    {
        EnsureSettingsPanel();
        SetupVolume();
        SetupFullscreen();
        EnsureInitialAutoSettings();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetupVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    private void SetupFullscreen()
    {
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = savedFullscreen;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = savedFullscreen;
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void EnsureInitialAutoSettings()
    {
        if (PlayerPrefs.GetInt("AutoSettingsInitialized", 0) == 1)
        {
            return;
        }

        ApplyAutoResolution();
        ApplyAutoQuality();
        PlayerPrefs.SetInt("AutoSettingsInitialized", 1);
        PlayerPrefs.Save();
    }

    private void ApplyAutoResolution()
    {
        Resolution bestResolution = Screen.currentResolution;
        Screen.SetResolution(bestResolution.width, bestResolution.height, Screen.fullScreen);
    }

    private void ApplyAutoQuality()
    {
        int qualityIndex = GetRecommendedQualityIndex();
        if (QualitySettings.names.Length <= 0)
        {
            qualityIndex = 0;
        }
        else
        {
            qualityIndex = Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1);
        }

        QualitySettings.SetQualityLevel(qualityIndex, true);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();
    }

    private int GetRecommendedQualityIndex()
    {
        int qualityCount = QualitySettings.names.Length;
        if (qualityCount <= 1)
        {
            return 0;
        }

        int performanceScore = SystemInfo.graphicsMemorySize / 512 + SystemInfo.systemMemorySize / 2048 + SystemInfo.processorCount;
        float normalizedScore = Mathf.InverseLerp(7f, 24f, performanceScore);
        return Mathf.Clamp(Mathf.RoundToInt(normalizedScore * (qualityCount - 1)), 0, qualityCount - 1);
    }

    private void EnsureSettingsPanel()
    {
        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find("Settings Panel");
        }

        if (settingsPanel == null)
        {
            return;
        }

        DisableAllChildren(settingsPanel.transform);

        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(640f, 620f);
        }

        Image panelImage = settingsPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = MutedColor(new Color(0f, 0f, 0f, 0.84f));
        }

        EnsureSettingsPanelContents();
        WireSettingsPanel();
    }

    private void EnsureSettingsPanelContents()
    {
        Transform panelTransform = settingsPanel.transform;

        EnsureOrCreateText(panelTransform, "Settings Title", "SETTINGS", 36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(500f, 54f));
        EnsureOrCreateText(panelTransform, "Volume Label", "Volume", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-180f, -130f), new Vector2(180f, 36f));

        if (volumeSlider == null)
        {
            volumeSlider = CreateUiSlider("Volume Slider", panelTransform, new Vector2(0.5f, 1f), new Vector2(80f, -130f), new Vector2(300f, 32f));
        }
        volumeSlider.gameObject.SetActive(true);

        if (fullscreenToggle == null)
        {
            fullscreenToggle = CreateUiToggle("Fullscreen Toggle", panelTransform, "Fullscreen", new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(360f, 44f));
        }
        fullscreenToggle.gameObject.SetActive(true);

        if (backButton == null)
        {
            backButton = CreateUiButton("Settings Back Button", panelTransform, "BACK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(260f, 52f));
        }
        ApplyButtonColor(backButton, new Color(0.34f, 0.38f, 0.42f, 1f));
        backButton.gameObject.SetActive(true);
    }

    private void WireSettingsPanel()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(CloseSettings);
            backButton.onClick.AddListener(CloseSettings);
        }
    }

    private static void DisableAllChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            parent.GetChild(i).gameObject.SetActive(false);
        }
    }

    private static void EnsureOrCreateText(Transform parent, string objectName, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform existing = parent.Find(objectName);
        if (existing == null)
        {
            CreateUiText(objectName, parent, text, fontSize, fontStyle, alignment, color, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            return;
        }

        existing.gameObject.SetActive(true);
        RectTransform rectTransform = existing.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        Text uiText = existing.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.fontStyle = fontStyle;
            uiText.alignment = alignment;
            uiText.color = color;
        }
    }

    private static Image CreateUiImage(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image image = imageObject.GetComponent<Image>();
        image.color = MutedColor(color);
        return image;
    }

    private static Text CreateUiText(string objectName, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.raycastTarget = false;

        return uiText;
    }

    private static Button CreateUiButton(string objectName, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Image image = CreateUiImage(objectName, parent, new Color(0.26f, 0.3f, 0.34f, 1f), anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Button button = image.gameObject.AddComponent<Button>();
        ApplyButtonColor(button, image.color);

        CreateUiText("Label", image.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        return button;
    }

    private static void ApplyButtonColor(Button button, Color color)
    {
        if (button == null)
        {
            return;
        }

        color = MutedColor(color);

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
            button.targetGraphic = image;
        }

        if (image != null)
        {
            image.color = color;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.42f);
        button.colors = colors;
    }

    private static Slider CreateUiSlider(string objectName, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = anchor;
        sliderRect.anchorMax = anchor;
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = sizeDelta;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        Image background = CreateUiImage("Background", sliderObject.transform, new Color(0.18f, 0.2f, 0.2f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.raycastTarget = false;

        RectTransform fillArea = CreateUiRect("Fill Area", sliderObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
        Image fill = CreateUiImage("Fill", fillArea, new Color(0.13f, 0.78f, 0.62f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform handleArea = CreateUiRect("Handle Slide Area", sliderObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
        Image handle = CreateUiImage("Handle", handleArea, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 36f));

        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;

        return slider;
    }

    private static Toggle CreateUiToggle(string objectName, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject toggleObject = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = anchor;
        toggleRect.anchorMax = anchor;
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = anchoredPosition;
        toggleRect.sizeDelta = sizeDelta;

        Toggle toggle = toggleObject.GetComponent<Toggle>();

        Image box = CreateUiImage("Box", toggleObject.transform, new Color(0.18f, 0.2f, 0.2f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(34f, 34f));
        Image checkmark = CreateUiImage("Checkmark", box.transform, new Color(0.13f, 0.78f, 0.62f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
        CreateUiText("Label", toggleObject.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(74f, 0f), new Vector2(-74f, 0f));

        toggle.targetGraphic = box;
        toggle.graphic = checkmark;

        return toggle;
    }

    private static RectTransform CreateUiRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        return rectTransform;
    }

    private static Color MutedColor(Color color)
    {
        float alpha = color.a;
        float gray = color.grayscale;
        Color grayColor = new Color(gray, gray, gray, alpha);
        Color mutedColor = Color.Lerp(color, grayColor, UiColorGrayMix);
        mutedColor.a = alpha;
        return mutedColor;
    }
}

