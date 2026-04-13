using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class ThreeDTetrisGame : MonoBehaviour
{
    private const int FrontLayer = 0;
    private static readonly string[] FourPointLightNames =
    {
        "Tetris Front Left Light",
        "Tetris Front Right Light",
        "Tetris Back Left Light",
        "Tetris Back Right Light"
    };

    [Header("Board")]
    [SerializeField, Min(3)] private int boardSide = 5;
    [SerializeField, Min(4)] private int boardHeight = 7;

    [Header("Gameplay")]
    [SerializeField, Min(0.1f)] private float cubeSize = 0.88f;
    [SerializeField, Min(0.05f)] private float fallInterval = 0.55f;
    [SerializeField, Min(0.01f)] private float softDropInterval = 0.055f;

    [Header("Scoring")]
    [SerializeField, Min(0)] private int pointsPerPlacedCube = 1;
    [SerializeField, Min(0)] private int pointsPerClearedCube = 1;

    [Header("Difficulty")]
    [SerializeField, Min(1)] private int rowsPerDifficultyLevel = 4;
    [SerializeField, Min(1)] private int piecesPerDifficultyLevel = 12;
    [SerializeField, Range(0, 40)] private int oneBlockPieceWeight = 5;
    [SerializeField, Range(0, 40)] private int twoBlockPieceWeight = 8;

    [Header("Lighting")]
    [SerializeField] private Color ambientLightColor = new Color(0.48f, 0.5f, 0.55f);
    [SerializeField, Min(0f)] private float fourPointLightIntensity = 0.72f;
    [SerializeField, Min(0.1f)] private float fourPointLightRange = 18f;
    [SerializeField, Min(0f)] private float fourPointLightHeight = 5.6f;
    [SerializeField, Min(0f)] private float fourPointLightDistance = 5.8f;
    [SerializeField] private Color frontLeftLightColor = new Color(1f, 0.92f, 0.82f);
    [SerializeField] private Color frontRightLightColor = new Color(0.74f, 0.86f, 1f);
    [SerializeField] private Color backLeftLightColor = new Color(0.72f, 1f, 0.88f);
    [SerializeField] private Color backRightLightColor = new Color(1f, 0.78f, 0.9f);

    [Header("View Rotation")]
    [SerializeField, InspectorName("View Turn Angle")] private float containerTurnAngle = 90f;
    [SerializeField, Min(1f), InspectorName("View Turn Speed")] private float containerTurnSpeed = 280f;

    [Header("Camera View")]
    [SerializeField, Min(1f)] private float cameraDistance = 24f;
    [SerializeField, Min(0f)] private float cameraHeight = 0f;
    [SerializeField, Min(0f)] private float cameraLookAtHeight = 0f;
    [SerializeField, Min(1f)] private float cameraOrthographicSize = 4.9f;

    [Header("F Preview")]
    [SerializeField] private float previewCameraYawOffset = 45f;
    [SerializeField, Min(0f)] private float previewCameraHeight = 8f;
    [SerializeField, Min(0f)] private float previewLookAtLift = 1.35f;
    [SerializeField, Min(0.1f)] private float previewCameraSpeed = 7f;
    [SerializeField, Min(1f)] private float previewCameraOrthographicSize = 5.8f;
    [SerializeField, Min(0.01f), InspectorName("Preview Projection Column Tolerance")] private float previewClearColumnTolerance = 0.08f;

    [Header("Next Preview")]
    [SerializeField] private Vector2 nextPreviewViewportPosition = new Vector2(0.82f, 0.68f);
    [SerializeField, Min(0.1f)] private float nextPreviewCameraDepth = 12f;
    [SerializeField, Min(0.1f)] private float nextPreviewCellSpacing = 0.75f;
    [SerializeField, Min(0.1f)] private float nextPreviewCubeSize = 0.65f;

    [Header("Preview And Effects")]
    [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.28f;
    [SerializeField] private Color ghostPreviewColor = new Color(0.9f, 0.92f, 0.94f);
    [SerializeField] private Color nextPreviewColor = new Color(0.82f, 0.84f, 0.86f);
    [SerializeField, Min(0.05f)] private float clearEffectDuration = 3f;
    [SerializeField, Min(1)] private int clearFragmentsPerCube = 5;
    [SerializeField, Min(0.05f)] private float clearFragmentSize = 0.32f;
    [SerializeField, Min(0.1f)] private float clearFragmentBlastSpeed = 3.8f;
    [SerializeField, Min(0f)] private float clearFragmentGravity = 0.95f;
    [SerializeField, Min(0f)] private float clearFragmentDrag = 0.32f;
    [SerializeField] private Color clearFallbackFragmentColor = new Color(1f, 0.92f, 0.2f, 1f);

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
    private int PointsPerPlacedCube => Mathf.Max(0, pointsPerPlacedCube);
    private int PointsPerClearedCube => Mathf.Max(0, pointsPerClearedCube);
    private int RowsPerDifficultyLevel => Mathf.Max(1, rowsPerDifficultyLevel);
    private int PiecesPerDifficultyLevel => Mathf.Max(1, piecesPerDifficultyLevel);
    private int OneBlockPieceWeight => Mathf.Max(0, oneBlockPieceWeight);
    private int TwoBlockPieceWeight => Mathf.Max(0, twoBlockPieceWeight);
    private float FourPointLightIntensity => Mathf.Max(0f, fourPointLightIntensity);
    private float FourPointLightRange => Mathf.Max(0.1f, fourPointLightRange);
    private float FourPointLightHeight => Mathf.Max(0f, fourPointLightHeight);
    private float FourPointLightDistance => Mathf.Max(0f, fourPointLightDistance);
    private float ContainerTurnAngle => containerTurnAngle;
    private float ContainerTurnSpeed => Mathf.Max(1f, containerTurnSpeed);
    private float CameraDistance => Mathf.Max(1f, cameraDistance);
    private float CameraHeight => Mathf.Max(0f, cameraHeight);
    private float CameraLookAtHeight => Mathf.Max(0f, cameraLookAtHeight);
    private float CameraOrthographicSize => Mathf.Max(1f, cameraOrthographicSize);
    private float PreviewCameraHeight => Mathf.Max(0f, previewCameraHeight);
    private float PreviewLookAtLift => Mathf.Max(0f, previewLookAtLift);
    private float PreviewCameraSpeed => Mathf.Max(0.1f, previewCameraSpeed);
    private float PreviewCameraOrthographicSize => Mathf.Max(1f, previewCameraOrthographicSize);
    private float PreviewClearColumnTolerance => Mathf.Max(0.01f, previewClearColumnTolerance);
    private float NextPreviewCameraDepth => Mathf.Max(0.1f, nextPreviewCameraDepth);
    private float NextPreviewCellSpacing => Mathf.Max(0.1f, nextPreviewCellSpacing);
    private float NextPreviewCubeSize => Mathf.Max(0.1f, nextPreviewCubeSize);
    private float GhostAlpha => Mathf.Clamp01(ghostAlpha);
    private float ClearEffectDuration => Mathf.Max(1.2f, clearEffectDuration);
    private int ClearFragmentsPerCube => Mathf.Max(1, clearFragmentsPerCube);
    private float ClearFragmentSize => Mathf.Max(0.05f, clearFragmentSize);
    private float ClearFragmentBlastSpeed => Mathf.Max(0.1f, clearFragmentBlastSpeed);
    private float ClearFragmentGravity => Mathf.Max(0f, clearFragmentGravity);
    private float ClearFragmentDrag => Mathf.Max(0f, clearFragmentDrag);

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
    private readonly List<ClearFragmentEffect> clearEffects = new List<ClearFragmentEffect>();

    private Vector3Int currentOrigin;
    private Vector3Int[] currentCells;
    private Camera gameplayCamera;
    private int currentPieceIndex;
    private int nextPieceIndex;
    private int score;
    private int layers;
    private int level;
    private int lockedPieces;
    private int activeFace;
    private float fallTimer;
    private float viewYaw;
    private float targetViewYaw;
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
        pointsPerPlacedCube = Mathf.Max(0, pointsPerPlacedCube);
        pointsPerClearedCube = Mathf.Max(0, pointsPerClearedCube);
        rowsPerDifficultyLevel = Mathf.Max(1, rowsPerDifficultyLevel);
        piecesPerDifficultyLevel = Mathf.Max(1, piecesPerDifficultyLevel);
        oneBlockPieceWeight = Mathf.Max(0, oneBlockPieceWeight);
        twoBlockPieceWeight = Mathf.Max(0, twoBlockPieceWeight);
        fourPointLightIntensity = Mathf.Max(0f, fourPointLightIntensity);
        fourPointLightRange = Mathf.Max(0.1f, fourPointLightRange);
        fourPointLightHeight = Mathf.Max(0f, fourPointLightHeight);
        fourPointLightDistance = Mathf.Max(0f, fourPointLightDistance);
        containerTurnSpeed = Mathf.Max(1f, containerTurnSpeed);
        cameraDistance = Mathf.Max(1f, cameraDistance);
        cameraHeight = Mathf.Max(0f, cameraHeight);
        cameraLookAtHeight = Mathf.Max(0f, cameraLookAtHeight);
        cameraOrthographicSize = Mathf.Max(1f, cameraOrthographicSize);
        previewCameraHeight = Mathf.Max(0f, previewCameraHeight);
        previewLookAtLift = Mathf.Max(0f, previewLookAtLift);
        previewCameraSpeed = Mathf.Max(0.1f, previewCameraSpeed);
        previewCameraOrthographicSize = Mathf.Max(1f, previewCameraOrthographicSize);
        previewClearColumnTolerance = Mathf.Max(0.01f, previewClearColumnTolerance);
        nextPreviewViewportPosition.x = Mathf.Clamp01(nextPreviewViewportPosition.x);
        nextPreviewViewportPosition.y = Mathf.Clamp01(nextPreviewViewportPosition.y);
        nextPreviewCameraDepth = Mathf.Max(0.1f, nextPreviewCameraDepth);
        nextPreviewCellSpacing = Mathf.Max(0.1f, nextPreviewCellSpacing);
        nextPreviewCubeSize = Mathf.Max(0.1f, nextPreviewCubeSize);
        ghostAlpha = Mathf.Clamp01(ghostAlpha);
        clearEffectDuration = Mathf.Max(0.05f, clearEffectDuration);
        clearFragmentsPerCube = Mathf.Max(1, clearFragmentsPerCube);
        clearFragmentSize = Mathf.Max(0.05f, clearFragmentSize);
        clearFragmentBlastSpeed = Mathf.Max(0.1f, clearFragmentBlastSpeed);
        clearFragmentGravity = Mathf.Max(0f, clearFragmentGravity);
        clearFragmentDrag = Mathf.Max(0f, clearFragmentDrag);

        if (!Application.isPlaying)
        {
            ApplyCameraTransform(0f, false, true);
            ConfigureNextPreviewRoot();
        }
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

    [ContextMenu("Apply Editable Lighting")]
    public void ApplyEditableLighting()
    {
        SetupLighting();
    }

    private void Update()
    {
        UpdateViewRotation();
        UpdatePreviewCamera();
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

        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyUp(KeyCode.F))
        {
            TryClearSelectedFace();
            RefreshGhostPiece();
            UpdateDeathWarning();
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
            targetViewYaw += ContainerTurnAngle;
            activeFace = WrapFace(activeFace - 1);
            TryClearSelectedFace();
            RefreshActivePiece();
            RefreshGhostPiece();
            RefreshDeathWarningVisual();
            UpdateDeathWarning();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            targetViewYaw -= ContainerTurnAngle;
            activeFace = WrapFace(activeFace + 1);
            TryClearSelectedFace();
            RefreshActivePiece();
            RefreshGhostPiece();
            RefreshDeathWarningVisual();
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
            new PieceDefinition("Dot1", new Color(0.92f, 0.98f, 1f), 1, new Vector3Int(0, 0, 0)),
            new PieceDefinition("I2", new Color(0.46f, 1f, 0.92f), 1, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0)),
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

        if (clearEffectMaterial == null || IsOldParticleMaterial(clearEffectMaterial))
        {
            clearEffectMaterial = MakeTransparentMaterial("Clear Block Fragment", Color.white, 0.02f, 0.45f);
        }
        else
        {
            ConfigureTransparentMaterial(clearEffectMaterial, Color.white);
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
        ConfigureNextPreviewRoot();

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
        deathWarningBar = CreateCube("Death Row Warning", Vector3.zero, Vector3.one, deathWarningMaterial, warningRoot);
        RefreshDeathWarningVisual();
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
        camera.orthographicSize = CameraOrthographicSize;
        camera.fieldOfView = 42f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 200f;

        gameplayCamera = camera;
        ApplyCameraTransform(0f, false, true);
    }

    private void ApplyCameraTransform(float yaw, bool preview, bool instant)
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            return;
        }

        float height = preview ? PreviewCameraHeight : CameraHeight;
        float lookAtHeight = preview ? PreviewLookAtLift : CameraLookAtHeight;
        float orthographicSize = preview ? PreviewCameraOrthographicSize : CameraOrthographicSize;
        Vector3 targetPosition = GetCameraPosition(yaw, height);
        Quaternion targetRotation = GetCameraRotation(targetPosition, lookAtHeight);

        if (instant)
        {
            gameplayCamera.transform.position = targetPosition;
            gameplayCamera.transform.rotation = targetRotation;
            gameplayCamera.orthographicSize = orthographicSize;
            return;
        }

        float lerp = 1f - Mathf.Exp(-PreviewCameraSpeed * Time.deltaTime);
        gameplayCamera.transform.position = Vector3.Lerp(gameplayCamera.transform.position, targetPosition, lerp);
        gameplayCamera.transform.rotation = Quaternion.Slerp(gameplayCamera.transform.rotation, targetRotation, lerp);
        gameplayCamera.orthographicSize = Mathf.Lerp(gameplayCamera.orthographicSize, orthographicSize, lerp);
    }

    private Vector3 GetCameraPosition(float yaw, float height)
    {
        return Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, height, -CameraDistance);
    }

    private Quaternion GetCameraRotation(Vector3 position, float lookAtHeight)
    {
        Vector3 forward = Vector3.up * lookAtHeight - position;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward, Vector3.up);
    }

    private void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientLightColor;
        RenderSettings.sun = null;

        DisableLegacyLights();

        float distance = FourPointLightDistance;
        float height = FourPointLightHeight;
        ConfigurePointLight(FourPointLightNames[0], new Vector3(-distance, height, -distance), frontLeftLightColor);
        ConfigurePointLight(FourPointLightNames[1], new Vector3(distance, height, -distance), frontRightLightColor);
        ConfigurePointLight(FourPointLightNames[2], new Vector3(-distance, height, distance), backLeftLightColor);
        ConfigurePointLight(FourPointLightNames[3], new Vector3(distance, height, distance), backRightLightColor);
    }

    private void DisableLegacyLights()
    {
        Light[] lights = FindObjectsOfType<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
            {
                continue;
            }

            if (light.type == LightType.Directional || light.name == "Tetris Fill Light")
            {
                light.enabled = false;
            }
        }
    }

    private void ConfigurePointLight(string lightName, Vector3 localPosition, Color color)
    {
        Light pointLight = GetOrCreatePointLight(lightName);
        pointLight.enabled = true;
        pointLight.type = LightType.Point;
        pointLight.color = color;
        pointLight.intensity = FourPointLightIntensity;
        pointLight.range = FourPointLightRange;
        pointLight.shadows = LightShadows.None;
        pointLight.renderMode = LightRenderMode.Auto;
        pointLight.bounceIntensity = 0.35f;

        Transform lightTransform = pointLight.transform;
        lightTransform.SetParent(transform, false);
        lightTransform.localPosition = localPosition;
        lightTransform.localRotation = Quaternion.identity;
        lightTransform.localScale = Vector3.one;
    }

    private Light GetOrCreatePointLight(string lightName)
    {
        Transform foundTransform = transform.Find(lightName);
        if (foundTransform == null)
        {
            GameObject foundObject = GameObject.Find(lightName);
            if (foundObject != null)
            {
                foundTransform = foundObject.transform;
            }
        }

        GameObject lightObject = foundTransform != null ? foundTransform.gameObject : new GameObject(lightName);
        lightObject.name = lightName;
        lightObject.SetActive(true);

        Light pointLight = lightObject.GetComponent<Light>();
        if (pointLight == null)
        {
            pointLight = lightObject.AddComponent<Light>();
        }

        return pointLight;
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

    private void UpdateViewRotation()
    {
        viewYaw = Mathf.MoveTowardsAngle(viewYaw, targetViewYaw, ContainerTurnSpeed * Time.deltaTime);
        if (containerRoot != null)
        {
            containerRoot.localRotation = Quaternion.identity;
        }
    }

    private void UpdatePreviewCamera()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            return;
        }

        bool preview = IsPreviewHeld();
        float yaw = viewYaw + (preview ? previewCameraYawOffset : 0f);
        ApplyCameraTransform(yaw, preview, false);
        ConfigureNextPreviewRoot();
    }

    private bool IsPreviewHeld()
    {
        return Input.GetKey(KeyCode.F);
    }

    private void ConfigureNextPreviewRoot()
    {
        if (nextRoot == null)
        {
            return;
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null)
        {
            return;
        }

        nextRoot.SetParent(gameplayCamera.transform, false);
        nextRoot.localRotation = Quaternion.identity;

        float orthographicSize = Mathf.Max(1f, gameplayCamera.orthographicSize);
        float aspect = gameplayCamera.aspect > 0f ? gameplayCamera.aspect : 16f / 9f;
        float localX = (Mathf.Clamp01(nextPreviewViewportPosition.x) - 0.5f) * 2f * orthographicSize * aspect;
        float localY = (Mathf.Clamp01(nextPreviewViewportPosition.y) - 0.5f) * 2f * orthographicSize;
        nextRoot.localPosition = new Vector3(localX, localY, NextPreviewCameraDepth);

        float scale = orthographicSize / CameraOrthographicSize;
        nextRoot.localScale = Vector3.one * scale;
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
        viewYaw = 0f;
        targetViewYaw = 0f;
        pieceFalling = false;
        gameOver = false;
        paused = false;
        nextPieceIndex = RandomPieceIndex();

        ApplyCameraTransform(0f, false, true);

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
        if (nextRoot == null)
        {
            return;
        }

        ConfigureNextPreviewRoot();
        ClearChildren(nextRoot);

        Vector3Int[] cells = pieces[nextPieceIndex].Cells;
        Vector3 anchor = GetNextPreviewAnchor(cells);
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 position = anchor + new Vector3(
                cells[i].x * NextPreviewCellSpacing,
                cells[i].y * NextPreviewCellSpacing,
                cells[i].z * NextPreviewCellSpacing);
            CreateCube("Next Block", position, Vector3.one * NextPreviewCubeSize, nextPreviewMaterial, nextRoot);
        }
    }

    private Vector3 GetNextPreviewAnchor(Vector3Int[] cells)
    {
        if (cells == null || cells.Length == 0)
        {
            return Vector3.zero;
        }

        Vector2 min = new Vector2(cells[0].x, cells[0].y);
        Vector2 max = min;
        for (int i = 1; i < cells.Length; i++)
        {
            min.x = Mathf.Min(min.x, cells[i].x);
            min.y = Mathf.Min(min.y, cells[i].y);
            max.x = Mathf.Max(max.x, cells[i].x);
            max.y = Mathf.Max(max.y, cells[i].y);
        }

        Vector2 center = (min + max) * 0.5f;
        return new Vector3(-center.x * NextPreviewCellSpacing, -center.y * NextPreviewCellSpacing, 0f);
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
        score += currentCells.Length * PointsPerPlacedCube;
        RecalculateLevel();
        ApplyClearedRows(ClearFullRows());

        SpawnPiece();
    }

    private void TryClearSelectedFace()
    {
        ApplyClearedRows(ClearFullRows());
    }

    private void ApplyClearedRows(ClearResult clearResult)
    {
        if (clearResult.Rows <= 0)
        {
            return;
        }

        score += clearResult.Blocks * PointsPerClearedCube;
        layers += clearResult.Rows;
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

        if (cells == 1)
        {
            return OneBlockPieceWeight;
        }

        if (cells == 2)
        {
            return TwoBlockPieceWeight;
        }

        if (cells == 3)
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

    private ClearResult ClearFullRows()
    {
        if (IsPreviewHeld())
        {
            return ClearPreviewThreeDLines();
        }

        return ClearSelectedFaceRows();
    }

    private ClearResult ClearSelectedFaceRows()
    {
        ClearResult clearResult = new ClearResult();

        for (int y = 0; y < BoardHeight; y++)
        {
            if (!IsSelectedFaceRowFull(y))
            {
                continue;
            }

            List<ClearBlockFragment> clearedBlocksForEffects = new List<ClearBlockFragment>();
            int clearedBlocks = ClearWorldLayer(y, clearedBlocksForEffects);
            SpawnClearEffect(clearedBlocksForEffects);
            clearResult.Blocks += clearedBlocks;
            CollapseWorldAbove(y);

            clearResult.Rows++;
            y--;
        }

        return clearResult;
    }

    private ClearResult ClearPreviewThreeDLines()
    {
        ClearResult clearResult = new ClearResult();
        HashSet<Vector3Int> cellsToClear = new HashSet<Vector3Int>();

        for (int y = 0; y < BoardHeight; y++)
        {
            clearResult.Rows += CollectPreviewLineCells(y, new Vector2Int(1, 0), cellsToClear);
            clearResult.Rows += CollectPreviewLineCells(y, new Vector2Int(0, 1), cellsToClear);
            clearResult.Rows += CollectPreviewLineCells(y, new Vector2Int(1, 1), cellsToClear);
            clearResult.Rows += CollectPreviewLineCells(y, new Vector2Int(1, -1), cellsToClear);
        }

        if (cellsToClear.Count == 0)
        {
            clearResult.Rows = 0;
            return clearResult;
        }

        List<Vector3Int> clearedCells = new List<Vector3Int>(cellsToClear);
        List<ClearBlockFragment> clearedBlocksForEffects = new List<ClearBlockFragment>();
        clearResult.Blocks = ClearWorldCells(clearedCells, clearedBlocksForEffects);
        SpawnClearEffect(clearedBlocksForEffects);
        CollapseColumnsAboveClearedCells(clearedCells);

        if (clearResult.Blocks <= 0)
        {
            clearResult.Rows = 0;
        }

        return clearResult;
    }

    private int CollectPreviewLineCells(int y, Vector2Int direction, HashSet<Vector3Int> cellsToClear)
    {
        int lines = 0;
        int lineLength = BoardWidth;

        for (int x = 0; x < BoardWidth; x++)
        {
            for (int z = 0; z < BoardDepth; z++)
            {
                int endX = x + direction.x * (lineLength - 1);
                int endZ = z + direction.y * (lineLength - 1);
                if (!IsInsideBoardXZ(endX, endZ) || IsInsideBoardXZ(x - direction.x, z - direction.y))
                {
                    continue;
                }

                if (!IsPreviewLineFull(x, z, y, direction, lineLength))
                {
                    continue;
                }

                for (int step = 0; step < lineLength; step++)
                {
                    cellsToClear.Add(new Vector3Int(x + direction.x * step, y, z + direction.y * step));
                }

                lines++;
            }
        }

        return lines;
    }

    private bool IsPreviewLineFull(int startX, int startZ, int y, Vector2Int direction, int lineLength)
    {
        for (int step = 0; step < lineLength; step++)
        {
            int x = startX + direction.x * step;
            int z = startZ + direction.y * step;
            if (!IsInsideBoardXZ(x, z) || grid[x, y, z] == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsInsideBoardXZ(int x, int z)
    {
        return x >= 0 && x < BoardWidth && z >= 0 && z < BoardDepth;
    }

    private bool IsSelectedFaceRowFull(int y)
    {
        for (int x = 0; x < BoardWidth; x++)
        {
            if (!IsProjectedOccupied(x, y))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPreviewProjectedRowFull(int y)
    {
        List<float> columns = GetPreviewProjectionColumns();
        if (columns.Count == 0)
        {
            return false;
        }

        bool[] occupiedColumns = new bool[columns.Count];
        Vector3 right = GetPreviewProjectionRight();
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int z = 0; z < BoardDepth; z++)
            {
                if (grid[x, y, z] == null)
                {
                    continue;
                }

                int column = FindPreviewProjectionColumn(columns, Vector3.Dot(GridToLocal(x, y, z), right));
                if (column >= 0)
                {
                    occupiedColumns[column] = true;
                }
            }
        }

        for (int i = 0; i < occupiedColumns.Length; i++)
        {
            if (!occupiedColumns[i])
            {
                return false;
            }
        }

        return true;
    }

    private List<float> GetPreviewProjectionColumns()
    {
        List<float> columns = new List<float>();
        Vector3 right = GetPreviewProjectionRight();
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int z = 0; z < BoardDepth; z++)
            {
                AddPreviewProjectionColumn(columns, Vector3.Dot(GridToLocal(x, 0, z), right));
            }
        }

        columns.Sort();
        return columns;
    }

    private Vector3 GetPreviewProjectionRight()
    {
        float yaw = targetViewYaw + previewCameraYawOffset;
        Vector3 position = GetCameraPosition(yaw, PreviewCameraHeight);
        return GetCameraRotation(position, PreviewLookAtLift) * Vector3.right;
    }

    private void AddPreviewProjectionColumn(List<float> columns, float projection)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            if (Mathf.Abs(columns[i] - projection) <= PreviewClearColumnTolerance)
            {
                return;
            }
        }

        columns.Add(projection);
    }

    private int FindPreviewProjectionColumn(List<float> columns, float projection)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            if (Mathf.Abs(columns[i] - projection) <= PreviewClearColumnTolerance)
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateDeathWarning()
    {
        if (deathWarningBar == null)
        {
            return;
        }

        RefreshDeathWarningVisual();
        bool nearDeath = !gameOver && IsNearDeath();
        deathWarningBar.gameObject.SetActive(nearDeath);
        if (!nearDeath)
        {
            return;
        }

        float alpha = Mathf.Lerp(0.09f, 0.38f, Mathf.PingPong(Time.time * 4f, 1f));
        deathWarningMaterial.color = new Color(1f, 0f, 0f, alpha);
    }

    private void RefreshDeathWarningVisual()
    {
        if (deathWarningBar == null)
        {
            return;
        }

        deathWarningBar.localPosition = ViewToLocal(BoardWidth / 2, BoardHeight - 1) + GetFaceOutwardNormal(activeFace) * 0.09f;
        deathWarningBar.localScale = IsSideFace(activeFace)
            ? new Vector3(0.045f, 0.92f, BoardWidth)
            : new Vector3(BoardWidth, 0.92f, 0.045f);
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

    private void SpawnClearEffect(List<ClearBlockFragment> clearedBlocks)
    {
        for (int i = 0; i < clearedBlocks.Count; i++)
        {
            SpawnBlockBreakEffect(clearedBlocks[i]);
        }
    }

    private void SpawnBlockBreakEffect(ClearBlockFragment block)
    {
        for (int i = 0; i < ClearFragmentsPerCube; i++)
        {
            SpawnFragment(block);
        }
    }

    private void SpawnFragment(ClearBlockFragment block)
    {
        Color fragmentColor = MakeFragmentColor(block.Color, Random.Range(0f, 0.18f), 1f);
        Material fragmentMaterial = MakeClearFragmentMaterial(fragmentColor);
        Vector3 fragmentScale = RandomFragmentScale();
        Transform fragment = CreateCube("Block Fragment", Vector3.zero, fragmentScale, fragmentMaterial, effectsRoot);
        fragment.position = block.Position + Random.insideUnitSphere * CubeSize * 0.16f;
        fragment.rotation = Random.rotationUniform;

        Vector3 direction = Random.onUnitSphere;
        direction.y = Mathf.Abs(direction.y) * 0.45f + Random.Range(0.08f, 0.42f);
        direction.Normalize();

        Vector3 velocity = direction * Random.Range(ClearFragmentBlastSpeed * 0.65f, ClearFragmentBlastSpeed * 1.12f);
        Vector3 angularVelocity = new Vector3(
            Random.Range(-280f, 280f),
            Random.Range(-360f, 360f),
            Random.Range(-300f, 300f));

        clearEffects.Add(new ClearFragmentEffect(
            fragment,
            fragmentMaterial,
            velocity,
            angularVelocity,
            Random.Range(ClearEffectDuration * 0.82f, ClearEffectDuration * 1.18f),
            fragmentColor,
            fragmentScale));
    }

    private void UpdateClearEffects()
    {
        float deltaTime = Time.deltaTime;
        for (int i = clearEffects.Count - 1; i >= 0; i--)
        {
            ClearFragmentEffect effect = clearEffects[i];
            effect.Age += deltaTime;

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

            effect.Velocity += Vector3.down * ClearFragmentGravity * deltaTime;
            effect.Velocity *= Mathf.Exp(-ClearFragmentDrag * deltaTime);
            effect.Transform.position += effect.Velocity * deltaTime;
            effect.Transform.Rotate(effect.AngularVelocity * deltaTime, Space.Self);

            float progress = Mathf.Clamp01(effect.Age / effect.Duration);
            if (effect.Material != null)
            {
                Color color = effect.Color;
                color.a = progress < 0.72f ? 1f : Mathf.InverseLerp(1f, 0.72f, progress);
                effect.Material.color = color;
            }

            if (progress > 0.82f)
            {
                float shrink = Mathf.Lerp(1f, 0.72f, Mathf.InverseLerp(0.82f, 1f, progress));
                effect.Transform.localScale = effect.StartScale * shrink;
            }

            clearEffects[i] = effect;
        }
    }

    private Vector3 RandomFragmentScale()
    {
        float baseSize = ClearFragmentSize;
        return new Vector3(
            Random.Range(baseSize * 0.72f, baseSize * 1.18f),
            Random.Range(baseSize * 0.62f, baseSize * 1.05f),
            Random.Range(baseSize * 0.7f, baseSize * 1.16f));
    }

    private int ClearWorldCells(List<Vector3Int> cells, List<ClearBlockFragment> clearedBlocksForEffects)
    {
        int clearedBlocks = 0;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int cell = cells[i];
            if (cell.x < 0 || cell.x >= BoardWidth || cell.y < 0 || cell.y >= BoardHeight || cell.z < 0 || cell.z >= BoardDepth)
            {
                continue;
            }

            Transform block = grid[cell.x, cell.y, cell.z];
            if (block == null)
            {
                continue;
            }

            clearedBlocksForEffects.Add(new ClearBlockFragment(block.position, GetBlockColor(block)));
            Destroy(block.gameObject);
            grid[cell.x, cell.y, cell.z] = null;
            clearedBlocks++;
        }

        return clearedBlocks;
    }

    private int ClearWorldLayer(int y, List<ClearBlockFragment> clearedBlocksForEffects)
    {
        int clearedBlocks = 0;

        for (int x = 0; x < BoardWidth; x++)
        {
            for (int z = 0; z < BoardDepth; z++)
            {
                Transform block = grid[x, y, z];
                if (block != null)
                {
                    clearedBlocksForEffects.Add(new ClearBlockFragment(block.position, GetBlockColor(block)));
                    Destroy(block.gameObject);
                    clearedBlocks++;
                }

                grid[x, y, z] = null;
            }
        }

        return clearedBlocks;
    }

    private Color GetBlockColor(Transform block)
    {
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = block.GetComponentInChildren<Renderer>();
        }

        if (renderer == null || renderer.sharedMaterial == null)
        {
            return clearFallbackFragmentColor;
        }

        Material material = renderer.sharedMaterial;
        Color blockColor = clearFallbackFragmentColor;
        if (material.HasProperty("_Color"))
        {
            blockColor = material.GetColor("_Color");
        }
        else if (material.HasProperty("_BaseColor"))
        {
            blockColor = material.GetColor("_BaseColor");
        }

        blockColor.a = 1f;
        return blockColor;
    }

    private Color MakeFragmentColor(Color blockColor, float whiteMix, float alpha)
    {
        Color color = whiteMix >= 0f
            ? Color.Lerp(blockColor, Color.white, whiteMix)
            : Color.Lerp(blockColor, Color.black, -whiteMix);
        color.a = alpha;
        return color;
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

    private void CollapseColumnsAboveClearedCells(List<Vector3Int> clearedCells)
    {
        Dictionary<Vector2Int, List<int>> clearedYByColumn = new Dictionary<Vector2Int, List<int>>();
        for (int i = 0; i < clearedCells.Count; i++)
        {
            Vector3Int cell = clearedCells[i];
            Vector2Int column = new Vector2Int(cell.x, cell.z);
            if (!clearedYByColumn.TryGetValue(column, out List<int> clearedYs))
            {
                clearedYs = new List<int>();
                clearedYByColumn.Add(column, clearedYs);
            }

            clearedYs.Add(cell.y);
        }

        foreach (KeyValuePair<Vector2Int, List<int>> column in clearedYByColumn)
        {
            List<int> clearedYs = column.Value;
            clearedYs.Sort();
            for (int i = 0; i < clearedYs.Count; i++)
            {
                int adjustedY = clearedYs[i] - i;
                CollapseColumnAbove(column.Key.x, column.Key.y, adjustedY);
            }
        }
    }

    private void CollapseColumnAbove(int x, int z, int clearedY)
    {
        for (int above = clearedY + 1; above < BoardHeight; above++)
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

            if (IsBuildBlocked(x, y))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBuildBlocked(int x, int y)
    {
        return IsPreviewHeld() ? IsExactBoardCellOccupied(x, y) : IsProjectedOccupied(x, y);
    }

    private bool IsExactBoardCellOccupied(int viewX, int y)
    {
        Vector3Int boardCell = ViewToBoardCell(viewX, y, FrontLayer);
        return grid[boardCell.x, boardCell.y, boardCell.z] != null;
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
        Vector3Int cell = ViewToBoardCell(x, y, FrontLayer);
        return GridToLocal(cell.x, cell.y, cell.z);
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

    private bool IsSideFace(int face)
    {
        int wrappedFace = WrapFace(face);
        return wrappedFace == 1 || wrappedFace == 3;
    }

    private Vector3 GetFaceOutwardNormal(int face)
    {
        switch (WrapFace(face))
        {
            case 1:
                return Vector3.right;
            case 2:
                return Vector3.forward;
            case 3:
                return Vector3.left;
            default:
                return Vector3.back;
        }
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
        ConfigureTransparentMaterial(material, color);
        return material;
    }

    private Material MakeClearFragmentMaterial(Color color)
    {
        Material material = clearEffectMaterial != null
            ? new Material(clearEffectMaterial)
            : MakeTransparentMaterial("Clear Block Fragment", color, 0.02f, 0.45f);
        material.name = "Clear Block Fragment Instance";
        ConfigureTransparentMaterial(material, color);
        return material;
    }

    private bool IsOldParticleMaterial(Material material)
    {
        return material != null && material.shader != null && material.shader.name.Contains("Particle");
    }

    private void ConfigureTransparentMaterial(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

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

    private struct ClearBlockFragment
    {
        public readonly Vector3 Position;
        public readonly Color Color;

        public ClearBlockFragment(Vector3 position, Color color)
        {
            Position = position;
            Color = color;
        }
    }

    private struct ClearFragmentEffect
    {
        public readonly Transform Transform;
        public readonly Material Material;
        public readonly float Duration;
        public readonly Color Color;
        public readonly Vector3 StartScale;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public float Age;

        public ClearFragmentEffect(Transform transform, Material material, Vector3 velocity, Vector3 angularVelocity, float duration, Color color, Vector3 startScale)
        {
            Transform = transform;
            Material = material;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            Duration = duration;
            Color = color;
            StartScale = startScale;
            Age = 0f;
        }
    }

    private struct ClearResult
    {
        public int Rows;
        public int Blocks;
    }
}
