using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class ThreeDTetrisGame : MonoBehaviour
{
    private const int FrontLayer = 0;

    [Header("Board")]
    [SerializeField, Min(3)] private int boardSide = 5;
    [SerializeField, Min(4)] private int boardHeight = 7;

    [Header("Gameplay")]
    [SerializeField, Min(0.1f)] private float cubeSize = 0.88f;
    [SerializeField, Min(0.05f)] private float fallInterval = 0.55f;
    [SerializeField, Min(0.01f)] private float softDropInterval = 0.055f;

    [Header("Difficulty")]
    [SerializeField, Min(1)] private int rowsPerDifficultyLevel = 4;
    [SerializeField, Min(1)] private int piecesPerDifficultyLevel = 12;

    [Header("Container")]
    [SerializeField] private float containerTurnAngle = 90f;
    [SerializeField, Min(1f)] private float containerTurnSpeed = 280f;

    [Header("Preview And Effects")]
    [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.28f;
    [SerializeField] private Color ghostPreviewColor = new Color(0.9f, 0.92f, 0.94f);
    [SerializeField] private Color nextPreviewColor = new Color(0.82f, 0.84f, 0.86f);
    [SerializeField, Min(0.05f)] private float clearEffectDuration = 0.36f;

    [Header("Editable 3D References")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Transform containerRoot;
    [SerializeField] private Transform containerVisualRoot;
    [SerializeField] private Transform lockedRoot;
    [SerializeField] private Transform activeRoot;
    [SerializeField] private Transform ghostRoot;
    [SerializeField] private Transform nextRoot;
    [SerializeField] private Transform warningRoot;
    [SerializeField] private Transform effectsRoot;

    [Header("Editable Materials")]
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material railMaterial;
    [SerializeField] private Material gridMaterial;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Material nextPreviewMaterial;
    [SerializeField] private Material deathWarningMaterial;
    [SerializeField] private Material clearEffectMaterial;

    [Header("Editable UI References")]
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text nextLabelText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image gameOverRedBar;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Text gameOverHintText;
    [SerializeField] private Button restartButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Text pauseText;

    private int BoardWidth => Mathf.Max(3, boardSide);
    private int BoardDepth => Mathf.Max(3, boardSide);
    private int BoardHeight => Mathf.Max(4, boardHeight);
    private float CubeSize => Mathf.Max(0.1f, cubeSize);
    private float FallInterval => Mathf.Max(0.05f, fallInterval);
    private float SoftDropInterval => Mathf.Max(0.01f, softDropInterval);
    private int RowsPerDifficultyLevel => Mathf.Max(1, rowsPerDifficultyLevel);
    private int PiecesPerDifficultyLevel => Mathf.Max(1, piecesPerDifficultyLevel);
    private float ContainerTurnAngle => containerTurnAngle;
    private float ContainerTurnSpeed => Mathf.Max(1f, containerTurnSpeed);
    private float GhostAlpha => Mathf.Clamp01(ghostAlpha);
    private float ClearEffectDuration => Mathf.Max(0.05f, clearEffectDuration);

    private readonly Vector3Int[] wallKicks =
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(-2, 0, 0),
        new Vector3Int(2, 0, 0),
        new Vector3Int(0, -2, 0),
        new Vector3Int(0, 2, 0),
        new Vector3Int(-3, 0, 0),
        new Vector3Int(3, 0, 0),
        new Vector3Int(0, -3, 0),
        new Vector3Int(0, 3, 0)
    };

    private Transform deathWarningBar;
    private Transform[,,] grid;
    private Transform[] activeCubes;
    private Transform[] ghostCubes;
    private Material[] pieceMaterials;
    private PieceDefinition[] pieces;
    private readonly List<ClearEffect> clearEffects = new List<ClearEffect>();

    private Vector3Int currentOrigin;
    private Vector3Int[] currentCells;
    private int currentPieceIndex;
    private int nextPieceIndex;
    private int score;
    private int layers;
    private int level;
    private int lockedPieces;
    private int activeFace;
    private float fallTimer;
    private float containerYaw;
    private float targetContainerYaw;
    private bool pieceFalling;
    private bool gameOver;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<ThreeDTetrisGame>() != null)
        {
            return;
        }

        GameObject gameObject = new GameObject("Editable Cuboid Tetris");
        gameObject.AddComponent<ThreeDTetrisGame>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        grid = new Transform[BoardWidth, BoardHeight, BoardDepth];
        activeCubes = new Transform[0];
        ghostCubes = new Transform[0];

        BuildPieceDefinitions();
        BuildMaterials();
        BuildWorld();
        ResetGame();
    }

    private void OnValidate()
    {
        boardSide = Mathf.Max(3, boardSide);
        boardHeight = Mathf.Max(4, boardHeight);
        cubeSize = Mathf.Max(0.1f, cubeSize);
        fallInterval = Mathf.Max(0.05f, fallInterval);
        softDropInterval = Mathf.Max(0.01f, softDropInterval);
        rowsPerDifficultyLevel = Mathf.Max(1, rowsPerDifficultyLevel);
        piecesPerDifficultyLevel = Mathf.Max(1, piecesPerDifficultyLevel);
        containerTurnSpeed = Mathf.Max(1f, containerTurnSpeed);
        ghostAlpha = Mathf.Clamp01(ghostAlpha);
        clearEffectDuration = Mathf.Max(0.05f, clearEffectDuration);
    }

    [ContextMenu("Build Editable Scene Objects")]
    public void BuildEditableSceneObjects()
    {
        BuildPieceDefinitions();
        BuildMaterials();
        BuildWorld();
        UpdateUi();
    }

    [ContextMenu("Rebuild Container Visuals")]
    public void RebuildContainerVisuals()
    {
        BuildMaterials();
        containerRoot = GetOrCreateChild(containerRoot, BoardWidth + "x" + BoardDepth + "x" + BoardHeight + " Cuboid", transform);
        containerVisualRoot = GetOrCreateChild(containerVisualRoot, "Container Visuals", containerRoot);
        ClearChildrenImmediate(containerVisualRoot);
        BuildCuboid();
    }

    private void Update()
    {
        UpdateContainerRotation();
        UpdateClearEffects();
        UpdateDeathWarning();
        UpdateUi();

        if (Input.GetKeyDown(KeyCode.N))
        {
            ResetGame();
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            paused = !paused;
            UpdateUi();
        }

        if (paused || gameOver)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.S))
        {
            pieceFalling = true;
            fallTimer = 0f;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            TryMove(new Vector3Int(-1, 0, 0));
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            TryMove(new Vector3Int(1, 0, 0));
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            targetContainerYaw -= ContainerTurnAngle;
            activeFace = WrapFace(activeFace - 1);
            TryClearSelectedFace();
            RefreshGhostPiece();
            UpdateDeathWarning();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            targetContainerYaw += ContainerTurnAngle;
            activeFace = WrapFace(activeFace + 1);
            TryClearSelectedFace();
            RefreshGhostPiece();
            UpdateDeathWarning();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            TryRotate(1);
        }

        if (!pieceFalling)
        {
            return;
        }

        float fallInterval = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.S) ? GetSoftDropInterval() : GetFallInterval();
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallInterval)
        {
            fallTimer = 0f;
            DropOneLayer();
        }
    }

    private void SetupEditableUi()
    {
        gameCanvas = GetOrCreateCanvas();
        EnsureEventSystem();

        if (scoreText == null)
        {
            Image scorePanel = CreateUiImage("Score Panel", gameCanvas.transform, new Color(0f, 0f, 0f, 0.48f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(250f, 76f));
            scoreText = CreateUiText("Score Text", scorePanel.transform, "Score 0", 42, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(222f, 64f));
        }

        if (nextLabelText == null)
        {
            nextLabelText = CreateUiText("Next Label", gameCanvas.transform, "NEXT", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-115f, -40f), new Vector2(160f, 32f));
        }

        if (gameOverRedBar == null)
        {
            gameOverRedBar = CreateUiImage("Game Over Red Bar", gameCanvas.transform, new Color(0.78f, 0.03f, 0.04f, 0.92f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -70f), new Vector2(0f, 70f));
        }

        if (gameOverText == null)
        {
            gameOverText = CreateUiText("Game Over Text", gameOverRedBar.transform, "GAME OVER", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        }

        if (gameOverPanel == null)
        {
            Image panel = CreateUiImage("Game Over Panel", gameCanvas.transform, new Color(0f, 0f, 0f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(320f, 104f));
            gameOverPanel = panel.gameObject;
        }

        if (gameOverHintText == null)
        {
            gameOverHintText = CreateUiText("Restart Hint", gameOverPanel.transform, "Click restart to play again", 18, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(300f, 28f));
        }

        if (restartButton == null)
        {
            restartButton = CreateUiButton("Restart Button", gameOverPanel.transform, "RESTART", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(190f, 42f));
        }

        restartButton.onClick.RemoveListener(ResetGame);
        restartButton.onClick.AddListener(ResetGame);

        if (pausePanel == null)
        {
            Image panel = CreateUiImage("Pause Panel", gameCanvas.transform, new Color(0f, 0f, 0f, 0.72f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 150f));
            pausePanel = panel.gameObject;
        }

        if (pauseText == null)
        {
            pauseText = CreateUiText("Pause Text", pausePanel.transform, "PAUSED\nPress P to resume", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        }

        UpdateUi();
    }

    private void UpdateUi()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score " + score;
        }

        if (nextLabelText != null)
        {
            nextLabelText.gameObject.SetActive(true);
        }

        if (gameOverRedBar != null)
        {
            gameOverRedBar.gameObject.SetActive(gameOver);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(gameOver);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(paused && !gameOver);
        }
    }

    private Canvas GetOrCreateCanvas()
    {
        if (gameCanvas != null)
        {
            ConfigureCanvas(gameCanvas);
            return gameCanvas;
        }

        gameCanvas = FindObjectOfType<Canvas>();
        if (gameCanvas != null)
        {
            ConfigureCanvas(gameCanvas);
            return gameCanvas;
        }

        GameObject canvasObject = new GameObject("Editable Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        gameCanvas = canvasObject.GetComponent<Canvas>();
        ConfigureCanvas(gameCanvas);
        return gameCanvas;
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private Image CreateUiImage(string objectName, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
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
        image.color = color;
        return image;
    }

    private Text CreateUiText(string objectName, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Text uiText = textObject.GetComponent<Text>();
        Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtInFont != null)
        {
            uiText.font = builtInFont;
        }

        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.raycastTarget = false;
        return uiText;
    }

    private Button CreateUiButton(string objectName, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Image image = CreateUiImage(objectName, parent, new Color(0.86f, 0.05f, 0.06f, 1f), anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.86f, 0.05f, 0.06f, 1f);
        colors.highlightedColor = new Color(1f, 0.14f, 0.14f, 1f);
        colors.pressedColor = new Color(0.58f, 0f, 0.02f, 1f);
        button.colors = colors;

        CreateUiText("Label", image.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        return button;
    }

    private void BuildPieceDefinitions()
    {
        pieces = new[]
        {
            new PieceDefinition("I3", new Color(0f, 0.9f, 1f), 1, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0)),
            new PieceDefinition("V3", new Color(0.22f, 0.42f, 1f), 1, new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("O", new Color(1f, 0.86f, 0.12f), 1, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("T", new Color(0.8f, 0.28f, 1f), 2, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0)),
            new PieceDefinition("S", new Color(0.14f, 0.9f, 0.28f), 2, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0)),
            new PieceDefinition("Z", new Color(1f, 0.18f, 0.18f), 2, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("J", new Color(0.22f, 0.42f, 1f), 2, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0)),
            new PieceDefinition("L", new Color(1f, 0.54f, 0.12f), 2, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("P5", new Color(1f, 0.36f, 0.68f), 4, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("U5", new Color(0.26f, 1f, 0.66f), 4, new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("V5", new Color(1f, 0.66f, 0.1f), 5, new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0), new Vector3Int(1, -2, 0), new Vector3Int(2, -2, 0)),
            new PieceDefinition("W5", new Color(0.55f, 1f, 0.25f), 5, new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0), new Vector3Int(1, -2, 0)),
            new PieceDefinition("X5", new Color(0.96f, 0.52f, 1f), 6, new Vector3Int(0, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("F5", new Color(0.12f, 0.78f, 1f), 6, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("C6", new Color(1f, 0.32f, 0.2f), 9, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, -2, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("Y6", new Color(0.58f, 0.72f, 1f), 9, new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0), new Vector3Int(0, -3, 0), new Vector3Int(-1, -1, 0), new Vector3Int(1, -2, 0))
        };
    }

    private void BuildMaterials()
    {
        if (bodyMaterial == null)
        {
            bodyMaterial = MakeTransparentMaterial("Transparent Cuboid", new Color(0.08f, 0.1f, 0.12f, 0.18f), 0f, 0.18f);
        }

        if (railMaterial == null)
        {
            railMaterial = MakeMaterial("Cuboid Rails", new Color(0.74f, 0.79f, 0.83f), 0.04f, 0.58f);
        }

        if (gridMaterial == null)
        {
            gridMaterial = MakeTransparentMaterial("Cuboid Grid", new Color(1f, 1f, 1f, 0.18f), 0f, 0.18f);
        }

        if (ghostMaterial == null)
        {
            ghostMaterial = MakeTransparentMaterial("Ghost Piece", new Color(ghostPreviewColor.r, ghostPreviewColor.g, ghostPreviewColor.b, GhostAlpha), 0f, 0.2f);
        }

        if (nextPreviewMaterial == null)
        {
            nextPreviewMaterial = MakeMaterial("Next Preview", nextPreviewColor, 0.02f, 0.48f);
        }

        if (deathWarningMaterial == null)
        {
            deathWarningMaterial = MakeTransparentMaterial("Death Warning", new Color(1f, 0f, 0f, 0f), 0f, 0.08f);
        }

        if (clearEffectMaterial == null)
        {
            clearEffectMaterial = MakeTransparentMaterial("Clear Row Flash", new Color(1f, 0.28f, 0.22f, 0.38f), 0f, 0.2f);
        }

        pieceMaterials = new Material[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            pieceMaterials[i] = MakeMaterial(pieces[i].Name + " Piece", pieces[i].Color, 0.02f, 0.58f);
        }
    }

    private void BuildWorld()
    {
        SetupCamera();
        SetupLighting();

        containerRoot = GetOrCreateChild(containerRoot, BoardWidth + "x" + BoardDepth + "x" + BoardHeight + " Cuboid", transform);
        containerVisualRoot = GetOrCreateChild(containerVisualRoot, "Container Visuals", containerRoot);
        lockedRoot = GetOrCreateChild(lockedRoot, "Locked Blocks", containerRoot);
        activeRoot = GetOrCreateChild(activeRoot, "Active Piece", transform);
        ghostRoot = GetOrCreateChild(ghostRoot, "Ghost Piece", transform);
        nextRoot = GetOrCreateChild(nextRoot, "Next Piece Preview", transform);
        warningRoot = GetOrCreateChild(warningRoot, "Death Warning", transform);
        effectsRoot = GetOrCreateChild(effectsRoot, "Clear Effects", transform);

        lockedRoot.SetParent(containerRoot, false);

        if (containerVisualRoot.childCount == 0)
        {
            BuildCuboid();
        }

        BuildDeathWarning();
        SetupEditableUi();
    }

    private void BuildCuboid()
    {
        CreateCube("Transparent " + BoardWidth + "x" + BoardDepth + "x" + BoardHeight + " Cuboid", Vector3.zero, new Vector3(BoardWidth + 0.32f, BoardHeight + 0.32f, BoardDepth + 0.32f), bodyMaterial, containerVisualRoot);

        float halfX = BoardWidth * 0.5f;
        float halfY = BoardHeight * 0.5f;
        float halfZ = BoardDepth * 0.5f;
        float rail = 0.14f;

        CreateCube("Front Left Rail", new Vector3(-halfX, 0f, -halfZ), new Vector3(rail, BoardHeight + 0.5f, rail), railMaterial, containerVisualRoot);
        CreateCube("Front Right Rail", new Vector3(halfX, 0f, -halfZ), new Vector3(rail, BoardHeight + 0.5f, rail), railMaterial, containerVisualRoot);
        CreateCube("Back Left Rail", new Vector3(-halfX, 0f, halfZ), new Vector3(rail, BoardHeight + 0.5f, rail), railMaterial, containerVisualRoot);
        CreateCube("Back Right Rail", new Vector3(halfX, 0f, halfZ), new Vector3(rail, BoardHeight + 0.5f, rail), railMaterial, containerVisualRoot);

        CreateCube("Front Top Rail", new Vector3(0f, halfY, -halfZ), new Vector3(BoardWidth + 0.5f, rail, rail), railMaterial, containerVisualRoot);
        CreateCube("Front Bottom Rail", new Vector3(0f, -halfY, -halfZ), new Vector3(BoardWidth + 0.5f, rail, rail), railMaterial, containerVisualRoot);
        CreateCube("Back Top Rail", new Vector3(0f, halfY, halfZ), new Vector3(BoardWidth + 0.5f, rail, rail), railMaterial, containerVisualRoot);
        CreateCube("Back Bottom Rail", new Vector3(0f, -halfY, halfZ), new Vector3(BoardWidth + 0.5f, rail, rail), railMaterial, containerVisualRoot);

        CreateCube("Top Left Depth Rail", new Vector3(-halfX, halfY, 0f), new Vector3(rail, rail, BoardDepth + 0.5f), railMaterial, containerVisualRoot);
        CreateCube("Top Right Depth Rail", new Vector3(halfX, halfY, 0f), new Vector3(rail, rail, BoardDepth + 0.5f), railMaterial, containerVisualRoot);
        CreateCube("Bottom Left Depth Rail", new Vector3(-halfX, -halfY, 0f), new Vector3(rail, rail, BoardDepth + 0.5f), railMaterial, containerVisualRoot);
        CreateCube("Bottom Right Depth Rail", new Vector3(halfX, -halfY, 0f), new Vector3(rail, rail, BoardDepth + 0.5f), railMaterial, containerVisualRoot);

        BuildFloorGrid(halfY);
    }

    private void BuildFloorGrid(float halfY)
    {
        float floorY = -halfY - 0.02f;
        for (int x = 0; x <= BoardWidth; x++)
        {
            float worldX = x - BoardWidth * 0.5f;
            CreateCube("Floor X Grid", new Vector3(worldX, floorY, 0f), new Vector3(0.025f, 0.035f, BoardDepth), gridMaterial, containerVisualRoot);
        }

        for (int z = 0; z <= BoardDepth; z++)
        {
            float worldZ = z - BoardDepth * 0.5f;
            CreateCube("Floor Z Grid", new Vector3(0f, floorY, worldZ), new Vector3(BoardWidth, 0.035f, 0.025f), gridMaterial, containerVisualRoot);
        }
    }

    private void BuildDeathWarning()
    {
        Vector3 position = ViewToLocal(BoardWidth / 2, BoardHeight - 1) + new Vector3(0f, 0f, -0.09f);
        deathWarningBar = CreateCube("Death Row Warning", position, new Vector3(BoardWidth, 0.92f, 0.045f), deathWarningMaterial, warningRoot);
        deathWarningBar.gameObject.SetActive(false);
    }

    private void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.075f, 0.095f);
        camera.orthographic = true;
        camera.orthographicSize = 4.9f;
        camera.fieldOfView = 42f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 200f;
        camera.transform.position = new Vector3(0f, 0f, -24f);
        camera.transform.LookAt(Vector3.zero, Vector3.up);
    }

    private void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.5f);

        Light mainLight = FindObjectOfType<Light>();
        if (mainLight == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            mainLight = lightObject.AddComponent<Light>();
        }

        mainLight.type = LightType.Directional;
        mainLight.intensity = 1.15f;
        mainLight.color = new Color(1f, 0.95f, 0.86f);
        mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Transform fillTransform = transform.Find("Tetris Fill Light");
        GameObject fillObject = fillTransform != null ? fillTransform.gameObject : new GameObject("Tetris Fill Light");
        fillObject.transform.SetParent(transform, false);
        fillObject.transform.position = new Vector3(-4f, 8f, -8f);
        Light fillLight = fillObject.GetComponent<Light>();
        if (fillLight == null)
        {
            fillLight = fillObject.AddComponent<Light>();
        }

        fillLight.type = LightType.Point;
        fillLight.range = 24f;
        fillLight.intensity = 0.8f;
        fillLight.color = new Color(0.45f, 0.68f, 1f);
    }

    private Transform CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        return child.transform;
    }

    private Transform GetOrCreateChild(Transform existing, string childName, Transform parent)
    {
        if (existing != null)
        {
            existing.SetParent(parent, false);
            return existing;
        }

        Transform found = parent.Find(childName);
        if (found != null)
        {
            return found;
        }

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private void UpdateContainerRotation()
    {
        containerYaw = Mathf.MoveTowardsAngle(containerYaw, targetContainerYaw, ContainerTurnSpeed * Time.deltaTime);
        if (containerRoot != null)
        {
            containerRoot.localRotation = Quaternion.Euler(0f, containerYaw, 0f);
        }
    }

    private void ResetGame()
    {
        ClearChildren(lockedRoot);
        ClearChildren(activeRoot);
        ClearChildren(ghostRoot);
        ClearChildren(nextRoot);
        ClearChildren(effectsRoot);
        for (int i = 0; i < clearEffects.Count; i++)
        {
            if (clearEffects[i].Material != null)
            {
                Destroy(clearEffects[i].Material);
            }
        }

        clearEffects.Clear();

        grid = new Transform[BoardWidth, BoardHeight, BoardDepth];
        activeCubes = new Transform[0];
        ghostCubes = new Transform[0];
        score = 0;
        layers = 0;
        level = 1;
        lockedPieces = 0;
        activeFace = 0;
        fallTimer = 0f;
        containerYaw = 0f;
        targetContainerYaw = 0f;
        pieceFalling = false;
        gameOver = false;
        paused = false;
        nextPieceIndex = RandomPieceIndex();

        if (containerRoot != null)
        {
            containerRoot.localRotation = Quaternion.identity;
        }

        if (deathWarningBar != null)
        {
            deathWarningBar.gameObject.SetActive(false);
        }

        SpawnPiece();
        UpdateUi();
    }

    private void SpawnPiece()
    {
        currentPieceIndex = nextPieceIndex;
        nextPieceIndex = RandomPieceIndex();
        currentCells = CopyCells(pieces[currentPieceIndex].Cells);
        currentOrigin = new Vector3Int(BoardWidth / 2, BoardHeight - 1, FrontLayer);
        pieceFalling = false;
        fallTimer = 0f;

        if (!IsValid(currentOrigin, currentCells))
        {
            gameOver = true;
            RefreshNextPreview();
            UpdateUi();
            return;
        }

        CreateActiveCubes();
        CreateGhostCubes();
        RefreshActivePiece();
        RefreshGhostPiece();
        RefreshNextPreview();
    }

    private void CreateActiveCubes()
    {
        ClearChildren(activeRoot);
        activeCubes = new Transform[currentCells.Length];
        for (int i = 0; i < currentCells.Length; i++)
        {
            activeCubes[i] = CreateCube("Active Block", Vector3.zero, Vector3.one * CubeSize, pieceMaterials[currentPieceIndex], activeRoot);
        }
    }

    private void CreateGhostCubes()
    {
        ClearChildren(ghostRoot);
        ghostCubes = new Transform[currentCells.Length];
        ghostMaterial.color = new Color(ghostPreviewColor.r, ghostPreviewColor.g, ghostPreviewColor.b, GhostAlpha);

        for (int i = 0; i < currentCells.Length; i++)
        {
            ghostCubes[i] = CreateCube("Ghost Block", Vector3.zero, Vector3.one * CubeSize, ghostMaterial, ghostRoot);
        }
    }

    private void RefreshActivePiece()
    {
        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector3Int cell = currentOrigin + currentCells[i];
            activeCubes[i].localPosition = ViewToLocal(cell.x, cell.y);
        }
    }

    private void RefreshGhostPiece()
    {
        Vector3Int ghostOrigin = currentOrigin;
        while (IsValid(ghostOrigin + Vector3Int.down, currentCells))
        {
            ghostOrigin += Vector3Int.down;
        }

        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector3Int cell = ghostOrigin + currentCells[i];
            ghostCubes[i].localPosition = ViewToLocal(cell.x, cell.y) + new Vector3(0f, 0.03f, 0f);
        }
    }

    private void RefreshNextPreview()
    {
        ClearChildren(nextRoot);

        Vector3 anchor = new Vector3(BoardWidth * 0.5f + 3f, 3.1f, 0f);
        Vector3Int[] cells = pieces[nextPieceIndex].Cells;
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 position = anchor + new Vector3(cells[i].x * 0.75f, cells[i].y * 0.75f, cells[i].z * 0.75f);
            CreateCube("Next Block", position, Vector3.one * 0.65f, nextPreviewMaterial, nextRoot);
        }
    }

    private bool TryMove(Vector3Int delta)
    {
        Vector3Int candidate = currentOrigin + delta;
        if (!IsValid(candidate, currentCells))
        {
            return false;
        }

        currentOrigin = candidate;
        RefreshActivePiece();
        RefreshGhostPiece();
        return true;
    }

    private bool TryRotate(int direction)
    {
        Vector3Int[] rotated = new Vector3Int[currentCells.Length];
        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector3Int cell = currentCells[i];
            rotated[i] = direction > 0 ? new Vector3Int(cell.y, -cell.x, 0) : new Vector3Int(-cell.y, cell.x, 0);
        }

        for (int i = 0; i < wallKicks.Length; i++)
        {
            Vector3Int candidateOrigin = currentOrigin + wallKicks[i];
            if (!IsValid(candidateOrigin, rotated))
            {
                continue;
            }

            currentCells = rotated;
            currentOrigin = candidateOrigin;
            RefreshActivePiece();
            RefreshGhostPiece();
            return true;
        }

        return false;
    }

    private bool DropOneLayer()
    {
        if (TryMove(Vector3Int.down))
        {
            return true;
        }

        LockPiece();
        return false;
    }

    private void LockPiece()
    {
        for (int i = 0; i < currentCells.Length; i++)
        {
            Vector3Int viewCell = currentOrigin + currentCells[i];
            Vector3Int cell = ViewToBoardCell(viewCell.x, viewCell.y);
            Transform block = activeCubes[i];
            block.name = "Locked Block";
            block.SetParent(lockedRoot, false);
            block.localPosition = GridToLocal(cell.x, cell.y, cell.z);
            grid[cell.x, cell.y, cell.z] = block;
        }

        activeCubes = new Transform[0];
        ClearChildren(ghostRoot);

        lockedPieces++;
        score += 10 * level;
        RecalculateLevel();
        ApplyClearedRows(ClearFullRows());

        SpawnPiece();
    }

    private void TryClearSelectedFace()
    {
        ApplyClearedRows(ClearFullRows());
    }

    private void ApplyClearedRows(int clearedRows)
    {
        if (clearedRows <= 0)
        {
            return;
        }

        int[] rowScores = { 0, 500, 1500, 3500, 7000 };
        score += rowScores[Mathf.Min(clearedRows, 4)] * level;
        layers += clearedRows;
        RecalculateLevel();
    }

    private void RecalculateLevel()
    {
        level = 1 + layers / RowsPerDifficultyLevel + lockedPieces / PiecesPerDifficultyLevel;
    }

    private int RandomPieceIndex()
    {
        int totalWeight = 0;
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].UnlockLevel <= level)
            {
                totalWeight += GetPieceWeight(pieces[i]);
            }
        }

        int target = Random.Range(0, totalWeight);
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].UnlockLevel > level)
            {
                continue;
            }

            int weight = GetPieceWeight(pieces[i]);
            if (target < weight)
            {
                return i;
            }

            target -= weight;
        }

        return 0;
    }

    private int GetPieceWeight(PieceDefinition piece)
    {
        int age = Mathf.Max(0, level - piece.UnlockLevel);
        int cells = piece.Cells.Length;

        if (cells <= 3)
        {
            return Mathf.Max(45, 95 - level * 4);
        }

        if (cells == 4)
        {
            return Mathf.Clamp(42 + age * 5, 42, 78);
        }

        if (cells == 5)
        {
            return Mathf.Clamp(8 + age * 4, 8, 26);
        }

        return Mathf.Clamp(3 + age * 2, 3, 12);
    }

    private string GetPieceComplexityLabel()
    {
        if (level >= 9)
        {
            return "Rare 6-block";
        }

        if (level >= 4)
        {
            return "Rare 5-block";
        }

        if (level >= 2)
        {
            return "Classic";
        }

        return "Simple";
    }

    private int ClearFullRows()
    {
        int cleared = 0;

        for (int y = 0; y < BoardHeight; y++)
        {
            bool full = true;
            for (int x = 0; x < BoardWidth; x++)
            {
                if (!IsProjectedOccupied(x, y))
                {
                    full = false;
                    break;
                }
            }

            if (!full)
            {
                continue;
            }

            SpawnClearEffect(y);
            ClearWorldLayer(y);
            CollapseWorldAbove(y);

            cleared++;
            y--;
        }

        return cleared;
    }

    private void UpdateDeathWarning()
    {
        if (deathWarningBar == null)
        {
            return;
        }

        bool nearDeath = !gameOver && IsNearDeath();
        deathWarningBar.gameObject.SetActive(nearDeath);
        if (!nearDeath)
        {
            return;
        }

        float alpha = Mathf.Lerp(0.09f, 0.38f, Mathf.PingPong(Time.time * 4f, 1f));
        deathWarningMaterial.color = new Color(1f, 0f, 0f, alpha);
    }

    private bool IsNearDeath()
    {
        for (int y = BoardHeight - 2; y < BoardHeight; y++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                if (IsProjectedOccupied(x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SpawnClearEffect(int y)
    {
        Material effectMaterial = new Material(clearEffectMaterial);
        Vector3 position = ViewToLocal(BoardWidth / 2, y) + new Vector3(0f, 0f, -0.12f);
        Transform effect = CreateCube("Clear Row Flash", position, new Vector3(BoardWidth, 0.92f, 0.06f), effectMaterial, effectsRoot);
        clearEffects.Add(new ClearEffect(effect, effectMaterial, ClearEffectDuration));
    }

    private void UpdateClearEffects()
    {
        for (int i = clearEffects.Count - 1; i >= 0; i--)
        {
            ClearEffect effect = clearEffects[i];
            effect.Age += Time.deltaTime;

            if (effect.Transform == null || effect.Age >= effect.Duration)
            {
                if (effect.Transform != null)
                {
                    Destroy(effect.Transform.gameObject);
                }

                if (effect.Material != null)
                {
                    Destroy(effect.Material);
                }

                clearEffects.RemoveAt(i);
                continue;
            }

            float progress = effect.Age / effect.Duration;
            float alpha = Mathf.Lerp(0.45f, 0f, progress);
            effect.Material.color = new Color(1f, Mathf.Lerp(0.22f, 0.95f, progress), 0.16f, alpha);
            effect.Transform.localScale = new Vector3(BoardWidth + progress * 0.7f, 0.92f + progress * 0.35f, 0.06f);
            clearEffects[i] = effect;
        }
    }

    private void ClearWorldLayer(int y)
    {
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int z = 0; z < BoardDepth; z++)
            {
                Transform block = grid[x, y, z];
                if (block != null)
                {
                    Destroy(block.gameObject);
                }

                grid[x, y, z] = null;
            }
        }
    }

    private void CollapseWorldAbove(int clearedY)
    {
        for (int above = clearedY + 1; above < BoardHeight; above++)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                for (int z = 0; z < BoardDepth; z++)
                {
                    Transform block = grid[x, above, z];
                    grid[x, above - 1, z] = block;
                    if (block != null)
                    {
                        block.localPosition = GridToLocal(x, above - 1, z);
                    }

                    grid[x, above, z] = null;
                }
            }
        }
    }

    private bool IsValid(Vector3Int origin, Vector3Int[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            int x = origin.x + cells[i].x;
            int y = origin.y + cells[i].y;
            int z = origin.z + cells[i].z;

            if (x < 0 || x >= BoardWidth || y < 0 || y >= BoardHeight || z != FrontLayer)
            {
                return false;
            }

            if (IsProjectedOccupied(x, y))
            {
                return false;
            }
        }

        return true;
    }

    private float GetFallInterval()
    {
        return FallInterval;
    }

    private float GetSoftDropInterval()
    {
        return SoftDropInterval;
    }

    private Vector3 GridToLocal(int x, int y, int z)
    {
        return new Vector3(x - BoardWidth * 0.5f + 0.5f, y - BoardHeight * 0.5f + 0.5f, z - BoardDepth * 0.5f + 0.5f);
    }

    private bool IsProjectedOccupied(int x, int y)
    {
        for (int depth = 0; depth < BoardDepth; depth++)
        {
            Vector3Int cell = ViewToBoardCell(x, y, depth);
            if (grid[cell.x, cell.y, cell.z] != null)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 ViewToLocal(int x, int y)
    {
        return GridToLocal(x, y, FrontLayer);
    }

    private Vector3Int ViewToBoardCell(int x, int y, int depth = 0)
    {
        switch (activeFace)
        {
            case 1:
                return new Vector3Int(BoardWidth - 1 - depth, y, x);
            case 2:
                return new Vector3Int(BoardWidth - 1 - x, y, BoardDepth - 1 - depth);
            case 3:
                return new Vector3Int(depth, y, BoardDepth - 1 - x);
            default:
                return new Vector3Int(x, y, depth);
        }
    }

    private int WrapFace(int face)
    {
        return (face % 4 + 4) % 4;
    }

    private Vector3Int[] CopyCells(Vector3Int[] source)
    {
        Vector3Int[] copy = new Vector3Int[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            copy[i] = source[i];
        }

        return copy;
    }

    private Transform CreateCube(string cubeName, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject cube = cubePrefab != null ? Instantiate(cubePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = cubeName;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = cube.GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider[] colliders = cube.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            DestroyUnityObject(colliders[i]);
        }

        return cube.transform;
    }

    private Material MakeMaterial(string materialName, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Diffuse");
        }

        Material material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        return material;
    }

    private Material MakeTransparentMaterial(string materialName, Color color, float metallic, float smoothness)
    {
        Material material = MakeMaterial(materialName, color, metallic, smoothness);

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void ClearChildrenImmediate(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            DestroyUnityObject(child);
        }
    }

    private void DestroyUnityObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private struct PieceDefinition
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly int UnlockLevel;
        public readonly Vector3Int[] Cells;

        public PieceDefinition(string name, Color color, int unlockLevel, params Vector3Int[] cells)
        {
            Name = name;
            Color = color;
            UnlockLevel = unlockLevel;
            Cells = cells;
        }
    }

    private struct ClearEffect
    {
        public readonly Transform Transform;
        public readonly Material Material;
        public readonly float Duration;
        public float Age;

        public ClearEffect(Transform transform, Material material, float duration)
        {
            Transform = transform;
            Material = material;
            Duration = duration;
            Age = 0f;
        }
    }
}
