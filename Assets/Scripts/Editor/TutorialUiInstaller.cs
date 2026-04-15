using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TutorialUiInstaller
{
    private const string StartScenePath = "Assets/Scenes/Start.unity";

    [MenuItem("Tools/3D Tetris/Install Tutorial UI")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in Start scene.");
            return;
        }

        MainMenu mainMenu = Object.FindObjectOfType<MainMenu>();
        if (mainMenu == null)
        {
            GameObject manager = FindSceneObject("Start Menu");
            if (manager == null)
            {
                manager = FindSceneObject("GameManager");
            }

            if (manager == null)
            {
                manager = new GameObject("GameManager");
            }

            mainMenu = manager.GetComponent<MainMenu>();
            if (mainMenu == null)
            {
                mainMenu = manager.AddComponent<MainMenu>();
            }
        }

        Button tutorialButton = CreateButton(
            "Tutorial Button",
            canvas.transform,
            "TUTORIAL",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -500f),
            new Vector2(550f, 84f),
            new Color(0.25f, 0.31f, 0.36f, 1f));
        ReplaceListener(tutorialButton.onClick, mainMenu.OpenTutorialFromMenu);

        GameObject promptPanel = BuildTutorialPrompt(canvas.transform, mainMenu);
        GameObject oldTutorialPanel = FindSceneObject("Tutorial Panel");
        if (oldTutorialPanel != null)
        {
            Object.DestroyImmediate(oldTutorialPanel);
        }

        SerializedObject mainMenuObject = new SerializedObject(mainMenu);
        mainMenuObject.FindProperty("targetSceneName").stringValue = "GamePlay";
        mainMenuObject.FindProperty("tutorialPromptPanel").objectReferenceValue = promptPanel;
        mainMenuObject.FindProperty("tutorialPanel").objectReferenceValue = null;
        mainMenuObject.ApplyModifiedPropertiesWithoutUndo();

        promptPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject BuildTutorialPrompt(Transform parent, MainMenu mainMenu)
    {
        RectTransform panel = CreatePanel("Tutorial Prompt Panel", parent, new Color(0f, 0f, 0f, 0.98f), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(820f, 440f));
        ClearChildren(panel);

        CreateText("Tutorial Prompt Title", panel, "TUTORIAL", 64f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(680f, 92f));
        CreateText("Tutorial Prompt Text", panel, "Learn the controls before your first run?\nYou can skip and start now.", 34f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.86f, 0.9f, 0.88f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(700f, 130f));

        Button startTutorialButton = CreateButton("Start Tutorial Button", panel, "START TUTORIAL", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 82f), new Vector2(280f, 64f), new Color(0.13f, 0.66f, 0.55f, 1f));
        Button skipTutorialButton = CreateButton("Skip Tutorial Button", panel, "SKIP", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 82f), new Vector2(280f, 64f), new Color(0.35f, 0.38f, 0.41f, 1f));
        ReplaceListener(startTutorialButton.onClick, mainMenu.StartTutorialFromPrompt);
        ReplaceListener(skipTutorialButton.onClick, mainMenu.SkipTutorial);
        return panel.gameObject;
    }

    private static RectTransform CreatePanel(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Image image = CreateImage(objectName, parent, color, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        image.raycastTarget = true;
        return image.rectTransform;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        Image image = CreateImage(objectName, parent, color, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Button button = image.GetComponent<Button>();
        if (button == null)
        {
            button = image.gameObject.AddComponent<Button>();
        }

        ConfigureButton(button, color);
        ClearChildren(image.rectTransform);
        CreateText("Label", image.transform, label, 30f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform existing = parent.Find(objectName);
        GameObject gameObject;
        if (existing != null)
        {
            gameObject = existing.gameObject;
            if (gameObject.GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }

            if (gameObject.GetComponent<Image>() == null)
            {
                gameObject.AddComponent<Image>();
            }
        }
        else
        {
            gameObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
        }

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
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
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI textComponent = gameObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.color = color;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static void ConfigureButton(Button button, Color color)
    {
        button.targetGraphic = button.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.42f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    private static void ReplaceListener(UnityEvent unityEvent, UnityAction action)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(unityEvent, i);
        }

        UnityEventTools.AddPersistentListener(unityEvent, action);
    }

    private static void ClearChildren(Transform transform)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(transform.GetChild(i).gameObject);
        }
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
}
