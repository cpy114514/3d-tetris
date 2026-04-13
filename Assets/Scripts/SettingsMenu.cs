using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;

    private Resolution[] resolutions;

    void Start()
    {
        // 默认隐藏设置面板
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetupVolume();
        SetupFullscreen();
        SetupResolution();
        SetupQuality();
    }

    void SetupVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    void SetupFullscreen()
    {
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = savedFullscreen;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = savedFullscreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    void SetupResolution()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        List<Resolution> uniqueResolutions = new List<Resolution>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution res = resolutions[i];

            bool alreadyExists = false;
            for (int j = 0; j < uniqueResolutions.Count; j++)
            {
                if (uniqueResolutions[j].width == res.width &&
                    uniqueResolutions[j].height == res.height)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                uniqueResolutions.Add(res);
                options.Add(res.width + " x " + res.height);

                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = uniqueResolutions.Count - 1;
                }
            }
        }

        resolutions = uniqueResolutions.ToArray();

        resolutionDropdown.AddOptions(options);

        int savedResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        savedResolutionIndex = Mathf.Clamp(savedResolutionIndex, 0, resolutions.Length - 1);

        resolutionDropdown.value = savedResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        ApplySavedResolution(savedResolutionIndex);
    }

    void SetupQuality()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();

        List<string> qualityOptions = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(qualityOptions);

        int savedQualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        savedQualityIndex = Mathf.Clamp(savedQualityIndex, 0, QualitySettings.names.Length - 1);

        qualityDropdown.value = savedQualityIndex;
        qualityDropdown.RefreshShownValue();

        QualitySettings.SetQualityLevel(savedQualityIndex, true);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
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

    public void SetResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;

        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);

        PlayerPrefs.SetInt("ResolutionIndex", index);
        PlayerPrefs.Save();
    }

    public void SetQuality(int index)
    {
        if (index < 0 || index >= QualitySettings.names.Length) return;

        QualitySettings.SetQualityLevel(index, true);

        PlayerPrefs.SetInt("QualityLevel", index);
        PlayerPrefs.Save();
    }

    private void ApplySavedResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;

        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
}