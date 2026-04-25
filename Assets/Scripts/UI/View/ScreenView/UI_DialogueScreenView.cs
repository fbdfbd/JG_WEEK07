using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DialogueScreenView : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private GameObject _titlePanel;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _bodyText;
    [SerializeField] private TextMeshProUGUI _effectSummaryText;

    [Header("Sub Panel")]
    [SerializeField] private UI_ChoiceView _choicePanel;
    [SerializeField] private UI_DialogView _dialogPanel;

    [Header("Continue Action")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _skipEventButton;

    public event Action ContinueRequested;
    public event Action EventSkipRequested;
    public event Action<int> ChoiceSelected;

    private readonly List<DialogueLinePresentation> _dialogueLines = new();
    private readonly List<string> _choiceLabels = new();

    private WeekFlowDialogueLogService _dialogueLogService;
    private WeekFlowCutsceneBridgeBase _cutsceneBridge;
    private WeekFlowScreen _currentScreen;
    private RuntimeChildState _currentChildState;
    private RuntimeWeekResult _currentWeekResult;
    private EDialogueLogSource _currentLogSource;
    private string _currentLogTitle = string.Empty;
    private int _currentDialogueIndex;
    private int _lastLoggedDialogueIndex = -1;
    private Coroutine _currentDialogueCutsceneRoutine;

    private void Awake()
    {
        BindDialogPanelEvents();
        BindChoicePanelEvents();
        BindContinueButtonEvent();
        BindSkipEventButtonEvent();
    }

    private void OnDestroy()
    {
        UnbindDialogPanelEvents();
        UnbindChoicePanelEvents();
        UnbindContinueButtonEvent();
        UnbindSkipEventButtonEvent();
    }

    public void ShowWeekFeedback(WeekFeedbackPresentation presentation)
    {
        ResetScreen();
        SetLogContext(EDialogueLogSource.WeekFeedback, presentation.Title);
        SetHeaderTexts(presentation.Title, presentation.SummaryLine, presentation.StatDeltaLine);
        AddWeekFeedbackLines(presentation);
        ShowView();
    }

    public void ShowInteractiveEvent(InteractiveEventPresentation presentation)
    {
        ResetScreen();
        SetLogContext(EDialogueLogSource.EventStep, presentation.Title);
        SetHeaderTexts(presentation.Title, presentation.BodyText, presentation.EffectSummaryLine);
        AddDialogueLines(presentation.DialogueLines);
        AddChoiceLabels(presentation.Choices);
        EnsureFallbackDialogueLine(presentation.BodyText, presentation.EffectSummaryLine);
        ShowView();
    }

    public void ShowInteractiveEventResult(InteractiveEventChoiceResultPresentation presentation)
    {
        ResetScreen();
        SetLogContext(EDialogueLogSource.ChoiceResult, "선택 결과");
        SetHeaderTexts("선택 결과", string.Empty, presentation.EffectSummaryLine);
        AddDialogueLines(presentation.DialogueLines);
        EnsureFallbackDialogueLine(string.Empty, presentation.EffectSummaryLine);
        ShowView();
    }

    public void ShowEnding(EndingPresentation presentation)
    {
        ResetScreen();
        SetLogContext(EDialogueLogSource.Ending, presentation.Title);
        SetHeaderTexts(presentation.Title, presentation.Summary, presentation.ReputationLine);
        AddEndingLines(presentation);
        EnsureFallbackDialogueLine(presentation.ClosingLine, presentation.Summary);
        ShowView();
    }

    public void HideView()
    {
        ResetScreen();
        gameObject.SetActive(false);
    }

    public void SetDialogueLogService(WeekFlowDialogueLogService dialogueLogService)
    {
        _dialogueLogService = dialogueLogService;
    }

    public void SetCutsceneBridge(WeekFlowCutsceneBridgeBase cutsceneBridge)
    {
        _cutsceneBridge = cutsceneBridge;
    }

    public void SetScreenContext(WeekFlowScreen screen, RuntimeChildState childState, RuntimeWeekResult lastWeekResult)
    {
        _currentScreen = screen;
        _currentChildState = childState;
        _currentWeekResult = lastWeekResult;
        RefreshInteractionButtons();
    }

    public System.Collections.IEnumerator PlayCurrentDialogueCutscene()
    {
        StopDialogueCutsceneRoutine(false);

        if (!CanPlayCurrentDialogueCutscene())
        {
            RefreshInteractionButtons();
            yield break;
        }

        yield return PlayCurrentDialogueCutsceneInternal();
    }

    public bool TryAdvance()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return false;
        }

        if (_dialogPanel != null && _dialogPanel.CompleteTypingImmediately())
        {
            RefreshInteractionButtons();
            return true;
        }

        if (TrySkipCurrentDialogueCutscene())
        {
            RefreshInteractionButtons();
            return true;
        }

        if (TryMoveToNextDialogueLine())
        {
            return true;
        }

        if (ShouldShowChoices())
        {
            return false;
        }

        ContinueRequested?.Invoke();
        return true;
    }

    private void BindChoicePanelEvents()
    {
        if (_choicePanel == null)
        {
            return;
        }

        _choicePanel.OnChoiceSelected += HandleChoiceSelected;
    }

    private void BindDialogPanelEvents()
    {
        if (_dialogPanel == null)
        {
            return;
        }

        _dialogPanel.TypingCompleted += HandleDialogTypingCompleted;
    }

    private void UnbindDialogPanelEvents()
    {
        if (_dialogPanel == null)
        {
            return;
        }

        _dialogPanel.TypingCompleted -= HandleDialogTypingCompleted;
    }

    private void UnbindChoicePanelEvents()
    {
        if (_choicePanel == null)
        {
            return;
        }

        _choicePanel.OnChoiceSelected -= HandleChoiceSelected;
    }

    private void BindContinueButtonEvent()
    {
        if (_continueButton == null)
        {
            return;
        }

        _continueButton.onClick.AddListener(HandleContinueButtonClicked);
    }

    private void BindSkipEventButtonEvent()
    {
        if (_skipEventButton == null)
        {
            return;
        }

        _skipEventButton.onClick.AddListener(HandleSkipEventButtonClicked);
    }

    private void UnbindContinueButtonEvent()
    {
        if (_continueButton == null)
        {
            return;
        }

        _continueButton.onClick.RemoveListener(HandleContinueButtonClicked);
    }

    private void UnbindSkipEventButtonEvent()
    {
        if (_skipEventButton == null)
        {
            return;
        }

        _skipEventButton.onClick.RemoveListener(HandleSkipEventButtonClicked);
    }

    private void HandleChoiceSelected(int choiceIndex)
    {
        ChoiceSelected?.Invoke(choiceIndex);
    }

    private void HandleContinueButtonClicked()
    {
        TryAdvance();
    }

    private void HandleSkipEventButtonClicked()
    {
        if (!CanRequestEventSkip())
        {
            return;
        }

        EventSkipRequested?.Invoke();
    }

    private void HandleDialogTypingCompleted()
    {
        RefreshInteractionButtons();
    }

    private bool TryMoveToNextDialogueLine()
    {
        if (_currentDialogueIndex + 1 >= _dialogueLines.Count)
        {
            return false;
        }

        _currentDialogueIndex++;
        RefreshDialogue(true);
        RefreshInteractionButtons();
        return true;
    }

    private void ShowView()
    {
        gameObject.SetActive(true);
        RefreshDialogue(false);
        RefreshInteractionButtons();
    }

    private void ResetScreen()
    {
        StopDialogueCutsceneRoutine(true);
        _dialogueLines.Clear();
        _choiceLabels.Clear();
        _currentDialogueIndex = 0;
        _lastLoggedDialogueIndex = -1;
        _currentLogSource = EDialogueLogSource.EventStep;
        _currentLogTitle = string.Empty;
        _currentScreen = null;
        _currentChildState = null;
        _currentWeekResult = null;

        SetHeaderTexts(string.Empty, string.Empty, string.Empty);

        if (_dialogPanel != null)
        {
            _dialogPanel.gameObject.SetActive(false);
        }

        if (_choicePanel != null)
        {
            _choicePanel.SetChoices(Array.Empty<string>());
            _choicePanel.gameObject.SetActive(false);
        }

        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(false);
        }

        if (_skipEventButton != null)
        {
            _skipEventButton.gameObject.SetActive(false);
        }
    }

    private void AddWeekFeedbackLines(WeekFeedbackPresentation presentation)
    {
        if (presentation.EventLines == null)
        {
            return;
        }

        for (int index = 0; index < presentation.EventLines.Count; index++)
        {
            string line = presentation.EventLines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            _dialogueLines.Add(new DialogueLinePresentation(string.Empty, line));
        }
    }

    private void AddEndingLines(EndingPresentation presentation)
    {
        if (presentation.DetailLines != null)
        {
            for (int index = 0; index < presentation.DetailLines.Count; index++)
            {
                string line = presentation.DetailLines[index];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                _dialogueLines.Add(new DialogueLinePresentation(string.Empty, line));
            }
        }

        if (!string.IsNullOrWhiteSpace(presentation.ClosingLine))
        {
            _dialogueLines.Add(new DialogueLinePresentation(NemoFeedbackResolver.DefaultSpeakerName, presentation.ClosingLine));
        }
    }

    private void AddDialogueLines(IReadOnlyList<DialogueLinePresentation> dialogueLines)
    {
        if (dialogueLines == null)
        {
            return;
        }

        for (int index = 0; index < dialogueLines.Count; index++)
        {
            DialogueLinePresentation line = dialogueLines[index];
            if (!line.HasContent)
            {
                continue;
            }

            _dialogueLines.Add(line);
        }
    }

    private void AddChoiceLabels(IReadOnlyList<InteractiveEventChoicePresentation> choices)
    {
        if (choices == null)
        {
            return;
        }

        for (int index = 0; index < choices.Count; index++)
        {
            InteractiveEventChoicePresentation choice = choices[index];
            if (string.IsNullOrWhiteSpace(choice.Label))
            {
                continue;
            }

            _choiceLabels.Add(choice.Label);
        }
    }

    private void EnsureFallbackDialogueLine(string primaryText, string secondaryText)
    {
        if (_dialogueLines.Count > 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(primaryText))
        {
            _dialogueLines.Add(new DialogueLinePresentation(string.Empty, primaryText));
            return;
        }

        if (!string.IsNullOrWhiteSpace(secondaryText))
        {
            _dialogueLines.Add(new DialogueLinePresentation(string.Empty, secondaryText));
        }
    }

    private void SetHeaderTexts(string title, string body, string effectSummary)
    {
        if (_titlePanel != null)
        {
            _titlePanel.SetActive(!string.IsNullOrWhiteSpace(title));
        }

        if (_titleText != null)
        {
            _titleText.text = title;
        }

        if (_bodyText != null)
        {
            _bodyText.text = body;
            _bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(body));
        }

        if (_effectSummaryText != null)
        {
            _effectSummaryText.text = effectSummary;
            _effectSummaryText.gameObject.SetActive(!string.IsNullOrWhiteSpace(effectSummary));
        }
    }

    private void RefreshDialogue()
    {
        RefreshDialogue(false);
    }

    private void RefreshDialogue(bool shouldPlayCutscene)
    {
        if (_dialogPanel == null)
        {
            return;
        }

        if (_dialogueLines.Count == 0)
        {
            _dialogPanel.gameObject.SetActive(false);
            return;
        }

        DialogueLinePresentation line = _dialogueLines[_currentDialogueIndex];
        _dialogPanel.gameObject.SetActive(true);
        _dialogPanel.SetDialogue(line.SpeakerName, line.Text);
        AppendCurrentDialogueLineToLog(line);

        if (shouldPlayCutscene)
        {
            StartCurrentDialogueCutscene();
        }
    }

    private void RefreshInteractionButtons()
    {
        bool shouldShowChoices = ShouldShowChoices();

        if (_choicePanel != null)
        {
            if (shouldShowChoices)
            {
                _choicePanel.SetChoices(_choiceLabels);
            }
            else
            {
                _choicePanel.SetChoices(Array.Empty<string>());
            }

            _choicePanel.gameObject.SetActive(shouldShowChoices);
        }

        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(!shouldShowChoices);
        }

        if (_skipEventButton != null)
        {
            _skipEventButton.gameObject.SetActive(CanRequestEventSkip());
        }
    }

    private bool ShouldShowChoices()
    {
        if (_choiceLabels.Count == 0)
        {
            return false;
        }

        if (_dialogPanel != null && _dialogPanel.IsTyping)
        {
            return false;
        }

        if (IsBlockingDialogueCutscenePlaying())
        {
            return false;
        }

        return _currentDialogueIndex >= _dialogueLines.Count - 1;
    }

    private bool CanRequestEventSkip()
    {
        if (!WeekFlowEventSkipPolicy.CanSkip(_currentScreen))
        {
            return false;
        }

        if (_dialogPanel != null && _dialogPanel.IsTyping)
        {
            return false;
        }

        return !IsBlockingDialogueCutscenePlaying();
    }

    private void SetLogContext(EDialogueLogSource source, string title)
    {
        _currentLogSource = source;
        _currentLogTitle = title;
    }

    private void AppendCurrentDialogueLineToLog(DialogueLinePresentation line)
    {
        if (_dialogueLogService == null)
        {
            return;
        }

        if (_currentDialogueIndex == _lastLoggedDialogueIndex)
        {
            return;
        }

        _dialogueLogService.Append(new DialogueLogEntry(
            _currentLogSource,
            _currentLogTitle,
            line.SpeakerName,
            line.Text));

        _lastLoggedDialogueIndex = _currentDialogueIndex;
    }

    private void StartCurrentDialogueCutscene()
    {
        StopDialogueCutsceneRoutine(false);

        if (!CanPlayCurrentDialogueCutscene())
        {
            RefreshInteractionButtons();
            return;
        }

        _currentDialogueCutsceneRoutine = StartCoroutine(PlayCurrentDialogueCutsceneRoutine());
    }

    private System.Collections.IEnumerator PlayCurrentDialogueCutsceneInternal()
    {
        yield return PlayCurrentDialogueCutsceneRoutine();
    }

    private System.Collections.IEnumerator PlayCurrentDialogueCutsceneRoutine()
    {
        if (!CanPlayCurrentDialogueCutscene())
        {
            _currentDialogueCutsceneRoutine = null;
            yield break;
        }

        yield return _cutsceneBridge.Play(WeekFlowCutsceneRequest.CreateLineEnter(
            _currentScreen,
            _currentDialogueIndex,
            _currentChildState,
            _currentWeekResult));

        _currentDialogueCutsceneRoutine = null;
        RefreshInteractionButtons();
    }

    private bool CanPlayCurrentDialogueCutscene()
    {
        return _cutsceneBridge != null
            && _currentScreen != null
            && _dialogueLines.Count > 0
            && _currentDialogueIndex >= 0
            && _currentDialogueIndex < _dialogueLines.Count;
    }

    private bool TrySkipCurrentDialogueCutscene()
    {
        if (_cutsceneBridge == null)
        {
            return false;
        }

        return _cutsceneBridge.TrySkip();
    }

    private bool IsBlockingDialogueCutscenePlaying()
    {
        return _cutsceneBridge != null && _cutsceneBridge.IsBlocking;
    }

    private void StopDialogueCutsceneRoutine(bool stopBridge)
    {
        if (_currentDialogueCutsceneRoutine != null)
        {
            StopCoroutine(_currentDialogueCutsceneRoutine);
            _currentDialogueCutsceneRoutine = null;
        }

        if (stopBridge && _cutsceneBridge != null)
        {
            _cutsceneBridge.StopImmediate();
        }
    }
}
