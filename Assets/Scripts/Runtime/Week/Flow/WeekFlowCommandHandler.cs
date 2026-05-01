using System;
using UnityEngine;

public sealed class WeekFlowCommandHandler
{
    private readonly WeekFlowRuntimeState _runtimeState;
    private readonly WeekUiTextProvider _weekUiText;
    private readonly WeekRunner _weekRunner;
    private readonly WeekSelectionState _weekSelectionState;
    private readonly WeekSequenceState _weekSequenceState;
    private readonly SO_EndingCatalog _endingCatalog;
    private readonly bool _isTest;

    public WeekFlowCommandHandler(
        WeekFlowRuntimeState runtimeState,
        WeekUiTextProvider weekUiText,
        WeekRunner weekRunner,
        WeekSelectionState weekSelectionState,
        WeekSequenceState weekSequenceState,
        SO_EndingCatalog endingCatalog,
        bool isTest)
    {
        _runtimeState = runtimeState;
        _weekUiText = weekUiText;
        _weekRunner = weekRunner;
        _weekSelectionState = weekSelectionState;
        _weekSequenceState = weekSequenceState;
        _endingCatalog = endingCatalog;
        _isTest = isTest;
    }

    public WeekFlowActionResult RunCurrentWeek()
    {
        if (_runtimeState.HasReachedEnding)
        {
            PublishStatusMessage(_weekUiText.GetEndingAlreadyReachedMessage());
            return WeekFlowActionResult.RefreshOnly();
        }

        SO_WeekDefinition currentWeekDefinition = _weekSequenceState.CurrentWeekDefinition;
        if (currentWeekDefinition == null)
        {
            PublishStatusMessage(_weekUiText.GetWeekDefinitionMissingMessage());
            return WeekFlowActionResult.RefreshOnly();
        }

        try
        {
            _runtimeState.CaptureWeekStartStats();
            _runtimeState.ChildState.ClearReactionLogs();

            RuntimeWeekSelection[] selections = _weekSelectionState.BuildSelections(
                WeekFlowQueryUtility.GetCurrentWeekEntries(currentWeekDefinition));
            _runtimeState.LastWeekResult = _weekRunner.RunWeek(currentWeekDefinition, _runtimeState.ChildState, selections);
            LogResolvedWeekAnalytics(currentWeekDefinition, _runtimeState.LastWeekResult);

            RuntimeChildState eventResolutionChildState = _runtimeState.LastWeekResult.EventResolutionChildState ?? _runtimeState.ChildState;
            _runtimeState.SetPendingDayEvents(WeekNarrativeResolver.ResolveDayEvents(
                currentWeekDefinition,
                eventResolutionChildState,
                _runtimeState.LastWeekResult));
            _runtimeState.SetPendingNightEvents(WeekNarrativeResolver.ResolveNightEvents(
                currentWeekDefinition,
                eventResolutionChildState,
                _runtimeState.LastWeekResult));
            _runtimeState.ShouldShowEndingAfterEvents = _weekSequenceState.IsCurrentWeekFinal;
            _runtimeState.ShouldAdvanceToNextWeekAfterEvents = !_runtimeState.ShouldShowEndingAfterEvents;

            PublishStatusMessage(_weekUiText.GetWeekCompletedMessage(currentWeekDefinition.WeekIndex));
            return ContinueAfterWeekFlow();
        }
        catch (Exception exception)
        {
            PublishStatusMessage(_weekUiText.GetWeekExecutionFailedMessage(exception.Message));
            Debug.LogError(exception);
            return WeekFlowActionResult.RefreshOnly();
        }
    }

    public WeekFlowActionResult ResetSelections()
    {
        _weekSelectionState.ResetAllSelections(
            WeekFlowQueryUtility.GetCurrentWeekEntries(_weekSequenceState.CurrentWeekDefinition));
        _runtimeState.LastWeekResult = null;
        PublishStatusMessage(_weekUiText.GetAllSelectionsResetMessage());
        return WeekFlowActionResult.ClearScreen();
    }

