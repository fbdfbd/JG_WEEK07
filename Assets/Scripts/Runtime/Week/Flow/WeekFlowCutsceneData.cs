public enum EWeekFlowCutsceneMoment
{
    EventEnter,
    EventExit,
    ScreenEnter,
    LineEnter
}

public readonly struct WeekFlowCutsceneRequest
{
    public WeekFlowCutsceneRequest(
        EWeekFlowCutsceneMoment moment,
        EWeekFlowScreenType screenType,
        SO_InteractiveEventDefinition eventDefinition,
        SO_InteractiveEventStepDefinition stepDefinition,
        InteractiveEventChoiceData choiceData,
        string weekId,
        int weekIndex,
        string eventId,
        string stepName,
        int dialogueIndex,
        string choiceLabel,
        SO_DialogueSpeakerDefinition dialogueSpeaker,
        RuntimeChildState childState,
        RuntimeWeekResult lastWeekResult)
    {
        Moment = moment;
        ScreenType = screenType;
        EventDefinition = eventDefinition;
        StepDefinition = stepDefinition;
        ChoiceData = choiceData;
        WeekId = weekId;
        WeekIndex = weekIndex;
        EventId = eventId;
        StepName = stepName;
        DialogueIndex = dialogueIndex;
        ChoiceLabel = choiceLabel;
        DialogueSpeaker = dialogueSpeaker;
        ChildState = childState;
        LastWeekResult = lastWeekResult;
    }

    public EWeekFlowCutsceneMoment Moment { get; }
    public EWeekFlowScreenType ScreenType { get; }
    public SO_InteractiveEventDefinition EventDefinition { get; }
    public SO_InteractiveEventStepDefinition StepDefinition { get; }
    public InteractiveEventChoiceData ChoiceData { get; }
    public string WeekId { get; }
    public int WeekIndex { get; }
    public string EventId { get; }
    public string StepName { get; }
    public int DialogueIndex { get; }
    public string ChoiceLabel { get; }
    public SO_DialogueSpeakerDefinition DialogueSpeaker { get; }
    public RuntimeChildState ChildState { get; }
    public RuntimeWeekResult LastWeekResult { get; }

    public static WeekFlowCutsceneRequest CreateEventEnter(
        WeekFlowScreen screen,
        RuntimeChildState childState,
        RuntimeWeekResult lastWeekResult)
    {
        return CreateEventRequest(EWeekFlowCutsceneMoment.EventEnter, screen, childState, lastWeekResult);
    }

    public static WeekFlowCutsceneRequest CreateEventExit(
        WeekFlowScreen screen,
        RuntimeChildState childState,
        RuntimeWeekResult lastWeekResult)
    {
        return CreateEventRequest(EWeekFlowCutsceneMoment.EventExit, screen, childState, lastWeekResult);
    }

    public static WeekFlowCutsceneRequest CreateScreenEnter(
        WeekFlowScreen screen,
        RuntimeChildState childState,
        RuntimeWeekResult lastWeekResult)
    {
        if (screen == null)
        {
            return default;
        }

        return new WeekFlowCutsceneRequest(
            EWeekFlowCutsceneMoment.ScreenEnter,
            screen.ScreenType,
            screen.EventDefinition,
            screen.StepDefinition,
            screen.ChoiceData,
            ResolveWeekId(screen.WeekDefinition),
            screen.WeekDefinition != null ? screen.WeekDefinition.WeekIndex : 0,
            ResolveEventId(screen.EventDefinition),
            ResolveStepName(screen.StepDefinition),
            -1,
            ResolveChoiceLabel(screen.ChoiceData),
            null,
            childState,
            lastWeekResult);
    }

    public static WeekFlowCutsceneRequest CreateLineEnter(
        WeekFlowScreen screen,
        int dialogueIndex,
        RuntimeChildState childState,
        RuntimeWeekResult lastWeekResult)
    {
        if (screen == null)
        {
            return default;
        }

        return new WeekFlowCutsceneRequest(
            EWeekFlowCutsceneMoment.LineEnter,
            screen.ScreenType,
            screen.EventDefinition,
            screen.StepDefinition,
            screen.ChoiceData,
            ResolveWeekId(screen.WeekDefinition),
            screen.WeekDefinition != null ? screen.WeekDefinition.WeekIndex : 0,
            ResolveEventId(screen.EventDefinition),
            ResolveStepName(screen.StepDefinition),
            dialogueIndex,
            ResolveChoiceLabel(screen.ChoiceData),
            ResolveDialogueSpeaker(screen, dialogueIndex),
            childState,
            lastWeekResult);
    }

    private static WeekFlowCutsceneRequest CreateEventRequest(
        EWeekFlowCutsceneMoment moment,
        WeekFlowScreen screen,
        RuntimeChildState childState,
        RuntimeWeekResult lastWeekResult)
    {
        if (screen == null)
        {
            return default;
        }

        return new WeekFlowCutsceneRequest(
            moment,
            screen.ScreenType,
            screen.EventDefinition,
            screen.StepDefinition,
            screen.ChoiceData,
            ResolveWeekId(screen.WeekDefinition),
            screen.WeekDefinition != null ? screen.WeekDefinition.WeekIndex : 0,
            ResolveEventId(screen.EventDefinition),
            ResolveStepName(screen.StepDefinition),
            -1,
            ResolveChoiceLabel(screen.ChoiceData),
            ResolveFirstDialogueSpeaker(screen),
            childState,
            lastWeekResult);
    }

    private static string ResolveWeekId(SO_WeekDefinition weekDefinition)
    {
        if (weekDefinition == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(weekDefinition.Id)
            ? weekDefinition.Id
            : weekDefinition.name;
    }

    private static string ResolveEventId(SO_InteractiveEventDefinition eventDefinition)
    {
        if (eventDefinition == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(eventDefinition.Id)
            ? eventDefinition.Id
            : eventDefinition.name;
    }

    private static string ResolveStepName(SO_InteractiveEventStepDefinition stepDefinition)
    {
        return stepDefinition != null ? stepDefinition.name : string.Empty;
    }

    private static string ResolveChoiceLabel(InteractiveEventChoiceData choiceData)
    {
        return choiceData != null ? choiceData.Label : string.Empty;
    }

    private static SO_DialogueSpeakerDefinition ResolveFirstDialogueSpeaker(WeekFlowScreen screen)
    {
        return ResolveDialogueSpeaker(screen, 0);
    }

    private static SO_DialogueSpeakerDefinition ResolveDialogueSpeaker(WeekFlowScreen screen, int dialogueIndex)
    {
        if (screen == null || dialogueIndex < 0)
        {
            return null;
        }

        DialogueLineData[] lines = screen.ScreenType == EWeekFlowScreenType.ChoiceResult
            ? screen.ChoiceData?.ResponseDialogueLines
            : screen.StepDefinition?.DialogueLines;

        if (lines == null || dialogueIndex >= lines.Length)
        {
            return null;
        }

        return lines[dialogueIndex]?.Speaker;
    }
}
