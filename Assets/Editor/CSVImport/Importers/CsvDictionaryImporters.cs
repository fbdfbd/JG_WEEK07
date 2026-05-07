using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FlagCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        foreach (FlagRow row in context.Dataset.Flags)
        {
            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Runtime",
                "Flags",
                $"{row.Id}.asset");
            SO_FlagDefinition asset = CsvImportAssetUtility.LoadOrCreateAsset<SO_FlagDefinition>(path, row.Id, context.Report);
            CsvImportAssetUtility.SetField(asset, "_id", row.Id);
            CsvImportAssetUtility.SetField(asset, "_displayName", row.DisplayName);
            CsvImportAssetUtility.SetField(asset, "_description", row.Description);
            CsvImportAssetUtility.MarkDirty(asset);
            context.FlagsById[row.Id] = asset;
        }
    }
}

public static class CardTypeCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        foreach (CardTypeRow row in context.Dataset.CardTypes)
        {
            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Card",
                "Types",
                $"{row.Id}.asset");
            SO_CardInfoTypeDefinition asset = CsvImportAssetUtility.LoadOrCreateAsset<SO_CardInfoTypeDefinition>(path, row.Id, context.Report);
            CsvImportAssetUtility.SetField(asset, "_id", row.Id);
            CsvImportAssetUtility.SetField(asset, "_displayName", row.DisplayName);
            CsvImportAssetUtility.MarkDirty(asset);
            context.CardTypesById[row.Id] = asset;
        }
    }
}

public static class SpeakerCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        foreach (SpeakerRow row in context.Dataset.Speakers)
        {
            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Week",
                "DialogueSpeakers",
                $"{row.Id}.asset");
            SO_DialogueSpeakerDefinition asset = CsvImportAssetUtility.LoadOrCreateAsset<SO_DialogueSpeakerDefinition>(path, row.Id, context.Report);
            CsvImportAssetUtility.SetField(asset, "_id", row.Id);
            CsvImportAssetUtility.SetField(asset, "_displayName", row.DisplayName);
            CsvImportAssetUtility.SetField(asset, "_defaultVisualState", Enum.Parse<ENemoVisualState>(row.DefaultVisualState, true));
            CsvImportAssetUtility.MarkDirty(asset);
            context.SpeakersById[row.Id] = asset;
        }
    }
}