    public WeekFlowActionResult ResetChildState()
    {
        _runtimeState.ResetChildState();
        PublishStatusMessage(_weekUiText.GetChildStateResetMessage());
        return WeekFlowActionResult.ClearScreen();
    }

    public WeekFlowActionResult SelectCardOption(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        if (!_weekSelectionState.TrySetSelectedOptionIndex(cardDefinition, optionIndex))
        {
            return WeekFlowActionResult.None;
        }

        PublishStatusMessage(_weekUiText.GetCardSelectionUpdatedMessage());
        return WeekFlowActionResult.RefreshOnly();
    }

    public WeekFlowActionResult SelectAllCardOptionsBySemantic(ECardOptionSemantic semantic)
    {
        WeekCardEntryData[] entries =
            WeekFlowQueryUtility.GetCurrentWeekEntries(_weekSequenceState.CurrentWeekDefinition);

        int selectedCount = _weekSelectionState.SelectAllBySemantic(entries, semantic);
        if (selectedCount <= 0)
        {
            return WeekFlowActionResult.None;
        }

        PublishStatusMessage(_weekUiText.GetCardSelectionUpdatedMessage());
        return WeekFlowActionResult.RefreshOnly();
    }

    private void PublishStatusMessage(string statusMessage)
    {
        _runtimeState.SetStatusMessage(statusMessage);
    }

    private WeekFlowActionResult ContinueAfterWeekFlow()
    {
        if (_runtimeState.CurrentEventSession != null || _runtimeState.TryStartNextDayEvent())
        {
            return BuildEventStepScreen();
        }

        if (ShouldShowWeeklyResultLog() &&
            _runtimeState.TryConsumeWeeklyResultLogs(out SO_EventResultDefinition[] resultLogs))
        {
            return BuildWeeklyResultLogScreen(resultLogs);
        }

        if (ShouldShowWeeklyResultLog() && _runtimeState.HasPendingWeeklyStatResult)
        {
            return BuildWeeklyStatResultScreen();
        }

        if (_runtimeState.TryStartNextNightEvent())
        {
            return BuildEventStepScreen();
        }

        if (_runtimeState.ShouldShowEndingAfterEvents)
        {
            return BuildEndingScreen();
        }

        if (_runtimeState.ShouldAdvanceToNextWeekAfterEvents)
        {
            MoveToNextWeek();
        }
        else
        {
            _runtimeState.ClearPendingEventState();
        }

        return WeekFlowActionResult.ClearScreen();
    }

