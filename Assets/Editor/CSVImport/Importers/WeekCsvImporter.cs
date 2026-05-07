using System;
using System.Linq;

public static class WeekCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        foreach (WeekRow row in context.Dataset.Weeks)
        {
            string weekFolder = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Week",
                row.Id);
            SO_WeekDefinition week = CsvImportAssetUtility.LoadOrCreateAsset<SO_WeekDefinition>(
                CsvImportAssetUtility.CombineAssetPath(weekFolder, $"WeekDefinition_{row.Id}.asset"),
                $"WeekDefinition_{row.Id}",
                context.Report);
            SO_WeekPreTurnDefinition preTurn = CsvImportAssetUtility.LoadOrCreateAsset<SO_WeekPreTurnDefinition>(
                CsvImportAssetUtility.CombineAssetPath(weekFolder, $"WeekPreTurn_{row.Id}.asset"),
                $"{row.Id}_PreTurn",
                context.Report);
            SO_WeekDayFlowDefinition dayFlow = CsvImportAssetUtility.LoadOrCreateAsset<SO_WeekDayFlowDefinition>(
                CsvImportAssetUtility.CombineAssetPath(weekFolder, $"WeekDayFlow_{row.Id}.asset"),
                $"{row.Id}_DayFlow",
                context.Report);
            SO_WeekNightFlowDefinition nightFlow = CsvImportAssetUtility.LoadOrCreateAsset<SO_WeekNightFlowDefinition>(
                CsvImportAssetUtility.CombineAssetPath(weekFolder, $"WeekNightFlow_{row.Id}.asset"),
                $"{row.Id}_NightFlow",
                context.Report);

            CsvImportAssetUtility.SetField(week, "_id", row.Id);
            CsvImportAssetUtility.SetField(week, "_weekIndex", row.WeekIndex);
            CsvImportAssetUtility.SetField(week, "_title", row.Title);
            CsvImportAssetUtility.SetField(week, "_summary", row.Summary);
            CsvImportAssetUtility.SetField(week, "_preTurn", preTurn);
            CsvImportAssetUtility.SetField(week, "_dayFlow", dayFlow);
            CsvImportAssetUtility.SetField(week, "_nightFlow", nightFlow);
            CsvImportAssetUtility.SetField(week, "_weekRules", Array.Empty<SO_WeekRuleDefinition>());
            CsvImportAssetUtility.SetField(week, "_onWeekStartInteractions", row.OnWeekStartInteractionIds.Select(id => context.InteractionsById[id]).ToArray());
            CsvImportAssetUtility.SetField(week, "_onWeekEndInteractions", row.OnWeekEndInteractionIds.Select(id => context.InteractionsById[id]).ToArray());

            CsvImportAssetUtility.SetField(preTurn, "_title", row.PreTurnTitle);
            CsvImportAssetUtility.SetField(preTurn, "_summary", row.PreTurnSummary);
            CsvImportAssetUtility.SetField(preTurn, "_informationCards", BuildWeekCards(row.Id, context));

            CsvImportAssetUtility.SetField(dayFlow, "_routineEvents", Array.Empty<SO_DayRoutineEventDefinition>());
            CsvImportAssetUtility.SetField(dayFlow, "_storyEvents", Array.Empty<SO_StoryEventDefinition>());
            CsvImportAssetUtility.SetField(nightFlow, "_dialogues", Array.Empty<SO_PrivateDialogueDefinition>());

            CsvImportAssetUtility.MarkDirty(week);
            CsvImportAssetUtility.MarkDirty(preTurn);
            CsvImportAssetUtility.MarkDirty(dayFlow);
            CsvImportAssetUtility.MarkDirty(nightFlow);

            context.WeeksById[row.Id] = new ImportedWeekBundle(week, preTurn, dayFlow, nightFlow);
        }
    }

    private static WeekCardEntryData[] BuildWeekCards(string weekId, CsvImportContext context)
    {
        return context.Dataset.WeekCards
            .Where(row => string.Equals(row.WeekId, weekId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.DisplayOrder)
            .Select(row =>
            {
                WeekCardEntryData entry = new();
                CsvImportAssetUtility.SetField(entry, "_card", context.CardsById[row.CardId]);
                CsvImportAssetUtility.SetField(entry, "_isRequired", row.IsRequired);
                CsvImportAssetUtility.SetField(entry, "_displayOrder", row.DisplayOrder);
                return entry;
            })
            .ToArray();
    }
}
