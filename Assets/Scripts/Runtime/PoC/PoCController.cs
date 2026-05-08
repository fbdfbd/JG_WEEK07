using System;
using UnityEngine;

public sealed class PoCController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private SO_PoCFlowDefinition _flow;
    [SerializeField] private bool _playOnStart = true;

    [Header("View")]
    [SerializeField] private PoCViewBase _view;

    private readonly PoCNightDialogueResolver _nightDialogueResolver = new();

    private PoCRuntimeState _state;
    private PoCPresenter _presenter;
    private int _introIndex;
    private int _dayIndex;
    private bool _isShowingNightChoiceResult;

    public PoCRuntimeState State => _state;

    private PoCDayData CurrentDay
    {
        get
        {
            if (_flow?.Days == null || _dayIndex < 0 || _dayIndex >= _flow.Days.Length)
            {
                return null;
            }

            return _flow.Days[_dayIndex];
        }
    }

    private void Awake()
    {
        ResolveView();
        CreateRuntimeObjects();
        BindViewEvents();
        RenderStats();
    }

    private void Start()
    {
        if (_playOnStart)
        {
            StartFlow();
        }
    }

    private void OnDestroy()
    {
        UnbindViewEvents();
        UnbindStateEvents();
    }

    public void StartFlow()
    {
        _introIndex = 0;
        _dayIndex = 0;
        _isShowingNightChoiceResult = false;

        if (HasIntroPage())
        {
            ShowIntro();
            return;
        }

        ShowCurrentDayReport();
    }

    private void ResolveView()
    {
        if (_view == null)
        {
            _view = GetComponent<PoCViewBase>();
        }
    }

    private void CreateRuntimeObjects()
    {
        _state = new PoCRuntimeState();
        _presenter = new PoCPresenter(_state);
        BindStateEvents();
    }

    private void BindViewEvents()
    {
        if (_view == null)
        {
            return;
        }

        _view.IntroContinueRequested += HandleIntroContinueRequested;
        _view.ReportContinueRequested += HandleReportContinueRequested;
        _view.MainChoiceSelected += HandleMainChoiceSelected;
        _view.DayEventContinueRequested += HandleDayEventContinueRequested;
        _view.NightContinueRequested += HandleNightContinueRequested;
        _view.NightChoiceSelected += HandleNightChoiceSelected;
        _view.EndContinueRequested += HandleEndContinueRequested;
    }

    private void UnbindViewEvents()
    {
        if (_view == null)
        {
            return;
        }

        _view.IntroContinueRequested -= HandleIntroContinueRequested;
        _view.ReportContinueRequested -= HandleReportContinueRequested;
        _view.MainChoiceSelected -= HandleMainChoiceSelected;
        _view.DayEventContinueRequested -= HandleDayEventContinueRequested;
        _view.NightContinueRequested -= HandleNightContinueRequested;
        _view.NightChoiceSelected -= HandleNightChoiceSelected;
        _view.EndContinueRequested -= HandleEndContinueRequested;
    }

    private void BindStateEvents()
    {
        if (_state == null)
        {
            return;
        }

        _state.StatsChanged += RenderStats;
    }

    private void UnbindStateEvents()
    {
        if (_state == null)
        {
            return;
        }

        _state.StatsChanged -= RenderStats;
    }

    private void HandleIntroContinueRequested()
    {
        _introIndex++;
        if (HasIntroPage())
        {
            ShowIntro();
            return;
        }

        ShowCurrentDayReport();
    }

    private void HandleReportContinueRequested()
    {
        PoCDayData day = CurrentDay;
        if (day == null)
        {
            ShowFlowEnd();
            return;
        }

        if (day.HasChoices)
        {
            ShowMainChoice();
            return;
        }

        ContinueAfterChoice();
    }

    private void HandleMainChoiceSelected(string choiceId)
    {
        PoCMainChoiceData choice = FindMainChoice(CurrentDay, choiceId);
        if (choice == null)
        {
            return;
        }

        _state.SelectMainChoice(choice.Id);
        ApplyChoiceFlags(choice);
        _state.ApplyStatChanges(choice.StatChanges);
        ContinueAfterChoice();
    }

    private void HandleDayEventContinueRequested()
    {
        ContinueAfterDayEvent();
    }

    private void HandleNightContinueRequested()
    {
        RuntimeInteractiveEventSession session = _state.NightSession;
        if (session == null)
        {
            MoveToNextDay();
            return;
        }

        if (!_isShowingNightChoiceResult && HasChoicesWaitingForSelection(session))
        {
            return;
        }

        _isShowingNightChoiceResult = false;
        if (session.TryMoveToNextStep(null))
        {
            ShowNightDialogue();
            return;
        }

        _state.ClearNightDialogue();
        MoveToNextDay();
    }

    private void HandleNightChoiceSelected(int choiceIndex)
    {
        RuntimeInteractiveEventSession session = _state.NightSession;
        if (session?.CurrentStep?.Choices == null || session.HasPendingChoiceResult)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= session.CurrentStep.Choices.Length)
        {
            return;
        }

        InteractiveEventChoiceData choice = session.CurrentStep.Choices[choiceIndex];
        session.SelectChoice(choice);

        InteractiveEventChoiceResultPresentation result = _presenter.BuildNightChoiceResult(choice);
        if (HasChoiceResultContent(result))
        {
            _isShowingNightChoiceResult = true;
            _state.SetPhase(PoCPhase.NightDialogue);
            _view?.ShowNightChoiceResult(result);
            return;
        }

        HandleNightContinueRequested();
    }

    private void HandleEndContinueRequested()
    {
        // The PoC flow ends here.
    }

    private void ContinueAfterChoice()
    {
        PoCDayData day = CurrentDay;
        if (day != null && day.HasDayEvent)
        {
            ShowDayEvent(day);
            return;
        }

        ContinueAfterDayEvent();
    }

    private void ContinueAfterDayEvent()
    {
        PoCDayData day = CurrentDay;
        if (day != null && day.HasNightDialogue)
        {
            StartNightDialogue(day);
            return;
        }

        MoveToNextDay();
    }

    private void MoveToNextDay()
    {
        _dayIndex++;
        ShowCurrentDayReport();
    }

    private void ShowIntro()
    {
        _state.SetPhase(PoCPhase.Intro);
        _view?.ShowIntro(_presenter.BuildText(_flow.IntroPages[_introIndex]));
    }

    private void ShowCurrentDayReport()
    {
        PoCDayData day = CurrentDay;
        if (day == null)
        {
            ShowFlowEnd();
            return;
        }

        _isShowingNightChoiceResult = false;
        _state.ClearNightDialogue();
        _state.SetPhase(PoCPhase.Report);
        _view?.ShowReport(_presenter.BuildReport(day));
    }

    private void ShowMainChoice()
    {
        _state.SetPhase(PoCPhase.MainChoice);
        _view?.ShowMainChoice(_presenter.BuildMainChoice(CurrentDay));
    }

    private void ShowDayEvent(PoCDayData day)
    {
        _state.SetPhase(PoCPhase.DayEvent);
        PoCDayEventResolvedData dayEvent = day.DayEvent.Resolve(_state);
        _state.ApplyStatChanges(dayEvent.StatChanges);
        _view?.ShowDayEvent(_presenter.BuildDayEvent(dayEvent));
    }

    private void StartNightDialogue(PoCDayData day)
    {
        SO_InteractiveEventDefinition dialogue = _nightDialogueResolver.Resolve(
            day != null ? day.NightDialogueRules : null,
            _state);

        if (dialogue == null)
        {
            MoveToNextDay();
            return;
        }

        _state.BeginNightDialogue(dialogue);
        ShowNightDialogue();
    }

    private void ShowNightDialogue()
    {
        _isShowingNightChoiceResult = false;
        _state.SetPhase(PoCPhase.NightDialogue);
        _view?.ShowNightDialogue(_presenter.BuildNightDialogue(_state.NightSession));
    }

    private void ShowFlowEnd()
    {
        _isShowingNightChoiceResult = false;
        _state.ClearNightDialogue();
        _state.SetPhase(PoCPhase.End);
        _view?.ShowEnd(PoCTextPresentation.Empty);
    }

    private void RenderStats()
    {
        _view?.RenderPublicStats(_presenter.BuildPublicStats());
    }

    private bool HasIntroPage()
    {
        return _flow?.IntroPages != null
            && _introIndex >= 0
            && _introIndex < _flow.IntroPages.Length
            && _flow.IntroPages[_introIndex] != null
            && _flow.IntroPages[_introIndex].HasContent;
    }

    private static PoCMainChoiceData FindMainChoice(PoCDayData day, string choiceId)
    {
        if (day?.MainChoices == null || string.IsNullOrWhiteSpace(choiceId))
        {
            return null;
        }

        for (int index = 0; index < day.MainChoices.Length; index++)
        {
            PoCMainChoiceData choice = day.MainChoices[index];
            if (choice != null && string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase))
            {
                return choice;
            }
        }

        return null;
    }

    private void ApplyChoiceFlags(PoCMainChoiceData choice)
    {
        if (choice?.SetFlags == null)
        {
            return;
        }

        for (int index = 0; index < choice.SetFlags.Length; index++)
        {
            string flag = choice.SetFlags[index];
            if (!string.IsNullOrWhiteSpace(flag))
            {
                _state.SetFlag(flag);
            }
        }
    }

    private static bool HasChoicesWaitingForSelection(RuntimeInteractiveEventSession session)
    {
        return session?.CurrentStep?.Choices != null
            && session.CurrentStep.Choices.Length > 0
            && !session.HasPendingChoiceResult;
    }

    private static bool HasChoiceResultContent(InteractiveEventChoiceResultPresentation result)
    {
        if (!string.IsNullOrWhiteSpace(result.EffectSummaryLine))
        {
            return true;
        }

        if (result.DialogueLines == null)
        {
            return false;
        }

        for (int index = 0; index < result.DialogueLines.Count; index++)
        {
            if (result.DialogueLines[index].HasContent)
            {
                return true;
            }
        }

        return false;
    }
}
