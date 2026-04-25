using System;
using System.Linq;
using DG.Tweening;

public static class EventCutsceneCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        ImportRuleCatalog(context);
        ImportSequenceLibrary(context);
    }

    private static void ImportRuleCatalog(CsvImportContext context)
    {
        if (context.Dataset.EventCutsceneRules == null || context.Dataset.EventCutsceneRules.Count == 0)
        {
            return;
        }

        string path = CsvImportAssetUtility.CombineAssetPath(
            context.Settings.OutputRootPath,
            "Cutscene",
            "EventCutsceneRuleCatalog_Generated.asset");

        SO_EventCutsceneRuleCatalog catalog = CsvImportAssetUtility.LoadOrCreateAsset<SO_EventCutsceneRuleCatalog>(
            path,
            "EventCutsceneRuleCatalog_Generated",
            context.Report);

        EventCutsceneRuleData[] rules = context.Dataset.EventCutsceneRules
            .Select(CreateRuleData)
            .ToArray();

        CsvImportAssetUtility.SetField(catalog, "_rules", rules);
        CsvImportAssetUtility.MarkDirty(catalog);
    }

    private static void ImportSequenceLibrary(CsvImportContext context)
    {
        if (context.Dataset.CutsceneSequenceCommands == null || context.Dataset.CutsceneSequenceCommands.Count == 0)
        {
            return;
        }

        string path = CsvImportAssetUtility.CombineAssetPath(
            context.Settings.OutputRootPath,
            "Cutscene",
            "CutsceneSequenceLibrary_Generated.asset");

        SO_CutsceneSequenceLibrary library = CsvImportAssetUtility.LoadOrCreateAsset<SO_CutsceneSequenceLibrary>(
            path,
            "CutsceneSequenceLibrary_Generated",
            context.Report);

        CutsceneSequenceData[] sequences = context.Dataset.CutsceneSequenceCommands
            .GroupBy(row => row.SequenceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateSequenceData(group.Key, group.OrderBy(row => row.Order).ToArray()))
            .ToArray();

        CsvImportAssetUtility.SetField(library, "_sequences", sequences);
        CsvImportAssetUtility.MarkDirty(library);
    }

    private static EventCutsceneRuleData CreateRuleData(EventCutsceneRuleRow row)
    {
        EventCutsceneRuleData data = new();
        CsvImportAssetUtility.SetField(data, "_ruleId", row.Id);
        CsvImportAssetUtility.SetField(data, "_enabled", row.Enabled);
        CsvImportAssetUtility.SetField(data, "_weekId", row.WeekId);
        CsvImportAssetUtility.SetField(data, "_eventId", row.EventId);
        CsvImportAssetUtility.SetField(data, "_moment", Enum.Parse<EWeekFlowCutsceneMoment>(row.Moment, true));
        CsvImportAssetUtility.SetField(data, "_sequenceId", row.SequenceId);
        CsvImportAssetUtility.SetField(data, "_specialPlayerId", row.SpecialPlayerId);
        return data;
    }

    private static CutsceneSequenceData CreateSequenceData(string sequenceId, CutsceneSequenceCommandRow[] rows)
    {
        CutsceneSequenceData sequence = new();
        CsvImportAssetUtility.SetField(sequence, "_sequenceId", sequenceId);
        CsvImportAssetUtility.SetField(sequence, "_commands", rows.Select(CreateCommandData).ToArray());
        return sequence;
    }

    private static CutsceneCommandData CreateCommandData(CutsceneSequenceCommandRow row)
    {
        CutsceneCommandData command = new();
        CsvImportAssetUtility.SetField(command, "_commandType", Enum.Parse<EDataCutsceneCommandType>(row.Command, true));
        CsvImportAssetUtility.SetField(command, "_targetKey", row.TargetKey);
        CsvImportAssetUtility.SetField(command, "_value1", row.Value1);
        CsvImportAssetUtility.SetField(command, "_value2", row.Value2);
        CsvImportAssetUtility.SetField(command, "_value3", row.Value3);
        CsvImportAssetUtility.SetField(command, "_duration", row.Duration);
        CsvImportAssetUtility.SetField(command, "_ease", ParseEase(row.Ease));
        CsvImportAssetUtility.SetField(command, "_blocking", row.Blocking);
        return command;
    }

    private static Ease ParseEase(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Ease.OutCubic
            : Enum.Parse<Ease>(value, true);
    }
}
