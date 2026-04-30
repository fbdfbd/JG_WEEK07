public sealed class WeekFlowCinematicResolver
{
    public SO_WeekFlowCueDefinition ResolveEnterCue(WeekFlowScreen screen)
    {
        return ResolveScreenCue(screen, true);
    }

    public SO_WeekFlowCueDefinition ResolveExitCue(WeekFlowScreen screen)
    {
        return ResolveScreenCue(screen, false);
    }

    public SO_WeekFlowCueDefinition ResolveWeekChangeOutCue(SO_WeekDefinition weekDefinition)
    {
        return weekDefinition?.CinematicProfile?.WeekChangeOutCue;
    }

    public SO_WeekFlowCueDefinition ResolveWeekChangeInCue(SO_WeekDefinition weekDefinition)
    {
        return weekDefinition?.CinematicProfile?.WeekChangeInCue;
    }

    private static SO_WeekFlowCueDefinition ResolveScreenCue(WeekFlowScreen screen, bool isEnter)
    {
        if (screen == null)
        {
            return null;
        }

        return screen.ScreenType switch
        {
            EWeekFlowScreenType.WeekFeedback => GetCue(screen.WeekDefinition?.CinematicProfile?.WeekFeedbackCues, isEnter),
            EWeekFlowScreenType.EventStep => GetCue(screen.StepDefinition?.CinematicCues, isEnter)
                ?? GetCue(screen.EventDefinition?.CinematicProfile?.EventStepCues, isEnter)
                ?? GetCue(screen.WeekDefinition?.CinematicProfile?.EventStepCues, isEnter),
            EWeekFlowScreenType.ChoiceResult => GetCue(screen.ChoiceData?.ResultCinematicCues, isEnter)
                ?? GetCue(screen.EventDefinition?.CinematicProfile?.ChoiceResultCues, isEnter)
                ?? GetCue(screen.WeekDefinition?.CinematicProfile?.ChoiceResultCues, isEnter),
            EWeekFlowScreenType.EventResult => null,
            EWeekFlowScreenType.Ending => GetCue(screen.WeekDefinition?.CinematicProfile?.EndingCues, isEnter),
            EWeekFlowScreenType.EndingFollowUp => null,
            _ => null,
        };
    }

    private static SO_WeekFlowCueDefinition GetCue(WeekFlowScreenCues screenCues, bool isEnter)
    {
        if (screenCues == null)
        {
            return null;
        }

        return isEnter ? screenCues.EnterCue : screenCues.ExitCue;
    }
}