    private WeekFlowActionResult BuildEventStepScreen()
    {
        RuntimeInteractiveEventSession eventSession = _runtimeState.CurrentEventSession;
        if (eventSession?.CurrentStep == null)
        {
            return ContinueAfterWeekFlow();
        }

        ApplyLinkedCardRewardsIfNeeded(eventSession);

        InteractiveEventPresentation presentation = WeekNarrativeResolver.CreatePresentation(
            eventSession,
            _runtimeState.ChildState,
            _weekUiText);
        DialogueLinePresentation line = WeekNarrativeResolver.GetPrimaryDialogueLine(presentation.DialogueLines);

        return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateEventStep(
            _weekSequenceState.CurrentWeekDefinition,
            eventSession.EventDefinition,
            eventSession.CurrentStep,
            presentation,
            new NemoFeedbackPresentation(line.SpeakerName, presentation.VisualState, line.Text)));
    }

    private WeekFlowActionResult BuildWeeklyResultLogScreen(SO_EventResultDefinition[] resultLogs)
    {
        _runtimeState.AddWeeklyResultLogHistory(_weekSequenceState.CurrentWeekDefinition, resultLogs);
        WeeklyResultLogPresentation presentation = WeekNarrativeResolver.CreateWeeklyResultLogPresentation(
            _runtimeState.WeeklyResultLogHistory,
            _weekSequenceState.CurrentWeekDefinition?.Id);
        return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateWeeklyResultLog(
            _weekSequenceState.CurrentWeekDefinition,
            presentation,
            new NemoFeedbackPresentation(ENemoVisualState.Neutral, string.Empty)));
    }

    private WeekFlowActionResult BuildWeeklyStatResultScreen()
    {
        WeeklyStatResultPresentation presentation = WeeklyStatResultResolver.Resolve(
            _runtimeState.ChildState,
            _runtimeState.WeekStartStats,
            _weekUiText);

        _runtimeState.MarkWeeklyStatResultConsumed();

        if (!presentation.HasChanges)
        {
            return ContinueAfterWeekFlow();
        }

        return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateWeeklyStatResult(
            _weekSequenceState.CurrentWeekDefinition,
            presentation,
            new NemoFeedbackPresentation(ENemoVisualState.Neutral, string.Empty)));
    }

    private bool ShouldShowWeeklyResultLog()
    {
        return !string.Equals(
            _weekSequenceState.CurrentWeekDefinition?.Id,
            "week_000",
            StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyLinkedCardRewardsIfNeeded(RuntimeInteractiveEventSession eventSession)
    {
        if (eventSession == null || eventSession.HasAppliedLinkedCardRewards)
        {
            return;
        }

        RuntimeResolvedCardRecord[] linkedCards = WeekNarrativeResolver.ResolveLinkedCards(
            eventSession.EventDefinition,
            _runtimeState.LastWeekResult);

        foreach (RuntimeResolvedCardRecord linkedCard in linkedCards)
        {
            linkedCard.TryApplyPendingEventReward(_runtimeState.ChildState);
        }

        eventSession.MarkLinkedCardRewardsApplied();
    }

    private WeekFlowActionResult BuildEndingScreen()
    {
        _runtimeState.ShouldShowEndingAfterEvents = false;
        _runtimeState.HasReachedEnding = true;
        _runtimeState.IsAwaitingEndingFollowUp = true;
        EndingResolver.LogDebugSnapshot(_runtimeState.ChildState, nameof(WeekFlowCommandHandler));
        EndingPresentation ending = EndingResolver.Resolve(_runtimeState.ChildState, _endingCatalog);
        GameplayAnalyticsLogger.LogEndingReached(_weekSequenceState.CurrentWeekDefinition, ending);
        PublishStatusMessage(_weekUiText.GetEndingReachedMessage());

        if (_isTest)
        {
            _runtimeState.IsAwaitingEndingFollowUp = false;
            return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateEndingFollowUp(
                _weekSequenceState.CurrentWeekDefinition,
                new NemoFeedbackPresentation(ENemoVisualState.Neutral, string.Empty)));
        }

        return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateEnding(
            _weekSequenceState.CurrentWeekDefinition,
            ending,
            new NemoFeedbackPresentation(ending.VisualState, ending.ClosingLine)));
    }

    private static void LogResolvedWeekAnalytics(SO_WeekDefinition weekDefinition, RuntimeWeekResult weekResult)
    {
        if (weekResult?.ResolvedCards != null)
        {
            foreach (RuntimeResolvedCardRecord resolvedCard in weekResult.ResolvedCards)
            {
                GameplayAnalyticsLogger.LogCardOptionSelected(weekDefinition, resolvedCard);
            }
        }

        GameplayAnalyticsLogger.LogWeekResolved(weekDefinition, weekResult);
    }

    private void MoveToNextWeek()
    {
        _runtimeState.ShouldAdvanceToNextWeekAfterEvents = false;
        if (!_weekSequenceState.TryMoveToNextWeek())
        {
            _runtimeState.ClearPendingEventState();
            return;
        }

        WeekCardEntryData[] entries = WeekFlowQueryUtility.GetCurrentWeekEntries(_weekSequenceState.CurrentWeekDefinition);
        _weekSelectionState.ApplyWeekEntries(entries);
        _weekSelectionState.ResetAllSelections(entries);
        _runtimeState.LastWeekResult = null;
        _runtimeState.ClearPendingEventState();

        SO_WeekDefinition currentWeek = _weekSequenceState.CurrentWeekDefinition;
        PublishStatusMessage(currentWeek == null
            ? _weekUiText.GetMovedToNextWeekFallbackMessage()
            : _weekUiText.GetReadyForWeekMessage(currentWeek.WeekIndex));
    }
}
