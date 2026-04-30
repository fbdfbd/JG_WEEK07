public enum EWeekFlowScreenType
{
    WeekFeedback,
    EventStep,
    ChoiceResult,
    EventResult,
    Ending,
    EndingFollowUp
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
        InteractiveEventResultPresentation eventResult,
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
        EventResult = eventResult;
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
    public InteractiveEventResultPresentation EventResult { get; }
    public EndingPresentation Ending { get; }
    public NemoFeedbackPresentation NemoFeedback { get; }

    public static WeekFlowScreen CreateWeekFeedback(
        SO_WeekDefinition weekDefinition,
        WeekFeedbackPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.WeekFeedback, weekDefinition, null, null, null, presentation, default, default, default, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateEventStep(
        SO_WeekDefinition weekDefinition,
        SO_InteractiveEventDefinition eventDefinition,
        SO_InteractiveEventStepDefinition stepDefinition,
        InteractiveEventPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.EventStep, weekDefinition, eventDefinition, stepDefinition, null, default, presentation, default, default, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateChoiceResult(
        SO_WeekDefinition weekDefinition,
        SO_InteractiveEventDefinition eventDefinition,
        InteractiveEventChoiceData choiceData,
        InteractiveEventChoiceResultPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.ChoiceResult, weekDefinition, eventDefinition, null, choiceData, default, default, presentation, default, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateEventResult(
        SO_WeekDefinition weekDefinition,
        InteractiveEventResultPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.EventResult, weekDefinition, null, null, null, default, default, default, presentation, default, nemoFeedback);
    }

    public static WeekFlowScreen CreateEnding(
        SO_WeekDefinition weekDefinition,
        EndingPresentation presentation,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.Ending, weekDefinition, null, null, null, default, default, default, default, presentation, nemoFeedback);
    }

    public static WeekFlowScreen CreateEndingFollowUp(
        SO_WeekDefinition weekDefinition,
        NemoFeedbackPresentation nemoFeedback)
    {
        return new WeekFlowScreen(EWeekFlowScreenType.EndingFollowUp, weekDefinition, null, null, null, default, default, default, default, default, nemoFeedback);
    }
}