public static class InteractionCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        foreach (InteractionRow row in context.Dataset.Interactions)
        {
            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Card",
                "Interactions",
                $"{row.Id}.asset");
            SO_CardInteractionDefinition asset = CreateOrLoadAsset(row, path, context);
            context.InteractionsById[row.Id] = asset;
        }

        foreach (InteractionRow row in context.Dataset.Interactions)
        {
            SO_CardInteractionDefinition asset = context.InteractionsById[row.Id];
            ConfigureAsset(asset, row, context);
            CsvImportAssetUtility.MarkDirty(asset);
        }
    }

    private static SO_CardInteractionDefinition CreateOrLoadAsset(InteractionRow row, string path, CsvImportContext context)
    {
        Type assetType = row.Kind switch
        {
            "stat_delta" => typeof(SO_CardInteraction_StatDelta),
            "set_flag" => typeof(SO_CardInteraction_SetFlag),
            "group" => typeof(SO_CardInteraction_GroupStatDelta),
            "conditional_stat_delta" => typeof(SO_CardInteraction_ConditionalStatDelta),
            "add_reaction_log" => typeof(SO_CardInteraction_AddReactionLog),
            _ => throw new InvalidOperationException($"Unsupported interaction kind: {row.Kind}"),
        };

        SO_CardInteractionDefinition existing = AssetDatabase.LoadAssetAtPath(path, assetType) as SO_CardInteractionDefinition;
        if (existing != null)
        {
            existing.name = row.Id;
            context.Report.RecordUpdated($"Updated {assetType.Name}: {path}");
            return existing;
        }

        CsvImportAssetUtility.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
        SO_CardInteractionDefinition created = (SO_CardInteractionDefinition)ScriptableObject.CreateInstance(assetType);
        created.name = row.Id;
        AssetDatabase.CreateAsset(created, path);
        context.Report.RecordCreated($"Created {assetType.Name}: {path}");
        return created;
    }

    private static void ConfigureAsset(SO_CardInteractionDefinition asset, InteractionRow row, CsvImportContext context)
    {
        CsvImportAssetUtility.SetField(asset, "_displayName", row.DisplayName);

        switch (asset)
        {
            case SO_CardInteraction_StatDelta statDelta:
                CsvImportAssetUtility.SetField(statDelta, "_statType", ParseEnum<EChildStatusType>(row.StatType));
                CsvImportAssetUtility.SetField(statDelta, "_amount", row.Amount);
                CsvImportAssetUtility.SetField(statDelta, "_toastMessage", row.DisplayName);
                break;
            case SO_CardInteraction_SetFlag setFlag:
                CsvImportAssetUtility.SetField(setFlag, "_flagDefinition", Resolve(context.FlagsById, row.FlagId));
                break;
            case SO_CardInteraction_GroupStatDelta groupStatDelta:
                CsvImportAssetUtility.SetField(
                    groupStatDelta,
                    "_interactions",
                    row.ChildInteractionIds.Select(id => Resolve(context.InteractionsById, id)).ToArray());
                break;
            case SO_CardInteraction_ConditionalStatDelta conditionalStatDelta:
                CsvImportAssetUtility.SetField(conditionalStatDelta, "_conditionStat", ParseEnum<EChildStatusType>(row.ConditionStat));
                CsvImportAssetUtility.SetField(conditionalStatDelta, "_minValue", row.MinValue);
                CsvImportAssetUtility.SetField(conditionalStatDelta, "_targetStat", ParseEnum<EChildStatusType>(row.TargetStat));
                CsvImportAssetUtility.SetField(conditionalStatDelta, "_amount", row.Amount);
                CsvImportAssetUtility.SetField(conditionalStatDelta, "_toastMessage", row.DisplayName);
                break;
            case SO_CardInteraction_AddReactionLog reactionLog:
                CsvImportAssetUtility.SetField(reactionLog, "_reactionText", row.ReactionText);
                break;
            default:
                throw new InvalidOperationException($"Unsupported interaction asset type: {asset.GetType().Name}");
        }
    }

    private static T Resolve<T>(System.Collections.Generic.IReadOnlyDictionary<string, T> dictionary, string id)
        where T : class
    {
        return dictionary[id];
    }

    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct
    {
        return Enum.Parse<TEnum>(value, true);
    }
}

public static class CardCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        foreach (CardRow row in context.Dataset.Cards)
        {
            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Card",
                "Cards",
                $"{row.Id}.asset");
            SO_CardInfoDefinition asset = CsvImportAssetUtility.LoadOrCreateAsset<SO_CardInfoDefinition>(path, row.Id, context.Report);
            CsvImportAssetUtility.SetField(asset, "_id", row.Id);
            CsvImportAssetUtility.SetField(asset, "_cardType", context.CardTypesById[row.CardTypeId]);
            CsvImportAssetUtility.SetField(asset, "_title", row.Title);
            CsvImportAssetUtility.SetField(asset, "_originalText", row.OriginalText);
            CsvImportAssetUtility.SetField(asset, "_options", BuildOptions(row.Id, context));
            CsvImportAssetUtility.MarkDirty(asset);
            context.CardsById[row.Id] = asset;
        }
    }

    private static CardOptionData[] BuildOptions(string cardId, CsvImportContext context)
    {
        return context.Dataset.CardOptions
            .Where(row => string.Equals(row.CardId, cardId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.OptionOrder)
            .Select(row =>
            {
                CardOptionData option = new();
                CsvImportAssetUtility.SetField(option, "_semantic", Enum.Parse<ECardOptionSemantic>(row.Semantic, true));
                CsvImportAssetUtility.SetField(option, "_label", row.Label);
                CsvImportAssetUtility.SetField(option, "_presentedText", row.PresentedText);
                CsvImportAssetUtility.SetField(
                    option,
                    "_interactions",
                    row.InteractionIds.Select(id => context.InteractionsById[id]).ToArray());
                return option;
            })
            .ToArray();
    }
}
