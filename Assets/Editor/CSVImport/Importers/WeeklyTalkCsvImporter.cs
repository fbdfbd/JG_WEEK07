using System;
using System.Linq;

public static class WeeklyTalkCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        if (context.Dataset.WeeklyTalks == null || context.Dataset.WeeklyTalks.Count == 0)
        {
            return;
        }

        SO_WeeklyTalk[] talks = context.Dataset.WeeklyTalks
            .GroupBy(row => row.WeekId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => ImportWeeklyTalk(context, group.Key, group.ToArray()))
            .ToArray();

        string catalogPath = CsvImportAssetUtility.CombineAssetPath(
            context.Settings.OutputRootPath,
            "WeeklyTalk",
            "SO_WeeklyTalkCatalog.asset");
        SO_WeeklyTalkCatalog catalog = CsvImportAssetUtility.LoadOrCreateAsset<SO_WeeklyTalkCatalog>(
            catalogPath,
            "SO_WeeklyTalkCatalog",
            context.Report);
        CsvImportAssetUtility.SetField(catalog, "_talks", talks.ToList());
        CsvImportAssetUtility.MarkDirty(catalog);
    }

    private static SO_WeeklyTalk ImportWeeklyTalk(
        CsvImportContext context,
        string weekId,
        WeeklyTalkRow[] rows)
    {
        string path = CsvImportAssetUtility.CombineAssetPath(
            context.Settings.OutputRootPath,
            "WeeklyTalk",
            $"SO_WeeklyTalk_{weekId.ToUpperInvariant()}.asset");
        SO_WeeklyTalk talk = CsvImportAssetUtility.LoadOrCreateAsset<SO_WeeklyTalk>(
            path,
            $"SO_WeeklyTalk_{weekId.ToUpperInvariant()}",
            context.Report);

        CsvImportAssetUtility.SetField(talk, "_weekId", weekId);
        CsvImportAssetUtility.SetField(talk, "_entries", rows
            .OrderByDescending(row => row.Priority)
            .ThenBy(row => Enum.Parse<WeeklyTalkStatDirection>(row.Direction, true))
            .ThenBy(row => row.VariantOrder)
            .Select(row => BuildEntry(context, row))
            .ToArray());

        CsvImportAssetUtility.MarkDirty(talk);
        return talk;
    }

    private static WeeklyTalkEntryData BuildEntry(CsvImportContext context, WeeklyTalkRow row)
    {
        WeeklyTalkEntryData entry = new();
        CsvImportAssetUtility.SetField(entry, "_id", BuildEntryId(row));
        CsvImportAssetUtility.SetField(entry, "_direction", Enum.Parse<WeeklyTalkStatDirection>(row.Direction, true));
        CsvImportAssetUtility.SetField(entry, "_variantOrder", row.VariantOrder);
        CsvImportAssetUtility.SetField(entry, "_priority", row.Priority);
        CsvImportAssetUtility.SetField(entry, "_weight", Math.Max(1, row.Weight));
        CsvImportAssetUtility.SetField(entry, "_displaySeconds", row.DisplaySeconds);
        CsvImportAssetUtility.SetField(entry, "_cooldownSeconds", row.CooldownSeconds);
        CsvImportAssetUtility.SetField(entry, "_allowAuto", row.AllowAuto);
        CsvImportAssetUtility.SetField(entry, "_allowClick", row.AllowClick);
        CsvImportAssetUtility.SetField(entry, "_context", row.Context);
        CsvImportAssetUtility.SetField(
            entry,
            "_nemoState",
            string.IsNullOrWhiteSpace(row.NemoState)
                ? NemoEmotionState.None
                : Enum.Parse<NemoEmotionState>(row.NemoState, true));
        CsvImportAssetUtility.SetField(
            entry,
            "_interactions",
            row.InteractionIds.Select(id => context.InteractionsById[id]).ToArray());
        CsvImportAssetUtility.SetField(entry, "_conditions", BuildConditions(context, row));
        return entry;
    }

    private static WeeklyTalkConditionData BuildConditions(CsvImportContext context, WeeklyTalkRow row)
    {
        WeeklyTalkConditionData conditions = new();
        CsvImportAssetUtility.SetField(
            conditions,
            "_requiredFlags",
            row.RequiredFlagIds.Select(id => context.FlagsById[id]).ToArray());
        CsvImportAssetUtility.SetField(
            conditions,
            "_blockedFlags",
            row.BlockedFlagIds.Select(id => context.FlagsById[id]).ToArray());
        CsvImportAssetUtility.SetField(conditions, "_statRequirements", Array.Empty<WeekStatRequirementData>());
        return conditions;
    }

    private static string BuildEntryId(WeeklyTalkRow row)
    {
        return $"{row.WeekId}_{row.Direction}_{row.VariantOrder}";
    }
}
