using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeekFlowController : MonoBehaviour
{
    [Header("Week Data")]
    [SerializeField] private SO_WeekDefinition _weekDefinition;
    [SerializeField] private SO_WeekDefinition[] _weekDefinitions = Array.Empty<SO_WeekDefinition>();
    [SerializeField] private SO_WeekUiTextCatalog _uiTextCatalog;
    [SerializeField] private SO_EndingCatalog _endingCatalog;

    [Header("View Connection")]
    [SerializeField] private WeekFlowViewBase _view;
    [SerializeField] private UI_ChildStateToastManager _toastManager;

    [Header("Test")]
    [SerializeField] private bool _isTest;
    [SerializeField] private bool _useEndingMoodThresholdCorrection;

    private readonly WeekRunner _weekRunner = new();
    private readonly WeekSelectionState _weekSelectionState = new();
    public static readonly WeekSequenceState _weekSequenceState = new();

    public static WeekFlowRuntimeState _runtimeState;
    private WeekUiTextProvider _weekUiText;
    private WeekFlowPresenter _presenter;
    private WeekFlowCommandHandler _commandHandler;
    private WeekFlowNarrativeHandler _narrativeHandler;
    private WeekFlowCinematicDirector _cinematicDirector;
    private readonly WeekFlowAnalyticsTracker _analyticsTracker = new();
    private WeekFlowCutsceneBridgeBase _cutsceneBridge;
    private WeekFlowScreen _currentScreen;
    private bool _isTransitionPlaying;
    private bool _skipEventToastWaitOnce;
    private Func<WeekFlowActionResult> _pendingFlowAction;
    private RuntimeChildState _boundChildState;

    public event Action<SO_WeekDefinition> WeekChanged;
    public event Action<RuntimeChildState> ChildStateSourceChanged;
    public event Action FlowPresentationCompleted;
    public event Action<WeekFlowPresentationContext> FlowPresentationCompletedWithContext;
    public SO_WeekDefinition CurrentWeekDefinition => _weekSequenceState.CurrentWeekDefinition;
    public RuntimeChildState CurrentChildState => _runtimeState?.ChildState;


    protected virtual void Awake()
    {
        ResolveConnectedView();
        BuildWeekFlowObjects();
        GameplayAnalyticsLogger.StartSession(CurrentWeekDefinition);
        BindViewEvents();
        _analyticsTracker.StartTurnDwell(CurrentWeekDefinition, "controller_awake");
        _presenter.RefreshAll();
        _presenter.PublishDefaultNemoFeedback();
    }

    protected virtual void Start()
    {
        if (!ShouldAutoRunCurrentWeekOnStart())
        {
            return;
        }

        RunFlowAction(_commandHandler.RunCurrentWeek);
    }

    protected virtual void OnDestroy()
    {
        GameplayAnalyticsLogger.EndSession();
        UnbindRuntimeStateEvents();
        UnbindViewEvents();
    }

    private void OnApplicationQuit()
    {
        GameplayAnalyticsLogger.EndSession();
    }

    private void ResolveConnectedView()
    {
        if (_view == null)
        {
            _view = GetComponent<WeekFlowViewBase>();
        }

        if (_toastManager == null)
        {
            _toastManager = FindAnyObjectByType<UI_ChildStateToastManager>();
        }
    }

    private void BuildWeekFlowObjects()
    {
        _runtimeState = new WeekFlowRuntimeState();
        _weekUiText = new WeekUiTextProvider(_uiTextCatalog);
        _weekSequenceState.InitializeWeekSequence(_weekDefinition, _weekDefinitions);
        _weekSelectionState.ApplyWeekEntries(WeekFlowQueryUtility.GetCurrentWeekEntries(_weekSequenceState.CurrentWeekDefinition));
        _presenter = new WeekFlowPresenter(_view, _runtimeState, _weekUiText, _weekSelectionState, _weekSequenceState);
        _commandHandler = new WeekFlowCommandHandler(
            _runtimeState,
            _weekUiText,
            _weekRunner,
            _weekSelectionState,
            _weekSequenceState,
            _endingCatalog,
            _isTest,
            _useEndingMoodThresholdCorrection);
        _narrativeHandler = new WeekFlowNarrativeHandler(
            _runtimeState,
            _weekUiText,
            _weekSelectionState,
            _weekSequenceState,
            _endingCatalog,
            _isTest,
            _useEndingMoodThresholdCorrection);
        _cinematicDirector = new WeekFlowCinematicDirector(_view, new WeekFlowCinematicResolver());
        _cutsceneBridge = _view != null ? _view.GetCutsceneBridge() : null;
        BindRuntimeStateEvents();
    }

    private void BindViewEvents()
    {
        if (_view == null)
        {
            return;
        }

        _view.RunWeekRequested += HandleRunWeekRequested;
        _view.ResetSelectionsRequested += HandleResetSelectionsRequested;
        _view.ResetChildStateRequested += HandleResetChildStateRequested;
        _view.CardOptionSelected += HandleCardOptionSelected;
        _view.AllCardSemanticSelected += HandleAllCardSemanticSelected;
        _view.WeekFeedbackClosed += HandleWeekFeedbackClosed;
        _view.InteractiveEventContinueRequested += HandleInteractiveEventContinueRequested;
        _view.InteractiveEventSkipRequested += HandleInteractiveEventSkipRequested;
        _view.InteractiveEventChoiceSelected += HandleInteractiveEventChoiceSelected;
        _view.WeeklyResultLogContinueRequested += HandleWeeklyResultLogContinueRequested;
        _view.WeeklyStatResultContinueRequested += HandleWeeklyStatResultContinueRequested;
    }

    private void UnbindViewEvents()
    {
        if (_view == null)
        {
            return;
        }

        _view.RunWeekRequested -= HandleRunWeekRequested;
        _view.ResetSelectionsRequested -= HandleResetSelectionsRequested;
        _view.ResetChildStateRequested -= HandleResetChildStateRequested;
        _view.CardOptionSelected -= HandleCardOptionSelected;
        _view.AllCardSemanticSelected -= HandleAllCardSemanticSelected;
        _view.WeekFeedbackClosed -= HandleWeekFeedbackClosed;
        _view.InteractiveEventContinueRequested -= HandleInteractiveEventContinueRequested;
        _view.InteractiveEventSkipRequested -= HandleInteractiveEventSkipRequested;
        _view.InteractiveEventChoiceSelected -= HandleInteractiveEventChoiceSelected;
        _view.WeeklyResultLogContinueRequested -= HandleWeeklyResultLogContinueRequested;
        _view.WeeklyStatResultContinueRequested -= HandleWeeklyStatResultContinueRequested;
    }

    private void HandleRunWeekRequested()
    {
        GameplayAnalyticsLogger.LogNemoPreTurnDialogCount(CurrentWeekDefinition, "run_week_requested");
        _analyticsTracker.EndTurnDwell(CurrentWeekDefinition, "run_week_requested");
        RunUserFlowAction(_commandHandler.RunCurrentWeek);
    }
    private void HandleResetSelectionsRequested() => RunUserFlowAction(_commandHandler.ResetSelections);
    private void HandleResetChildStateRequested() => RunUserFlowAction(_commandHandler.ResetChildState);
    private void HandleWeekFeedbackClosed() => RunUserFlowAction(_narrativeHandler.CloseWeekFeedback);
    private void HandleInteractiveEventContinueRequested() => RunUserFlowAction(_narrativeHandler.ContinueInteractiveEvent);
    private void HandleInteractiveEventSkipRequested() => RunFlowAction(SkipCurrentInteractiveEventWithoutToastWait, true);
    private void HandleWeeklyResultLogContinueRequested() => RunUserFlowAction(_narrativeHandler.ContinueWeeklyResultLog);
    private void HandleWeeklyStatResultContinueRequested() => RunUserFlowAction(_narrativeHandler.ContinueWeeklyStatResult);
    private void HandleCardOptionSelected(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        GameplayAnalyticsLogger.LogCardOptionClicked(CurrentWeekDefinition, cardDefinition, optionIndex);
        RunUserFlowAction(() => _commandHandler.SelectCardOption(cardDefinition, optionIndex));
    }
    private void HandleAllCardSemanticSelected(ECardOptionSemantic semantic)
    {
        RunUserFlowAction(() => _commandHandler.SelectAllCardOptionsBySemantic(semantic));
    }
    private void HandleInteractiveEventChoiceSelected(int choiceIndex) => RunUserFlowAction(() => _narrativeHandler.SelectInteractiveEventChoice(choiceIndex));

    private WeekFlowActionResult SkipCurrentInteractiveEventWithoutToastWait()
    {
        AppendRemainingSkippedEventDialogueLog();

        WeekFlowActionResult result = _narrativeHandler.SkipCurrentInteractiveEvent();
        if (result.ShouldRefreshUi)
        {
            _skipEventToastWaitOnce = true;
        }

        return result;
    }

    private void AppendRemainingSkippedEventDialogueLog()
    {
        if (_view == null || _runtimeState?.CurrentEventSession == null)
        {
            return;
        }

        RuntimeInteractiveEventSession eventSession = _runtimeState.CurrentEventSession;
        if (!WeekFlowEventSkipPolicy.CanSkip(eventSession, _weekSequenceState.CurrentWeekDefinition))
        {
            return;
        }

        List<DialogueLogEntry> entries = BuildRemainingSkippedEventDialogueLogEntries(eventSession);
        if (entries.Count == 0)
        {
            return;
        }

        _view.AppendDialogueLogEntries(entries);
    }

    private List<DialogueLogEntry> BuildRemainingSkippedEventDialogueLogEntries(
        RuntimeInteractiveEventSession eventSession)
    {
        List<DialogueLogEntry> entries = new();
        HashSet<SO_InteractiveEventStepDefinition> visitedSteps = new();
        SO_InteractiveEventStepDefinition step = ResolveNextSkippedStep(eventSession.CurrentStep);

        while (step != null && visitedSteps.Add(step))
        {
            RuntimeInteractiveEventSession previewSession = new(eventSession.EventDefinition, step);
            InteractiveEventPresentation presentation = WeekNarrativeResolver.CreatePresentation(
                previewSession,
                _runtimeState.ChildState,
                _weekUiText);

            AddSkippedEventPresentationLogEntries(entries, presentation);

            if (step.Choices != null && step.Choices.Length > 0)
            {
                break;
            }

            step = ResolveNextSkippedStep(step);
        }

        return entries;
    }

    private void AddSkippedEventPresentationLogEntries(
        List<DialogueLogEntry> entries,
        InteractiveEventPresentation presentation)
    {
        if (presentation.DialogueLines != null && presentation.DialogueLines.Count > 0)
        {
            for (int index = 0; index < presentation.DialogueLines.Count; index++)
            {
                DialogueLinePresentation line = presentation.DialogueLines[index];
                if (!line.HasContent)
                {
                    continue;
                }

                entries.Add(new DialogueLogEntry(
                    EDialogueLogSource.EventStep,
                    presentation.Title,
                    line.SpeakerName,
                    line.Text));
            }

            return;
        }

        string fallbackText = !string.IsNullOrWhiteSpace(presentation.BodyText)
            ? presentation.BodyText
            : presentation.EffectSummaryLine;
        if (string.IsNullOrWhiteSpace(fallbackText))
        {
            return;
        }

        entries.Add(new DialogueLogEntry(
            EDialogueLogSource.EventStep,
            presentation.Title,
            string.Empty,
            fallbackText));
    }

    private SO_InteractiveEventStepDefinition ResolveNextSkippedStep(SO_InteractiveEventStepDefinition step)
    {
        if (step == null)
        {
            return null;
        }

        if (step.ConditionalNext != null &&
            step.ConditionalNext.TryResolve(_runtimeState.ChildState, out SO_InteractiveEventStepDefinition conditionalStep))
        {
            return conditionalStep;
        }

        return step.NextStep;
    }

    private void BindRuntimeStateEvents()
    {
        if (_runtimeState == null)
        {
            return;
        }

        _runtimeState.ChildStateReplaced += HandleChildStateReplaced;
        BindChildState(_runtimeState.ChildState);
    }

    private void UnbindRuntimeStateEvents()
    {
        if (_runtimeState == null)
        {
            return;
        }

        _runtimeState.ChildStateReplaced -= HandleChildStateReplaced;
        BindChildState(null);
    }

    private void BindChildState(RuntimeChildState childState)
    {
        if (ReferenceEquals(_boundChildState, childState))
        {
            return;
        }

        if (_boundChildState != null)
        {
            _boundChildState.StatChanged -= HandleStatChanged;
            _boundChildState.FlagChanged -= HandleFlagChanged;
        }

        _boundChildState = childState;

        if (_boundChildState != null)
        {
            _boundChildState.StatChanged += HandleStatChanged;
            _boundChildState.FlagChanged += HandleFlagChanged;
        }

        ChildStateSourceChanged?.Invoke(_boundChildState);
    }

    private void HandleChildStateReplaced(RuntimeChildState childState)
    {
        BindChildState(childState);
        _presenter?.PublishChildState();
    }

    private void HandleStatChanged(StatChangeInfo changeInfo)
    {
        GameplayAnalyticsLogger.LogStatChanged(CurrentWeekDefinition, changeInfo);
        _presenter?.PublishChildState();
    }

    private void HandleFlagChanged(FlagChangeInfo _)
    {
        _presenter?.PublishChildState();
    }

    private bool ShouldAutoRunCurrentWeekOnStart()
    {
        SO_WeekDefinition currentWeek = _weekSequenceState.CurrentWeekDefinition;
        if (currentWeek == null)
        {
            return false;
        }

        return string.Equals(currentWeek.Id, "week_000", StringComparison.OrdinalIgnoreCase);
    }

    private void RunUserFlowAction(Func<WeekFlowActionResult> action)
    {
        RunFlowAction(action, false);
    }

    private void RunFlowAction(Func<WeekFlowActionResult> action)
    {
        RunFlowAction(action, true);
    }

    private void RunFlowAction(Func<WeekFlowActionResult> action, bool queueWhenTransitionPlaying)
    {
        Debug.Log(
            $"[WeeklyStatDebug] Controller.RunFlowAction begin " +
            $"transitionPlaying={_isTransitionPlaying} " +
            $"currentScreen={FormatScreenType(_currentScreen)} " +
            $"currentWeek={FormatWeekId(_weekSequenceState.CurrentWeekDefinition)} " +
            $"action={action?.Method.Name ?? "null"}");

        if (_isTransitionPlaying)
        {
            if (!queueWhenTransitionPlaying)
            {
                Debug.Log(
                    $"[WeeklyStatDebug] Controller.RunFlowAction ignored during transition " +
                    $"action={action?.Method.Name ?? "null"}");
                return;
            }

            _pendingFlowAction = action;
            Debug.Log(
                $"[WeeklyStatDebug] Controller.RunFlowAction queued " +
                $"action={action?.Method.Name ?? "null"}");
            return;
        }

        SO_WeekDefinition previousWeek = _weekSequenceState.CurrentWeekDefinition;
        WeekFlowActionResult result = action();
        Debug.Log(
            $"[WeeklyStatDebug] Controller.RunFlowAction result " +
            $"refresh={result.ShouldRefreshUi} " +
            $"replace={result.ShouldReplaceScreen} " +
            $"nextScreen={FormatScreenType(result.NextScreen)} " +
            $"previousWeek={FormatWeekId(previousWeek)} " +
            $"currentWeek={FormatWeekId(_weekSequenceState.CurrentWeekDefinition)}");

        if (!result.ShouldRefreshUi)
        {
            return;
        }

        StartCoroutine(ApplyFlowAction(result, previousWeek, _weekSequenceState.CurrentWeekDefinition));
    }

    private IEnumerator ApplyFlowAction(WeekFlowActionResult result, SO_WeekDefinition previousWeek, SO_WeekDefinition currentWeek)
    {
        _isTransitionPlaying = true;
        bool skipEventToastWait = _skipEventToastWaitOnce;
        _skipEventToastWaitOnce = false;

        WeekFlowScreen previousScreen = _currentScreen;
        WeekFlowScreen nextScreen = result.NextScreen;
        bool shouldShowMainCanvas = result.ShouldReplaceScreen
            ? nextScreen == null
            : previousScreen == null;

        Debug.Log(
            $"[WeeklyStatDebug] Controller.ApplyFlowAction begin " +
            $"replace={result.ShouldReplaceScreen} " +
            $"previousScreen={FormatScreenType(previousScreen)} " +
            $"nextScreen={FormatScreenType(nextScreen)} " +
            $"previousWeek={FormatWeekId(previousWeek)} " +
            $"currentWeek={FormatWeekId(currentWeek)} " +
            $"keepPreviousVisible={ShouldKeepPreviousScreenVisible(previousScreen, nextScreen)}");

        if (result.ShouldReplaceScreen && previousScreen != null)
        {
            if (!skipEventToastWait && ShouldWaitForEventCompletionToasts(previousScreen, nextScreen))
            {
                yield return _toastManager.ShowQueuedToastsAndWait(EChildStateToastFlushMode.Sequential);
            }

            yield return _cinematicDirector.PlayScreenExit(previousScreen);

            if (ShouldExitEventCutscene(previousScreen, nextScreen))
            {
                yield return PlayEventExitCutscene(previousScreen);
            }

            if (!ShouldKeepPreviousScreenVisible(previousScreen, nextScreen))
            {
                _presenter.HideFlowScreens();
            }
        }

        if (previousWeek != currentWeek)
        {
            yield return _cinematicDirector.PlayWeekChangeOut(previousWeek);
        }

        _view?.SetMainCanvasVisible(shouldShowMainCanvas);
        _presenter.RefreshAll();

        if (previousWeek != currentWeek)
        {
            yield return _cinematicDirector.PlayWeekChangeIn(currentWeek);

            if (_view != null)
            {
                yield return _view.PlayWeekEntryIntro(currentWeek);
            }

            WeekChanged?.Invoke(currentWeek);
        }

        if (result.ShouldReplaceScreen)
        {
            _currentScreen = nextScreen;
            if (_currentScreen != null)
            {
                Debug.Log(
                    $"[WeeklyStatDebug] Controller.ApplyFlowAction PresentScreen " +
                    $"screen={FormatScreenType(_currentScreen)} " +
                    $"week={FormatWeekId(_currentScreen.WeekDefinition)}");
                _presenter.PresentScreen(_currentScreen);
                _analyticsTracker.ObservePresentedScreen(
                    _currentScreen,
                    _runtimeState.IsCurrentEventFromDayFlow);
                GameplayAnalyticsLogger.LogEventStepShown(_currentScreen);
                _view?.SetFlowScreenContext(_currentScreen, _runtimeState.ChildState, _runtimeState.LastWeekResult);

                if (ShouldEnterEventCutscene(previousScreen, _currentScreen))
                {
                    yield return PlayEventEnterCutscene(_currentScreen);
                }

                _presenter.PublishNemoFeedback(_currentScreen.NemoFeedback);
                yield return _cinematicDirector.PlayScreenEnter(_currentScreen);
                yield return PlayScreenEnterCutscene(_currentScreen);
                yield return _view.PlayCurrentDialogueCutscene();
            }
        }

        _isTransitionPlaying = false;
        Debug.Log(
            $"[WeeklyStatDebug] Controller.ApplyFlowAction end " +
            $"currentScreen={FormatScreenType(_currentScreen)} " +
            $"pending={(_pendingFlowAction != null)}");
        WeekFlowPresentationContext presentationContext = new(
            previousWeek,
            currentWeek,
            previousScreen,
            nextScreen,
            result.ShouldReplaceScreen);
        _analyticsTracker.ObservePresentationCompleted(presentationContext);
        FlowPresentationCompletedWithContext?.Invoke(presentationContext);
        FlowPresentationCompleted?.Invoke();

        if (_pendingFlowAction != null)
        {
            Func<WeekFlowActionResult> pendingAction = _pendingFlowAction;
            _pendingFlowAction = null;
            RunFlowAction(pendingAction);
        }
    }

    private static string FormatScreenType(WeekFlowScreen screen)
    {
        return screen == null ? "null" : screen.ScreenType.ToString();
    }

    private static string FormatWeekId(SO_WeekDefinition weekDefinition)
    {
        return weekDefinition != null ? weekDefinition.Id : "null";
    }

    private IEnumerator PlayEventEnterCutscene(WeekFlowScreen screen)
    {
        if (_cutsceneBridge == null || screen?.EventDefinition == null)
        {
            yield break;
        }

        yield return _cutsceneBridge.Play(WeekFlowCutsceneRequest.CreateEventEnter(
            screen,
            _runtimeState.ChildState,
            _runtimeState.LastWeekResult));
    }

    private IEnumerator PlayEventExitCutscene(WeekFlowScreen screen)
    {
        if (_cutsceneBridge == null || screen?.EventDefinition == null)
        {
            yield break;
        }

        yield return _cutsceneBridge.Play(WeekFlowCutsceneRequest.CreateEventExit(
            screen,
            _runtimeState.ChildState,
            _runtimeState.LastWeekResult));
    }

    private IEnumerator PlayScreenEnterCutscene(WeekFlowScreen screen)
    {
        if (_cutsceneBridge == null || screen == null)
        {
            yield break;
        }

        yield return _cutsceneBridge.Play(WeekFlowCutsceneRequest.CreateScreenEnter(
            screen,
            _runtimeState.ChildState,
            _runtimeState.LastWeekResult));
    }

    private static bool ShouldKeepPreviousScreenVisible(WeekFlowScreen previousScreen, WeekFlowScreen nextScreen)
    {
        return previousScreen?.ScreenType == EWeekFlowScreenType.WeeklyResultLog
            && nextScreen?.ScreenType == EWeekFlowScreenType.WeeklyStatResult;
    }

    private static bool ShouldEnterEventCutscene(WeekFlowScreen previousScreen, WeekFlowScreen nextScreen)
    {
        return nextScreen?.EventDefinition != null && !IsSameEvent(previousScreen, nextScreen);
    }

    private static bool ShouldExitEventCutscene(WeekFlowScreen previousScreen, WeekFlowScreen nextScreen)
    {
        return previousScreen?.EventDefinition != null && !IsSameEvent(previousScreen, nextScreen);
    }

    private bool ShouldWaitForEventCompletionToasts(WeekFlowScreen previousScreen, WeekFlowScreen nextScreen)
    {
        return _toastManager != null
            && previousScreen?.EventDefinition != null
            && !IsSameEvent(previousScreen, nextScreen);
    }

    private static bool IsSameEvent(WeekFlowScreen first, WeekFlowScreen second)
    {
        if (first?.EventDefinition == null || second?.EventDefinition == null)
        {
            return false;
        }

        if (ReferenceEquals(first.EventDefinition, second.EventDefinition))
        {
            return true;
        }

        string firstEventId = first.EventDefinition.Id;
        string secondEventId = second.EventDefinition.Id;

        if (!string.IsNullOrWhiteSpace(firstEventId) || !string.IsNullOrWhiteSpace(secondEventId))
        {
            return string.Equals(firstEventId, secondEventId, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(first.EventDefinition.name, second.EventDefinition.name, StringComparison.OrdinalIgnoreCase);
    }
}
