using System;
using System.Collections;
using UnityEngine;

public class WeekFlowController : MonoBehaviour
{
    [Header("Week Data")]
    [SerializeField] private SO_WeekDefinition _weekDefinition;
    [SerializeField] private SO_WeekDefinition[] _weekDefinitions = Array.Empty<SO_WeekDefinition>();
    [SerializeField] private SO_WeekUiTextCatalog _uiTextCatalog;

    [Header("View Connection")]
    [SerializeField] private WeekFlowViewBase _view;

    private readonly WeekRunner _weekRunner = new();
    private readonly WeekSelectionState _weekSelectionState = new();
    public static readonly WeekSequenceState _weekSequenceState = new();

    public static WeekFlowRuntimeState _runtimeState;
    private WeekUiTextProvider _weekUiText;
    private WeekFlowPresenter _presenter;
    private WeekFlowCommandHandler _commandHandler;
    private WeekFlowNarrativeHandler _narrativeHandler;
    private WeekFlowCinematicDirector _cinematicDirector;
    private WeekFlowCutsceneBridgeBase _cutsceneBridge;
    private WeekFlowScreen _currentScreen;
    private bool _isTransitionPlaying;
    private RuntimeChildState _boundChildState;

    public event Action<SO_WeekDefinition> WeekChanged;
    public event Action<RuntimeChildState> ChildStateSourceChanged;
    public event Action FlowPresentationCompleted;
    public SO_WeekDefinition CurrentWeekDefinition => _weekSequenceState.CurrentWeekDefinition;
    public RuntimeChildState CurrentChildState => _runtimeState?.ChildState;


    protected virtual void Awake()
    {
        ResolveConnectedView();
        BuildWeekFlowObjects();
        GameplayAnalyticsLogger.StartSession(CurrentWeekDefinition);
        BindViewEvents();
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
    }

    private void BuildWeekFlowObjects()
    {
        _runtimeState = new WeekFlowRuntimeState();
        _weekUiText = new WeekUiTextProvider(_uiTextCatalog);
        _weekSequenceState.InitializeWeekSequence(_weekDefinition, _weekDefinitions);
        _weekSelectionState.ApplyWeekEntries(WeekFlowQueryUtility.GetCurrentWeekEntries(_weekSequenceState.CurrentWeekDefinition));
        _presenter = new WeekFlowPresenter(_view, _runtimeState, _weekUiText, _weekSelectionState, _weekSequenceState);
        _commandHandler = new WeekFlowCommandHandler(_runtimeState, _weekUiText, _weekRunner, _weekSelectionState, _weekSequenceState);
        _narrativeHandler = new WeekFlowNarrativeHandler(_runtimeState, _weekUiText, _weekSelectionState, _weekSequenceState);
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
        _view.WeekFeedbackClosed += HandleWeekFeedbackClosed;
        _view.InteractiveEventContinueRequested += HandleInteractiveEventContinueRequested;
        _view.InteractiveEventSkipRequested += HandleInteractiveEventSkipRequested;
        _view.InteractiveEventChoiceSelected += HandleInteractiveEventChoiceSelected;
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
        _view.WeekFeedbackClosed -= HandleWeekFeedbackClosed;
        _view.InteractiveEventContinueRequested -= HandleInteractiveEventContinueRequested;
        _view.InteractiveEventSkipRequested -= HandleInteractiveEventSkipRequested;
        _view.InteractiveEventChoiceSelected -= HandleInteractiveEventChoiceSelected;
    }

    private void HandleRunWeekRequested() => RunFlowAction(_commandHandler.RunCurrentWeek);
    private void HandleResetSelectionsRequested() => RunFlowAction(_commandHandler.ResetSelections);
    private void HandleResetChildStateRequested() => RunFlowAction(_commandHandler.ResetChildState);
    private void HandleWeekFeedbackClosed() => RunFlowAction(_narrativeHandler.CloseWeekFeedback);
    private void HandleInteractiveEventContinueRequested() => RunFlowAction(_narrativeHandler.ContinueInteractiveEvent);
    private void HandleInteractiveEventSkipRequested() => RunFlowAction(_narrativeHandler.SkipCurrentInteractiveEvent);
    private void HandleCardOptionSelected(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        GameplayAnalyticsLogger.LogCardOptionClicked(CurrentWeekDefinition, cardDefinition, optionIndex);
        RunFlowAction(() => _commandHandler.SelectCardOption(cardDefinition, optionIndex));
    }
    private void HandleInteractiveEventChoiceSelected(int choiceIndex) => RunFlowAction(() => _narrativeHandler.SelectInteractiveEventChoice(choiceIndex));

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

    private void RunFlowAction(Func<WeekFlowActionResult> action)
    {
        if (_isTransitionPlaying)
        {
            return;
        }

        SO_WeekDefinition previousWeek = _weekSequenceState.CurrentWeekDefinition;
        WeekFlowActionResult result = action();
        if (!result.ShouldRefreshUi)
        {
            return;
        }

        StartCoroutine(ApplyFlowAction(result, previousWeek, _weekSequenceState.CurrentWeekDefinition));
    }

    private IEnumerator ApplyFlowAction(WeekFlowActionResult result, SO_WeekDefinition previousWeek, SO_WeekDefinition currentWeek)
    {
        _isTransitionPlaying = true;

        WeekFlowScreen previousScreen = _currentScreen;
        WeekFlowScreen nextScreen = result.NextScreen;
        bool shouldShowMainCanvas = result.ShouldReplaceScreen
            ? nextScreen == null
            : previousScreen == null;

        if (result.ShouldReplaceScreen && previousScreen != null)
        {
            yield return _cinematicDirector.PlayScreenExit(previousScreen);

            if (ShouldExitEventCutscene(previousScreen, nextScreen))
            {
                yield return PlayEventExitCutscene(previousScreen);
            }

            _presenter.HideFlowScreens();
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
            WeekChanged?.Invoke(currentWeek);
        }

        if (result.ShouldReplaceScreen)
        {
            _currentScreen = nextScreen;
            if (_currentScreen != null)
            {
                _presenter.PresentScreen(_currentScreen);
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
        FlowPresentationCompleted?.Invoke();
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

    private static bool ShouldEnterEventCutscene(WeekFlowScreen previousScreen, WeekFlowScreen nextScreen)
    {
        return nextScreen?.EventDefinition != null && !IsSameEvent(previousScreen, nextScreen);
    }

    private static bool ShouldExitEventCutscene(WeekFlowScreen previousScreen, WeekFlowScreen nextScreen)
    {
        return previousScreen?.EventDefinition != null && !IsSameEvent(previousScreen, nextScreen);
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
