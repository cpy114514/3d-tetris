using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class MainMenuAnimator : MonoBehaviour
{
    private const string AnimatedTitleName = "Animated Block Title";

    [Header("Title")]
    [SerializeField] private Vector2 titleAnchoredPosition = new Vector2(0f, 300f);
    [SerializeField, Min(0.1f)] private float titleScale = 1.8f;
    [SerializeField, Min(0f)] private float loadAnimationDelay = 0.4f;
    [SerializeField, Min(0f)] private float titleFloatAmplitude = 3.5f;
    [SerializeField, Min(0f)] private float titleFloatSpeed = 1.6f;
    [SerializeField] private bool syncExistingTitleColors = true;
    [SerializeField] private Color[] titleColors =
    {
        new Color(0.09f, 0.56f, 0.62f, 1f),
        new Color(0.72f, 0.55f, 0.10f, 1f),
        new Color(0.48f, 0.24f, 0.68f, 1f),
        new Color(0.10f, 0.54f, 0.30f, 1f),
        new Color(0.68f, 0.16f, 0.17f, 1f),
        new Color(0.16f, 0.34f, 0.70f, 1f),
        new Color(0.68f, 0.36f, 0.10f, 1f),
        new Color(0.62f, 0.20f, 0.42f, 1f),
        new Color(0.10f, 0.56f, 0.40f, 1f)
    };

    private static readonly TitleShape[] TitleShapes =
    {
        new TitleShape("I3", 0, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)),
        new TitleShape("I3V", 0, new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)),
        new TitleShape("I2", 0, new Vector2Int(0, 0), new Vector2Int(1, 0)),
        new TitleShape("I2V", 0, new Vector2Int(0, 0), new Vector2Int(0, 1)),
        new TitleShape("T", 2, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)),
        new TitleShape("L", 6, new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2)),
        new TitleShape("J", 5, new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 2)),
        new TitleShape("O", 1, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
        new TitleShape("S", 3, new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
        new TitleShape("Z", 4, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)),
        new TitleShape("V3", 5, new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
        new TitleShape("Dot1", 8, new Vector2Int(0, 0))
    };

    private static readonly int[] TitleColorCycle = { 4, 1, 2, 6, 3, 5, 7, 0, 8, 4, 6, 2, 1, 3 };

    private readonly List<TitlePiece> titlePieces = new List<TitlePiece>();
    private bool built;
    private float titleStartTime;

    private void OnEnable()
    {
        if (Application.isPlaying || !IsStartScene())
        {
            return;
        }

        Build(false);
    }

    private void Start()
    {
        if (!Application.isPlaying || !IsStartScene())
        {
            return;
        }

        StartCoroutine(BuildAfterSceneReady());
    }

    private IEnumerator BuildAfterSceneReady()
    {
        HideOriginalTitle();
        yield return null;

        if (loadAnimationDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(loadAnimationDelay);
        }

        Build(true);
    }

    [ContextMenu("Rebuild Animated Title")]
    private void RebuildAnimatedTitle()
    {
        RectTransform sourceTitle = FindOriginalTitleRect();
        Transform titleParent = sourceTitle != null && sourceTitle.parent != null ? sourceTitle.parent : GetUiRoot();
        if (titleParent == null)
        {
            return;
        }

        Transform existing = titleParent.Find(AnimatedTitleName);
        if (existing != null)
        {
            DestroyObject(existing.gameObject);
        }

        Build(false);
    }

    private void Update()
    {
        if (!Application.isPlaying || !built)
        {
            return;
        }

        float now = Time.unscaledTime;
        for (int i = 0; i < titlePieces.Count; i++)
        {
            TitlePiece piece = titlePieces[i];
            if (piece.RectTransform == null)
            {
                continue;
            }

            float rawT = (now - titleStartTime - piece.Delay) / piece.Duration;
            float t = Mathf.Clamp01(rawT);
            if (rawT < 1f)
            {
                float eased = EaseOutBounce(t);
                piece.RectTransform.anchoredPosition = Vector2.LerpUnclamped(piece.StartPosition, piece.TargetPosition, eased);
                piece.RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(piece.StartRotation, 0f, Mathf.SmoothStep(0f, 1f, t)));
            }
            else
            {
                float wave = Mathf.Sin((now - titleStartTime) * titleFloatSpeed + i * 0.61f) * titleFloatAmplitude;
                float rotationWave = Mathf.Sin((now - titleStartTime) * titleFloatSpeed * 0.72f + i * 0.43f) * 1.4f;
                piece.RectTransform.anchoredPosition = piece.TargetPosition + new Vector2(0f, wave);
                piece.RectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationWave);
            }

            for (int imageIndex = 0; imageIndex < piece.Images.Count; imageIndex++)
            {
                Image image = piece.Images[imageIndex];
                if (image == null)
                {
                    continue;
                }

                Color color = image.color;
                color.a = Mathf.SmoothStep(0f, 1f, t);
                image.color = color;
            }
        }
    }

    private void Build(bool animate)
    {
        RectTransform sourceTitle = FindOriginalTitleRect();
        Transform titleParent = sourceTitle != null && sourceTitle.parent != null ? sourceTitle.parent : GetUiRoot();
        if (titleParent == null)
        {
            return;
        }

        HideOriginalTitle();
        BuildBlockTitle(titleParent, sourceTitle, animate);

        if (animate)
        {
            Transform uiRoot = GetUiRoot();
            AttachButtonAnimations(uiRoot != null ? uiRoot : titleParent);
        }

        built = animate;
    }

    private Transform GetUiRoot()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            return canvas.transform;
        }

        canvas = FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private void HideOriginalTitle()
    {
        RectTransform titleRect = FindOriginalTitleRect();
        if (titleRect == null)
        {
            return;
        }

        CanvasGroup group = titleRect.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = titleRect.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    private RectTransform FindOriginalTitleRect()
    {
        GameObject titleObject = GameObject.Find("Title");
        return titleObject != null ? titleObject.GetComponent<RectTransform>() : null;
    }

    private static bool IsStartScene()
    {
        return SceneManager.GetActiveScene().name == "Start";
    }

    private void BuildBlockTitle(Transform parent, RectTransform sourceTitle, bool animate)
    {
        Transform existing = parent.Find(AnimatedTitleName);
        if (existing != null)
        {
            if (!animate)
            {
                if (syncExistingTitleColors)
                {
                    ApplyTitleColorsToExisting(existing);
                }

                return;
            }

            DestroyObject(existing.gameObject);
        }

        GameObject root = new GameObject(AnimatedTitleName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();

        if (sourceTitle != null)
        {
            rootRect.anchorMin = sourceTitle.anchorMin;
            rootRect.anchorMax = sourceTitle.anchorMax;
            rootRect.pivot = sourceTitle.pivot;
            rootRect.anchoredPosition = sourceTitle.anchoredPosition;
            rootRect.sizeDelta = sourceTitle.sizeDelta;
            root.transform.SetSiblingIndex(Mathf.Min(sourceTitle.GetSiblingIndex() + 1, parent.childCount - 1));
        }
        else
        {
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = titleAnchoredPosition;
            rootRect.sizeDelta = new Vector2(900f, 180f);
            root.transform.SetAsFirstSibling();
        }

        rootRect.localScale = Vector3.one * titleScale;

        titlePieces.Clear();
        string text = "3D TETRIS";
        float tileSize = 18f;
        float gap = 4f;
        float letterGap = 14f;
        float cursor = 0f;
        List<LetterLayout> letters = new List<LetterLayout>();

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character == ' ')
            {
                cursor += tileSize * 1.5f;
                continue;
            }

            string[] pattern = GetPattern(character);
            float letterWidth = pattern[0].Length * (tileSize + gap) - gap;
            letters.Add(new LetterLayout(character, cursor, pattern));
            cursor += letterWidth + letterGap;
        }

        float totalWidth = Mathf.Max(0f, cursor - letterGap);
        float startX = -totalWidth * 0.5f;
        int pieceIndex = 0;
        for (int letterIndex = 0; letterIndex < letters.Count; letterIndex++)
        {
            LetterLayout letter = letters[letterIndex];
            bool[,] occupied = BuildLetterOccupancy(letter.Pattern);
            bool[,] used = new bool[occupied.GetLength(0), occupied.GetLength(1)];
            for (int y = 0; y < occupied.GetLength(1); y++)
            {
                for (int x = 0; x < occupied.GetLength(0); x++)
                {
                    if (!occupied[x, y] || used[x, y])
                    {
                        continue;
                    }

                    TitleShape shape = FindShapeForCell(occupied, used, x, y, letterIndex + pieceIndex);
                    CreateTitlePiece(root.transform, shape, used, x, y, startX + letter.OffsetX, tileSize, gap, pieceIndex, letterIndex, animate);
                    pieceIndex++;
                }
            }
        }

        titleStartTime = Time.unscaledTime + 0.12f;
    }

    private static void DestroyObject(GameObject target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

        DestroyImmediate(target);
    }

    private void CreateTitlePiece(Transform parent, TitleShape shape, bool[,] used, int originX, int originY, float letterStartX, float tileSize, float gap, int pieceIndex, int letterIndex, bool animate)
    {
        GameObject pieceObject = new GameObject("Title " + shape.Name + " Piece", typeof(RectTransform));
        pieceObject.transform.SetParent(parent, false);
        RectTransform pieceRect = pieceObject.GetComponent<RectTransform>();
        pieceRect.anchorMin = new Vector2(0.5f, 0.5f);
        pieceRect.anchorMax = new Vector2(0.5f, 0.5f);
        pieceRect.pivot = new Vector2(0.5f, 0.5f);
        pieceRect.sizeDelta = Vector2.zero;

        Vector2 target = GridToTitlePosition(letterStartX, originX, originY, tileSize, gap);
        Vector2 startPosition = animate ? target + new Vector2(0f, 580f + (pieceIndex % 7) * 26f) : target;
        float startRotation = animate ? -34f + (pieceIndex % 9) * 8f : 0f;
        pieceRect.anchoredPosition = startPosition;
        pieceRect.localRotation = Quaternion.Euler(0f, 0f, startRotation);

        int colorIndex = TitleColorCycle[Mathf.Abs(pieceIndex + letterIndex * 3) % TitleColorCycle.Length];
        Color color = MutedTitleColor(GetTitleColor(colorIndex));
        List<Image> images = new List<Image>(shape.Cells.Length);
        for (int i = 0; i < shape.Cells.Length; i++)
        {
            Vector2Int cell = shape.Cells[i];
            int x = originX + cell.x;
            int y = originY + cell.y;
            used[x, y] = true;

            GameObject blockObject = new GameObject("Block", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blockObject.transform.SetParent(pieceObject.transform, false);
            RectTransform blockRect = blockObject.GetComponent<RectTransform>();
            blockRect.anchorMin = new Vector2(0.5f, 0.5f);
            blockRect.anchorMax = new Vector2(0.5f, 0.5f);
            blockRect.pivot = new Vector2(0.5f, 0.5f);
            blockRect.sizeDelta = new Vector2(tileSize, tileSize);
            blockRect.anchoredPosition = GridToTitlePosition(letterStartX, x, y, tileSize, gap) - target;

            Image image = blockObject.GetComponent<Image>();
            color.a = animate ? 0f : 1f;
            image.color = color;
            image.raycastTarget = false;
            images.Add(image);
        }

        if (animate)
        {
            titlePieces.Add(new TitlePiece(pieceRect, images, target, pieceRect.anchoredPosition, pieceRect.localEulerAngles.z, pieceIndex * 0.045f, 0.54f));
        }
    }

    private static Vector2 GridToTitlePosition(float letterStartX, int x, int y, float tileSize, float gap)
    {
        return new Vector2(letterStartX + x * (tileSize + gap), 54f - y * (tileSize + gap));
    }

    private static bool[,] BuildLetterOccupancy(string[] pattern)
    {
        int width = pattern[0].Length;
        int height = pattern.Length;
        bool[,] occupied = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                occupied[x, y] = pattern[y][x] == '1';
            }
        }

        return occupied;
    }

    private static TitleShape FindShapeForCell(bool[,] occupied, bool[,] used, int originX, int originY, int seed)
    {
        int nonSingleShapeCount = TitleShapes.Length - 1;
        int startIndex = Mathf.Abs(seed) % nonSingleShapeCount;
        for (int pass = 0; pass < nonSingleShapeCount; pass++)
        {
            TitleShape shape = TitleShapes[(startIndex + pass) % nonSingleShapeCount];
            if (CanPlaceShape(shape, occupied, used, originX, originY))
            {
                return shape;
            }
        }

        return TitleShapes[TitleShapes.Length - 1];
    }

    private static bool CanPlaceShape(TitleShape shape, bool[,] occupied, bool[,] used, int originX, int originY)
    {
        int width = occupied.GetLength(0);
        int height = occupied.GetLength(1);
        for (int i = 0; i < shape.Cells.Length; i++)
        {
            int x = originX + shape.Cells[i].x;
            int y = originY + shape.Cells[i].y;
            if (x < 0 || x >= width || y < 0 || y >= height || !occupied[x, y] || used[x, y])
            {
                return false;
            }
        }

        return true;
    }

    private static Color MutedTitleColor(Color color)
    {
        float gray = color.grayscale;
        Color muted = Color.Lerp(color, new Color(gray, gray, gray, color.a), 0.12f);
        muted *= 0.94f;
        muted.a = color.a;
        return muted;
    }

    private Color GetTitleColor(int index)
    {
        if (titleColors == null || titleColors.Length == 0)
        {
            return new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        return titleColors[Mathf.Abs(index) % titleColors.Length];
    }

    private void ApplyTitleColorsToExisting(Transform titleRoot)
    {
        int pieceIndex = 0;
        for (int i = 0; i < titleRoot.childCount; i++)
        {
            Transform piece = titleRoot.GetChild(i);
            if (!piece.name.StartsWith("Title "))
            {
                continue;
            }

            int colorIndex = TitleColorCycle[Mathf.Abs(pieceIndex) % TitleColorCycle.Length];
            Color color = MutedTitleColor(GetTitleColor(colorIndex));
            color.a = 1f;

            Image[] images = piece.GetComponentsInChildren<Image>(true);
            for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
            {
                Image image = images[imageIndex];
                if (image != null && !image.raycastTarget)
                {
                    image.color = color;
                }
            }

            pieceIndex++;
        }
    }

    private void AttachButtonAnimations(Transform root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            UiFloatButton floatButton = button.GetComponent<UiFloatButton>();
            if (floatButton == null)
            {
                floatButton = button.gameObject.AddComponent<UiFloatButton>();
            }

            floatButton.SetDelay(0.45f + i * 0.06f);
        }
    }

    private static string[] GetPattern(char character)
    {
        switch (character)
        {
            case '3':
                return new[] { "111", "001", "111", "001", "111" };
            case 'D':
                return new[] { "110", "101", "101", "101", "110" };
            case 'T':
                return new[] { "111", "010", "010", "010", "010" };
            case 'E':
                return new[] { "111", "100", "111", "100", "111" };
            case 'R':
                return new[] { "110", "101", "110", "101", "101" };
            case 'I':
                return new[] { "1", "1", "1", "1", "1" };
            case 'S':
                return new[] { "111", "100", "111", "001", "111" };
            default:
                return new[] { "111", "101", "101", "101", "111" };
        }
    }

    private static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
        {
            return n1 * t * t;
        }

        if (t < 2f / d1)
        {
            t -= 1.5f / d1;
            return n1 * t * t + 0.75f;
        }

        if (t < 2.5f / d1)
        {
            t -= 2.25f / d1;
            return n1 * t * t + 0.9375f;
        }

        t -= 2.625f / d1;
        return n1 * t * t + 0.984375f;
    }

    private readonly struct LetterLayout
    {
        public readonly char Character;
        public readonly float OffsetX;
        public readonly string[] Pattern;

        public LetterLayout(char character, float offsetX, string[] pattern)
        {
            Character = character;
            OffsetX = offsetX;
            Pattern = pattern;
        }
    }

    private readonly struct TitleShape
    {
        public readonly string Name;
        public readonly int ColorIndex;
        public readonly Vector2Int[] Cells;

        public TitleShape(string name, int colorIndex, params Vector2Int[] cells)
        {
            Name = name;
            ColorIndex = colorIndex;
            Cells = cells;
        }
    }

    private readonly struct TitlePiece
    {
        public readonly RectTransform RectTransform;
        public readonly List<Image> Images;
        public readonly Vector2 TargetPosition;
        public readonly Vector2 StartPosition;
        public readonly float StartRotation;
        public readonly float Delay;
        public readonly float Duration;

        public TitlePiece(RectTransform rectTransform, List<Image> images, Vector2 targetPosition, Vector2 startPosition, float startRotation, float delay, float duration)
        {
            RectTransform = rectTransform;
            Images = images;
            TargetPosition = targetPosition;
            StartPosition = startPosition;
            StartRotation = startRotation;
            Delay = delay;
            Duration = duration;
        }
    }
}
