using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StartScenePolisher
{
    private const string StartScenePath = "Assets/Scenes/Start.unity";
    private const string GameScenePath = "Assets/Scenes/GamePlay.unity";

    [MenuItem("Tools/3D Tetris/Rebuild Start Scene")]
    public static void RebuildStartScene()
    {
        Scene scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);

        Camera camera = GetOrCreateCamera();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.07f, 0.065f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.46f, 0.44f);
        RenderSettings.sun = null;

        Canvas canvas = GetOrCreateCanvas();
        ClearChildren(canvas.transform);
        ConfigureCanvas(canvas);

        GameObject manager = GetOrCreateGameObject("GameManager");
        MainMenu mainMenu = GetOrAdd<MainMenu>(manager);
        SettingsMenu settingsMenu = GetOrAdd<SettingsMenu>(manager);

        CreateImage("Background", canvas.transform, new Color(0.06f, 0.07f, 0.065f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateImage("Top Accent", canvas.transform, new Color(0.14f, 0.75f, 0.62f, 0.88f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(0f, 16f));
        CreateImage("Bottom Accent", canvas.transform, new Color(0.96f, 0.34f, 0.28f, 0.82f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 16f));

        RectTransform hero = CreatePanel("Start Menu", canvas.transform, new Color(0.1f, 0.12f, 0.115f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 650f));
        CreateText("Title", hero, "CUBOID TETRIS", 68f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.98f, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(690f, 92f));
        CreateText("Subtitle", hero, "5 x 5 x 7 3D board", 28f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.52f, 0.92f, 0.82f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -153f), new Vector2(620f, 44f));
        CreateText("Controls", hero, "A/D move   W rotate   S or Space drop\nQ/E change face   F hold 3D preview", 25f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.9f, 0.92f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(650f, 96f));

        Button playButton = CreateButton("Play Button", hero, "PLAY", new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(320f, 64f), new Color(0.13f, 0.78f, 0.62f), Color.white);
        Button settingsButton = CreateButton("Settings Button", hero, "SETTINGS", new Vector2(0.5f, 0.5f), new Vector2(0f, -126f), new Vector2(320f, 58f), new Color(0.28f, 0.33f, 0.31f), Color.white);
        Button quitButton = CreateButton("Quit Button", hero, "QUIT", new Vector2(0.5f, 0.5f), new Vector2(0f, -204f), new Vector2(320f, 58f), new Color(0.78f, 0.22f, 0.2f), Color.white);

        UnityEventTools.AddPersistentListener(playButton.onClick, mainMenu.StartGame);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, settingsMenu.OpenSettings);
        UnityEventTools.AddPersistentListener(quitButton.onClick, mainMenu.QuitGame);

        RectTransform settingsPanel = CreatePanel("Settings Panel", canvas.transform, new Color(0.09f, 0.105f, 0.1f, 0.97f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 470f));
        CreateText("Settings Title", settingsPanel, "SETTINGS", 46f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(560f, 70f));
        CreateText("Volume Label", settingsPanel, "Volume", 24f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -142f), new Vector2(180f, 42f));
        Slider volumeSlider = CreateSlider("Volume Slider", settingsPanel, new Vector2(0.5f, 1f), new Vector2(92f, -163f), new Vector2(330f, 32f));
        Toggle fullscreenToggle = CreateToggle("Fullscreen Toggle", settingsPanel, "Fullscreen", new Vector2(0.5f, 1f), new Vector2(-82f, -235f), new Vector2(430f, 48f));
        CreateText("Settings Note", settingsPanel, "Resolution and quality use your current Unity/project defaults.", 20f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.82f, 0.86f, 0.84f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -302f), new Vector2(560f, 56f));
        Button backButton = CreateButton("Back Button", settingsPanel, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(250f, 54f), new Color(0.28f, 0.33f, 0.31f), Color.white);
        UnityEventTools.AddPersistentListener(backButton.onClick, settingsMenu.CloseSettings);

        SerializedObject mainMenuObject = new SerializedObject(mainMenu);
        mainMenuObject.FindProperty("targetSceneName").stringValue = "GamePlay";
        mainMenuObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject settingsObject = new SerializedObject(settingsMenu);
        settingsObject.FindProperty("settingsPanel").objectReferenceValue = settingsPanel.gameObject;
        settingsObject.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
        settingsObject.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
        settingsObject.FindProperty("resolutionDropdown").objectReferenceValue = null;
        settingsObject.FindProperty("qualityDropdown").objectReferenceValue = null;
        settingsObject.ApplyModifiedPropertiesWithoutUndo();
        settingsPanel.gameObject.SetActive(false);

        EnsureEventSystem();
        EnsureBuildScenes();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Camera GetOrCreateCamera()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            return camera;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static Canvas GetOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            canvas.name = "Canvas";
            return canvas;
        }

        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        return canvasObject.GetComponent<Canvas>();
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(canvas.gameObject);
    }

    private static GameObject GetOrCreateGameObject(string objectName)
    {
        GameObject gameObject = GameObject.Find(objectName);
        return gameObject != null ? gameObject : new GameObject(objectName);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static RectTransform CreatePanel(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Image image = CreateImage(objectName, parent, color, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        image.raycastTarget = true;
        return image.rectTransform;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        TextMeshProUGUI textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = color;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, Color backgroundColor, Color textColor)
    {
        Image image = CreateImage(objectName, parent, backgroundColor, anchor, anchor, anchoredPosition, sizeDelta);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateText("Label", image.transform, label, 28f, FontStyles.Bold, TextAlignmentOptions.Center, textColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static Slider CreateSlider(string objectName, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = anchor;
        sliderRect.anchorMax = anchor;
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = sizeDelta;

        Image background = CreateImage("Background", sliderObject.transform, new Color(0.22f, 0.25f, 0.24f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.raycastTarget = false;
        RectTransform fillArea = CreateEmptyRect("Fill Area", sliderObject.transform, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(0f, 0f), new Vector2(-24f, 0f));
        Image fill = CreateImage("Fill", fillArea, new Color(0.14f, 0.75f, 0.62f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform handleArea = CreateEmptyRect("Handle Slide Area", sliderObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
        Image handle = CreateImage("Handle", handleArea, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 36f));

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Toggle CreateToggle(string objectName, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject toggleObject = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = anchor;
        toggleRect.anchorMax = anchor;
        toggleRect.anchoredPosition = anchoredPosition;
        toggleRect.sizeDelta = sizeDelta;

        Image background = CreateImage("Box", toggleObject.transform, new Color(0.22f, 0.25f, 0.24f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(34f, 34f));
        Image checkmark = CreateImage("Checkmark", background.transform, new Color(0.14f, 0.75f, 0.62f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
        CreateText("Label", toggleObject.transform, label, 24f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(74f, 0f), new Vector2(-74f, 0f));

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        toggle.isOn = true;
        return toggle;
    }

    private static RectTransform CreateEmptyRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        return rectTransform;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.position = Vector3.zero;
    }

    private static void EnsureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(StartScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
