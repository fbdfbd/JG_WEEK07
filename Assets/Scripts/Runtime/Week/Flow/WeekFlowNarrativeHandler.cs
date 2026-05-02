using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WeekFlowNarrativeHandler
{
    private readonly WeekFlowRuntimeState _runtimeState;
    private readonly WeekUiTextProvider _weekUiText;
    private readonly WeekSelectionState _weekSelectionState;
    private readonly WeekSequenceState _weekSequenceState;
    private readonly SO_EndingCatalog _endingCatalog;
    private readonly bool _isTest;
    private readonly bool _useEndingMoodThresholdCorrection;

    public WeekFlowNarrativeHandler(
        WeekFlowRuntimeState runtimeState,
        WeekUiTextProvider weekUiText,
        WeekSelectionState weekSelectionState,
        WeekSequenceState weekSequenceState,
        SO_EndingCatalog endingCatalog,
        bool isTest,
        bool useEndingMoodThresholdCorrection = false)
    {
        _runtimeState = runtimeState;
        _weekUiText = weekUiText;
        _weekSelectionState = weekSelectionState;
        _weekSequenceState = weekSequenceState;
        _endingCatalog = endingCatalog;
        _isTest = isTest;
        _useEndingMoodThresholdCorrection = useEndingMoodThresholdCorrection;
    }

    public WeekFlowActionResult CloseWeekFeedback()
    {
        return ContinuePostWeekFlow();
    }

    public WeekFlowActionResult ContinueWeeklyResultLog()
    {
        Debug.Log(
            $"[WeeklyStatDebug] ContinueWeeklyResultLog " +
            $"hasPending={_runtimeState.HasPendingWeeklyStatResult} " +
            $"weekStartStats={_runtimeState.WeekStartStats?.Count ?? -1} " +
            $"currentWeek={_weekSequenceState.CurrentWeekDefinition?.Id}");

        if (_runtimeState.HasPendingWeeklyStatResult)
        {
            return BuildWeeklyStatResultScreen();
        }

        return ContinuePostWeekFlow();
    }

    public WeekFlowActionResult ContinueWeeklyStatResult()
    {
        Debug.Log(
            $"[WeeklyStatDebug] ContinueWeeklyStatResult " +
            $"currentWeek={_weekSequenceState.CurrentWeekDefinition?.Id}");

        _runtimeState.MarkWeeklyStatResultConsumed();
        return ContinuePostWeekFlow();
    }

    public WeekFlowActionResult ContinueInteractiveEvent()
    {
        RuntimeInteractiveEventSession eventSession = _runtimeState.CurrentEventSession;
        if (eventSession == null)
        {
            if (_runtimeState.IsAwaitingEndingFollowUp)
            {
                return BuildEndingFollowUpScreen();
            }

            return ContinuePostWeekFlow();
        }

        if (eventSession.CurrentStep?.Choices?.Length > 0 && !eventSession.HasPendingChoiceResult)
        {
            return WeekFlowActionResult.None;
        }

        ApplyCurrentStepEffectsIfNeeded(eventSession);

        if (eventSession.TryMoveToNextStep(_runtimeState.ChildState))
        {
            return BuildEventStepScreen();
        }

        CompleteCurrentEvent();
        return ContinuePostWeekFlow();
    }

    public WeekFlowActionResult SkipCurrentInteractiveEvent()
    {
        RuntimeInteractiveEventSession eventSession = _runtimeState.CurrentEventSession;
        if (!WeekFlowEventSkipPolicy.CanSkip(eventSession, _weekSequenceState.CurrentWeekDefinition))
        {
            return WeekFlowActionResult.None;
        }

        GameplayAnalyticsLogger.LogInteractiveEventSkipped(
            _weekSequenceState.CurrentWeekDefinition,
            eventSession.EventDefinition,
            eventSession.CurrentStep);
        CompleteCurrentEvent();
        return ContinuePostWeekFlow();
    }

    public WeekFlowActionResult SelectInteractiveEventChoice(int choiceIndex)
    {
        RuntimeInteractiveEventSession eventSession = _runtimeState.CurrentEventSession;
        if (eventSession?.CurrentStep?.Choices == null || eventSession.HasPendingChoiceResult)
        {
            return WeekFlowActionResult.None;
        }

        if (choiceIndex < 0 || choiceIndex >= eventSession.CurrentStep.Choices.Length)
        {
            return WeekFlowActionResult.None;
        }

        InteractiveEventChoiceData selectedChoice = eventSession.CurrentStep.Choices[choiceIndex];
        GameplayAnalyticsLogger.LogInteractiveChoiceSelected(
            _weekSequenceState.CurrentWeekDefinition,
            eventSession.EventDefinition,
            eventSession.CurrentStep,
            choiceIndex,
            selectedChoice);
        eventSession.SelectChoice(selectedChoice);
        GameplayInteractionExecutor.ApplyAll(selectedChoice.Interactions, _runtimeState.ChildState);

        InteractiveEventChoiceResultPresentation result = WeekNarrativeResolver.CreateChoiceResultPresentation(selectedChoice, _weekUiText);
        PublishStatusMessage(_weekUiText.GetPrivateDialogueChoiceAppliedMessage());

        if (!HasChoiceResultContent(result))
        {
            return ContinueInteractiveEvent();
        }

        DialogueLinePresentation line = WeekNarrativeResolver.GetPrimaryDialogueLine(result.DialogueLines);

        return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateChoiceResult(
            _weekSequenceState.CurrentWeekDefinition,
            eventSession.EventDefinition,
            selectedChoice,
            result,
            new NemoFeedbackPresentation(
                line.SpeakerName,
                WeekNarrativeResolver.GetVisualStateForCurrentState(_runtimeState.ChildState),
                line.Text)));
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

    private void PublishStatusMessage(string statusMessage)
    {
        _runtimeState.SetStatusMessage(statusMessage);
    }

    private WeekFlowActionResult ContinuePostWeekFlow()
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
            Debug.Log(
                $"[WeeklyStatDebug] ContinuePostWeekFlow -> BuildWeeklyStatResultScreen " +
                $"currentWeek={_weekSequenceState.CurrentWeekDefinition?.Id}");
            return BuildWeeklyStatResultScreen();
        }

        if (_runtimeState.TryStartNextNightEvent())
        {
            Debug.Log(
                $"[WeeklyStatDebug] ContinuePostWeekFlow -> StartNextNightEvent " +
                $"currentWeek={_weekSequenceState.CurrentWeekDefinition?.Id}");
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
            return ContinuePostWeekFlow();
        }

        ApplyLinkedCardRewardsIfNeeded(eventSession);

        InteractiveEventPresentation presentation = WeekNarrativeResolver.CreatePresentation(eventSession, _runtimeState.ChildState, _weekUiText);
        if (ShouldAutoAdvanceEmptyStep(presentation))
        {
            ApplyCurrentStepEffectsIfNeeded(eventSession);
            if (eventSession.TryMoveToNextStep(_runtimeState.ChildState))
            {
                return BuildEventStepScreen();
            }

            CompleteCurrentEvent();
            return ContinuePostWeekFlow();
        }

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
        WeeklyResultStatDeltaPresentation[] statSummary = WeekNarrativeResolver.CreateWeeklyResultStatSummary(
            _runtimeState.ChildState,
            _runtimeState.WeekStartStats,
            _weekUiText);
        _runtimeState.AddWeeklyResultLogHistory(_weekSequenceState.CurrentWeekDefinition, resultLogs, statSummary);
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

        Debug.Log(
            $"[WeeklyStatDebug] BuildWeeklyStatResultScreen " +
            $"currentWeek={_weekSequenceState.CurrentWeekDefinition?.Id} " +
            $"hasChanges={presentation.HasChanges} " +
            $"changeCount={presentation.Changes?.Count ?? -1} " +
            $"weekStartStats={_runtimeState.WeekStartStats?.Count ?? -1}");

        _runtimeState.MarkWeeklyStatResultConsumed();

        if (!presentation.HasChanges)
        {
            Debug.Log("[WeeklyStatDebug] BuildWeeklyStatResultScreen skipped: no changes");
            return ContinuePostWeekFlow();
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

    private static bool ShouldAutoAdvanceEmptyStep(InteractiveEventPresentation presentation)
    {
        return string.IsNullOrWhiteSpace(presentation.BodyText)
            && string.IsNullOrWhiteSpace(presentation.EffectSummaryLine)
            && (presentation.DialogueLines == null || presentation.DialogueLines.Count == 0)
            && (presentation.Choices == null || presentation.Choices.Count == 0);
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

    private void ApplyCurrentStepEffectsIfNeeded(RuntimeInteractiveEventSession eventSession)
    {
        if (eventSession == null || eventSession.HasAppliedCurrentStepEffects)
        {
            return;
        }

        GameplayInteractionExecutor.ApplyAll(eventSession.CurrentStep.OnEnterInteractions, _runtimeState.ChildState);
        eventSession.MarkCurrentStepEffectsApplied();
    }

    private void CompleteCurrentEvent()
    {
        SO_InteractiveEventDefinition eventDefinition = _runtimeState.CurrentEventSession.EventDefinition;
        GameplayInteractionExecutor.ApplyAll(
            eventDefinition.OnCompletedInteractions,
            _runtimeState.ChildState);
        WeekEventRuntimeAugmentationService.ApplyOnCompleted(
            eventDefinition,
            _runtimeState.ChildState);
        if (_runtimeState.IsCurrentEventFromDayFlow)
        {
            _runtimeState.AddWeeklyResultLog(eventDefinition.Result);
        }

        _runtimeState.ClearCurrentEventSession();
    }

    private WeekFlowActionResult BuildEndingScreen()
    {
        _runtimeState.ShouldShowEndingAfterEvents = false;
        _runtimeState.HasReachedEnding = true;
        _runtimeState.IsAwaitingEndingFollowUp = true;
        EndingResolver.LogDebugSnapshot(
            _runtimeState.ChildState,
            nameof(WeekFlowNarrativeHandler),
            _useEndingMoodThresholdCorrection);
        EndingPresentation ending = EndingResolver.Resolve(
            _runtimeState.ChildState,
            _endingCatalog,
            _useEndingMoodThresholdCorrection);
        GameplayAnalyticsLogger.LogEndingReached(_weekSequenceState.CurrentWeekDefinition, ending);
        PublishStatusMessage(_weekUiText.GetEndingReachedMessage());

        if (_isTest)
        {
            return BuildEndingFollowUpScreen();
        }

        return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateEnding(
            _weekSequenceState.CurrentWeekDefinition,
            ending,
            new NemoFeedbackPresentation(ending.VisualState, ending.ClosingLine)));
    }

    private WeekFlowActionResult BuildEndingFollowUpScreen()
    {
        _runtimeState.IsAwaitingEndingFollowUp = false;
        return WeekFlowActionResult.ReplaceScreen(WeekFlowScreen.CreateEndingFollowUp(
            _weekSequenceState.CurrentWeekDefinition,
            new NemoFeedbackPresentation(ENemoVisualState.Neutral, string.Empty)));
    }

    private void MoveToNextWeek()
    {
        _runtimeState.ShouldAdvanceToNextWeekAfterEvents = false;
        if (!_weekSequenceState.TryMoveToNextWeek())
        {
            _runtimeState.ClearPendingEventState();
            return;
        }

        WeekCardEntryData[] entries = WeekFlowQueryUtility.GetCurrentWeekEntries(
            _weekSequenceState.CurrentWeekDefinition,
            _runtimeState.ChildState);
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

public static class WeekFlowEventSkipPolicy
{
    public static bool CanSkip(RuntimeInteractiveEventSession eventSession)
    {
        return CanSkip(eventSession, null);
    }

    public static bool CanSkip(RuntimeInteractiveEventSession eventSession, SO_WeekDefinition weekDefinition)
    {
        if (eventSession?.CurrentStep == null || eventSession.HasPendingChoiceResult)
        {
            return false;
        }

        if (IsWeekZeroStoryException(weekDefinition, eventSession.EventDefinition))
        {
            return true;
        }

        if (eventSession.EventDefinition is SO_StoryEventDefinition)
        {
            return false;
        }

        return HasNoChoices(eventSession.EventDefinition)
            && HasNoChoicesFromStep(eventSession.CurrentStep);
    }

    public static bool CanSkip(WeekFlowScreen screen)
    {
        if (screen == null || screen.ScreenType != EWeekFlowScreenType.EventStep)
        {
            return false;
        }

        if (IsWeekZeroStoryException(screen.WeekDefinition, screen.EventDefinition))
        {
            return true;
        }

        if (screen.EventDefinition is SO_StoryEventDefinition)
        {
            return false;
        }

        return HasNoChoices(screen.EventDefinition)
            && HasNoChoicesFromStep(screen.StepDefinition);
    }

    private static bool IsWeekZeroStoryException(
        SO_WeekDefinition weekDefinition,
        SO_InteractiveEventDefinition eventDefinition)
    {
        if (weekDefinition == null || eventDefinition is not SO_StoryEventDefinition)
        {
            return false;
        }

        return weekDefinition.WeekIndex == 0
            || string.Equals(weekDefinition.Id, "week_000", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(weekDefinition.Id, "week000", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasNoChoices(SO_InteractiveEventDefinition eventDefinition)
    {
        return eventDefinition == null || HasNoChoicesFromStep(eventDefinition.FirstStep);
    }

    private static bool HasNoChoicesFromStep(SO_InteractiveEventStepDefinition startStep)
    {
        HashSet<SO_InteractiveEventStepDefinition> visitedSteps = new();
        SO_InteractiveEventStepDefinition currentStep = startStep;

        while (currentStep != null && visitedSteps.Add(currentStep))
        {
            if (currentStep.Choices != null && currentStep.Choices.Length > 0)
            {
                return false;
            }

            currentStep = currentStep.NextStep;
        }

        return true;
    }
}
