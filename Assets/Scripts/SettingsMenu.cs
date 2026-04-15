using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string GameplayDifficultyKey = "GameplayDifficulty";
    private static readonly string[] GameplayDifficultyNames =
    {
        "EASY",
        "NORMAL",
        "HARD"
    };

    [Header("UI")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public Button backButton;
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;
    public Dropdown gameplayDifficultyDropdown;

    private Resolution[] availableResolutions;
    private bool updatingControls;

    private void Awake()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Start()
    {
        WireSettingsPanel();
        SetupVolume();
        SetupFullscreen();
        EnsureInitialAutoSettings();
        SetupGameplayDifficulty();
    }

    public void OpenSettings()
    {
        WireSettingsPanel();
        SetupVolume();
        SetupFullscreen();
        SetupResolution();
        SetupQuality();
        SetupGameplayDifficulty();

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
            bool shouldOpen = !settingsPanel.activeSelf;
            if (shouldOpen)
            {
                OpenSettings();
                return;
            }

            settingsPanel.SetActive(false);
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

    public void SetResolutionIndex(int index)
    {
        if (updatingControls)
        {
            return;
        }

        ApplyResolutionIndex(index, true);
    }

    public void SetQualityIndex(int index)
    {
        if (updatingControls)
        {
            return;
        }

        ApplyQualityIndex(index, true);
    }

    public void SetGameplayDifficultyIndex(int index)
    {
        if (updatingControls)
        {
            return;
        }

        ApplyGameplayDifficultyIndex(index, true);
    }

    public void ApplyAutoResolution()
    {
        RefreshResolutionOptions();
        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            return;
        }

        Resolution nativeResolution = Screen.currentResolution;
        int bestIndex = 0;
        int bestDelta = int.MaxValue;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            int delta = Mathf.Abs(availableResolutions[i].width - nativeResolution.width) + Mathf.Abs(availableResolutions[i].height - nativeResolution.height);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestIndex = i;
            }
        }

        ApplyResolutionIndex(bestIndex, true);
    }

    public void ApplyAutoQuality()
    {
        ApplyQualityIndex(GetRecommendedQualityIndex(), true);
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

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(SetResolutionIndex);
            resolutionDropdown.onValueChanged.AddListener(SetResolutionIndex);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveListener(SetQualityIndex);
            qualityDropdown.onValueChanged.AddListener(SetQualityIndex);
        }

        EnsureGameplayDifficultyControl();
        if (gameplayDifficultyDropdown != null)
        {
            gameplayDifficultyDropdown.onValueChanged.RemoveListener(SetGameplayDifficultyIndex);
            gameplayDifficultyDropdown.onValueChanged.AddListener(SetGameplayDifficultyIndex);
        }
    }

    private void SetupResolution()
    {
        RefreshResolutionOptions();
        if (resolutionDropdown == null || availableResolutions == null || availableResolutions.Length == 0)
        {
            return;
        }

        int currentIndex = FindCurrentResolutionIndex();
        int savedIndex = Mathf.Clamp(PlayerPrefs.GetInt("ResolutionIndex", currentIndex), 0, availableResolutions.Length - 1);

        updatingControls = true;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution resolution = availableResolutions[i];
            options.Add(resolution.width + " x " + resolution.height);
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();
        updatingControls = false;

        ApplyResolutionIndex(savedIndex, false);
    }

    private void SetupQuality()
    {
        if (qualityDropdown == null)
        {
            return;
        }

        string[] qualityNames = QualitySettings.names;
        int qualityCount = qualityNames.Length;
        int savedIndex = qualityCount > 0 ? Mathf.Clamp(PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel()), 0, qualityCount - 1) : 0;

        updatingControls = true;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(qualityNames.Length > 0 ? qualityNames : new[] { "Default" }));
        qualityDropdown.value = savedIndex;
        qualityDropdown.RefreshShownValue();
        updatingControls = false;

        ApplyQualityIndex(savedIndex, false);
    }

    private void SetupGameplayDifficulty()
    {
        EnsureGameplayDifficultyControl();
        if (gameplayDifficultyDropdown == null)
        {
            return;
        }

        int savedIndex = Mathf.Clamp(PlayerPrefs.GetInt(GameplayDifficultyKey, 1), 0, GameplayDifficultyNames.Length - 1);
        updatingControls = true;
        gameplayDifficultyDropdown.ClearOptions();
        gameplayDifficultyDropdown.AddOptions(new List<string>(GameplayDifficultyNames));
        gameplayDifficultyDropdown.value = savedIndex;
        gameplayDifficultyDropdown.RefreshShownValue();
        updatingControls = false;

        ApplyGameplayDifficultyIndex(savedIndex, false);
    }

    private void EnsureGameplayDifficultyControl()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (gameplayDifficultyDropdown == null)
        {
            Transform existingDropdown = settingsPanel.transform.Find("Gameplay Difficulty Dropdown");
            gameplayDifficultyDropdown = existingDropdown != null ? existingDropdown.GetComponent<Dropdown>() : null;
        }

        if (gameplayDifficultyDropdown == null && qualityDropdown != null)
        {
            gameplayDifficultyDropdown = Instantiate(qualityDropdown, settingsPanel.transform);
            gameplayDifficultyDropdown.name = "Gameplay Difficulty Dropdown";
            gameplayDifficultyDropdown.onValueChanged = new Dropdown.DropdownEvent();
        }

        Transform label = settingsPanel.transform.Find("Gameplay Difficulty Label");
        if (label == null)
        {
            GameObject labelObject = new GameObject("Gameplay Difficulty Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(settingsPanel.transform, false);
            label = labelObject.transform;

            Text labelText = labelObject.GetComponent<Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                labelText.font = font;
            }

            labelText.text = "Difficulty";
            labelText.fontSize = 32;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
        }

        SetRect(label, new Vector2(0.5f, 1f), new Vector2(-190f, -500f), new Vector2(180f, 36f));

        if (gameplayDifficultyDropdown != null)
        {
            gameplayDifficultyDropdown.gameObject.SetActive(true);
            SetRect(gameplayDifficultyDropdown.transform, new Vector2(0.5f, 1f), new Vector2(102f, -500f), new Vector2(300f, 42f));
        }
    }

    private void RefreshResolutionOptions()
    {
        List<Resolution> uniqueResolutions = new List<Resolution>();
        Resolution[] screenResolutions = Screen.resolutions;
        for (int i = 0; i < screenResolutions.Length; i++)
        {
            Resolution resolution = screenResolutions[i];
            bool exists = false;
            for (int existing = 0; existing < uniqueResolutions.Count; existing++)
            {
                if (uniqueResolutions[existing].width == resolution.width && uniqueResolutions[existing].height == resolution.height)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                uniqueResolutions.Add(resolution);
            }
        }

        if (uniqueResolutions.Count == 0)
        {
            uniqueResolutions.Add(Screen.currentResolution);
        }

        availableResolutions = uniqueResolutions.ToArray();
    }

    private int FindCurrentResolutionIndex()
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == Screen.currentResolution.width && availableResolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }

        return Mathf.Clamp(availableResolutions.Length - 1, 0, availableResolutions.Length - 1);
    }

    private void ApplyResolutionIndex(int index, bool apply)
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            RefreshResolutionOptions();
        }

        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, availableResolutions.Length - 1);
        if (resolutionDropdown != null && resolutionDropdown.value != clampedIndex)
        {
            updatingControls = true;
            resolutionDropdown.value = clampedIndex;
            resolutionDropdown.RefreshShownValue();
            updatingControls = false;
        }

        if (apply)
        {
            Resolution resolution = availableResolutions[clampedIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }

        PlayerPrefs.SetInt("ResolutionIndex", clampedIndex);
        PlayerPrefs.Save();
    }

    private void ApplyQualityIndex(int index, bool apply)
    {
        int qualityCount = QualitySettings.names.Length;
        if (qualityCount <= 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, qualityCount - 1);
        if (qualityDropdown != null && qualityDropdown.value != clampedIndex)
        {
            updatingControls = true;
            qualityDropdown.value = clampedIndex;
            qualityDropdown.RefreshShownValue();
            updatingControls = false;
        }

        if (apply)
        {
            QualitySettings.SetQualityLevel(clampedIndex, true);
        }

        PlayerPrefs.SetInt("QualityLevel", clampedIndex);
        PlayerPrefs.Save();
    }

    private void ApplyGameplayDifficultyIndex(int index, bool apply)
    {
        int clampedIndex = Mathf.Clamp(index, 0, GameplayDifficultyNames.Length - 1);
        if (gameplayDifficultyDropdown != null && gameplayDifficultyDropdown.value != clampedIndex)
        {
            updatingControls = true;
            gameplayDifficultyDropdown.value = clampedIndex;
            gameplayDifficultyDropdown.RefreshShownValue();
            updatingControls = false;
        }

        if (apply)
        {
            PlayerPrefs.SetInt(GameplayDifficultyKey, clampedIndex);
            PlayerPrefs.Save();
        }
    }

    private static void SetRect(Transform target, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = target as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

}

