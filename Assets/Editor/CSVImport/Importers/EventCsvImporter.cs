using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EventCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        CreateAllSteps(context);
        ConfigureAllSteps(context);
        CreateAndConfigureEvents(context);
        LinkWeekFlows(context);
    }

    private static void CreateAllSteps(CsvImportContext context)
    {
        foreach (EventStepRow row in context.Dataset.EventSteps)
        {
            EventRow eventRow = GetEventRow(context, row.EventId);
            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Week",
                eventRow.WeekId,
                "Steps",
                $"{row.EventId}__{row.StepId}.asset");
            SO_InteractiveEventStepDefinition step = CsvImportAssetUtility.LoadOrCreateAsset<SO_InteractiveEventStepDefinition>(
                path,
                $"{row.EventId}__{row.StepId}",
                context.Report);
            context.StepsByKey[CsvImportContext.BuildStepKey(row.EventId, row.StepId)] = step;
        }
    }

    private static void ConfigureAllSteps(CsvImportContext context)
    {
        foreach (EventStepRow row in context.Dataset.EventSteps)
        {
            SO_InteractiveEventStepDefinition step = context.StepsByKey[CsvImportContext.BuildStepKey(row.EventId, row.StepId)];
            DialogueLineData[] dialogueLines = BuildStepDialogueLines(context, row.EventId, row.StepId);
            CsvImportAssetUtility.SetField(step, "_titleOverride", row.TitleOverride);
            CsvImportAssetUtility.SetField(step, "_bodyText", row.BodyText);
            CsvImportAssetUtility.SetField(step, "_dialogueLines", dialogueLines);
            CsvImportAssetUtility.SetField(step, "_nemoLine", dialogueLines.Length > 0 ? dialogueLines[0].Text : string.Empty);
            CsvImportAssetUtility.SetField(step, "_useCustomVisualState", row.UseCustomVisualState);
            CsvImportAssetUtility.SetField(step, "_visualState", Enum.Parse<ENemoVisualState>(row.VisualState, true));
            CsvImportAssetUtility.SetField(step, "_onEnterInteractions", row.OnEnterInteractionIds.Select(id => context.InteractionsById[id]).ToArray());
            CsvImportAssetUtility.SetField(step, "_choices", BuildChoices(context, row.EventId, row.StepId));
            CsvImportAssetUtility.SetField(step, "_conditionalNext", BuildConditionalNext(context, row));
            CsvImportAssetUtility.SetField(
                step,
                "_nextStep",
                string.IsNullOrWhiteSpace(row.DefaultNextStepId)
                    ? null
                    : context.StepsByKey[CsvImportContext.BuildStepKey(row.EventId, row.DefaultNextStepId)]);
            CsvImportAssetUtility.MarkDirty(step);
        }
    }

    private static void CreateAndConfigureEvents(CsvImportContext context)
    {
        foreach (EventRow row in context.Dataset.Events)
        {
            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Week",
                row.WeekId,
                "Events",
                $"{row.Id}.asset");
            SO_InteractiveEventDefinition asset = CreateOrLoadEventAsset(row, path, context);
            CsvImportAssetUtility.SetField(asset, "_id", row.Id);
            CsvImportAssetUtility.SetField(asset, "_title", row.Title);
            CsvImportAssetUtility.SetField(asset, "_priority", row.Priority);
            CsvImportAssetUtility.SetField(asset, "_firstStep", context.StepsByKey[CsvImportContext.BuildStepKey(row.Id, row.FirstStepId)]);
            CsvImportAssetUtility.SetField(asset, "_onCompletedInteractions", row.OnCompletedInteractionIds.Select(id => context.InteractionsById[id]).ToArray());
            CsvImportAssetUtility.SetField(asset, "_conditions", BuildConditions(context, row.Id));

            if (asset is SO_DayRoutineEventDefinition routineEvent)
            {
                CsvImportAssetUtility.SetField(routineEvent, "_relatedInformationTypes", row.RelatedInformationTypeIds.Select(id => context.CardTypesById[id]).ToArray());
                CsvImportAssetUtility.SetField(routineEvent, "_preferredSemantics", row.PreferredSemantics.Select(value => Enum.Parse<ECardOptionSemantic>(value, true)).ToArray());
                CsvImportAssetUtility.SetField(
                    routineEvent,
                    "_linkedCard",
                    string.IsNullOrWhiteSpace(row.LinkedCardId)
                        ? null
                        : context.CardsById[row.LinkedCardId]);
                CsvImportAssetUtility.SetField(routineEvent, "_selectionRule", BuildSelectionRule(context, row.Id));
            }

            CsvImportAssetUtility.MarkDirty(asset);
            context.EventsById[row.Id] = asset;
        }
    }

    private static void LinkWeekFlows(CsvImportContext context)
    {
        foreach (WeekRow weekRow in context.Dataset.Weeks)
        {
            ImportedWeekBundle bundle = context.WeeksById[weekRow.Id];
            EventRow[] weekEvents = context.Dataset.Events
                .Where(row => string.Equals(row.WeekId, weekRow.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(row => row.EventOrder)
                .ThenByDescending(row => row.Priority)
                .ToArray();

            SO_DayRoutineEventDefinition[] routineEvents = weekEvents
                .Where(row => string.Equals(row.EventKind, "routine", StringComparison.OrdinalIgnoreCase))
                .Select(row => (SO_DayRoutineEventDefinition)context.EventsById[row.Id])
                .ToArray();
            SO_StoryEventDefinition[] storyEvents = weekEvents
                .Where(row => string.Equals(row.EventKind, "story", StringComparison.OrdinalIgnoreCase))
                .Select(row => (SO_StoryEventDefinition)context.EventsById[row.Id])
                .ToArray();
            SO_PrivateDialogueDefinition[] dialogues = weekEvents
                .Where(row => string.Equals(row.EventKind, "private_dialogue", StringComparison.OrdinalIgnoreCase))
                .Select(row => (SO_PrivateDialogueDefinition)context.EventsById[row.Id])
                .ToArray();

            CsvImportAssetUtility.SetField(bundle.DayFlow, "_routineEvents", routineEvents);
            CsvImportAssetUtility.SetField(bundle.DayFlow, "_storyEvents", storyEvents);
            CsvImportAssetUtility.SetField(bundle.NightFlow, "_dialogues", dialogues);

            CsvImportAssetUtility.MarkDirty(bundle.DayFlow);
            CsvImportAssetUtility.MarkDirty(bundle.NightFlow);
        }
    }

    private static SO_InteractiveEventDefinition CreateOrLoadEventAsset(EventRow row, string path, CsvImportContext context)
    {
        Type assetType = row.EventKind switch
        {
            "routine" => typeof(SO_DayRoutineEventDefinition),
            "story" => typeof(SO_StoryEventDefinition),
            "private_dialogue" => typeof(SO_PrivateDialogueDefinition),
            _ => throw new InvalidOperationException($"Unsupported event kind: {row.EventKind}"),
        };

        SO_InteractiveEventDefinition existing = AssetDatabase.LoadAssetAtPath(path, assetType) as SO_InteractiveEventDefinition;
        if (existing != null)
        {
            existing.name = row.Id;
            context.Report.RecordUpdated($"Updated {assetType.Name}: {path}");
            return existing;
        }

        CsvImportAssetUtility.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
        SO_InteractiveEventDefinition created = (SO_InteractiveEventDefinition)ScriptableObject.CreateInstance(assetType);
        created.name = row.Id;
        AssetDatabase.CreateAsset(created, path);
        context.Report.RecordCreated($"Created {assetType.Name}: {path}");
        return created;
    }

    private static WeekEventConditionData BuildConditions(CsvImportContext context, string eventId)
    {
        WeekEventConditionData conditions = new();
        CsvImportAssetUtility.SetField(
            conditions,
            "_requiredFlags",
            context.Dataset.EventFlagConditions
                .Where(row => string.Equals(row.EventId, eventId, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(row.Mode, "required", StringComparison.OrdinalIgnoreCase))
                .Select(row => context.FlagsById[row.FlagId])
                .ToArray());
        CsvImportAssetUtility.SetField(
            conditions,
            "_blockedFlags",
            context.Dataset.EventFlagConditions
                .Where(row => string.Equals(row.EventId, eventId, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(row.Mode, "blocked", StringComparison.OrdinalIgnoreCase))
                .Select(row => context.FlagsById[row.FlagId])
                .ToArray());
        CsvImportAssetUtility.SetField(
            conditions,
            "_statRequirements",
            context.Dataset.EventStatConditions
                .Where(row => string.Equals(row.EventId, eventId, StringComparison.OrdinalIgnoreCase))
                .Select(row =>
                {
                    WeekStatRequirementData requirement = new();
                    CsvImportAssetUtility.SetField(requirement, "_statType", Enum.Parse<EChildStatusType>(row.StatType, true));
                    CsvImportAssetUtility.SetField(requirement, "_useMinimum", row.UseMinimum);
                    CsvImportAssetUtility.SetField(requirement, "_minimumValue", row.MinimumValue);
                    CsvImportAssetUtility.SetField(requirement, "_useMaximum", row.UseMaximum);
                    CsvImportAssetUtility.SetField(requirement, "_maximumValue", row.MaximumValue);
                    return requirement;
                })
                .ToArray());
        CsvImportAssetUtility.SetField(
            conditions,
            "_informationRequirements",
            context.Dataset.EventInformationConditions
                .Where(row => string.Equals(row.EventId, eventId, StringComparison.OrdinalIgnoreCase))
                .Select(row =>
                {
                    InformationTypeConditionData requirement = new();
                    CsvImportAssetUtility.SetField(requirement, "_informationType", context.CardTypesById[row.InformationTypeId]);
                    CsvImportAssetUtility.SetField(requirement, "_useSemanticFilter", row.UseSemanticFilter);
                    CsvImportAssetUtility.SetField(
                        requirement,
                        "_semantic",
                        string.IsNullOrWhiteSpace(row.Semantic)
                            ? default(ECardOptionSemantic)
                            : Enum.Parse<ECardOptionSemantic>(row.Semantic, true));
                    CsvImportAssetUtility.SetField(requirement, "_minimumCount", row.MinimumCount);
                    return requirement;
                })
                .ToArray());
        return conditions;
    }

    private static EventSelectionRuleData BuildSelectionRule(CsvImportContext context, string eventId)
    {
        EventSelectionRuleData rule = new();
        EventSelectionRuleRow row = context.Dataset.EventSelectionRules
            .FirstOrDefault(selectionRule => string.Equals(selectionRule.EventId, eventId, StringComparison.OrdinalIgnoreCase));

        if (row == null)
        {
            return rule;
        }

        EEventSelectionMode mode = ParseSelectionMode(row.SelectorMode, row.IsDefault);
        CsvImportAssetUtility.SetField(rule, "_mode", mode);
        if (!string.IsNullOrWhiteSpace(row.SelectorStat))
        {
            CsvImportAssetUtility.SetField(rule, "_selectorStat", Enum.Parse<EChildStatusType>(row.SelectorStat, true));
        }

        CsvImportAssetUtility.SetField(rule, "_threshold", row.Threshold);
        CsvImportAssetUtility.SetField(rule, "_isDefault", row.IsDefault || mode == EEventSelectionMode.Default);
        return rule;
    }

    private static EEventSelectionMode ParseSelectionMode(string value, bool isDefault)
    {
        if (isDefault && string.IsNullOrWhiteSpace(value))
        {
            return EEventSelectionMode.Default;
        }

        return value?.Trim().ToLowerInvariant() switch
        {
            null or "" => EEventSelectionMode.None,
            "none" => EEventSelectionMode.None,
            "max_positive" => EEventSelectionMode.MaxPositive,
            "max_negative" => EEventSelectionMode.MaxNegative,
            "max_any" => EEventSelectionMode.MaxAny,
            "min_any" => EEventSelectionMode.MinAny,
            "default" => EEventSelectionMode.Default,
            _ => EEventSelectionMode.None,
        };
    }

    private static ConditionalStepNextData BuildConditionalNext(CsvImportContext context, EventStepRow row)
    {
        bool hasConditionalData =
            HasCondition(row.ConditionStat1, row.ConditionMin1, row.ConditionMax1) ||
            HasCondition(row.ConditionStat2, row.ConditionMin2, row.ConditionMax2) ||
            !string.IsNullOrWhiteSpace(row.ConditionalNextStepId) ||
            !string.IsNullOrWhiteSpace(row.ConditionalFallbackStepId);

        if (!hasConditionalData)
        {
            return null;
        }

        ConditionalStepNextData conditionalNext = new();
        CsvImportAssetUtility.SetField(
            conditionalNext,
            "_nextStep",
            string.IsNullOrWhiteSpace(row.ConditionalNextStepId)
                ? null
                : context.StepsByKey[CsvImportContext.BuildStepKey(row.EventId, row.ConditionalNextStepId)]);
        CsvImportAssetUtility.SetField(
            conditionalNext,
            "_fallbackStep",
            string.IsNullOrWhiteSpace(row.ConditionalFallbackStepId)
                ? null
                : context.StepsByKey[CsvImportContext.BuildStepKey(row.EventId, row.ConditionalFallbackStepId)]);
        CsvImportAssetUtility.SetField(
            conditionalNext,
            "_statRequirements",
            BuildConditionalStatRequirements(row).ToArray());
        return conditionalNext;
    }

    private static IEnumerable<WeekStatRequirementData> BuildConditionalStatRequirements(EventStepRow row)
    {
        WeekStatRequirementData first = BuildConditionalStatRequirement(
            row.ConditionStat1,
            row.ConditionMin1,
            row.ConditionMax1);
        if (first != null)
        {
            yield return first;
        }

        WeekStatRequirementData second = BuildConditionalStatRequirement(
            row.ConditionStat2,
            row.ConditionMin2,
            row.ConditionMax2);
        if (second != null)
        {
            yield return second;
        }
    }

    private static WeekStatRequirementData BuildConditionalStatRequirement(
        string statType,
        string minimumValue,
        string maximumValue)
    {
        if (!HasCondition(statType, minimumValue, maximumValue))
        {
            return null;
        }

        WeekStatRequirementData requirement = new();
        CsvImportAssetUtility.SetField(requirement, "_statType", Enum.Parse<EChildStatusType>(statType, true));
        CsvImportAssetUtility.SetField(requirement, "_useMinimum", !string.IsNullOrWhiteSpace(minimumValue));
        CsvImportAssetUtility.SetField(requirement, "_minimumValue", ParseOptionalInt(minimumValue));
        CsvImportAssetUtility.SetField(requirement, "_useMaximum", !string.IsNullOrWhiteSpace(maximumValue));
        CsvImportAssetUtility.SetField(requirement, "_maximumValue", ParseOptionalInt(maximumValue));
        return requirement;
    }

    private static bool HasCondition(string statType, string minimumValue, string maximumValue)
    {
        return !string.IsNullOrWhiteSpace(statType) ||
               !string.IsNullOrWhiteSpace(minimumValue) ||
               !string.IsNullOrWhiteSpace(maximumValue);
    }

    private static int ParseOptionalInt(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? 0 : int.Parse(value);
    }

    private static InteractiveEventChoiceData[] BuildChoices(CsvImportContext context, string eventId, string stepId)
    {
        return context.Dataset.EventChoices
            .Where(row =>
                string.Equals(row.EventId, eventId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(row.StepId, stepId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.ChoiceOrder)
            .Select(row =>
            {
                DialogueLineData[] dialogueLines = BuildChoiceDialogueLines(context, row.EventId, row.StepId, row.ChoiceId);
                InteractiveEventChoiceData choice = new();
                CsvImportAssetUtility.SetField(choice, "_label", row.Label);
                CsvImportAssetUtility.SetField(choice, "_responseDialogueLines", dialogueLines);
                CsvImportAssetUtility.SetField(choice, "_responseLine", dialogueLines.Length > 0 ? dialogueLines[0].Text : string.Empty);
                CsvImportAssetUtility.SetField(choice, "_interactions", row.InteractionIds.Select(id => context.InteractionsById[id]).ToArray());
                CsvImportAssetUtility.SetField(
                    choice,
                    "_nextStep",
                    string.IsNullOrWhiteSpace(row.NextStepId)
                        ? null
                        : context.StepsByKey[CsvImportContext.BuildStepKey(row.EventId, row.NextStepId)]);
                return choice;
            })
            .ToArray();
    }

    private static DialogueLineData[] BuildStepDialogueLines(CsvImportContext context, string eventId, string stepId)
    {
        return context.Dataset.EventStepDialogueLines
            .Where(row =>
                string.Equals(row.EventId, eventId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(row.StepId, stepId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.LineOrder)
            .Select(row => CreateDialogueLine(context, row.SpeakerId, row.Text))
            .ToArray();
    }

    private static DialogueLineData[] BuildChoiceDialogueLines(CsvImportContext context, string eventId, string stepId, string choiceId)
    {
        return context.Dataset.EventChoiceDialogueLines
            .Where(row =>
                string.Equals(row.EventId, eventId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(row.StepId, stepId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(row.ChoiceId, choiceId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => row.LineOrder)
            .Select(row => CreateDialogueLine(context, row.SpeakerId, row.Text))
            .ToArray();
    }

    private static DialogueLineData CreateDialogueLine(CsvImportContext context, string speakerId, string text)
    {
        DialogueLineData line = new();
        CsvImportAssetUtility.SetField(line, "_speaker", string.IsNullOrWhiteSpace(speakerId) ? null : context.SpeakersById[speakerId]);
        CsvImportAssetUtility.SetField(line, "_text", text);
        return line;
    }

    private static EventRow GetEventRow(CsvImportContext context, string eventId)
    {
        return context.Dataset.Events.First(row => string.Equals(row.Id, eventId, StringComparison.OrdinalIgnoreCase));
    }
}
