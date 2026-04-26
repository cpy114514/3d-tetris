using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ThreeDTetrisGame : MonoBehaviour
{
    private const int FrontLayer = 0;
    private const float DefaultColorGrayMix = 0.34f;
    private const string GameplayDifficultyKey = "GameplayDifficulty";
    private static readonly string[] GameplayDifficultyNames =
    {
        "EASY",
        "NORMAL",
        "HARD"
    };
    private static readonly string[] LegacyGameplayMenuObjectNames =
    {
        "Tutorial Button",
        "Difficulty Selection Panel",
        "Tutorial Prompt Panel",
        "Tutorial Panel",
        "back"
    };

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
    [SerializeField, Range(0, 2)] private int gameplayDifficulty = 1;
    [SerializeField, Min(1)] private int rowsPerDifficultyLevel = 3;
    [SerializeField, Min(1)] private int piecesPerDifficultyLevel = 5;
    [SerializeField, Range(0, 40)] private int oneBlockPieceWeight = 1;
    [SerializeField, Range(0, 40)] private int twoBlockPieceWeight = 2;

    [Header("Lighting")]
    [SerializeField] private Color ambientLightColor = new Color(0.48f, 0.5f, 0.55f);
    [SerializeField, Min(0f)] private float fourPointLightIntensity = 0.72f;
    [SerializeField, Min(0.1f)] private float fourPointLightRange = 18f;
    [SerializeField, Min(0f)] private float fourPointLightHeight = 5.6f;
    [SerializeField, Min(0f)] private float fourPointLightDistance = 5.8f;
    [SerializeField] private Color frontLeftLightColor = new Color(0.94f, 0.9f, 0.84f);
    [SerializeField] private Color frontRightLightColor = new Color(0.78f, 0.85f, 0.94f);
    [SerializeField] private Color backLeftLightColor = new Color(0.77f, 0.92f, 0.85f);
    [SerializeField] private Color backRightLightColor = new Color(0.94f, 0.82f, 0.88f);

    [Header("View Rotation")]
    [SerializeField, InspectorName("View Turn Angle")] private float containerTurnAngle = 90f;
    [SerializeField, Min(1f), InspectorName("View Turn Speed")] private float containerTurnSpeed = 280f;

    [Header("Camera View")]
    [SerializeField, Min(1f)] private float cameraDistance = 32f;
    [SerializeField, Min(0f)] private float cameraHeight = 0f;
    [SerializeField, Min(0f)] private float cameraLookAtHeight = 0f;
    [SerializeField, Min(1f)] private float cameraOrthographicSize = 6.2f;

    [Header("F Preview")]
    [SerializeField] private float previewCameraYawOffset = 45f;
    [SerializeField, Min(0f)] private float previewCameraHeight = 8f;
    [SerializeField, Min(0f)] private float previewLookAtLift = 1.35f;
    [SerializeField, Min(0.1f)] private float previewCameraSpeed = 7f;
    [SerializeField, Min(1f)] private float previewCameraOrthographicSize = 5.8f;

    [Header("Next Preview")]
    [SerializeField] private Vector2 nextPreviewViewportPosition = new Vector2(0.82f, 0.68f);
    [SerializeField, Min(0.1f)] private float nextPreviewCameraDepth = 12f;
    [SerializeField, Min(0.1f)] private float nextPreviewCellSpacing = 0.75f;
    [SerializeField, Min(0.1f)] private float nextPreviewCubeSize = 0.65f;

    [Header("Preview And Effects")]
    [SerializeField, Range(0f, 0.8f)] private float colorGrayMix = DefaultColorGrayMix;
    [SerializeField, Range(0f, 1f)] private float ghostAlpha = 0.28f;
    [SerializeField] private Color ghostPreviewColor = new Color(0.9f, 0.92f, 0.94f);
    [SerializeField] private Color nextPreviewColor = new Color(0.82f, 0.84f, 0.86f);
    [SerializeField, Min(0.05f)] private float clearEffectDuration = 3f;
    [SerializeField, Min(1)] private int clearFragmentsPerCube = 5;
    [SerializeField, Min(0.05f)] private float clearFragmentSize = 0.32f;
    [SerializeField, Min(0.1f)] private float clearFragmentBlastSpeed = 3.8f;
    [SerializeField, Min(0f)] private float clearFragmentGravity = 0.95f;
    [SerializeField, Min(0f)] private float clearFragmentDrag = 0.32f;
    [SerializeField] private Color clearFallbackFragmentColor = new Color(0.88f, 0.82f, 0.44f, 1f);

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
    [SerializeField] private Button gameOverBackButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Text pauseText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseSettingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Dropdown qualityDropdown;
    [SerializeField] private Dropdown gameplayDifficultyDropdown;
    [SerializeField] private Text resolutionValueText;
    [SerializeField] private Button previousResolutionButton;
    [SerializeField] private Button nextResolutionButton;
    [SerializeField] private Text qualityValueText;
    [SerializeField] private Button previousQualityButton;
    [SerializeField] private Button nextQualityButton;
    [SerializeField] private Text gameplayDifficultyValueText;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private GameObject tutorialOverlayPanel;
    [SerializeField] private Text tutorialStepText;
    [SerializeField] private Button tutorialSkipButton;
    [SerializeField] private TutorialManager tutorialManager;

    [System.Serializable]
    private struct UiPositionConfig
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;

        public UiPositionConfig(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            this.anchorMin = anchorMin;
            this.anchorMax = anchorMax;
            this.pivot = new Vector2(0.5f, 0.5f);
            this.anchoredPosition = anchoredPosition;
            this.sizeDelta = sizeDelta;
        }

        public void ApplyTo(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }

    [System.Serializable]
    private struct UiTextConfig
    {
        public string text;
        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public Color color;
        public UiPositionConfig position;

        public UiTextConfig(string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color, UiPositionConfig position)
        {
            this.text = text;
            this.fontSize = fontSize;
            this.fontStyle = fontStyle;
            this.alignment = alignment;
            this.color = color;
            this.position = position;
        }
    }

    [System.Serializable]
    private struct UiButtonConfig
    {
        public string label;
        public Color color;
        public UiPositionConfig position;

        public UiButtonConfig(string label, Color color, UiPositionConfig position)
        {
            this.label = label;
            this.color = color;
            this.position = position;
        }
    }

    [Header("UI - Game Over Panel")]
    [SerializeField] private bool useGameOverPanelConfig;
    [SerializeField] private GameObject gameOverPanelPrefab;
    [SerializeField] private Color gameOverPanelColor = new Color(0f, 0f, 0f, 0.68f);
    [SerializeField] private UiPositionConfig gameOverPanelPosition = new UiPositionConfig(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(420f, 116f));
    [SerializeField] private Color gameOverRedBarColor = new Color(0.78f, 0.03f, 0.04f, 0.92f);
    [SerializeField] private UiPositionConfig gameOverRedBarPosition = new UiPositionConfig(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -70f), new Vector2(0f, 70f));
    [SerializeField] private UiTextConfig gameOverTitleConfig = new UiTextConfig("GAME OVER", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new UiPositionConfig(new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero));
    [SerializeField] private UiTextConfig gameOverHintConfig = new UiTextConfig("Choose an action", 18, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, new UiPositionConfig(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(320f, 24f)));
    [SerializeField] private UiButtonConfig gameOverRetryButtonConfig = new UiButtonConfig("RETRY", new Color(0.86f, 0.18f, 0.16f, 1f), new UiPositionConfig(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-104f, 24f), new Vector2(176f, 40f)));
    [SerializeField] private UiButtonConfig gameOverBackButtonConfig = new UiButtonConfig("BACK", new Color(0.34f, 0.38f, 0.42f, 1f), new UiPositionConfig(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(104f, 24f), new Vector2(176f, 40f)));

    [Header("UI - Pause Menu")]
    [SerializeField] private bool usePauseMenuConfig;
    [SerializeField] private GameObject pausePanelPrefab;
    [SerializeField] private Color pausePanelColor = new Color(0f, 0f, 0f, 0.78f);
    [SerializeField] private UiPositionConfig pausePanelPosition = new UiPositionConfig(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 520f));
    [SerializeField] private UiTextConfig pauseTextConfig = new UiTextConfig("PAUSED", 38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new UiPositionConfig(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(360f, 60f)));
    [SerializeField] private UiButtonConfig pauseResumeButtonConfig = new UiButtonConfig("RESUME", new Color(0.13f, 0.68f, 0.44f, 1f), new UiPositionConfig(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(280f, 52f)));
    [SerializeField] private UiButtonConfig pauseRestartButtonConfig = new UiButtonConfig("RESTART", new Color(0.9f, 0.36f, 0.18f, 1f), new UiPositionConfig(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(280f, 52f)));
    [SerializeField] private UiButtonConfig pauseSettingsButtonConfig = new UiButtonConfig("SETTINGS", new Color(0.18f, 0.48f, 0.86f, 1f), new UiPositionConfig(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(280f, 52f)));
    [SerializeField] private UiButtonConfig pauseMainMenuButtonConfig = new UiButtonConfig("MAIN MENU", new Color(0.58f, 0.42f, 0.84f, 1f), new UiPositionConfig(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(280f, 52f)));

    [Header("UI - Settings Panel")]
    [SerializeField] private bool useSettingsPanelConfig;
    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private Color settingsPanelColor = new Color(0f, 0f, 0f, 0.98f);
    [SerializeField] private UiPositionConfig settingsPanelPosition = new UiPositionConfig(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 850f));
    [SerializeField] private UiTextConfig settingsTitleConfig = new UiTextConfig("SETTINGS", 100, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new UiPositionConfig(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66.841f), new Vector2(667.7963f, 190.3173f)));

    [Header("UI - Tutorial Overlay")]
    [SerializeField] private bool useTutorialConfig;
    [SerializeField] private GameObject tutorialOverlayPrefab;
    [SerializeField] private Color tutorialOverlayColor = new Color(0f, 0f, 0f, 0.76f);
    [SerializeField] private UiPositionConfig tutorialOverlayPosition = new UiPositionConfig(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(1120f, 190f));
    [SerializeField] private UiTextConfig tutorialStepTextConfig = new UiTextConfig("", 26, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new UiPositionConfig(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-48f, 14f), new Vector2(-204f, -30f)));
    [SerializeField] private UiButtonConfig tutorialSkipButtonConfig = new UiButtonConfig("SKIP", new Color(0.35f, 0.38f, 0.41f, 1f), new UiPositionConfig(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-92f, 38f), new Vector2(144f, 44f)));

    [Header("UI References (for runtime binding)")]
    [SerializeField] private Text gameOverTitleText;
    private int BoardWidth => Mathf.Max(3, boardSide);
    private int BoardDepth => Mathf.Max(3, boardSide);
    private int BoardHeight => Mathf.Max(4, boardHeight);
    private float CubeSize => Mathf.Max(0.1f, cubeSize);
    private float FallInterval => Mathf.Max(0.05f, fallInterval);
    private float SoftDropInterval => Mathf.Max(0.01f, softDropInterval);
    private int PointsPerPlacedCube => Mathf.Max(0, pointsPerPlacedCube);
    private int PointsPerClearedCube => Mathf.Max(0, pointsPerClearedCube);
    private int GameplayDifficultyIndex => Mathf.Clamp(gameplayDifficulty, 0, GameplayDifficultyNames.Length - 1);
    private int RowsPerDifficultyLevel
    {
        get
        {
            switch (GameplayDifficultyIndex)
            {
                case 0:
                    return Mathf.Max(1, rowsPerDifficultyLevel + 2);
                case 2:
                    return Mathf.Max(1, rowsPerDifficultyLevel - 1);
                default:
                    return Mathf.Max(1, rowsPerDifficultyLevel);
            }
        }
    }

    private int PiecesPerDifficultyLevel
    {
        get
        {
            switch (GameplayDifficultyIndex)
            {
                case 0:
                    return Mathf.Max(1, piecesPerDifficultyLevel + 4);
                case 2:
                    return Mathf.Max(1, piecesPerDifficultyLevel - 2);
                default:
                    return Mathf.Max(1, piecesPerDifficultyLevel);
            }
        }
    }
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
        new Vector3Int(-2, 0, 0),
        new Vector3Int(2, 0, 0),
        new Vector3Int(-3, 0, 0),
        new Vector3Int(3, 0, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(-2, 1, 0),
        new Vector3Int(2, 1, 0),
        new Vector3Int(-1, -1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0)
    };

    private Transform deathWarningBar;
    private Transform tutorialTargetMarker;
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
    private int pendingFace;
    private float fallTimer;
    private float viewYaw;
    private float targetViewYaw;
    private float faceTurnStartYaw;
    private float faceTurnTargetYaw;
    private bool hasPendingFaceChange;
    private bool pieceFalling;
    private bool gameOver;
    private bool paused;
    private bool settingsOpen;
    private Resolution[] availableResolutions;
    private int selectedResolutionIndex;
    private int selectedQualityIndex;
    private bool updatingSettingsControls;
    private int tutorialMoveTargetX;
    private Color cachedDeathWarningColor = new Color(0.62f, 0.03f, 0.04f, 0f);
    private float cachedDeathWarningAlpha;
    private Vector3 cachedGravityVelocity;
    private Color cachedFragmentColor;
    private Vector3 cachedAngularVelocity;

    private bool IsTutorialRunning => tutorialManager != null && tutorialManager.IsActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "GamePlay")
        {
            return;
        }

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

        CleanupLegacyMenuArtifacts();
        BuildPieceDefinitions();
        BuildMaterials();
        BuildWorld();
        ResetGame();
        InitializeTutorialManager();
        tutorialManager.StartIfRequested();
    }

    private void OnValidate()
    {
        gameplayDifficulty = Mathf.Clamp(gameplayDifficulty, 0, GameplayDifficultyNames.Length - 1);
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
        UpdateTutorialTargetMarker();
        if (tutorialManager != null)
        {
            tutorialManager.Tick(Time.deltaTime, paused || gameOver);
        }

        UpdateUi();

        if (!IsTutorialRunning && Input.GetKeyDown(KeyCode.N))
        {
            ResetGame();
            return;
        }

        if (!IsTutorialRunning && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (paused || gameOver)
        {
            return;
        }

        if (IsTutorialRunning)
        {
            UpdateTutorialInput();
            UpdateTutorialFalling();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyUp(KeyCode.F))
        {
            TryClearSelectedFace();
            RefreshGhostPiece();
            UpdateDeathWarning();
        }

        bool previewHeld = IsPreviewHeld();
        if (Input.GetKeyDown(KeyCode.Space) || IsDownInputPressed())
        {
            if (!previewHeld)
            {
                pieceFalling = true;
                fallTimer = 0f;
            }
        }

        if (IsLeftInputPressed())
        {
            TryMove(new Vector3Int(-1, 0, 0));
        }

        if (IsRightInputPressed())
        {
            TryMove(new Vector3Int(1, 0, 0));
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            QueueFaceTurn(-1, ContainerTurnAngle);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            QueueFaceTurn(1, -ContainerTurnAngle);
        }

        if (IsUpInputPressed())
        {
            TryRotate(1);
        }

        if (!pieceFalling)
        {
            return;
        }

        if (previewHeld)
        {
            fallTimer = 0f;
            return;
        }

        float fallInterval = Input.GetKey(KeyCode.Space) || IsDownInputHeld() ? GetSoftDropInterval() : GetFallInterval();
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallInterval)
        {
            fallTimer = 0f;
            DropOneLayer();
        }
    }

    private static bool IsLeftInputPressed()
    {
        return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
    }

    private static bool IsRightInputPressed()
    {
        return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
    }

    private static bool IsUpInputPressed()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    }

    private static bool IsDownInputPressed()
    {
        return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
    }

    private static bool IsDownInputHeld()
    {
        return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }

    private void UpdateTutorialInput()
    {
        if (tutorialManager == null || !tutorialManager.IsActive)
        {
            return;
        }

        TutorialManager.TutorialStep step = tutorialManager.CurrentStep;

        if (step == TutorialManager.TutorialStep.MoveHorizontal)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (tutorialManager.AllowsAction(TutorialManager.TutorialAction.MoveHorizontal) && TryMove(new Vector3Int(-1, 0, 0)))
                {
                    if (currentOrigin.x <= tutorialMoveTargetX)
                    {
                        tutorialManager.ReportActionSuccess(TutorialManager.TutorialAction.MoveHorizontal);
                    }
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                if (tutorialManager.AllowsAction(TutorialManager.TutorialAction.MoveHorizontal) && TryMove(new Vector3Int(1, 0, 0)))
                {
                    if (currentOrigin.x <= tutorialMoveTargetX)
                    {
                        tutorialManager.ReportActionSuccess(TutorialManager.TutorialAction.MoveHorizontal);
                    }
                }
                return;
            }
        }
        else if (step == TutorialManager.TutorialStep.RotatePiece)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (tutorialManager.AllowsAction(TutorialManager.TutorialAction.RotatePiece) && TryRotate(1))
                {
                    tutorialManager.ReportActionSuccess(TutorialManager.TutorialAction.RotatePiece);
                }
                else
                {
                    tutorialManager.ReportWrongAction("Rotate blocked. Try again.");
                }

                return;
            }
        }
        else if (step == TutorialManager.TutorialStep.PlacePiece || step == TutorialManager.TutorialStep.ConnectedClear)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.S))
            {
                if (tutorialManager.AllowsAction(TutorialManager.TutorialAction.PlacePiece))
                {
                    if (!IsPreviewHeld())
                    {
                        pieceFalling = true;
                        fallTimer = 0f;
                        tutorialManager.ReportActionSuccess(TutorialManager.TutorialAction.PlacePiece);
                    }
                }
                return;
            }
        }
        else if (step == TutorialManager.TutorialStep.RotateContainer)
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            {
                int faceDelta = Input.GetKeyDown(KeyCode.Q) ? -1 : 1;
                float yawDelta = faceDelta < 0 ? ContainerTurnAngle : -ContainerTurnAngle;
                if (tutorialManager.AllowsAction(TutorialManager.TutorialAction.RotateContainer) && QueueFaceTurn(faceDelta, yawDelta))
                {
                    tutorialManager.ReportActionSuccess(TutorialManager.TutorialAction.RotateContainer);
                }
                else
                {
                    tutorialManager.ReportWrongAction("Wait for the current turn to finish.");
                }
                return;
            }
        }
        else if (step == TutorialManager.TutorialStep.Preview)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (tutorialManager.AllowsAction(TutorialManager.TutorialAction.Preview))
                {
                    TryClearSelectedFace();
                    RefreshGhostPiece();
                    UpdateDeathWarning();
                    tutorialManager.ReportActionSuccess(TutorialManager.TutorialAction.Preview);
                }
                return;
            }
        }

        if (Input.anyKeyDown || Input.GetKeyUp(KeyCode.F))
        {
            tutorialManager.ReportWrongAction("Not now. Please follow the tutorial.");
        }
    }

    private void UpdateTutorialFalling()
    {
        if (!pieceFalling || tutorialManager == null || !tutorialManager.IsActive)
        {
            return;
        }

        if (!tutorialManager.AllowsAction(TutorialManager.TutorialAction.PlacePiece))
        {
            pieceFalling = false;
            fallTimer = 0f;
            return;
        }

        if (IsPreviewHeld())
        {
            fallTimer = 0f;
            return;
        }

        bool softDropHeld = Input.GetKey(KeyCode.Space) || IsDownInputHeld();
        float interval = softDropHeld ? GetSoftDropInterval() : GetFallInterval();
        fallTimer += Time.deltaTime;
        if (fallTimer >= interval)
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
        EnsureTextPulseAnimation(scoreText);

        if (nextLabelText == null)
        {
            nextLabelText = CreateUiText("Next Label", gameCanvas.transform, "NEXT", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-115f, -40f), new Vector2(160f, 32f));
        }

        if (gameOverRedBar == null)
        {
            gameOverRedBar = CreateUiImage("Game Over Red Bar", gameCanvas.transform, new Color(0.78f, 0.03f, 0.04f, 0.92f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -70f), new Vector2(0f, 70f));
        }
        gameOverRedBar.color = MutedColor(new Color(0.78f, 0.03f, 0.04f, 0.92f));

        if (gameOverText == null)
        {
            gameOverText = CreateUiText("Game Over Text", gameOverRedBar.transform, "GAME OVER", 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        }

        if (gameOverPanel == null)
        {
            Image panel = CreateUiImage("Game Over Panel", gameCanvas.transform, new Color(0f, 0f, 0f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(420f, 116f));
            gameOverPanel = panel.gameObject;
        }
        SetUiRect(gameOverPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 44f), new Vector2(420f, 116f));
        EnsurePanelAnimation(gameOverPanel);
        CleanupLegacyGameOverControls();

        if (gameOverHintText == null)
        {
            gameOverHintText = CreateUiText("Restart Hint", gameOverPanel.transform, "Choose an action", 18, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(300f, 24f));
        }
        gameOverHintText.text = "Choose an action";
        SetUiRect(gameOverHintText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(320f, 24f));

        if (restartButton == null)
        {
            restartButton = CreateUiButton("Restart Button", gameOverPanel.transform, "RETRY", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-104f, 24f), new Vector2(176f, 40f));
        }
        if (restartButton.transform.parent != gameOverPanel.transform)
        {
            restartButton.transform.SetParent(gameOverPanel.transform, false);
        }
        SetUiRect(restartButton, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-104f, 24f), new Vector2(176f, 40f));
        SetButtonLabel(restartButton, "RETRY");
        ApplyButtonColor(restartButton, new Color(0.86f, 0.18f, 0.16f, 1f));

        restartButton.onClick.RemoveListener(ResetGame);
        restartButton.onClick.AddListener(ResetGame);

        if (gameOverBackButton == null)
        {
            gameOverBackButton = CreateUiButton("Game Over Back Button", gameOverPanel.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(104f, 24f), new Vector2(176f, 40f));
        }
        if (gameOverBackButton.transform.parent != gameOverPanel.transform)
        {
            gameOverBackButton.transform.SetParent(gameOverPanel.transform, false);
        }
        SetUiRect(gameOverBackButton, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(104f, 24f), new Vector2(176f, 40f));
        SetButtonLabel(gameOverBackButton, "BACK");
        ApplyButtonColor(gameOverBackButton, new Color(0.34f, 0.38f, 0.42f, 1f));
        gameOverBackButton.onClick.RemoveListener(ReturnToStartScene);
        gameOverBackButton.onClick.AddListener(ReturnToStartScene);

        if (pausePanel == null)
        {
            Image panel = CreateUiImage("Pause Panel", gameCanvas.transform, new Color(0f, 0f, 0f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 520f));
            pausePanel = panel.gameObject;
        }
        else
        {
            RectTransform pauseRect = pausePanel.GetComponent<RectTransform>();
            if (pauseRect != null)
            {
                pauseRect.sizeDelta = new Vector2(460f, 520f);
            }
        }
        EnsurePanelAnimation(pausePanel);

        if (pauseText == null)
        {
            pauseText = CreateUiText("Pause Text", pausePanel.transform, "PAUSED", 38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(360f, 60f));
        }
        pauseText.text = "PAUSED";
        RectTransform pauseTextRect = pauseText.GetComponent<RectTransform>();
        if (pauseTextRect != null)
        {
            pauseTextRect.anchorMin = new Vector2(0.5f, 1f);
            pauseTextRect.anchorMax = new Vector2(0.5f, 1f);
            pauseTextRect.anchoredPosition = new Vector2(0f, -62f);
            pauseTextRect.sizeDelta = new Vector2(360f, 60f);
        }

        if (resumeButton == null)
        {
            resumeButton = CreateUiButton("Resume Button", pausePanel.transform, "RESUME", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(280f, 52f));
        }
        ApplyButtonColor(resumeButton, new Color(0.13f, 0.68f, 0.44f, 1f));

        if (pauseRestartButton == null)
        {
            pauseRestartButton = CreateUiButton("Pause Restart Button", pausePanel.transform, "RESTART", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(280f, 52f));
        }
        ApplyButtonColor(pauseRestartButton, new Color(0.9f, 0.36f, 0.18f, 1f));

        if (pauseSettingsButton == null)
        {
            pauseSettingsButton = CreateUiButton("Pause Settings Button", pausePanel.transform, "SETTINGS", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(280f, 52f));
        }
        ApplyButtonColor(pauseSettingsButton, new Color(0.18f, 0.48f, 0.86f, 1f));

        if (mainMenuButton == null)
        {
            mainMenuButton = CreateUiButton("Main Menu Button", pausePanel.transform, "MAIN MENU", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(280f, 52f));
        }
        ApplyButtonColor(mainMenuButton, new Color(0.58f, 0.42f, 0.84f, 1f));

        WirePauseButtons();
        SetupSettingsPanel();
        SetupTutorialOverlay();
        InitializeTutorialManager();

        if (useGameOverPanelConfig || usePauseMenuConfig || useSettingsPanelConfig || useTutorialConfig)
        {
            ApplyAllUiConfigs();
        }

        UpdateUi();
    }

    private void WirePauseButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (pauseRestartButton != null)
        {
            pauseRestartButton.onClick.RemoveListener(RestartFromPauseMenu);
            pauseRestartButton.onClick.AddListener(RestartFromPauseMenu);
        }

        if (pauseSettingsButton != null)
        {
            pauseSettingsButton.onClick.RemoveListener(OpenSettingsPanel);
            pauseSettingsButton.onClick.AddListener(OpenSettingsPanel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(ReturnToStartScene);
            mainMenuButton.onClick.AddListener(ReturnToStartScene);
        }
    }

    private void SetupSettingsPanel()
    {
        if (settingsPanel == null)
        {
            Image panel = CreateUiImage("Settings Panel", gameCanvas.transform, new Color(0f, 0f, 0f, 0.98039216f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 850f));
            settingsPanel = panel.gameObject;
        }
        else
        {
            RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
            if (settingsRect != null)
            {
                settingsRect.sizeDelta = new Vector2(1000f, 850f);
            }

            Image settingsImage = settingsPanel.GetComponent<Image>();
            if (settingsImage != null)
            {
                settingsImage.color = new Color(0f, 0f, 0f, 0.98039216f);
            }
        }
        EnsurePanelAnimation(settingsPanel);

        Transform title = settingsPanel.transform.Find("Settings Title");
        if (title == null)
        {
            title = CreateUiText("Settings Title", settingsPanel.transform, "SETTINGS", 100, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66.841f), new Vector2(667.7963f, 190.3173f)).transform;
        }

        SetUiRect(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66.841f), new Vector2(667.7963f, 190.3173f));
        Text titleText = title.GetComponent<Text>();
        if (titleText != null)
        {
            titleText.fontSize = 100;
        }

        Transform volumeLabel = settingsPanel.transform.Find("Volume Label");
        if (volumeLabel == null)
        {
            volumeLabel = CreateUiText("Volume Label", settingsPanel.transform, "Volume", 32, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -221f), new Vector2(180f, 36f)).transform;
        }
        SetUiRect(volumeLabel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -221f), new Vector2(180f, 36f));
        Text volumeLabelText = volumeLabel.GetComponent<Text>();
        if (volumeLabelText != null)
        {
            volumeLabelText.fontSize = 32;
        }

        if (volumeSlider == null)
        {
            volumeSlider = CreateUiSlider("Volume Slider", settingsPanel.transform, new Vector2(0.5f, 1f), new Vector2(102f, -223f), new Vector2(300f, 32f));
        }
        SetUiRect(volumeSlider, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(102f, -223f), new Vector2(300f, 32f));

        if (fullscreenToggle == null)
        {
            fullscreenToggle = CreateUiToggle("Fullscreen Toggle", settingsPanel.transform, "Fullscreen", new Vector2(0.5f, 1f), new Vector2(0f, -285f), new Vector2(400f, 50f));
        }
        SetUiRect(fullscreenToggle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -285f), new Vector2(400f, 50f));

        Transform resolutionLabel = settingsPanel.transform.Find("Resolution Label");
        SetUiObjectActive(resolutionLabel, true);
        if (resolutionLabel != null)
        {
            SetUiRect(resolutionLabel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -372f), new Vector2(180f, 36f));
            Text resolutionLabelText = resolutionLabel.GetComponent<Text>();
            if (resolutionLabelText != null)
            {
                resolutionLabelText.fontSize = 32;
                resolutionLabelText.fontStyle = FontStyle.Bold;
                resolutionLabelText.alignment = TextAnchor.MiddleLeft;
                resolutionLabelText.color = Color.white;
            }
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.gameObject.SetActive(true);
            SetUiRect(resolutionDropdown, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(102f, -374f), new Vector2(300f, 42f));
        }

        if (previousResolutionButton == null)
        {
            previousResolutionButton = CreateUiButton("Previous Resolution Button", settingsPanel.transform, "<", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-72f, -260f), new Vector2(48f, 42f));
        }
        HideButtonObject(previousResolutionButton);

        if (resolutionValueText == null)
        {
            resolutionValueText = CreateUiText("Resolution Value", settingsPanel.transform, "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(80f, -260f), new Vector2(230f, 42f));
        }
        SetUiRect(resolutionValueText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(86f, -260f), new Vector2(300f, 42f));
        HideUiObject(resolutionValueText);

        if (nextResolutionButton == null)
        {
            nextResolutionButton = CreateUiButton("Next Resolution Button", settingsPanel.transform, ">", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(232f, -260f), new Vector2(48f, 42f));
        }
        HideButtonObject(nextResolutionButton);

        Transform qualityLabel = settingsPanel.transform.Find("Quality Label");
        SetUiObjectActive(qualityLabel, true);
        if (qualityLabel != null)
        {
            SetUiRect(qualityLabel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -446f), new Vector2(180f, 36f));
            Text qualityLabelText = qualityLabel.GetComponent<Text>();
            if (qualityLabelText != null)
            {
                qualityLabelText.fontSize = 32;
                qualityLabelText.fontStyle = FontStyle.Bold;
                qualityLabelText.alignment = TextAnchor.MiddleLeft;
                qualityLabelText.color = Color.white;
            }
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.gameObject.SetActive(true);
            SetUiRect(qualityDropdown, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(102f, -448f), new Vector2(300f, 42f));
        }

        if (previousQualityButton == null)
        {
            previousQualityButton = CreateUiButton("Previous Quality Button", settingsPanel.transform, "<", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-72f, -340f), new Vector2(48f, 42f));
        }
        HideButtonObject(previousQualityButton);

        if (qualityValueText == null)
        {
            qualityValueText = CreateUiText("Quality Value", settingsPanel.transform, "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(80f, -340f), new Vector2(230f, 42f));
        }
        SetUiRect(qualityValueText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(86f, -340f), new Vector2(300f, 42f));
        HideUiObject(qualityValueText);

        if (nextQualityButton == null)
        {
            nextQualityButton = CreateUiButton("Next Quality Button", settingsPanel.transform, ">", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(232f, -340f), new Vector2(48f, 42f));
        }
        HideButtonObject(nextQualityButton);

        HideGameplayDifficultySettingsControls();

        if (settingsBackButton == null)
        {
            settingsBackButton = CreateUiButton("Settings Back Button", settingsPanel.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(260f, 52f));
        }
        ApplyButtonColor(settingsBackButton, new Color(0.34f, 0.38f, 0.42f, 1f));

        WireSettingsButtons();
        EnsureInitialAutoSettings();
        RefreshSettingsControls();
    }

    private void HideGameplayDifficultySettingsControls()
    {
        if (settingsPanel == null)
        {
            return;
        }

        SetUiObjectActive(settingsPanel.transform.Find("Gameplay Difficulty Label"), false);
        SetUiObjectActive(settingsPanel.transform.Find("Gameplay Difficulty Dropdown"), false);
        SetUiObjectActive(settingsPanel.transform.Find("Gameplay Difficulty Value"), false);

        if (gameplayDifficultyDropdown != null)
        {
            gameplayDifficultyDropdown.onValueChanged.RemoveListener(SetGameplayDifficultyDropdownIndex);
            gameplayDifficultyDropdown.gameObject.SetActive(false);
        }

        if (gameplayDifficultyValueText != null)
        {
            gameplayDifficultyValueText.gameObject.SetActive(false);
        }
    }

    private void SetupTutorialOverlay()
    {
        Transform overlayParent = GetOrCreateTutorialUiContainer();
        if (tutorialOverlayPanel == null)
        {
            Image panel = CreateUiImage("Interactive Tutorial Panel", overlayParent, new Color(0f, 0f, 0f, 0.76f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(1120f, 190f));
            tutorialOverlayPanel = panel.gameObject;
        }
        else
        {
            if (tutorialOverlayPanel.transform.parent != overlayParent)
            {
                tutorialOverlayPanel.transform.SetParent(overlayParent, false);
            }

            RectTransform rect = tutorialOverlayPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 84f);
                rect.sizeDelta = new Vector2(1120f, 190f);
            }
        }
        EnsurePanelAnimation(tutorialOverlayPanel);

        if (tutorialStepText == null)
        {
            tutorialStepText = CreateUiText("Interactive Tutorial Text", tutorialOverlayPanel.transform, "", 26, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-48f, 14f), new Vector2(-204f, -30f));
        }
        else
        {
            tutorialStepText.fontSize = 26;
            tutorialStepText.fontStyle = FontStyle.Bold;
            tutorialStepText.alignment = TextAnchor.MiddleLeft;
            tutorialStepText.color = Color.white;
            SetUiRect(tutorialStepText, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-48f, 14f), new Vector2(-204f, -30f));
        }

        if (tutorialSkipButton == null)
        {
            tutorialSkipButton = CreateUiButton("Skip Interactive Tutorial Button", tutorialOverlayPanel.transform, "SKIP", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-92f, 38f), new Vector2(144f, 44f));
        }
        else
        {
            SetUiRect(tutorialSkipButton, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-92f, 38f), new Vector2(144f, 44f));
        }

        ApplyButtonColor(tutorialSkipButton, new Color(0.35f, 0.38f, 0.41f, 1f));
        tutorialOverlayPanel.SetActive(false);
    }

    private Transform GetOrCreateTutorialUiContainer()
    {
        if (gameCanvas == null)
        {
            return transform;
        }

        Transform existing = gameCanvas.transform.Find("Container");
        if (existing != null)
        {
            RectTransform existingRect = existing as RectTransform;
            if (existingRect != null)
            {
                existingRect.anchorMin = Vector2.zero;
                existingRect.anchorMax = Vector2.one;
                existingRect.pivot = new Vector2(0.5f, 0.5f);
                existingRect.anchoredPosition = Vector2.zero;
                existingRect.sizeDelta = Vector2.zero;
                existingRect.offsetMin = Vector2.zero;
                existingRect.offsetMax = Vector2.zero;
            }

            return existing;
        }

        RectTransform container = CreateUiRect("Container", gameCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
        container.sizeDelta = Vector2.zero;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        return container;
    }

    private void InitializeTutorialManager()
    {
        if (tutorialManager == null)
        {
            tutorialManager = GetComponent<TutorialManager>();
        }

        if (tutorialManager == null)
        {
            tutorialManager = gameObject.AddComponent<TutorialManager>();
        }

        tutorialManager.ConfigureUi(tutorialOverlayPanel, tutorialStepText, tutorialSkipButton);
        tutorialManager.SetStepEnteredCallback(PrepareTutorialScenario);
    }

    private void WireSettingsButtons()
    {
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveListener(CloseSettingsPanel);
            settingsBackButton.onClick.AddListener(CloseSettingsPanel);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(SetResolutionDropdownIndex);
            resolutionDropdown.onValueChanged.AddListener(SetResolutionDropdownIndex);
        }

        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveListener(SetQualityDropdownIndex);
            qualityDropdown.onValueChanged.AddListener(SetQualityDropdownIndex);
        }

    }

    private void TogglePause()
    {
        if (gameOver)
        {
            return;
        }

        if (!paused)
        {
            paused = true;
            settingsOpen = false;
            UpdateUi();
            return;
        }

        if (settingsOpen)
        {
            CloseSettingsPanel();
            return;
        }

        ResumeGame();
    }

    private void ResumeGame()
    {
        paused = false;
        settingsOpen = false;
        UpdateUi();
    }

    private void RestartFromPauseMenu()
    {
        settingsOpen = false;
        paused = false;
        ResetGame();
    }

    private void OpenSettingsPanel()
    {
        settingsOpen = true;
        RefreshSettingsControls();
        UpdateUi();
    }

    private void CloseSettingsPanel()
    {
        settingsOpen = false;
        UpdateUi();
    }

    private void ReturnToStartScene()
    {
        SceneManager.LoadScene("Start");
    }

    private void EnsureInitialAutoSettings()
    {
        if (PlayerPrefs.GetInt("AutoSettingsInitialized", 0) == 1)
        {
            return;
        }

        RefreshResolutionOptions(false);
        ApplyAutoResolution();
        ApplyAutoQuality();
        PlayerPrefs.SetInt("AutoSettingsInitialized", 1);
        PlayerPrefs.Save();
    }

    private void RefreshSettingsControls()
    {
        updatingSettingsControls = true;
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
            AudioListener.volume = volumeSlider.value;
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            Screen.fullScreen = fullscreenToggle.isOn;
        }

        RefreshResolutionOptions(false);
        RefreshQualityOptions(false);
        updatingSettingsControls = false;
    }

    private void RefreshResolutionOptions(bool keepSelection)
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
            Resolution current = Screen.currentResolution;
            uniqueResolutions.Add(current);
        }

        availableResolutions = uniqueResolutions.ToArray();
        if (keepSelection)
        {
            selectedResolutionIndex = Mathf.Clamp(selectedResolutionIndex, 0, availableResolutions.Length - 1);
            UpdateResolutionText();
            return;
        }

        int currentIndex = 0;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == Screen.currentResolution.width && availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
                break;
            }
        }

        selectedResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt("ResolutionIndex", currentIndex), 0, availableResolutions.Length - 1);
        PopulateResolutionDropdown();
        UpdateResolutionText();
    }

    private void RefreshQualityOptions(bool keepSelection)
    {
        if (QualitySettings.names.Length == 0)
        {
            selectedQualityIndex = 0;
            UpdateQualityText();
            return;
        }

        if (keepSelection)
        {
            selectedQualityIndex = Mathf.Clamp(selectedQualityIndex, 0, QualitySettings.names.Length - 1);
            UpdateQualityText();
            return;
        }

        selectedQualityIndex = Mathf.Clamp(PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(selectedQualityIndex, true);
        PopulateQualityDropdown();
        UpdateQualityText();
    }

    private void RefreshGameplayDifficultyOptions(bool keepSelection)
    {
        if (!keepSelection)
        {
            gameplayDifficulty = Mathf.Clamp(PlayerPrefs.GetInt(GameplayDifficultyKey, gameplayDifficulty), 0, GameplayDifficultyNames.Length - 1);
        }
        else
        {
            gameplayDifficulty = GameplayDifficultyIndex;
        }

        PopulateGameplayDifficultyDropdown();
        UpdateGameplayDifficultyText();
        RecalculateLevel();
    }

    private void SetResolutionDropdownIndex(int index)
    {
        if (updatingSettingsControls)
        {
            return;
        }

        SetResolutionIndex(index, true);
    }

    private void SetQualityDropdownIndex(int index)
    {
        if (updatingSettingsControls)
        {
            return;
        }

        SetQualityIndex(index, true);
    }

    private void SetGameplayDifficultyDropdownIndex(int index)
    {
        if (updatingSettingsControls)
        {
            return;
        }

        SetGameplayDifficultyIndex(index, true);
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void PreviousResolution()
    {
        SetResolutionIndex(selectedResolutionIndex - 1, true);
    }

    private void NextResolution()
    {
        SetResolutionIndex(selectedResolutionIndex + 1, true);
    }

    private void ApplyAutoResolution()
    {
        RefreshResolutionOptions(true);
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

        SetResolutionIndex(bestIndex, true);
    }

    private void SetResolutionIndex(int index, bool apply)
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            RefreshResolutionOptions(false);
        }

        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            return;
        }

        selectedResolutionIndex = (index % availableResolutions.Length + availableResolutions.Length) % availableResolutions.Length;
        Resolution resolution = availableResolutions[selectedResolutionIndex];
        if (apply)
        {
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolutionIndex", selectedResolutionIndex);
            PlayerPrefs.Save();
        }

        UpdateResolutionDropdownValue();
        UpdateResolutionText();
    }

    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null || availableResolutions == null || availableResolutions.Length == 0)
        {
            return;
        }

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution resolution = availableResolutions[i];
            options.Add(resolution.width + " x " + resolution.height);
        }

        resolutionDropdown.AddOptions(options);
        UpdateResolutionDropdownValue();
    }

    private void UpdateResolutionDropdownValue()
    {
        if (resolutionDropdown == null || availableResolutions == null || availableResolutions.Length == 0)
        {
            return;
        }

        bool wasUpdating = updatingSettingsControls;
        updatingSettingsControls = true;
        resolutionDropdown.value = Mathf.Clamp(selectedResolutionIndex, 0, availableResolutions.Length - 1);
        resolutionDropdown.RefreshShownValue();
        updatingSettingsControls = wasUpdating;
    }

    private void UpdateResolutionText()
    {
        if (resolutionValueText == null)
        {
            return;
        }

        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            resolutionValueText.text = "Auto";
            return;
        }

        Resolution resolution = availableResolutions[Mathf.Clamp(selectedResolutionIndex, 0, availableResolutions.Length - 1)];
        resolutionValueText.text = resolution.width + " x " + resolution.height;
    }

    private void PreviousQuality()
    {
        SetQualityIndex(selectedQualityIndex - 1, true);
    }

    private void NextQuality()
    {
        SetQualityIndex(selectedQualityIndex + 1, true);
    }

    private void ApplyAutoQuality()
    {
        SetQualityIndex(GetRecommendedQualityIndex(), true);
    }

    private void SetQualityIndex(int index, bool apply)
    {
        int qualityCount = QualitySettings.names.Length;
        if (qualityCount <= 0)
        {
            selectedQualityIndex = 0;
            UpdateQualityText();
            return;
        }

        selectedQualityIndex = (index % qualityCount + qualityCount) % qualityCount;
        if (apply)
        {
            QualitySettings.SetQualityLevel(selectedQualityIndex, true);
            PlayerPrefs.SetInt("QualityLevel", selectedQualityIndex);
            PlayerPrefs.Save();
        }

        UpdateQualityDropdownValue();
        UpdateQualityText();
    }

    private void SetGameplayDifficultyIndex(int index, bool apply)
    {
        gameplayDifficulty = Mathf.Clamp(index, 0, GameplayDifficultyNames.Length - 1);
        if (apply)
        {
            PlayerPrefs.SetInt(GameplayDifficultyKey, gameplayDifficulty);
            PlayerPrefs.Save();
        }

        RecalculateLevel();
        UpdateGameplayDifficultyDropdownValue();
        UpdateGameplayDifficultyText();
    }

    private int GetRecommendedQualityIndex()
    {
        int qualityCount = QualitySettings.names.Length;
        if (qualityCount <= 1)
        {
            return 0;
        }

        int performanceScore = SystemInfo.graphicsMemorySize / 512 + SystemInfo.systemMemorySize / 2048 + SystemInfo.processorCount;
        float normalized = Mathf.InverseLerp(7f, 24f, performanceScore);
        return Mathf.Clamp(Mathf.RoundToInt(normalized * (qualityCount - 1)), 0, qualityCount - 1);
    }

    private void UpdateQualityText()
    {
        if (qualityValueText == null)
        {
            return;
        }

        if (QualitySettings.names.Length == 0)
        {
            qualityValueText.text = "Default";
            return;
        }

        qualityValueText.text = QualitySettings.names[Mathf.Clamp(selectedQualityIndex, 0, QualitySettings.names.Length - 1)];
    }

    private void PopulateQualityDropdown()
    {
        if (qualityDropdown == null)
        {
            return;
        }

        string[] qualityNames = QualitySettings.names;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(qualityNames.Length > 0 ? qualityNames : new[] { "Default" }));
        UpdateQualityDropdownValue();
    }

    private void UpdateQualityDropdownValue()
    {
        if (qualityDropdown == null)
        {
            return;
        }

        bool wasUpdating = updatingSettingsControls;
        updatingSettingsControls = true;
        int qualityCount = Mathf.Max(qualityDropdown.options.Count, 1);
        qualityDropdown.value = Mathf.Clamp(selectedQualityIndex, 0, qualityCount - 1);
        qualityDropdown.RefreshShownValue();
        updatingSettingsControls = wasUpdating;
    }

    private void PopulateGameplayDifficultyDropdown()
    {
        if (gameplayDifficultyDropdown == null)
        {
            return;
        }

        gameplayDifficultyDropdown.ClearOptions();
        gameplayDifficultyDropdown.AddOptions(new List<string>(GameplayDifficultyNames));
        UpdateGameplayDifficultyDropdownValue();
        gameplayDifficultyDropdown.onValueChanged.RemoveListener(SetGameplayDifficultyDropdownIndex);
        gameplayDifficultyDropdown.onValueChanged.AddListener(SetGameplayDifficultyDropdownIndex);
    }

    private void UpdateGameplayDifficultyDropdownValue()
    {
        if (gameplayDifficultyDropdown == null)
        {
            return;
        }

        bool wasUpdating = updatingSettingsControls;
        updatingSettingsControls = true;
        gameplayDifficultyDropdown.value = GameplayDifficultyIndex;
        gameplayDifficultyDropdown.RefreshShownValue();
        updatingSettingsControls = wasUpdating;
    }

    private void UpdateGameplayDifficultyText()
    {
        if (gameplayDifficultyValueText != null)
        {
            gameplayDifficultyValueText.text = GameplayDifficultyNames[GameplayDifficultyIndex];
        }
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
            if (gameOver)
            {
                EnsurePanelAnimation(gameOverPanel);
            }

            gameOverPanel.SetActive(gameOver);
        }

        if (pausePanel != null)
        {
            bool showPause = paused && !gameOver && !settingsOpen;
            if (showPause)
            {
                EnsurePanelAnimation(pausePanel);
            }

            pausePanel.SetActive(showPause);
        }

        if (settingsPanel != null)
        {
            bool showSettings = paused && !gameOver && settingsOpen;
            if (showSettings)
            {
                EnsurePanelAnimation(settingsPanel);
            }

            settingsPanel.SetActive(showSettings);
        }

        if (tutorialOverlayPanel != null)
        {
            bool showTutorial = IsTutorialRunning && !gameOver && !paused;
            if (showTutorial)
            {
                EnsurePanelAnimation(tutorialOverlayPanel);
            }

            tutorialOverlayPanel.SetActive(showTutorial);
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

    private void CleanupLegacyMenuArtifacts()
    {
        MainMenu[] legacyMenus = FindObjectsOfType<MainMenu>(true);
        for (int i = 0; i < legacyMenus.Length; i++)
        {
            MainMenu menu = legacyMenus[i];
            if (menu == null || menu.gameObject == null || menu.gameObject == gameObject || menu.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            DisableAndDestroyObject(menu.gameObject);
        }

        for (int i = 0; i < LegacyGameplayMenuObjectNames.Length; i++)
        {
            DestroySceneObjectsByName(LegacyGameplayMenuObjectNames[i]);
        }
    }

    private void CleanupLegacyGameOverControls()
    {
        if (gameOverPanel == null)
        {
            return;
        }

        Button[] buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            string name = button.gameObject.name;
            if (name == "Restart Button" || name == "Game Over Back Button")
            {
                continue;
            }

            DisableAndDestroyObject(button.gameObject);
        }

        MainMenu[] legacyMenus = gameOverPanel.GetComponentsInChildren<MainMenu>(true);
        for (int i = 0; i < legacyMenus.Length; i++)
        {
            if (legacyMenus[i] != null)
            {
                DisableAndDestroyObject(legacyMenus[i].gameObject);
            }
        }
    }

    private void DestroySceneObjectsByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return;
        }

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate == null || candidate.scene != gameObject.scene || candidate == gameObject)
            {
                continue;
            }

            if (candidate.name == objectName)
            {
                DisableAndDestroyObject(candidate);
            }
        }
    }

    private static void DisableAndDestroyObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(false);
        Destroy(target);
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
        image.color = MutedColor(color);
        return image;
    }

    private void SetUiRect(Component component, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (component == null)
        {
            return;
        }

        SetUiRect(component.transform, anchorMin, anchorMax, anchoredPosition, sizeDelta);
    }

    private void SetUiRect(Transform transform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        if (transform == null)
        {
            return;
        }

        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    private void HideButtonObject(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.gameObject.SetActive(false);
    }

    private void HideUiObject(Component component)
    {
        if (component == null)
        {
            return;
        }

        component.gameObject.SetActive(false);
    }

    private void HideUiObject(Transform transform)
    {
        if (transform == null)
        {
            return;
        }

        transform.gameObject.SetActive(false);
    }

    private void SetUiObjectActive(Transform transform, bool active)
    {
        if (transform == null)
        {
            return;
        }

        transform.gameObject.SetActive(active);
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
        Image image = CreateUiImage(objectName, parent, new Color(0.26f, 0.3f, 0.34f, 1f), anchorMin, anchorMax, anchoredPosition, sizeDelta);
        Button button = image.gameObject.AddComponent<Button>();
        ApplyButtonColor(button, image.color);
        EnsureButtonAnimation(button.gameObject);

        CreateUiText("Label", image.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        return button;
    }

    private static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        Text label = button.GetComponentInChildren<Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(button.transform, false);
            label = labelObject.GetComponent<Text>();
        }

        RectTransform labelRect = label.GetComponent<RectTransform>();
        if (labelRect != null)
        {
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
        }

        if (label.font == null)
        {
            Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtInFont != null)
            {
                label.font = builtInFont;
            }
        }

        label.text = text;
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
    }

    private void EnsurePanelAnimation(GameObject panel)
    {
        if (panel != null && panel.GetComponent<UiPanelAnimator>() == null)
        {
            panel.AddComponent<UiPanelAnimator>();
        }
    }

    private void EnsureButtonAnimation(GameObject buttonObject)
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

        floatButton.SetDelay(0f);
    }

    private void EnsureTextPulseAnimation(Text text)
    {
        if (text != null && text.GetComponent<UiTextPulseOnChange>() == null)
        {
            text.gameObject.AddComponent<UiTextPulseOnChange>();
        }
    }

    private void ApplyButtonColor(Button button, Color color)
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
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.42f);
        button.colors = colors;
    }

    private Slider CreateUiSlider(string objectName, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject sliderObject = new GameObject(objectName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = anchor;
        sliderRect.anchorMax = anchor;
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = sizeDelta;

        Image background = CreateUiImage("Background", sliderObject.transform, new Color(0.18f, 0.2f, 0.2f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.raycastTarget = true;
        RectTransform fillArea = CreateUiRect("Fill Area", sliderObject.transform, new Vector2(0f, 0.2f), new Vector2(1f, 0.8f), Vector2.zero, new Vector2(-24f, 0f));
        Image fill = CreateUiImage("Fill", fillArea, new Color(0.13f, 0.78f, 0.62f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform handleArea = CreateUiRect("Handle Slide Area", sliderObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
        Image handle = CreateUiImage("Handle", handleArea, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 36f));

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        return slider;
    }

    private Toggle CreateUiToggle(string objectName, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject toggleObject = new GameObject(objectName, typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = anchor;
        toggleRect.anchorMax = anchor;
        toggleRect.anchoredPosition = anchoredPosition;
        toggleRect.sizeDelta = sizeDelta;

        Image box = CreateUiImage("Box", toggleObject.transform, new Color(0.18f, 0.2f, 0.2f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(34f, 34f));
        Image checkmark = CreateUiImage("Checkmark", box.transform, new Color(0.13f, 0.78f, 0.62f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
        CreateUiText("Label", toggleObject.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(74f, 0f), new Vector2(-74f, 0f));

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = checkmark;
        toggle.isOn = true;
        return toggle;
    }

    private RectTransform CreateUiRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        return rectTransform;
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
            new PieceDefinition("T", new Color(0.8f, 0.28f, 1f), 1, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0)),
            new PieceDefinition("S", new Color(0.14f, 0.9f, 0.28f), 1, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0)),
            new PieceDefinition("Z", new Color(1f, 0.18f, 0.18f), 1, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("J", new Color(0.22f, 0.42f, 1f), 1, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0)),
            new PieceDefinition("L", new Color(1f, 0.54f, 0.12f), 1, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("P5", new Color(1f, 0.36f, 0.68f), 1, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("U5", new Color(0.26f, 1f, 0.66f), 1, new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0)),
            new PieceDefinition("V5", new Color(1f, 0.66f, 0.1f), 2, new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0), new Vector3Int(1, -2, 0), new Vector3Int(2, -2, 0)),
            new PieceDefinition("W5", new Color(0.55f, 1f, 0.25f), 2, new Vector3Int(-1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0), new Vector3Int(1, -2, 0)),
            new PieceDefinition("N5", new Color(0.24f, 0.95f, 0.88f), 2, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0), new Vector3Int(1, -2, 0)),
            new PieceDefinition("T5", new Color(0.86f, 0.5f, 1f), 3, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("X5", new Color(0.96f, 0.52f, 1f), 3, new Vector3Int(0, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("F5", new Color(0.12f, 0.78f, 1f), 3, new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("C6", new Color(1f, 0.32f, 0.2f), 4, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, -2, 0), new Vector3Int(0, -2, 0)),
            new PieceDefinition("I4T", new Color(0.42f, 0.8f, 1f), 99, new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0)),
            new PieceDefinition("Y6", new Color(0.58f, 0.72f, 1f), 4, new Vector3Int(0, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, -2, 0), new Vector3Int(0, -3, 0), new Vector3Int(-1, -1, 0), new Vector3Int(1, -2, 0))
        };
    }

    private void BuildMaterials()
    {
        Color bodyColor = new Color(0.08f, 0.1f, 0.12f, 0.18f);
        Color railColor = new Color(0.74f, 0.79f, 0.83f);
        Color gridColor = new Color(1f, 1f, 1f, 0.18f);
        Color ghostColor = new Color(ghostPreviewColor.r, ghostPreviewColor.g, ghostPreviewColor.b, GhostAlpha);
        Color deathWarningColor = new Color(0.62f, 0.03f, 0.04f, 0f);

        if (bodyMaterial == null)
        {
            bodyMaterial = MakeTransparentMaterial("Transparent Cuboid", bodyColor, 0f, 0.18f);
        }
        else
        {
            ConfigureTransparentMaterial(bodyMaterial, bodyColor);
        }

        if (railMaterial == null)
        {
            railMaterial = MakeMaterial("Cuboid Rails", railColor, 0.04f, 0.58f);
        }
        else
        {
            ApplyMaterialColor(railMaterial, railColor);
        }

        if (gridMaterial == null)
        {
            gridMaterial = MakeTransparentMaterial("Cuboid Grid", gridColor, 0f, 0.18f);
        }
        else
        {
            ConfigureTransparentMaterial(gridMaterial, gridColor);
        }

        if (ghostMaterial == null)
        {
            ghostMaterial = MakeTransparentMaterial("Ghost Piece", ghostColor, 0f, 0.2f);
        }
        else
        {
            ConfigureTransparentMaterial(ghostMaterial, ghostColor);
        }

        if (nextPreviewMaterial == null)
        {
            nextPreviewMaterial = MakeMaterial("Next Preview", nextPreviewColor, 0.02f, 0.48f);
        }
        else
        {
            ApplyMaterialColor(nextPreviewMaterial, nextPreviewColor);
        }

        if (deathWarningMaterial == null)
        {
            deathWarningMaterial = MakeTransparentMaterial("Death Warning", deathWarningColor, 0f, 0.08f);
        }
        else
        {
            ConfigureTransparentMaterial(deathWarningMaterial, deathWarningColor);
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
        EnsureTutorialTargetMarker();
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

    private void EnsureTutorialTargetMarker()
    {
        if (tutorialTargetMarker == null)
        {
            Transform existing = warningRoot != null ? warningRoot.Find("Tutorial Target Marker") : null;
            if (existing != null)
            {
                tutorialTargetMarker = existing;
            }
        }

        if (tutorialTargetMarker == null)
        {
            Material markerMaterial = MakeTransparentMaterial(
                "Tutorial Target Marker Material",
                new Color(0.24f, 0.95f, 0.62f, 0.34f),
                0f,
                0.08f);
            tutorialTargetMarker = CreateCube(
                "Tutorial Target Marker",
                Vector3.zero,
                new Vector3(CubeSize * 1.02f, CubeSize * 1.02f, CubeSize * 0.26f),
                markerMaterial,
                warningRoot);
        }

        tutorialTargetMarker.gameObject.SetActive(false);
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
        RenderSettings.ambientLight = MutedColor(ambientLightColor);
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
        pointLight.color = MutedColor(color);
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
        UpdatePendingFaceChange();
        if (tutorialManager != null && Mathf.Abs(Mathf.DeltaAngle(viewYaw, targetViewYaw)) <= 0.5f)
        {
            tutorialManager.ReportContainerTurnCompleted();
        }

        if (containerRoot != null)
        {
            containerRoot.localRotation = Quaternion.identity;
        }
    }

    private bool QueueFaceTurn(int faceDelta, float yawDelta)
    {
        if (hasPendingFaceChange || Mathf.Abs(Mathf.DeltaAngle(viewYaw, targetViewYaw)) > 0.5f)
        {
            return false;
        }

        pendingFace = WrapFace(activeFace + faceDelta);
        faceTurnStartYaw = viewYaw;
        faceTurnTargetYaw = targetViewYaw + yawDelta;
        targetViewYaw = faceTurnTargetYaw;
        hasPendingFaceChange = true;
        return true;
    }

    private void UpdatePendingFaceChange()
    {
        if (!hasPendingFaceChange)
        {
            return;
        }

        float totalAngle = Mathf.Abs(Mathf.DeltaAngle(faceTurnStartYaw, faceTurnTargetYaw));
        float remainingAngle = Mathf.Abs(Mathf.DeltaAngle(viewYaw, faceTurnTargetYaw));
        if (totalAngle <= 0.01f || remainingAngle <= totalAngle * 0.5f)
        {
            ApplyPendingFaceChange();
        }
    }

    private void ApplyPendingFaceChange()
    {
        activeFace = pendingFace;
        hasPendingFaceChange = false;
        TryClearSelectedFace();
        StickWaitingPieceToTop();
        RefreshActivePiece();
        RefreshGhostPiece();
        RefreshDeathWarningVisual();
        UpdateDeathWarning();
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
        gameplayDifficulty = Mathf.Clamp(PlayerPrefs.GetInt(GameplayDifficultyKey, gameplayDifficulty), 0, GameplayDifficultyNames.Length - 1);
        level = 1;
        lockedPieces = 0;
        activeFace = 0;
        pendingFace = 0;
        fallTimer = 0f;
        viewYaw = 0f;
        targetViewYaw = 0f;
        faceTurnStartYaw = 0f;
        faceTurnTargetYaw = 0f;
        hasPendingFaceChange = false;
        pieceFalling = false;
        gameOver = false;
        paused = false;
        settingsOpen = false;
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

    private void PrepareTutorialScenario(TutorialManager.TutorialStep step)
    {
        switch (step)
        {
            case TutorialManager.TutorialStep.MoveHorizontal:
                SetupTutorialMoveExample();
                break;
            case TutorialManager.TutorialStep.RotatePiece:
                SetupTutorialRotatePieceStep();
                break;
            case TutorialManager.TutorialStep.PlacePiece:
                SetupTutorialPlaceStep();
                break;
            case TutorialManager.TutorialStep.RotateContainer:
                SetupTutorialRotateContainerStep();
                break;
            case TutorialManager.TutorialStep.ConnectedClear:
                SetupTutorialConnectedClearExample();
                break;
            case TutorialManager.TutorialStep.Preview:
                SetupTutorialPreviewStep();
                break;
        }
    }

    private void SetupTutorialMoveExample()
    {
        ResetTutorialBoard();
        ConfigureTutorialBaseView(true);
        tutorialMoveTargetX = 0;
        ForceCurrentPiece("I2");
        currentOrigin = new Vector3Int(BoardWidth - 2, GetSpawnOriginY(currentCells), FrontLayer);
        RefreshTutorialPieceState();
    }

    private void SetupTutorialPlaceStep()
    {
        pieceFalling = false;
        fallTimer = 0f;
    }

    private void SetupTutorialRotatePieceStep()
    {
        pieceFalling = false;
        fallTimer = 0f;
    }

    private void SetupTutorialRotateContainerStep()
    {
        ConfigureTutorialBaseView(true);
        ForceCurrentPiece("Dot1");
        currentOrigin = new Vector3Int(BoardWidth / 2, GetSpawnOriginY(currentCells), FrontLayer);
        RefreshTutorialPieceState();
    }

    private void SetupTutorialConnectedClearExample()
    {
        ConfigureTutorialBaseView(false);
        ForceCurrentPiece("I4T");
        int originX = activeFace == 3 ? 1 : 2;
        currentOrigin = new Vector3Int(originX, GetSpawnOriginY(currentCells), FrontLayer);
        RefreshTutorialPieceState();
    }

    private void SetupTutorialPreviewStep()
    {
        ConfigureTutorialBaseView(false);
    }

    private void ConfigureTutorialBaseView(bool resetFace)
    {
        if (resetFace)
        {
            activeFace = 0;
            pendingFace = 0;
            viewYaw = 0f;
            targetViewYaw = 0f;
            faceTurnStartYaw = 0f;
            faceTurnTargetYaw = 0f;
        }
        else
        {
            pendingFace = activeFace;
            targetViewYaw = viewYaw;
            faceTurnStartYaw = viewYaw;
            faceTurnTargetYaw = viewYaw;
        }

        hasPendingFaceChange = false;
        ApplyCameraTransform(viewYaw, false, true);
        pieceFalling = false;
        fallTimer = 0f;
    }

    private void RefreshTutorialPieceState()
    {
        pieceFalling = false;
        fallTimer = 0f;
        RefreshActivePiece();
        RefreshGhostPiece();
        RefreshDeathWarningVisual();
        UpdateDeathWarning();
    }

    private void ResetTutorialBoard()
    {
        ClearChildren(lockedRoot);
        ClearChildren(activeRoot);
        ClearChildren(ghostRoot);
        grid = new Transform[BoardWidth, BoardHeight, BoardDepth];
        activeCubes = new Transform[0];
        ghostCubes = new Transform[0];
    }

    private void ForceCurrentPiece(string pieceName)
    {
        int pieceIndex = FindPieceIndex(pieceName);
        currentPieceIndex = pieceIndex;
        nextPieceIndex = pieceIndex;
        currentCells = CopyCells(pieces[pieceIndex].Cells);
        currentOrigin = new Vector3Int(BoardWidth / 2, GetSpawnOriginY(currentCells), FrontLayer);
        pieceFalling = false;
        fallTimer = 0f;

        CreateActiveCubes();
        CreateGhostCubes();
        RefreshActivePiece();
        RefreshGhostPiece();
        RefreshNextPreview();
    }

    private int FindPieceIndex(string pieceName)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].Name == pieceName)
            {
                return i;
            }
        }

        return 0;
    }

    private void SpawnPiece()
    {
        currentPieceIndex = nextPieceIndex;
        nextPieceIndex = RandomPieceIndex();
        currentCells = CopyCells(pieces[currentPieceIndex].Cells);
        currentOrigin = new Vector3Int(BoardWidth / 2, GetSpawnOriginY(currentCells), FrontLayer);
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
        ghostMaterial.color = MutedColor(new Color(ghostPreviewColor.r, ghostPreviewColor.g, ghostPreviewColor.b, GhostAlpha));

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
        if (!pieceFalling)
        {
            candidate = GetTopStuckOrigin(candidate, currentCells);
        }

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
            if (!pieceFalling)
            {
                candidateOrigin = GetTopStuckOrigin(candidateOrigin, rotated);
            }

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
        if (IsPreviewHeld())
        {
            fallTimer = 0f;
            return true;
        }

        if (TryMove(Vector3Int.down))
        {
            return true;
        }

        if (HasAboveBoardCells(currentOrigin, currentCells))
        {
            gameOver = true;
            pieceFalling = false;
            RefreshNextPreview();
            UpdateUi();
            return false;
        }

        LockPiece();
        return false;
    }

    private void LockPiece()
    {
        if (HasAboveBoardCells(currentOrigin, currentCells))
        {
            gameOver = true;
            pieceFalling = false;
            UpdateUi();
            return;
        }

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
        if (tutorialManager != null)
        {
            tutorialManager.ReportPieceLocked();
        }

        SpawnPiece();
        if (IsTutorialRunning)
        {
            PrepareTutorialScenario(tutorialManager.CurrentStep);
        }
    }

    private void StickWaitingPieceToTop()
    {
        if (pieceFalling || currentCells == null)
        {
            return;
        }

        currentOrigin = GetTopStuckOrigin(currentOrigin, currentCells);
    }

    private Vector3Int GetTopStuckOrigin(Vector3Int origin, Vector3Int[] cells)
    {
        origin.y = GetSpawnOriginY(cells);
        return origin;
    }

    private int GetSpawnOriginY(Vector3Int[] cells)
    {
        int minY = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            minY = Mathf.Min(minY, cells[i].y);
        }

        return BoardHeight - minY;
    }

    private bool HasAboveBoardCells(Vector3Int origin, Vector3Int[] cells)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (origin.y + cells[i].y >= BoardHeight)
            {
                return true;
            }
        }

        return false;
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
        if (tutorialManager != null)
        {
            tutorialManager.ReportRowsCleared(clearResult.Rows);
        }
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
        int difficulty = GameplayDifficultyIndex;

        if (cells == 1)
        {
            switch (difficulty)
            {
                case 0:
                    return Mathf.Max(OneBlockPieceWeight, 4);
                case 2:
                    return Mathf.Max(1, OneBlockPieceWeight);
                default:
                    return OneBlockPieceWeight;
            }
        }

        if (cells == 2)
        {
            switch (difficulty)
            {
                case 0:
                    return Mathf.Max(TwoBlockPieceWeight, 6);
                case 2:
                    return Mathf.Max(1, TwoBlockPieceWeight - 1);
                default:
                    return TwoBlockPieceWeight;
            }
        }

        if (cells == 3)
        {
            switch (difficulty)
            {
                case 0:
                    return Mathf.Max(34, 72 - level * 4);
                case 2:
                    return Mathf.Max(10, 44 - level * 8);
                default:
                    return Mathf.Max(18, 56 - level * 6);
            }
        }

        if (cells == 4)
        {
            switch (difficulty)
            {
                case 0:
                    return Mathf.Clamp(44 + age * 3, 44, 82);
                case 2:
                    return Mathf.Clamp(90 + age * 8, 90, 140);
                default:
                    return Mathf.Clamp(68 + age * 5, 68, 110);
            }
        }

        if (cells == 5)
        {
            switch (difficulty)
            {
                case 0:
                    return Mathf.Clamp(16 + age * 4, 16, 48);
                case 2:
                    return Mathf.Clamp(58 + age * 10, 58, 122);
                default:
                    return Mathf.Clamp(34 + age * 8, 34, 90);
            }
        }

        switch (difficulty)
        {
            case 0:
                return Mathf.Clamp(6 + age * 3, 6, 24);
            case 2:
                return Mathf.Clamp(30 + age * 8, 30, 82);
            default:
                return Mathf.Clamp(16 + age * 6, 16, 55);
        }
    }

    private string GetPieceComplexityLabel()
    {
        int sixBlockLabelLevel = GameplayDifficultyIndex == 0 ? 7 : GameplayDifficultyIndex == 2 ? 4 : 5;
        int fiveBlockLabelLevel = GameplayDifficultyIndex == 0 ? 3 : 2;

        if (level >= sixBlockLabelLevel)
        {
            return "Rare 6-block";
        }

        if (level >= fiveBlockLabelLevel)
        {
            return "Rare 5-block";
        }

        return "Classic";
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

        float alpha = Mathf.Lerp(0.2f, 0.62f, Mathf.PingPong(Time.time * 4f, 1f));
        if (alpha != cachedDeathWarningAlpha)
        {
            cachedDeathWarningAlpha = alpha;
            cachedDeathWarningColor.a = alpha;
            deathWarningMaterial.color = MutedColor(cachedDeathWarningColor);
        }
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

    private void UpdateTutorialTargetMarker()
    {
        if (tutorialTargetMarker == null || tutorialManager == null)
        {
            return;
        }

        bool show = IsTutorialRunning && tutorialManager.CurrentStep == TutorialManager.TutorialStep.MoveHorizontal;
        tutorialTargetMarker.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        int markerY = currentOrigin.y;
        int minX = 0;
        int maxX = 0;
        if (currentCells != null && currentCells.Length > 0)
        {
            minX = currentCells[0].x;
            maxX = currentCells[0].x;
            for (int i = 1; i < currentCells.Length; i++)
            {
                minX = Mathf.Min(minX, currentCells[i].x);
                maxX = Mathf.Max(maxX, currentCells[i].x);
            }
        }

        int widthCells = Mathf.Max(1, maxX - minX + 1);
        int originX = tutorialMoveTargetX + minX;
        Vector3 markerPosition = ViewToLocal(originX, markerY) + GetFaceOutwardNormal(activeFace) * 0.09f;
        if (widthCells > 1)
        {
            Vector3 stepVector = ViewToLocal(originX + 1, markerY) - ViewToLocal(originX, markerY);
            markerPosition += stepVector * (widthCells - 1) * 0.5f;
        }

        tutorialTargetMarker.localPosition = markerPosition;
        tutorialTargetMarker.localScale = IsSideFace(activeFace)
            ? new Vector3(CubeSize * 0.22f, CubeSize * 1.02f, CubeSize * (1.02f * widthCells))
            : new Vector3(CubeSize * (1.02f * widthCells), CubeSize * 1.02f, CubeSize * 0.22f);
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
        cachedAngularVelocity.Set(Random.Range(-280f, 280f), Random.Range(-360f, 360f), Random.Range(-300f, 300f));

        clearEffects.Add(new ClearFragmentEffect(
            fragment,
            fragmentMaterial,
            velocity,
            cachedAngularVelocity,
            Random.Range(ClearEffectDuration * 0.82f, ClearEffectDuration * 1.18f),
            fragmentColor,
            fragmentScale));
    }

    private void UpdateClearEffects()
    {
        float deltaTime = Time.deltaTime;
        float gravity = ClearFragmentGravity;
        float drag = ClearFragmentDrag;
        float dragFactor = Mathf.Exp(-drag * deltaTime);
        cachedGravityVelocity.Set(0f, -gravity * deltaTime, 0f);

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

            effect.Velocity += cachedGravityVelocity;
            effect.Velocity *= dragFactor;
            effect.Transform.position += effect.Velocity * deltaTime;
            effect.Transform.Rotate(effect.AngularVelocity * deltaTime, Space.Self);

            float progress = Mathf.Clamp01(effect.Age / effect.Duration);
            if (effect.Material != null)
            {
                cachedFragmentColor = effect.Color;
                cachedFragmentColor.a = progress < 0.72f ? 1f : Mathf.InverseLerp(1f, 0.72f, progress);
                effect.Material.color = cachedFragmentColor;
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

            if (x < 0 || x >= BoardWidth || y < 0 || z != FrontLayer)
            {
                return false;
            }

            if (y >= BoardHeight)
            {
                continue;
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

    private Color MutedColor(Color color)
    {
        return MutedColor(color, colorGrayMix);
    }

    private static Color MutedColor(Color color, float grayMix)
    {
        float alpha = color.a;
        float gray = color.grayscale;
        Color grayColor = new Color(gray, gray, gray, alpha);
        Color mutedColor = Color.Lerp(color, grayColor, Mathf.Clamp01(grayMix));
        mutedColor.a = alpha;
        return mutedColor;
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
            color = MutedColor(color)
        };

        ApplyMaterialColor(material, color);

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

    private void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        Color mutedColor = MutedColor(color);
        material.color = mutedColor;

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", mutedColor);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", mutedColor);
        }
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

        Color mutedColor = MutedColor(color);
        material.color = mutedColor;

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", mutedColor);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", mutedColor);
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

    private void ApplyGameOverRedBarConfig()
    {
        if (gameOverRedBar == null) return;
        gameOverRedBar.color = MutedColor(gameOverRedBarColor);
        gameOverRedBarPosition.ApplyTo(gameOverRedBar.GetComponent<RectTransform>());
    }

    private void ApplyGameOverPanelConfig()
    {
        if (gameOverPanel == null) return;
        Image panelImage = gameOverPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = gameOverPanelColor;
        }
        gameOverPanelPosition.ApplyTo(gameOverPanel.GetComponent<RectTransform>());
    }

    private void ApplyGameOverTitleConfig()
    {
        if (gameOverTitleText == null) return;
        gameOverTitleText.text = gameOverTitleConfig.text;
        gameOverTitleText.fontSize = gameOverTitleConfig.fontSize;
        gameOverTitleText.fontStyle = gameOverTitleConfig.fontStyle;
        gameOverTitleText.alignment = gameOverTitleConfig.alignment;
        gameOverTitleText.color = gameOverTitleConfig.color;
        gameOverTitleConfig.position.ApplyTo(gameOverTitleText.GetComponent<RectTransform>());
    }

    private void ApplyGameOverHintConfig()
    {
        if (gameOverHintText == null) return;
        gameOverHintText.text = gameOverHintConfig.text;
        gameOverHintText.fontSize = gameOverHintConfig.fontSize;
        gameOverHintText.fontStyle = gameOverHintConfig.fontStyle;
        gameOverHintText.alignment = gameOverHintConfig.alignment;
        gameOverHintText.color = gameOverHintConfig.color;
        gameOverHintConfig.position.ApplyTo(gameOverHintText.GetComponent<RectTransform>());
    }

    private void ApplyGameOverRetryButtonConfig()
    {
        if (restartButton == null) return;
        SetButtonLabel(restartButton, gameOverRetryButtonConfig.label);
        ApplyButtonColor(restartButton, gameOverRetryButtonConfig.color);
        gameOverRetryButtonConfig.position.ApplyTo(restartButton.GetComponent<RectTransform>());
    }

    private void ApplyGameOverBackButtonConfig()
    {
        if (gameOverBackButton == null) return;
        SetButtonLabel(gameOverBackButton, gameOverBackButtonConfig.label);
        ApplyButtonColor(gameOverBackButton, gameOverBackButtonConfig.color);
        gameOverBackButtonConfig.position.ApplyTo(gameOverBackButton.GetComponent<RectTransform>());
    }

    private void ApplyPausePanelConfig()
    {
        if (pausePanel == null) return;
        Image panelImage = pausePanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = pausePanelColor;
        }
        pausePanelPosition.ApplyTo(pausePanel.GetComponent<RectTransform>());
    }

    private void ApplyPauseTextConfig()
    {
        if (pauseText == null) return;
        pauseText.text = pauseTextConfig.text;
        pauseText.fontSize = pauseTextConfig.fontSize;
        pauseText.fontStyle = pauseTextConfig.fontStyle;
        pauseText.alignment = pauseTextConfig.alignment;
        pauseText.color = pauseTextConfig.color;
        pauseTextConfig.position.ApplyTo(pauseText.GetComponent<RectTransform>());
    }

    private void ApplyPauseResumeButtonConfig()
    {
        if (resumeButton == null) return;
        SetButtonLabel(resumeButton, pauseResumeButtonConfig.label);
        ApplyButtonColor(resumeButton, pauseResumeButtonConfig.color);
        pauseResumeButtonConfig.position.ApplyTo(resumeButton.GetComponent<RectTransform>());
    }

    private void ApplyPauseRestartButtonConfig()
    {
        if (pauseRestartButton == null) return;
        SetButtonLabel(pauseRestartButton, pauseRestartButtonConfig.label);
        ApplyButtonColor(pauseRestartButton, pauseRestartButtonConfig.color);
        pauseRestartButtonConfig.position.ApplyTo(pauseRestartButton.GetComponent<RectTransform>());
    }

    private void ApplyPauseSettingsButtonConfig()
    {
        if (pauseSettingsButton == null) return;
        SetButtonLabel(pauseSettingsButton, pauseSettingsButtonConfig.label);
        ApplyButtonColor(pauseSettingsButton, pauseSettingsButtonConfig.color);
        pauseSettingsButtonConfig.position.ApplyTo(pauseSettingsButton.GetComponent<RectTransform>());
    }

    private void ApplyPauseMainMenuButtonConfig()
    {
        if (mainMenuButton == null) return;
        SetButtonLabel(mainMenuButton, pauseMainMenuButtonConfig.label);
        ApplyButtonColor(mainMenuButton, pauseMainMenuButtonConfig.color);
        pauseMainMenuButtonConfig.position.ApplyTo(mainMenuButton.GetComponent<RectTransform>());
    }

    private void ApplySettingsPanelConfig()
    {
        if (settingsPanel == null) return;
        Image panelImage = settingsPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = settingsPanelColor;
        }
        settingsPanelPosition.ApplyTo(settingsPanel.GetComponent<RectTransform>());
    }

    private void ApplyTutorialOverlayConfig()
    {
        if (tutorialOverlayPanel == null) return;
        Image panelImage = tutorialOverlayPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = tutorialOverlayColor;
        }
        tutorialOverlayPosition.ApplyTo(tutorialOverlayPanel.GetComponent<RectTransform>());
    }

    private void ApplyTutorialStepTextConfig()
    {
        if (tutorialStepText == null) return;
        tutorialStepText.text = tutorialStepTextConfig.text;
        tutorialStepText.fontSize = tutorialStepTextConfig.fontSize;
        tutorialStepText.fontStyle = tutorialStepTextConfig.fontStyle;
        tutorialStepText.alignment = tutorialStepTextConfig.alignment;
        tutorialStepText.color = tutorialStepTextConfig.color;
        tutorialStepTextConfig.position.ApplyTo(tutorialStepText.GetComponent<RectTransform>());
    }

    private void ApplyTutorialSkipButtonConfig()
    {
        if (tutorialSkipButton == null) return;
        SetButtonLabel(tutorialSkipButton, tutorialSkipButtonConfig.label);
        ApplyButtonColor(tutorialSkipButton, tutorialSkipButtonConfig.color);
        tutorialSkipButtonConfig.position.ApplyTo(tutorialSkipButton.GetComponent<RectTransform>());
    }

    private void ApplyAllUiConfigs()
    {
        ApplyGameOverRedBarConfig();
        ApplyGameOverPanelConfig();
        ApplyGameOverTitleConfig();
        ApplyGameOverHintConfig();
        ApplyGameOverRetryButtonConfig();
        ApplyGameOverBackButtonConfig();
        ApplyPausePanelConfig();
        ApplyPauseTextConfig();
        ApplyPauseResumeButtonConfig();
        ApplyPauseRestartButtonConfig();
        ApplyPauseSettingsButtonConfig();
        ApplyPauseMainMenuButtonConfig();
        ApplySettingsPanelConfig();
        ApplyTutorialOverlayConfig();
        ApplyTutorialStepTextConfig();
        ApplyTutorialSkipButtonConfig();
    }
}
