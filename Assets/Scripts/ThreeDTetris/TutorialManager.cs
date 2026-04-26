using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialManager : MonoBehaviour
{
    private const string TutorialEnteredKey = "TutorialEntered";
    private const string TutorialPromptHandledKey = "TutorialPromptHandled";
    private const string StartTutorialModeKey = "StartTutorialMode";

    public enum TutorialStep
    {
        None,
        MoveHorizontal,
        RotatePiece,
        PlacePiece,
        RotateContainer,
        ConnectedClear,
        Preview,
        Complete
    }

    public enum TutorialAction
    {
        MoveHorizontal,
        RotatePiece,
        PlacePiece,
        RotateContainer,
        Preview
    }

    [Header("Tutorial Control")]
    [SerializeField] private bool tutorialEnabled = true;
    [SerializeField, Min(0.2f)] private float popupDuration = 1.2f;
    [SerializeField, Min(24)] private int instructionFontSize = 44;
    [SerializeField, Min(0f)] private float stepSwitchDelay = 0.2f;

    private GameObject panel;
    private Text instructionText;
    private Text statusText;
    private GameObject warningPopupPanel;
    private Text warningPopupText;
    private Button skipButton;
    private Action<TutorialStep> onStepEntered;

    private TutorialStep currentStep = TutorialStep.None;
    private bool active;
    private float popupTimer;
    private string popupMessage = string.Empty;
    private Color popupColor = Color.white;
    private bool popupIsWarning;
    private bool waitingContainerTurnEffect;
    private bool stepSwitchPending;
    private TutorialStep pendingStep = TutorialStep.None;
    private float stepSwitchTimer;

    public bool IsActive => active;
    public TutorialStep CurrentStep => currentStep;

    public void ConfigureUi(GameObject tutorialPanel, Text tutorialInstruction, Button tutorialSkipButton)
    {
        panel = tutorialPanel;
        instructionText = tutorialInstruction;
        skipButton = tutorialSkipButton;

        if (instructionText != null)
        {
            instructionText.fontSize = instructionFontSize;
            instructionText.fontStyle = FontStyle.Bold;
            instructionText.alignment = TextAnchor.MiddleLeft;
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipTutorial);
            skipButton.onClick.AddListener(SkipTutorial);

            Text buttonText = skipButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "SKIP TUTORIAL";
            }
        }

        EnsureStatusText();
        EnsureWarningPopup();
        RefreshUiText();
    }

    public void SetStepEnteredCallback(Action<TutorialStep> callback)
    {
        onStepEntered = callback;
    }

    public void StartIfRequested()
    {
        if (!tutorialEnabled || PlayerPrefs.GetInt(StartTutorialModeKey, 0) != 1)
        {
            active = false;
            currentStep = TutorialStep.None;
            RefreshUiText();
            return;
        }

        PlayerPrefs.SetInt(StartTutorialModeKey, 0);
        PlayerPrefs.SetInt(TutorialEnteredKey, 1);
        PlayerPrefs.SetInt(TutorialPromptHandledKey, 1);
        PlayerPrefs.Save();

        active = true;
        currentStep = TutorialStep.MoveHorizontal;
        popupMessage = string.Empty;
        popupTimer = 0f;
        waitingContainerTurnEffect = false;
        onStepEntered?.Invoke(currentStep);
        RefreshUiText();
    }

    public void Tick(float deltaTime, bool hiddenByGameState)
    {
        if (popupTimer > 0f)
        {
            popupTimer -= deltaTime;
            if (popupTimer <= 0f)
            {
                popupTimer = 0f;
                popupMessage = string.Empty;
                popupIsWarning = false;
                RefreshUiText();
            }
        }

        if (stepSwitchPending)
        {
            stepSwitchTimer -= deltaTime;
            if (stepSwitchTimer <= 0f)
            {
                ApplyPendingStepSwitch();
            }
        }

        if (panel != null)
        {
            panel.SetActive(active && !hiddenByGameState);
        }

        if (warningPopupPanel != null && (!active || hiddenByGameState))
        {
            warningPopupPanel.SetActive(false);
        }
    }

    public bool AllowsAction(TutorialAction action)
    {
        if (!active)
        {
            return true;
        }

        switch (currentStep)
        {
            case TutorialStep.MoveHorizontal:
                return action == TutorialAction.MoveHorizontal;
            case TutorialStep.RotatePiece:
                return action == TutorialAction.RotatePiece;
            case TutorialStep.PlacePiece:
                return action == TutorialAction.PlacePiece;
            case TutorialStep.RotateContainer:
                return action == TutorialAction.RotateContainer;
            case TutorialStep.ConnectedClear:
                return action == TutorialAction.PlacePiece;
            case TutorialStep.Preview:
                return action == TutorialAction.Preview;
            default:
                return false;
        }
    }

    public void ReportWrongAction(string message = "Please follow the tutorial")
    {
        if (!active)
        {
            return;
        }

        popupMessage = string.IsNullOrEmpty(message) ? "Not now" : message;
        popupColor = new Color(1f, 0.36f, 0.36f, 1f);
        popupIsWarning = true;
        popupTimer = popupDuration;
        RefreshUiText();
    }

    public void ReportActionSuccess(TutorialAction action)
    {
        if (!active)
        {
            return;
        }

        switch (currentStep)
        {
            case TutorialStep.MoveHorizontal:
                if (action == TutorialAction.MoveHorizontal)
                {
                    AdvanceWithSuccess("Good. Position reached.");
                }
                break;

            case TutorialStep.RotatePiece:
                if (action == TutorialAction.RotatePiece)
                {
                    AdvanceWithSuccess("Good. Piece rotated.");
                }
                break;

            case TutorialStep.RotateContainer:
                if (action == TutorialAction.RotateContainer)
                {
                    waitingContainerTurnEffect = true;
                    popupMessage = "Turning...";
                    popupColor = new Color(0.72f, 0.88f, 1f, 1f);
                    popupIsWarning = false;
                    popupTimer = popupDuration;
                    RefreshUiText();
                }
                break;

            case TutorialStep.Preview:
                if (action == TutorialAction.Preview)
                {
                    AdvanceWithSuccess("Good. Preview shown.");
                }
                break;
        }
    }

    public void ReportPieceLocked()
    {
        if (!active || currentStep != TutorialStep.PlacePiece)
        {
            return;
        }

        AdvanceWithSuccess("Good. Piece placed.");
    }

    public void ReportRowsCleared(int rows)
    {
        if (!active || rows <= 0 || currentStep != TutorialStep.ConnectedClear)
        {
            return;
        }

        AdvanceWithSuccess("Good. Connected clear done.");
    }

    public void ReportContainerTurnCompleted()
    {
        if (!active || currentStep != TutorialStep.RotateContainer || !waitingContainerTurnEffect)
        {
            return;
        }

        waitingContainerTurnEffect = false;
        AdvanceWithSuccess("Good. Container rotated.");
    }

    public void SkipTutorial()
    {
        if (!active)
        {
            return;
        }

        EndTutorial();
    }

    private void AdvanceWithSuccess(string successMessage)
    {
        popupMessage = successMessage;
        popupColor = new Color(0.45f, 1f, 0.63f, 1f);
        popupIsWarning = false;
        popupTimer = popupDuration;

        TutorialStep nextStep;
        switch (currentStep)
        {
            case TutorialStep.MoveHorizontal:
                nextStep = TutorialStep.RotatePiece;
                break;
            case TutorialStep.RotatePiece:
                nextStep = TutorialStep.PlacePiece;
                break;
            case TutorialStep.PlacePiece:
                nextStep = TutorialStep.RotateContainer;
                break;
            case TutorialStep.RotateContainer:
                nextStep = TutorialStep.ConnectedClear;
                break;
            case TutorialStep.ConnectedClear:
                nextStep = TutorialStep.Preview;
                break;
            case TutorialStep.Preview:
                nextStep = TutorialStep.Complete;
                break;
            default:
                nextStep = TutorialStep.Complete;
                break;
        }

        waitingContainerTurnEffect = false;
        stepSwitchPending = true;
        pendingStep = nextStep;
        stepSwitchTimer = stepSwitchDelay;
        RefreshUiText();
    }

    private void ApplyPendingStepSwitch()
    {
        stepSwitchPending = false;
        stepSwitchTimer = 0f;
        currentStep = pendingStep;
        pendingStep = TutorialStep.None;

        if (currentStep == TutorialStep.Complete)
        {
            EndTutorial();
            return;
        }

        onStepEntered?.Invoke(currentStep);
        RefreshUiText();
    }

    private void EndTutorial()
    {
        active = false;
        currentStep = TutorialStep.None;
        waitingContainerTurnEffect = false;
        stepSwitchPending = false;
        pendingStep = TutorialStep.None;
        stepSwitchTimer = 0f;
        popupMessage = string.Empty;
        popupTimer = 0f;
        popupIsWarning = false;
        PlayerPrefs.SetInt(TutorialEnteredKey, 1);
        PlayerPrefs.SetInt(TutorialPromptHandledKey, 1);
        PlayerPrefs.SetInt(StartTutorialModeKey, 0);
        PlayerPrefs.Save();
        RefreshUiText();
    }

    private string GetInstructionText()
    {
        switch (currentStep)
        {
            case TutorialStep.MoveHorizontal:
                return "Step 1/6\nUse A or D.\nMove the block to the green marker.";
            case TutorialStep.RotatePiece:
                return "Step 2/6\nPress W.\nRotate the block once.";
            case TutorialStep.PlacePiece:
                return "Step 3/6\nUse S or Space.\nDrop and place the block.";
            case TutorialStep.RotateContainer:
                return "Step 4/6\nUse Q or E.\nRotate the container once.";
            case TutorialStep.ConnectedClear:
                return "Step 5/6\nUse S or Space.\nDrop this block.\nIt clears with the next face.";
            case TutorialStep.Preview:
                return "Step 6/6\nPress F.\nShow the preview.";
            default:
                return string.Empty;
        }
    }

    private void RefreshUiText()
    {
        if (instructionText != null)
        {
            instructionText.text = active ? GetInstructionText() : string.Empty;
        }

        if (statusText != null)
        {
            bool showStatus = active && popupTimer > 0f && !string.IsNullOrEmpty(popupMessage);
            statusText.gameObject.SetActive(showStatus);
            if (showStatus)
            {
                statusText.text = popupMessage;
                statusText.color = popupColor;
            }
        }

        if (warningPopupPanel != null)
        {
            bool showWarningPopup = active && popupIsWarning && popupTimer > 0f && !string.IsNullOrEmpty(popupMessage);
            warningPopupPanel.SetActive(showWarningPopup);
            if (showWarningPopup && warningPopupText != null)
            {
                warningPopupText.text = popupMessage;
            }
        }

        if (panel != null && !active)
        {
            panel.SetActive(false);
        }
    }

    private void EnsureStatusText()
    {
        if (panel == null || statusText != null)
        {
            return;
        }

        Transform existing = panel.transform.Find("Tutorial Status Text");
        if (existing != null)
        {
            statusText = existing.GetComponent<Text>();
            return;
        }

        GameObject statusObject = new GameObject("Tutorial Status Text", typeof(RectTransform), typeof(Text));
        statusObject.transform.SetParent(panel.transform, false);
        RectTransform rect = statusObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-28f, 24f);
        rect.sizeDelta = new Vector2(-220f, 30f);

        statusText = statusObject.GetComponent<Text>();
        Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtInFont != null)
        {
            statusText.font = builtInFont;
        }

        statusText.fontSize = Mathf.Max(22, instructionFontSize - 10);
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.raycastTarget = false;
        statusText.text = string.Empty;
        statusText.gameObject.SetActive(false);
    }

    private void EnsureWarningPopup()
    {
        if (panel == null || warningPopupPanel != null)
        {
            return;
        }

        Transform root = panel.transform.parent != null ? panel.transform.parent : panel.transform;
        Transform existing = root.Find("Tutorial Warning Popup");
        if (existing != null)
        {
            warningPopupPanel = existing.gameObject;
            warningPopupText = warningPopupPanel.GetComponentInChildren<Text>();
            RectTransform existingRect = warningPopupPanel.GetComponent<RectTransform>();
            if (existingRect != null)
            {
                existingRect.anchorMin = new Vector2(0.5f, 0.5f);
                existingRect.anchorMax = new Vector2(0.5f, 0.5f);
                existingRect.pivot = new Vector2(0.5f, 0.5f);
                existingRect.anchoredPosition = new Vector2(0f, 104f);
                existingRect.sizeDelta = new Vector2(860f, 110f);
            }

            Image existingImage = warningPopupPanel.GetComponent<Image>();
            if (existingImage != null)
            {
                existingImage.color = new Color(0f, 0f, 0f, 0.86f);
                existingImage.raycastTarget = false;
            }

            if (warningPopupText != null)
            {
                warningPopupText.fontSize = Mathf.Max(26, instructionFontSize - 8);
                warningPopupText.fontStyle = FontStyle.Bold;
                warningPopupText.alignment = TextAnchor.MiddleCenter;
                warningPopupText.color = new Color(1f, 0.36f, 0.36f, 1f);
                warningPopupText.horizontalOverflow = HorizontalWrapMode.Wrap;
                warningPopupText.verticalOverflow = VerticalWrapMode.Overflow;
                warningPopupText.raycastTarget = false;
            }

            warningPopupPanel.SetActive(false);
            return;
        }

        GameObject popupObject = new GameObject("Tutorial Warning Popup", typeof(RectTransform), typeof(Image));
        popupObject.transform.SetParent(root, false);
        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = new Vector2(0f, 104f);
        popupRect.sizeDelta = new Vector2(860f, 110f);

        Image popupImage = popupObject.GetComponent<Image>();
        popupImage.color = new Color(0f, 0f, 0f, 0.86f);
        popupImage.raycastTarget = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(popupObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(-20f, -12f);

        warningPopupText = textObject.GetComponent<Text>();
        Font builtInFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtInFont != null)
        {
            warningPopupText.font = builtInFont;
        }

        warningPopupText.fontSize = Mathf.Max(26, instructionFontSize - 8);
        warningPopupText.fontStyle = FontStyle.Bold;
        warningPopupText.alignment = TextAnchor.MiddleCenter;
        warningPopupText.color = new Color(1f, 0.36f, 0.36f, 1f);
        warningPopupText.horizontalOverflow = HorizontalWrapMode.Wrap;
        warningPopupText.verticalOverflow = VerticalWrapMode.Overflow;
        warningPopupText.raycastTarget = false;
        warningPopupText.text = string.Empty;

        warningPopupPanel = popupObject;
        warningPopupPanel.SetActive(false);
    }
}
