using UnityEngine;

public sealed class WeekFlowAnalyticsTracker
{
    private SO_WeekDefinition _turnDwellWeek;
    private SO_WeekDefinition _postDayResultWeek;
    private float _turnDwellStartedAt;
    private float _postDayResultStartedAt;
    private bool _isTurnDwellActive;
    private bool _isPostDayResultPhaseActive;

    public void StartTurnDwell(SO_WeekDefinition week, string source)
    {
        if (week == null)
        {
            return;
        }

        if (_isTurnDwellActive && ReferenceEquals(_turnDwellWeek, week))
        {
            return;
        }

        _turnDwellWeek = week;
        _turnDwellStartedAt = Time.realtimeSinceStartup;
        _isTurnDwellActive = true;
        GameplayAnalyticsLogger.LogTurnDwellStarted(week, source);
    }

    public void EndTurnDwell(SO_WeekDefinition week, string source)
    {
        SO_WeekDefinition targetWeek = week != null ? week : _turnDwellWeek;
        if (!_isTurnDwellActive || targetWeek == null)
        {
            return;
        }

        float durationSeconds = Time.realtimeSinceStartup - _turnDwellStartedAt;
        GameplayAnalyticsLogger.LogTurnDwellEnded(targetWeek, durationSeconds, source);
        _isTurnDwellActive = false;
        _turnDwellWeek = null;
        _turnDwellStartedAt = 0f;
    }

    public void ObservePresentedScreen(WeekFlowScreen screen, bool isCurrentEventFromDayFlow)
    {
        if (screen == null)
        {
            return;
        }

        if (IsWeeklyResultScreen(screen.ScreenType))
        {
            StartPostDayResultPhase(screen);
            return;
        }

        if (screen.ScreenType == EWeekFlowScreenType.EventStep && !isCurrentEventFromDayFlow)
        {
            TryLogNightFlowStarted(screen);
        }
    }

    public void ObservePresentationCompleted(WeekFlowPresentationContext context)
    {
        if (context.DidClearScreen)
        {
            ClearPostDayResultPhase();
            StartTurnDwell(context.CurrentWeek, "flow_screen_cleared");
        }
    }

    public void Reset()
    {
        _turnDwellWeek = null;
        _postDayResultWeek = null;
        _turnDwellStartedAt = 0f;
        _postDayResultStartedAt = 0f;
        _isTurnDwellActive = false;
        _isPostDayResultPhaseActive = false;
    }

    private void StartPostDayResultPhase(WeekFlowScreen screen)
    {
        if (_isPostDayResultPhaseActive && ReferenceEquals(_postDayResultWeek, screen.WeekDefinition))
        {
            return;
        }

        _postDayResultWeek = screen.WeekDefinition;
        _postDayResultStartedAt = Time.realtimeSinceStartup;
        _isPostDayResultPhaseActive = true;
        GameplayAnalyticsLogger.LogPostDayResultPhaseStarted(screen.WeekDefinition, screen.ScreenType);
    }

    private void TryLogNightFlowStarted(WeekFlowScreen screen)
    {
        if (!_isPostDayResultPhaseActive)
        {
            return;
        }

        if (!IsSameWeek(_postDayResultWeek, screen.WeekDefinition))
        {
            ClearPostDayResultPhase();
            return;
        }

        float durationSeconds = Time.realtimeSinceStartup - _postDayResultStartedAt;
        GameplayAnalyticsLogger.LogNightFlowStarted(
            screen.WeekDefinition,
            screen.EventDefinition,
            durationSeconds);

        ClearPostDayResultPhase();
    }

    private static bool IsWeeklyResultScreen(EWeekFlowScreenType screenType)
    {
        return screenType == EWeekFlowScreenType.WeeklyResultLog
            || screenType == EWeekFlowScreenType.WeeklyStatResult;
    }

    private void ClearPostDayResultPhase()
    {
        _postDayResultWeek = null;
        _postDayResultStartedAt = 0f;
        _isPostDayResultPhaseActive = false;
    }

    private static bool IsSameWeek(SO_WeekDefinition first, SO_WeekDefinition second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first == null || second == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(first.Id)
            && string.Equals(first.Id, second.Id, System.StringComparison.OrdinalIgnoreCase);
    }
}
