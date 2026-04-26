using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class MainMenu : MonoBehaviour
{
    private const string TutorialEnteredKey = "TutorialEntered";
    private const string TutorialPromptHandledKey = "TutorialPromptHandled";
    private const string StartTutorialModeKey = "StartTutorialMode";
    private const string GameplayDifficultyKey = "GameplayDifficulty";

    [SerializeField] private string targetSceneName = "GamePlay";
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject tutorialPromptPanel;
    [SerializeField] private GameObject tutorialPanel;

    public void StartGame()
    {
        OpenDifficultyPanel();
    }

    public void SelectEasy()
    {
        SelectDifficultyAndContinue(0);
    }

    public void SelectNormal()
    {
        SelectDifficultyAndContinue(1);
    }

    public void SelectHard()
    {
        SelectDifficultyAndContinue(2);
    }

    public void CloseDifficulty()
    {
        SetPanelActive(difficultyPanel, false);
    }

    private void ContinueStartAfterDifficulty()
    {
        if (ShouldShowTutorialPrompt())
        {
            OpenTutorialPrompt();
            return;
        }

        LoadTargetScene();
    }

    public void OpenTutorialFromMenu()
    {
        StartInteractiveTutorial();
    }

    public void StartTutorialFromPrompt()
    {
        StartInteractiveTutorial();
    }

    public void SkipTutorial()
    {
        PlayerPrefs.SetInt(TutorialPromptHandledKey, 1);
        PlayerPrefs.Save();
        LoadTargetScene();
    }

    public void CloseTutorial()
    {
        SetPanelActive(difficultyPanel, false);
        SetPanelActive(tutorialPromptPanel, false);
        SetPanelActive(tutorialPanel, false);
    }

    public void FinishTutorialAndStartGame()
    {
        StartInteractiveTutorial();
    }

    public void QuitGame()
    {
        Debug.Log("Quit game.");
        Application.Quit();
    }

    private void Awake()
    {
        EnsureMenuAnimator();
        EnsureTutorialUi();
        SetPanelActive(difficultyPanel, false);
        SetPanelActive(tutorialPromptPanel, false);
        SetPanelActive(tutorialPanel, false);
    }

    private void EnsureTutorialUi()
    {
        Transform uiRoot = GetUiRoot();
        if (uiRoot == null)
        {
            return;
        }

        Button tutorialButton = FindButton("Tutorial Button");
        if (tutorialButton == null)
        {
            tutorialButton = CreateButton("Tutorial Button", uiRoot, "TUTORIAL", new Vector2(0.5f, 0.5f), new Vector2(0f, -500f), new Vector2(550f, 84f), new Color(0.25f, 0.31f, 0.36f, 1f), 30);
        }

        tutorialButton.onClick.RemoveListener(OpenTutorialFromMenu);
        tutorialButton.onClick.AddListener(OpenTutorialFromMenu);

        if (difficultyPanel == null)
        {
            GameObject existingDifficulty = FindSceneObject("Difficulty Selection Panel");
            difficultyPanel = existingDifficulty != null ? existingDifficulty : BuildDifficultyPanel(uiRoot);
        }
        EnsurePanelAnimator(difficultyPanel);

        if (tutorialPromptPanel == null)
        {
            GameObject existingPrompt = FindSceneObject("Tutorial Prompt Panel");
            tutorialPromptPanel = existingPrompt != null ? existingPrompt : BuildTutorialPrompt(uiRoot);
        }
        EnsurePanelAnimator(tutorialPromptPanel);
        EnsurePanelAnimator(tutorialPanel);

        WireTutorialPanelButtons();
        EnsureButtonAnimations(uiRoot);
    }

    private void EnsureMenuAnimator()
    {
        if (GetComponent<MainMenuAnimator>() == null)
        {
            gameObject.AddComponent<MainMenuAnimator>();
        }
    }

    private Transform GetUiRoot()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            return transform is RectTransform ? transform : canvas.transform;
        }

        canvas = FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private void WireTutorialPanelButtons()
    {
        WireButton("Easy Difficulty Button", SelectEasy);
        WireButton("Normal Difficulty Button", SelectNormal);
        WireButton("Hard Difficulty Button", SelectHard);
        WireButton("Difficulty Back Button", CloseDifficulty);
        WireButton("Start Tutorial Button", StartTutorialFromPrompt);
        WireButton("Skip Tutorial Button", SkipTutorial);
        WireButton("Tutorial Back Button", CloseTutorial);
        WireButton("Tutorial Start Game Button", FinishTutorialAndStartGame);
    }

    private void WireButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindButton(buttonName);
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Button FindButton(string buttonName)
    {
        GameObject buttonObject = FindSceneObject(buttonName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].name == objectName && objects[i].scene.IsValid())
            {
                return objects[i];
            }
        }

        return null;
    }

    private GameObject BuildDifficultyPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Difficulty Selection Panel", parent, new Vector2(880f, 560f));
        CreateText("Difficulty Title", panel.transform, "DIFFICULTY", 72, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(720f, 100f));
        CreateText("Difficulty Text", panel.transform, "Choose one.", 34, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(700f, 58f));
        CreateButton("Easy Difficulty Button", panel.transform, "EASY", new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(520f, 72f), new Color(0.13f, 0.58f, 0.48f, 1f), 30);
        CreateButton("Normal Difficulty Button", panel.transform, "NORMAL", new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(520f, 72f), new Color(0.22f, 0.36f, 0.55f, 1f), 30);
        CreateButton("Hard Difficulty Button", panel.transform, "HARD", new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(520f, 72f), new Color(0.56f, 0.16f, 0.18f, 1f), 30);
        CreateButton("Difficulty Back Button", panel.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(260f, 54f), new Color(0.35f, 0.38f, 0.41f, 1f), 24);
        return panel;
    }

    private GameObject BuildTutorialPrompt(Transform parent)
    {
        GameObject panel = CreatePanel("Tutorial Prompt Panel", parent, new Vector2(820f, 440f));
        CreateText("Tutorial Prompt Title", panel.transform, "TUTORIAL", 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(680f, 92f));
        CreateText("Tutorial Prompt Text", panel.transform, "Learn the controls before your first run?\nYou can skip and start now.", 34, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(700f, 130f));
        CreateButton("Start Tutorial Button", panel.transform, "START TUTORIAL", new Vector2(0.5f, 0f), new Vector2(-160f, 82f), new Vector2(280f, 64f), new Color(0.13f, 0.66f, 0.55f, 1f), 24);
        CreateButton("Skip Tutorial Button", panel.transform, "SKIP", new Vector2(0.5f, 0f), new Vector2(160f, 82f), new Vector2(280f, 64f), new Color(0.35f, 0.38f, 0.41f, 1f), 24);
        return panel;
    }

    private void StartInteractiveTutorial()
    {
        MarkTutorialEntered();
        PlayerPrefs.SetInt(StartTutorialModeKey, 1);
        PlayerPrefs.Save();
        SetPanelActive(tutorialPromptPanel, false);
        SetPanelActive(tutorialPanel, false);
        LoadTargetScene();
    }

    private GameObject BuildTutorialPanel(Transform parent)
    {
        GameObject panel = CreatePanel("Tutorial Panel", parent, new Vector2(1000f, 850f));
        CreateText("Tutorial Title", panel.transform, "TUTORIAL", 92, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(760f, 130f));
        CreateText("Tutorial Example 1", panel.transform, "Example 1 - place a block\nGap on the right: press D or Right Arrow until the piece is above it. Press W or Up Arrow to rotate. Press Space, S, or Down Arrow to drop.", 28, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(800f, 125f));
        CreateText("Tutorial Example 2", panel.transform, "Example 2 - change face\nThe front face is crowded. Press Q or E to look at the next face, then place the same falling piece on that face.", 28, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(800f, 105f));
        CreateText("Tutorial Example 3", panel.transform, "Example 3 - clear a line\nIf the face you see has a full horizontal line, dropping a piece to complete that visible line clears it and gives points.", 28, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(800f, 105f));
        CreateText("Tutorial Example 4", panel.transform, "Example 4 - preview check\nHold F to preview the 3D shape. You cannot place while holding F. If only the preview view forms a 3D line, only those preview-matching blocks clear.", 28, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -615f), new Vector2(800f, 125f));
        CreateButton("Tutorial Back Button", panel.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(-160f, 82f), new Vector2(260f, 58f), new Color(0.35f, 0.38f, 0.41f, 1f), 24);
        CreateButton("Tutorial Start Game Button", panel.transform, "START GAME", new Vector2(0.5f, 0f), new Vector2(160f, 82f), new Vector2(260f, 58f), new Color(0.13f, 0.66f, 0.55f, 1f), 24);
        return panel;
    }

    private static GameObject CreatePanel(string objectName, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.98f);
        image.raycastTarget = true;
        EnsurePanelAnimator(panel);
        return panel;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color, int fontSize)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.42f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        CreateText("Label", buttonObject.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 0f), Vector2.zero, Vector2.zero);
        EnsureButtonAnimation(buttonObject, 0f);
        return button;
    }

    private static Text CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor == Vector2.zero ? Vector2.one : anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text textComponent = textObject.GetComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            textComponent.font = font;
        }

        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private bool ShouldShowTutorialPrompt()
    {
        return PlayerPrefs.GetInt(TutorialEnteredKey, 0) == 0 && PlayerPrefs.GetInt(TutorialPromptHandledKey, 0) == 0 && tutorialPromptPanel != null;
    }

    private void OpenTutorialPrompt()
    {
        SetPanelActive(difficultyPanel, false);
        SetPanelActive(tutorialPanel, false);
        SetPanelActive(tutorialPromptPanel, true);
    }

    private void OpenDifficultyPanel()
    {
        SetPanelActive(tutorialPromptPanel, false);
        SetPanelActive(tutorialPanel, false);
        SetPanelActive(difficultyPanel, true);
    }

    private void SelectDifficultyAndContinue(int difficultyIndex)
    {
        PlayerPrefs.SetInt(GameplayDifficultyKey, Mathf.Clamp(difficultyIndex, 0, 2));
        PlayerPrefs.Save();
        SetPanelActive(difficultyPanel, false);
        ContinueStartAfterDifficulty();
    }

    private void MarkTutorialEntered()
    {
        PlayerPrefs.SetInt(TutorialEnteredKey, 1);
        PlayerPrefs.SetInt(TutorialPromptHandledKey, 1);
        PlayerPrefs.Save();
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene is not set.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            if (active)
            {
                EnsurePanelAnimator(panel);
            }

            panel.SetActive(active);
        }
    }

    private static void EnsurePanelAnimator(GameObject panel)
    {
        if (panel != null && panel.GetComponent<UiPanelAnimator>() == null)
        {
            panel.AddComponent<UiPanelAnimator>();
        }
    }

    private static void EnsureButtonAnimations(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            EnsureButtonAnimation(buttons[i].gameObject, i * 0.05f);
        }
    }

    private static void EnsureButtonAnimation(GameObject buttonObject, float delay)
    {
        if (buttonObject == null)
        {
            return;
        }

        UiFloatButton floatButton = buttonObject.GetComponent<UiFloatButton>();
        if (floatButton == null)
        {
            floatButton = buttonObject.AddComponent<UiFloatButton>();
        }

        floatButton.SetDelay(delay);
    }
}
