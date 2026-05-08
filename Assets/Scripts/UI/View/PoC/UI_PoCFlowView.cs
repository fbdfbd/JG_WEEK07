using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class UI_PoCFlowView : PoCViewBase
{
    private enum TextContinueMode
    {
        None,
        Intro,
        Report,
        DayEvent,
        End
    }

    [Header("Panels")]
    [SerializeField] private GameObject _textPanel;
    [SerializeField] private GameObject _choicePanel;
    [SerializeField] private GameObject _nightDialoguePanel;

    [Header("Text Panel")]
    [SerializeField] private TextMeshProUGUI _phaseLabelText;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _bodyText;
    [SerializeField] private TextMeshProUGUI _effectSummaryText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _continueButtonText;

    [Header("Main Choices")]
    [SerializeField] private TextMeshProUGUI _choiceTitleText;
    [SerializeField] private TextMeshProUGUI _choiceBodyText;
    [SerializeField] private Button[] _mainChoiceButtons = Array.Empty<Button>();
    [SerializeField] private TextMeshProUGUI[] _mainChoiceLabelTexts = Array.Empty<TextMeshProUGUI>();
    [SerializeField] private TextMeshProUGUI[] _mainChoiceDescriptionTexts = Array.Empty<TextMeshProUGUI>();
    [SerializeField] private TextMeshProUGUI[] _mainChoiceEffectTexts = Array.Empty<TextMeshProUGUI>();

    [Header("Night Dialogue")]
    [SerializeField] private UI_DialogueScreenView _nightDialogueView;

    [Header("Debug Stats")]
    [SerializeField] private GameObject _statsPanel;
    [SerializeField] private TextMeshProUGUI[] _statLabelTexts = Array.Empty<TextMeshProUGUI>();
    [SerializeField] private TextMeshProUGUI[] _statValueTexts = Array.Empty<TextMeshProUGUI>();

    private readonly List<PoCMainChoiceOptionPresentation> _currentMainChoices = new();
    private TextContinueMode _textContinueMode;
    private UnityAction[] _mainChoiceButtonActions = Array.Empty<UnityAction>();

#if UNITY_EDITOR
    private bool _editorFallbackBuildQueued;
#endif

    private void Awake()
    {
        EnsureRuntimeFallbackUi();
        BindContinueButton();
        BindMainChoiceButtons();
        BindNightDialogueView();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || _editorFallbackBuildQueued)
        {
            return;
        }

        if (_textPanel != null && _choicePanel != null && _nightDialogueView != null)
        {
            return;
        }

        _editorFallbackBuildQueued = true;
        UnityEditor.EditorApplication.delayCall += BuildOrRepairPlaceholderUiInEditor;
    }

    [ContextMenu("Build/Repair Placeholder UI")]
    public void BuildOrRepairPlaceholderUiInEditor()
    {
        _editorFallbackBuildQueued = false;

        if (this == null || Application.isPlaying)
        {
            return;
        }

        EnsureRuntimeFallbackUi();
        UnityEditor.EditorUtility.SetDirty(this);

        if (gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif

    private void OnDestroy()
    {
        UnbindContinueButton();
        UnbindMainChoiceButtons();
        UnbindNightDialogueView();
    }

    public override void ShowIntro(PoCTextPresentation presentation)
    {
        ShowText("Intro", presentation.Title, presentation.Body, string.Empty, "Continue", TextContinueMode.Intro);
    }

    public override void ShowReport(PoCTextPresentation presentation)
    {
        ShowText("Report", presentation.Title, presentation.Body, string.Empty, "Review", TextContinueMode.Report);
    }

    public override void ShowMainChoice(PoCMainChoicePresentation presentation)
    {
        CloseFlowPanels();
        SetActive(_choicePanel, true);

        SetText(_choiceTitleText, presentation.Title);
        SetText(_choiceBodyText, presentation.Body);

        _currentMainChoices.Clear();
        if (presentation.Options != null)
        {
            _currentMainChoices.AddRange(presentation.Options);
        }

        RefreshMainChoiceButtons();
    }

    public override void ShowDayEvent(PoCDayEventPresentation presentation)
    {
        ShowText("Day Result", presentation.Title, presentation.Body, presentation.EffectSummary, "Continue", TextContinueMode.DayEvent);
    }

    public override void ShowNightDialogue(InteractiveEventPresentation presentation)
    {
        ShowNightPanel();

        if (_nightDialogueView != null)
        {
            _nightDialogueView.ShowInteractiveEvent(presentation);
        }
    }

    public override void ShowNightChoiceResult(InteractiveEventChoiceResultPresentation presentation)
    {
        ShowNightPanel();

        if (_nightDialogueView != null)
        {
            _nightDialogueView.ShowInteractiveEventResult(presentation);
        }
    }

    public override void ShowEnd(PoCTextPresentation presentation)
    {
        string title = string.IsNullOrWhiteSpace(presentation.Title) ? "End" : presentation.Title;
        string body = string.IsNullOrWhiteSpace(presentation.Body) ? "Observation finished." : presentation.Body;
        ShowText("End", title, body, string.Empty, "Close", TextContinueMode.End);
    }

    public override void RenderPublicStats(IReadOnlyList<PoCStatPresentation> stats)
    {
        if (_statsPanel != null)
        {
            _statsPanel.SetActive(stats != null && stats.Count > 0);
        }

        int statCount = stats?.Count ?? 0;
        int labelCount = _statLabelTexts?.Length ?? 0;
        int valueCount = _statValueTexts?.Length ?? 0;
        int count = Mathf.Max(labelCount, valueCount);

        for (int index = 0; index < count; index++)
        {
            bool hasStat = index < statCount;
            if (index < labelCount)
            {
                SetText(_statLabelTexts[index], hasStat ? stats[index].Label : string.Empty);
                SetActive(_statLabelTexts[index]?.gameObject, hasStat);
            }

            if (index < valueCount)
            {
                SetText(_statValueTexts[index], hasStat ? stats[index].Value.ToString() : string.Empty);
                SetActive(_statValueTexts[index]?.gameObject, hasStat);
            }
        }
    }

    private void ShowText(
        string phaseLabel,
        string title,
        string body,
        string effectSummary,
        string continueLabel,
        TextContinueMode continueMode)
    {
        CloseFlowPanels();
        SetActive(_textPanel, true);

        _textContinueMode = continueMode;
        SetText(_phaseLabelText, phaseLabel);
        SetText(_titleText, title);
        SetText(_bodyText, body);
        SetText(_effectSummaryText, effectSummary);
        SetText(_continueButtonText, continueLabel);
        SetActive(_effectSummaryText?.gameObject, !string.IsNullOrWhiteSpace(effectSummary));
        SetActive(_continueButton?.gameObject, true);
    }

    private void ShowNightPanel()
    {
        CloseFlowPanels();
        SetActive(_nightDialoguePanel, true);
        SetActive(_nightDialogueView != null ? _nightDialogueView.gameObject : null, true);
    }

    private void CloseFlowPanels()
    {
        SetActive(_textPanel, false);
        SetActive(_choicePanel, false);
        SetActive(_nightDialoguePanel, false);
    }

    private void RefreshMainChoiceButtons()
    {
        int buttonCount = _mainChoiceButtons?.Length ?? 0;
        for (int index = 0; index < buttonCount; index++)
        {
            bool hasChoice = index < _currentMainChoices.Count;
            Button button = _mainChoiceButtons[index];
            if (button != null)
            {
                button.gameObject.SetActive(hasChoice);
                button.interactable = hasChoice;
            }

            PoCMainChoiceOptionPresentation option = hasChoice
                ? _currentMainChoices[index]
                : default;

            SetIndexedText(_mainChoiceLabelTexts, index, hasChoice ? option.Label : string.Empty);
            SetIndexedText(_mainChoiceDescriptionTexts, index, hasChoice ? option.Description : string.Empty);
            SetIndexedText(_mainChoiceEffectTexts, index, string.Empty);
            SetIndexedActive(_mainChoiceEffectTexts, index, false);
        }
    }

    private void BindContinueButton()
    {
        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(HandleContinueButtonClicked);
        }
    }

    private void UnbindContinueButton()
    {
        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(HandleContinueButtonClicked);
        }
    }

    private void BindMainChoiceButtons()
    {
        if (_mainChoiceButtons == null)
        {
            return;
        }

        _mainChoiceButtonActions = new UnityAction[_mainChoiceButtons.Length];
        for (int index = 0; index < _mainChoiceButtons.Length; index++)
        {
            Button button = _mainChoiceButtons[index];
            if (button == null)
            {
                continue;
            }

            int choiceIndex = index;
            UnityAction action = () => HandleMainChoiceButtonClicked(choiceIndex);
            _mainChoiceButtonActions[index] = action;
            button.onClick.AddListener(action);
        }
    }

    private void UnbindMainChoiceButtons()
    {
        if (_mainChoiceButtons == null)
        {
            return;
        }

        for (int index = 0; index < _mainChoiceButtons.Length; index++)
        {
            Button button = _mainChoiceButtons[index];
            if (button != null &&
                _mainChoiceButtonActions != null &&
                index < _mainChoiceButtonActions.Length &&
                _mainChoiceButtonActions[index] != null)
            {
                button.onClick.RemoveListener(_mainChoiceButtonActions[index]);
            }
        }

        _mainChoiceButtonActions = Array.Empty<UnityAction>();
    }

    private void BindNightDialogueView()
    {
        if (_nightDialogueView == null)
        {
            return;
        }

        _nightDialogueView.ContinueRequested += RaiseNightContinueRequested;
        _nightDialogueView.ChoiceSelected += RaiseNightChoiceSelected;
    }

    private void UnbindNightDialogueView()
    {
        if (_nightDialogueView == null)
        {
            return;
        }

        _nightDialogueView.ContinueRequested -= RaiseNightContinueRequested;
        _nightDialogueView.ChoiceSelected -= RaiseNightChoiceSelected;
    }

    private void HandleContinueButtonClicked()
    {
        switch (_textContinueMode)
        {
            case TextContinueMode.Intro:
                RaiseIntroContinueRequested();
                break;
            case TextContinueMode.Report:
                RaiseReportContinueRequested();
                break;
            case TextContinueMode.DayEvent:
                RaiseDayEventContinueRequested();
                break;
            case TextContinueMode.End:
                RaiseEndContinueRequested();
                break;
        }
    }

    private void HandleMainChoiceButtonClicked(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= _currentMainChoices.Count)
        {
            return;
        }

        RaiseMainChoiceSelected(_currentMainChoices[choiceIndex].Id);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static void SetIndexedText(IReadOnlyList<TextMeshProUGUI> texts, int index, string value)
    {
        if (texts != null && index >= 0 && index < texts.Count)
        {
            SetText(texts[index], value);
        }
    }

    private static void SetIndexedActive(IReadOnlyList<TextMeshProUGUI> texts, int index, bool active)
    {
        if (texts != null && index >= 0 && index < texts.Count && texts[index] != null)
        {
            texts[index].gameObject.SetActive(active);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void EnsureRuntimeFallbackUi()
    {
        if (_textPanel != null && _choicePanel != null && _nightDialogueView != null)
        {
            return;
        }

        GameObject canvasObject = new("RuntimeFallbackCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.AddComponent<RectTransform>();
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect, 0, 0, 0, 0);

        _textPanel = CreateRuntimePanel("TextPanel", canvasObject.transform, new Color(0.08f, 0.08f, 0.1f, 0.88f));
        Stretch(_textPanel.GetComponent<RectTransform>(), 160, 120, 160, 120);
        _phaseLabelText = CreateRuntimeText("PhaseLabelText", _textPanel.transform, "PHASE", 24, TextAlignmentOptions.Left);
        Place(_phaseLabelText.rectTransform, 30, -34, 520, 50, Anchor.TopLeft);
        _titleText = CreateRuntimeText("TitleText", _textPanel.transform, "Title", 42, TextAlignmentOptions.Left);
        Place(_titleText.rectTransform, 30, -92, 1260, 70, Anchor.TopLeft);
        _bodyText = CreateRuntimeText("BodyText", _textPanel.transform, "Body", 30, TextAlignmentOptions.TopLeft);
        Place(_bodyText.rectTransform, 30, -180, 1260, 520, Anchor.TopLeft);
        _effectSummaryText = CreateRuntimeText("EffectSummaryText", _textPanel.transform, "Effect", 22, TextAlignmentOptions.Left);
        Place(_effectSummaryText.rectTransform, 30, 92, 900, 60, Anchor.BottomLeft);
        _continueButton = CreateRuntimeButton("ContinueButton", _textPanel.transform, "Continue", out _continueButtonText);
        Place(_continueButton.GetComponent<RectTransform>(), -260, 36, 220, 70, Anchor.BottomRight);

        _choicePanel = CreateRuntimePanel("ChoicePanel", canvasObject.transform, new Color(0.08f, 0.08f, 0.1f, 0.88f));
        Stretch(_choicePanel.GetComponent<RectTransform>(), 120, 90, 120, 90);
        _choiceTitleText = CreateRuntimeText("ChoiceTitleText", _choicePanel.transform, "Choice Title", 40, TextAlignmentOptions.Left);
        Place(_choiceTitleText.rectTransform, 40, -44, 1280, 70, Anchor.TopLeft);
        _choiceBodyText = CreateRuntimeText("ChoiceBodyText", _choicePanel.transform, "Choice Body", 26, TextAlignmentOptions.TopLeft);
        Place(_choiceBodyText.rectTransform, 40, -120, 1280, 110, Anchor.TopLeft);

        _mainChoiceButtons = new Button[3];
        _mainChoiceLabelTexts = new TextMeshProUGUI[3];
        _mainChoiceDescriptionTexts = new TextMeshProUGUI[3];
        _mainChoiceEffectTexts = new TextMeshProUGUI[3];

        for (int index = 0; index < 3; index++)
        {
            GameObject card = CreateRuntimePanel($"Choice{index + 1}", _choicePanel.transform, new Color(0.18f, 0.18f, 0.22f, 0.94f));
            Place(card.GetComponent<RectTransform>(), 40 + index * 470, -300, 420, 430, Anchor.TopLeft);
            Button button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            _mainChoiceButtons[index] = button;
            _mainChoiceLabelTexts[index] = CreateRuntimeText("LabelText", card.transform, "Choice", 34, TextAlignmentOptions.Center);
            Place(_mainChoiceLabelTexts[index].rectTransform, 24, -28, 372, 62, Anchor.TopLeft);
            _mainChoiceDescriptionTexts[index] = CreateRuntimeText("DescriptionText", card.transform, "Description", 24, TextAlignmentOptions.TopLeft);
            Place(_mainChoiceDescriptionTexts[index].rectTransform, 28, -118, 364, 220, Anchor.TopLeft);
            _mainChoiceEffectTexts[index] = CreateRuntimeText("EffectText", card.transform, "Effect", 20, TextAlignmentOptions.BottomLeft);
            Place(_mainChoiceEffectTexts[index].rectTransform, 28, 26, 364, 70, Anchor.BottomLeft);
            _mainChoiceEffectTexts[index].gameObject.SetActive(false);
        }

        _nightDialoguePanel = CreateRuntimePanel("NightDialoguePanel", canvasObject.transform, new Color(0.04f, 0.04f, 0.05f, 0.2f));
        Stretch(_nightDialoguePanel.GetComponent<RectTransform>(), 0, 0, 0, 0);
        _nightDialogueView = _nightDialoguePanel.AddComponent<UI_DialogueScreenView>();

        GameObject titlePanel = CreateRuntimePanel("NightTitlePanel", _nightDialoguePanel.transform, new Color(0.08f, 0.08f, 0.1f, 0.84f));
        Place(titlePanel.GetComponent<RectTransform>(), 160, -120, 850, 120, Anchor.TopLeft);
        TextMeshProUGUI nightTitle = CreateRuntimeText("NightTitleText", titlePanel.transform, "Night", 34, TextAlignmentOptions.Left);
        Stretch(nightTitle.rectTransform, 24, 16, 24, 16);
        TextMeshProUGUI nightBody = CreateRuntimeText("NightBodyText", _nightDialoguePanel.transform, "Night Body", 24, TextAlignmentOptions.Left);
        Place(nightBody.rectTransform, 160, -245, 1100, 80, Anchor.TopLeft);
        TextMeshProUGUI nightEffect = CreateRuntimeText("NightEffectText", _nightDialoguePanel.transform, "Effect", 22, TextAlignmentOptions.Left);
        Place(nightEffect.rectTransform, 160, -330, 1100, 60, Anchor.TopLeft);

        GameObject dialogBox = CreateRuntimePanel("DialogBox", _nightDialoguePanel.transform, new Color(0.08f, 0.08f, 0.1f, 0.92f));
        Place(dialogBox.GetComponent<RectTransform>(), 260, 90, 1400, 250, Anchor.BottomLeft);
        UI_DialogView dialogView = dialogBox.AddComponent<UI_DialogView>();
        GameObject nameTag = CreateRuntimePanel("NameTag", dialogBox.transform, new Color(0.16f, 0.16f, 0.22f, 1f));
        Place(nameTag.GetComponent<RectTransform>(), 30, 190, 260, 58, Anchor.BottomLeft);
        TextMeshProUGUI nameText = CreateRuntimeText("NameText", nameTag.transform, "N-03", 24, TextAlignmentOptions.Center);
        Stretch(nameText.rectTransform, 8, 4, 8, 4);
        TextMeshProUGUI dialogueText = CreateRuntimeText("DialogueText", dialogBox.transform, "Dialogue", 30, TextAlignmentOptions.TopLeft);
        Stretch(dialogueText.rectTransform, 44, 70, 44, 32);
        Button nightContinue = CreateRuntimeButton("NightContinueButton", _nightDialoguePanel.transform, "Continue", out _);
        Place(nightContinue.GetComponent<RectTransform>(), -380, 115, 240, 70, Anchor.BottomRight);

        AssignRuntimeDialogueView(_nightDialogueView, titlePanel, nightTitle, nightBody, nightEffect, dialogView, nightContinue);
        AssignRuntimeDialogView(dialogView, nameTag, nameText, dialogueText);
    }

    private static GameObject CreateRuntimePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new(name);
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TextMeshProUGUI CreateRuntimeText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static Button CreateRuntimeButton(string name, Transform parent, string label, out TextMeshProUGUI labelText)
    {
        GameObject buttonObject = CreateRuntimePanel(name, parent, new Color(0.25f, 0.25f, 0.3f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        labelText = CreateRuntimeText("Text", buttonObject.transform, label, 24, TextAlignmentOptions.Center);
        Stretch(labelText.rectTransform, 12, 6, 12, 6);
        return button;
    }

    private static void AssignRuntimeDialogueView(
        UI_DialogueScreenView dialogueView,
        GameObject titlePanel,
        TextMeshProUGUI titleText,
        TextMeshProUGUI bodyText,
        TextMeshProUGUI effectText,
        UI_DialogView dialogPanel,
        Button continueButton)
    {
        SetPrivateField(dialogueView, "_titlePanel", titlePanel);
        SetPrivateField(dialogueView, "_titleText", titleText);
        SetPrivateField(dialogueView, "_bodyText", bodyText);
        SetPrivateField(dialogueView, "_effectSummaryText", effectText);
        SetPrivateField(dialogueView, "_dialogPanel", dialogPanel);
        SetPrivateField(dialogueView, "_continueButton", continueButton);
    }

    private static void AssignRuntimeDialogView(
        UI_DialogView dialogView,
        GameObject nameTag,
        TextMeshProUGUI nameText,
        TextMeshProUGUI contentText)
    {
        SetPrivateField(dialogView, "_nameTagPanel", nameTag);
        SetPrivateField(dialogView, "_nameText", nameText);
        SetPrivateField(dialogView, "_mainContentText", contentText);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }

    private enum Anchor
    {
        TopLeft,
        BottomLeft,
        BottomRight
    }

    private static void Stretch(RectTransform rectTransform, float left, float top, float right, float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private static void Place(RectTransform rectTransform, float x, float y, float width, float height, Anchor anchor)
    {
        switch (anchor)
        {
            case Anchor.TopLeft:
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                break;
            case Anchor.BottomRight:
                rectTransform.anchorMin = new Vector2(1, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.pivot = new Vector2(1, 0);
                break;
            default:
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.zero;
                rectTransform.pivot = Vector2.zero;
                break;
        }

        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = new Vector2(width, height);
    }
}
