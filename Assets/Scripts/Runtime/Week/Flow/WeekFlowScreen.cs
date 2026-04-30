public enum EWeekFlowScreenType
{
    WeekFeedback,
    EventStep,
    ChoiceResult,
    WeeklyResultLog,
    WeeklyStatResult,
    Ending,
    EndingFollowUp
}

public readonly struct WeekFlowPresentationContext
{
    public WeekFlowPresentationContext(
        SO_WeekDefinition previousWeek,
        SO_WeekDefinition currentWeek,
        WeekFlowScreen previousScreen,
        WeekFlowScreen nextScreen,
        bool didReplaceScreen)
    {
        PreviousWeek = previousWeek;
        CurrentWeek = currentWeek;
        PreviousScreen = previousScreen;
        NextScreen = nextScreen;
        DidReplaceScreen = didReplaceScreen;
    }

    public SO_WeekDefinition PreviousWeek { get; }
    public SO_WeekDefinition CurrentWeek { get; }
    public WeekFlowScreen PreviousScreen { get; }
    public WeekFlowScreen NextScreen { get; }
    public bool DidReplaceScreen { get; }
    public bool DidChangeWeek => PreviousWeek != CurrentWeek;
    public bool DidClearScreen => DidReplaceScreen && NextScreen == null;
}

public sealed class WeekFlowScreen
{
    private WeekFlowScreen(
        EWeekFlowScreenType screenType,
        SO_WeekDefinition weekDefinition,
        SO_InteractiveEventDefinition eventDefinition,
        SO_InteractiveEventStepDefinition stepDefinition,
        InteractiveEventChoiceData choiceData,
        WeekFeedbackPresentation weekFeedback,
        InteractiveEventPresentation eventStep,
        InteractiveEventChoiceResultPresentation choiceResult,
        WeeklyResultLogPresentation weeklyResultLog,
        WeeklyStatResultPresentation weeklyStatResult,
        EndingPresentation ending,
        NemoFeedbackPresentation nemoFeedback)
    {
        ScreenType = screenType;
        WeekDefinition = weekDefinition;
        EventDefinition = eventDefinition;
        StepDefinition = stepDefinition;
        ChoiceData = choiceData;
        WeekFeedback = weekFeedback;
        EventStep = eventStep;
        ChoiceResult = choiceResult;
        WeeklyResultLog = weeklyResultLog;
        WeeklyStatResult = weeklyStatResult;
        Ending = ending;
        NemoFeedback = nemoFeedback;
    }

    public EWeekFlowScreenType ScreenType { get; }
    public SO_WeekDefinition WeekDefinition { get; }
    public SO_InteractiveEventDefinition EventDefinition { get; }
    public SO_InteractiveEventStepDefinition StepDefinition { get; }
    public InteractiveEventChoiceData ChoiceData { get; }
    public WeekFeedbackPresentation WeekFeedback { get; }
    public InteractiveEventPresentation EventStep { get; }
    public InteractiveEventChoiceResultPresentation ChoiceResult { get; }
    public WeeklyResultLogPresentation WeeklyResultLog { get; }
    public WeeklyStatResultPresentation WeeklyStatResult { get; }
    public EndingPresentation Ending { get; }
    public NemoFeedbackPresentation NemoFeedback { get; }

    public static WeekFlowScreen CreateWeekFeedback(
        SO_WeekDefinition weekDefinition,
        WeekFeedbackPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.WeekFeedback, weekDefinition, null, null, null, presentation, default, default, default, default, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateEventStep(
        SO_WeekDefinition weekDefinition,
        SO_InteractiveEventDefinition eventDefinition,
        SO_InteractiveEventStepDefinition stepDefinition,
        InteractiveEventPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.EventStep, weekDefinition, eventDefinition, stepDefinition, null, default, presentation, default, default, default, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateChoiceResult(
        SO_WeekDefinition weekDefinition,
        SO_InteractiveEventDefinition eventDefinition,
        InteractiveEventChoiceData choiceData,
        InteractiveEventChoiceResultPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.ChoiceResult, weekDefinition, eventDefinition, null, choiceData, default, default, presentation, default, default, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateWeeklyResultLog(
        SO_WeekDefinition weekDefinition,
        WeeklyResultLogPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.WeeklyResultLog, weekDefinition, null, null, null, default, default, default, presentation, default, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateWeeklyStatResult(
        SO_WeekDefinition weekDefinition,
        WeeklyStatResultPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.WeeklyStatResult, weekDefinition, null, null, null, default, default, default, default, presentation, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateEnding(
        SO_WeekDefinition weekDefinition,
        EndingPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.Ending, weekDefinition, null, null, null, default, default, default, default, default, presentation, nemoFeedback);
    }

    public static WeekFlowScreen CreateEndingFollowUp(
        SO_WeekDefinition weekDefinition,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.EndingFollowUp, weekDefinition, null, null, null, default, default, default, default, default, default, nemoFeedback);
    }
}
