using System.Linq;

public sealed class WeekFlowPresenter
{
    private readonly WeekFlowViewBase _view;
    private readonly WeekFlowRuntimeState _runtimeState;
    private readonly WeekUiTextProvider _weekUiText;
    private readonly WeekSelectionState _weekSelectionState;
    private readonly WeekSequenceState _weekSequenceState;

    public WeekFlowPresenter(
        WeekFlowViewBase view,
        WeekFlowRuntimeState runtimeState,
        WeekUiTextProvider weekUiText,
        WeekSelectionState weekSelectionState,
        WeekSequenceState weekSequenceState)
    {
        _view = view;
        _runtimeState = runtimeState;
        _weekUiText = weekUiText;
        _weekSelectionState = weekSelectionState;
        _weekSequenceState = weekSequenceState;
    }

    public void RefreshAll()
    {
        PublishWeekHeader();
        PublishSelectionEntries();
        PublishChildState();
        PublishStatusMessage();
        PublishCurrentNemoFeedback();
    }

    public void PublishSelectionEntries()
    {
        WeekSelectionCategoryGroupPresentation[] selectionGroups =
            _weekSelectionState.BuildSelectionGroupPresentations(
                WeekFlowQueryUtility.GetCurrentWeekEntries(_weekSequenceState.CurrentWeekDefinition),
                _weekUiText.GetUnknownCardType());

        _view?.RenderSelectionGroups(selectionGroups);
    }


    public void PublishChildState()
    {
        WeekStatPresentation[] statPresentations = RuntimeChildState.AllStatTypes
            .Select(statType => new WeekStatPresentation(
                statType,
                _weekUiText.GetStatLabel(statType),
                _runtimeState.ChildState.GetStat(statType)))
            .ToArray();

        string[] flagNames = _runtimeState.ChildState.Flags
            .Select(flagDefinition => flagDefinition != null && !string.IsNullOrWhiteSpace(flagDefinition.DisplayName)
                ? flagDefinition.DisplayName
                : flagDefinition != null ? flagDefinition.name : string.Empty)
            .Where(flagName => !string.IsNullOrWhiteSpace(flagName))
            .ToArray();

        string[] reactionLogs = _runtimeState.ChildState.ReactionLogs
            .ToArray();

        _view?.RenderChildState(new ChildStatePresentation(statPresentations, flagNames, reactionLogs));
    }

    public void PublishStatusMessage()
    {
        _view?.RenderStatusMessage(_runtimeState.StatusMessage);
    }

    public void PublishCurrentNemoFeedback()
    {
        RuntimeResolvedCardRecord lastResolvedCard = _runtimeState.LastWeekResult != null && _runtimeState.LastWeekResult.ResolvedCards.Count > 0
            ? _runtimeState.LastWeekResult.ResolvedCards[_runtimeState.LastWeekResult.ResolvedCards.Count - 1]
            : null;

        PublishNemoFeedback(lastResolvedCard == null
            ? new NemoFeedbackPresentation(ENemoVisualState.Neutral, _weekUiText.GetDefaultNemoLine())
            : NemoFeedbackResolver.Resolve(_runtimeState.ChildState, lastResolvedCard));
    }

    public void PublishDefaultNemoFeedback()
    {
        PublishNemoFeedback(new NemoFeedbackPresentation(
            ENemoVisualState.Neutral,
            _weekUiText.GetDefaultNemoLine()));
    }

    public void PublishNemoFeedback(NemoFeedbackPresentation presentation)
    {
        _view?.PresentNemoFeedback(presentation);
    }

    public void ShowWeekFeedback(WeekFeedbackPresentation presentation)
    {
        _view?.ShowWeekFeedback(presentation);
    }

    public void ShowInteractiveEvent(InteractiveEventPresentation presentation)
    {
        _view?.ShowInteractiveEvent(presentation);
    }

    public void ShowInteractiveEventResult(InteractiveEventChoiceResultPresentation presentation)
    {
        _view?.ShowInteractiveEventResult(presentation);
    }

    public void ShowWeeklyResultLog(WeeklyResultLogPresentation presentation)
    {
        _view?.ShowWeeklyResultLog(presentation);
    }

    public void ShowEnding(EndingPresentation presentation)
    {
        _view?.ShowEnding(presentation);
    }

    public void ShowEndingFollowUp()
    {
        _view?.ShowEndingFollowUp();
    }

    public void PresentScreen(WeekFlowScreen screen)
    {
        if (screen == null)
        {
            return;
        }

        switch (screen.ScreenType)
        {
            case EWeekFlowScreenType.WeekFeedback:
                ShowWeekFeedback(screen.WeekFeedback);
                break;
            case EWeekFlowScreenType.EventStep:
                ShowInteractiveEvent(screen.EventStep);
                break;
            case EWeekFlowScreenType.ChoiceResult:
                ShowInteractiveEventResult(screen.ChoiceResult);
                break;
            case EWeekFlowScreenType.WeeklyResultLog:
                ShowWeeklyResultLog(screen.WeeklyResultLog);
                break;
            case EWeekFlowScreenType.Ending:
                ShowEnding(screen.Ending);
                break;
            case EWeekFlowScreenType.EndingFollowUp:
                ShowEndingFollowUp();
                break;
        }
    }

    public void HideFlowScreens()
    {
        _view?.HideTransientViews();
    }

    private void PublishWeekHeader()
    {
        SO_WeekDefinition currentWeekDefinition = _weekSequenceState.CurrentWeekDefinition;
        if (currentWeekDefinition == null)
        {
            _view?.RenderWeekHeader(new WeekHeaderPresentation(
                _weekUiText.GetNoWeekLabel(),
                _weekUiText.GetNoWeekTitle(),
                string.Empty));
            return;
        }

        _view?.RenderWeekHeader(new WeekHeaderPresentation(
            _weekUiText.GetWeekLabel(currentWeekDefinition.WeekIndex),
            currentWeekDefinition.Title,
            currentWeekDefinition.Summary));
    }
}
