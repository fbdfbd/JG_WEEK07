using System.Collections.Generic;

public sealed class CsvImportContext
{
    public CsvImportContext(CsvImportSettings settings, CsvDataset dataset, CsvImportReport report)
    {
        Settings = settings;
        Dataset = dataset;
        Report = report;
    }

    public CsvImportSettings Settings { get; }
    public CsvDataset Dataset { get; }
    public CsvImportReport Report { get; }

    public Dictionary<string, SO_FlagDefinition> FlagsById { get; } = new();
    public Dictionary<string, SO_CardInfoTypeDefinition> CardTypesById { get; } = new();
    public Dictionary<string, SO_DialogueSpeakerDefinition> SpeakersById { get; } = new();
    public Dictionary<string, SO_CardInteractionDefinition> InteractionsById { get; } = new();
    public Dictionary<string, SO_CardInfoDefinition> CardsById { get; } = new();
    public Dictionary<string, ImportedWeekBundle> WeeksById { get; } = new();
    public Dictionary<string, SO_InteractiveEventDefinition> EventsById { get; } = new();
    public Dictionary<string, SO_EventResultDefinition> EventResultsByEventId { get; } = new();
    public Dictionary<string, SO_InteractiveEventStepDefinition> StepsByKey { get; } = new();

    public static string BuildStepKey(string eventId, string stepId)
    {
        return $"{eventId}::{stepId}";
    }

    public static string BuildChoiceKey(string eventId, string stepId, string choiceId)
    {
        return $"{eventId}::{stepId}::{choiceId}";
    }
}

public sealed class ImportedWeekBundle
{
    public ImportedWeekBundle(
        SO_WeekDefinition week,
        SO_WeekPreTurnDefinition preTurn,
        SO_WeekDayFlowDefinition dayFlow,
        SO_WeekNightFlowDefinition nightFlow)
    {
        Week = week;
        PreTurn = preTurn;
        DayFlow = dayFlow;
        NightFlow = nightFlow;
    }

    public SO_WeekDefinition Week { get; }
    public SO_WeekPreTurnDefinition PreTurn { get; }
    public SO_WeekDayFlowDefinition DayFlow { get; }
    public SO_WeekNightFlowDefinition NightFlow { get; }
}
