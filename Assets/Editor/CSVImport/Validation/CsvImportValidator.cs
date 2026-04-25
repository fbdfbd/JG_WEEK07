using System;
using System.Collections.Generic;
using System.Linq;

public static class CsvImportValidator
{
    public static void Validate(CsvDataset dataset)
    {
        List<string> errors = new();

        ValidateRequiredIds(dataset.Flags.Select(row => row.Id), "flag_id", errors);
        ValidateRequiredIds(dataset.CardTypes.Select(row => row.Id), "card_type_id", errors);
        ValidateRequiredIds(dataset.Speakers.Select(row => row.Id), "speaker_id", errors);
        ValidateRequiredIds(dataset.Interactions.Select(row => row.Id), "interaction_id", errors);
        ValidateRequiredIds(dataset.Cards.Select(row => row.Id), "card_id", errors);
        ValidateRequiredIds(dataset.Weeks.Select(row => row.Id), "week_id", errors);
        ValidateRequiredIds(dataset.Events.Select(row => row.Id), "event_id", errors);

        HashSet<string> flagIds = dataset.Flags.Select(row => row.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> cardTypeIds = dataset.CardTypes.Select(row => row.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> speakerIds = dataset.Speakers.Select(row => row.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> interactionIds = dataset.Interactions.Select(row => row.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> cardIds = dataset.Cards.Select(row => row.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> weekIds = dataset.Weeks.Select(row => row.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> eventIds = dataset.Events.Select(row => row.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> sequenceIds = dataset.CutsceneSequenceCommands
            .Select(row => row.SequenceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> stepKeys = dataset.EventSteps
            .Select(row => CsvImportContext.BuildStepKey(row.EventId, row.StepId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> choiceKeys = dataset.EventChoices
            .Select(row => CsvImportContext.BuildChoiceKey(row.EventId, row.StepId, row.ChoiceId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        ValidateReferences(dataset.Cards.Select(row => (row.CardTypeId, "cards.csv -> card_type_id")), cardTypeIds, errors);
        ValidateReferences(dataset.CardOptions.Select(row => (row.CardId, "card_options.csv -> card_id")), cardIds, errors);
        ValidateReferences(dataset.WeekCards.Select(row => (row.WeekId, "week_cards.csv -> week_id")), weekIds, errors);
        ValidateReferences(dataset.WeekCards.Select(row => (row.CardId, "week_cards.csv -> card_id")), cardIds, errors);
        ValidateReferences(dataset.Events.Select(row => (row.WeekId, "events.csv -> week_id")), weekIds, errors);
        ValidateReferences(dataset.EventFlagConditions.Select(row => (row.EventId, "event_flag_conditions.csv -> event_id")), eventIds, errors);
        ValidateReferences(dataset.EventFlagConditions.Select(row => (row.FlagId, "event_flag_conditions.csv -> flag_id")), flagIds, errors);
        ValidateReferences(dataset.EventStatConditions.Select(row => (row.EventId, "event_stat_conditions.csv -> event_id")), eventIds, errors);
        ValidateReferences(dataset.EventInformationConditions.Select(row => (row.EventId, "event_information_conditions.csv -> event_id")), eventIds, errors);
        ValidateReferences(dataset.EventInformationConditions.Select(row => (row.InformationTypeId, "event_information_conditions.csv -> information_type_id")), cardTypeIds, errors);
        ValidateReferences(dataset.EventSelectionRules.Select(row => (row.EventId, "event_selection_rules.csv -> event_id")), eventIds, errors);
        ValidateReferences(dataset.EventSteps.Select(row => (row.EventId, "event_steps.csv -> event_id")), eventIds, errors);
        ValidateReferences(dataset.EventStepDialogueLines.Select(row => (row.SpeakerId, "event_step_dialogue_lines.csv -> speaker_id")), speakerIds, errors);
        ValidateReferences(dataset.EventStepDialogueLines.Select(row => (CsvImportContext.BuildStepKey(row.EventId, row.StepId), "event_step_dialogue_lines.csv -> step")), stepKeys, errors);
        ValidateReferences(dataset.EventChoices.Select(row => (CsvImportContext.BuildStepKey(row.EventId, row.StepId), "event_choices.csv -> step")), stepKeys, errors);
        ValidateReferences(dataset.EventChoiceDialogueLines.Select(row => (row.SpeakerId, "event_choice_dialogue_lines.csv -> speaker_id")), speakerIds, errors);
        ValidateReferences(dataset.EventChoiceDialogueLines.Select(row => (CsvImportContext.BuildChoiceKey(row.EventId, row.StepId, row.ChoiceId), "event_choice_dialogue_lines.csv -> choice")), choiceKeys, errors);
        ValidateReferences(dataset.EventCutsceneRules.Select(row => (row.WeekId, "event_cutscene_rules.csv -> week_id")), weekIds, errors);
        ValidateReferences(dataset.EventCutsceneRules.Select(row => (row.EventId, "event_cutscene_rules.csv -> event_id")), eventIds, errors);
        ValidateReferences(dataset.EventCutsceneRules.Select(row => (row.SequenceId, "event_cutscene_rules.csv -> sequence_id")), sequenceIds, errors);

        foreach (InteractionRow row in dataset.Interactions)
        {
            ValidateReferences(row.ChildInteractionIds.Select(id => (id, $"interactions.csv -> child_interaction_ids ({row.Id})")), interactionIds, errors);
            ValidateOptionalReference(row.FlagId, flagIds, $"interactions.csv -> flag_id ({row.Id})", errors);
            ValidateEnum<EChildStatusType>(row.StatType, $"interactions.csv -> stat_type ({row.Id})", errors, allowBlank: true);
            ValidateEnum<EChildStatusType>(row.ConditionStat, $"interactions.csv -> condition_stat ({row.Id})", errors, allowBlank: true);
            ValidateEnum<EChildStatusType>(row.TargetStat, $"interactions.csv -> target_stat ({row.Id})", errors, allowBlank: true);
        }

        foreach (CardOptionRow row in dataset.CardOptions)
        {
            ValidateReferences(row.InteractionIds.Select(id => (id, $"card_options.csv -> interaction_ids ({row.CardId})")), interactionIds, errors);
            ValidateEnum<ECardOptionSemantic>(row.Semantic, $"card_options.csv -> semantic ({row.CardId})", errors);
        }

        foreach (WeekRow row in dataset.Weeks)
        {
            ValidateReferences(row.OnWeekStartInteractionIds.Select(id => (id, $"weeks.csv -> on_week_start_interaction_ids ({row.Id})")), interactionIds, errors);
            ValidateReferences(row.OnWeekEndInteractionIds.Select(id => (id, $"weeks.csv -> on_week_end_interaction_ids ({row.Id})")), interactionIds, errors);
        }

        foreach (EventRow row in dataset.Events)
        {
            ValidateReferences(row.OnCompletedInteractionIds.Select(id => (id, $"events.csv -> on_completed_interaction_ids ({row.Id})")), interactionIds, errors);
            ValidateReferences(row.RelatedInformationTypeIds.Select(id => (id, $"events.csv -> related_information_type_ids ({row.Id})")), cardTypeIds, errors);
            ValidateOptionalReference(row.LinkedCardId, cardIds, $"events.csv -> linked_card_id ({row.Id})", errors);
            foreach (string semantic in row.PreferredSemantics)
            {
                ValidateEnum<ECardOptionSemantic>(semantic, $"events.csv -> preferred_semantics ({row.Id})", errors);
            }

            ValidateEnum<EventKind>(row.EventKind, $"events.csv -> event_kind ({row.Id})", errors);
            ValidateOptionalReference(CsvImportContext.BuildStepKey(row.Id, row.FirstStepId), stepKeys, $"events.csv -> first_step_id ({row.Id})", errors);

            if (string.Equals(row.EventKind, nameof(EventKind.routine), StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(row.LinkedCardId))
            {
                errors.Add($"Missing linked card: events.csv -> linked_card_id ({row.Id})");
            }
        }

        foreach (EventFlagConditionRow row in dataset.EventFlagConditions)
        {
            if (!string.Equals(row.Mode, "required", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(row.Mode, "blocked", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"event_flag_conditions.csv -> mode must be required or blocked ({row.EventId}).");
            }
        }

        foreach (EventStatConditionRow row in dataset.EventStatConditions)
        {
            ValidateEnum<EChildStatusType>(row.StatType, $"event_stat_conditions.csv -> stat_type ({row.EventId})", errors);
        }

        foreach (EventInformationConditionRow row in dataset.EventInformationConditions)
        {
            ValidateEnum<ECardOptionSemantic>(row.Semantic, $"event_information_conditions.csv -> semantic ({row.EventId})", errors, allowBlank: !row.UseSemanticFilter);
        }

        foreach (EventSelectionRuleRow row in dataset.EventSelectionRules)
        {
            ValidateSelectionMode(row.SelectorMode, $"event_selection_rules.csv -> selector_mode ({row.EventId})", errors);
            string selectorMode = row.SelectorMode?.Trim();
            bool allowsBlankStat =
                string.IsNullOrWhiteSpace(selectorMode) ||
                string.Equals(selectorMode, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(selectorMode, "default", StringComparison.OrdinalIgnoreCase);
            ValidateEnum<EChildStatusType>(row.SelectorStat, $"event_selection_rules.csv -> selector_stat ({row.EventId})", errors, allowBlank: allowsBlankStat);
        }

        foreach (EventStepRow row in dataset.EventSteps)
        {
            ValidateReferences(row.OnEnterInteractionIds.Select(id => (id, $"event_steps.csv -> on_enter_interaction_ids ({row.EventId}/{row.StepId})")), interactionIds, errors);
            ValidateEnum<ENemoVisualState>(row.VisualState, $"event_steps.csv -> visual_state ({row.EventId}/{row.StepId})", errors);
            if (!string.IsNullOrWhiteSpace(row.DefaultNextStepId))
            {
                ValidateOptionalReference(
                    CsvImportContext.BuildStepKey(row.EventId, row.DefaultNextStepId),
                    stepKeys,
                    $"event_steps.csv -> default_next_step_id ({row.EventId}/{row.StepId})",
                    errors);
            }

            ValidateStepConditionSlot(row.EventId, row.StepId, 1, row.ConditionStat1, row.ConditionMin1, row.ConditionMax1, errors);
            ValidateStepConditionSlot(row.EventId, row.StepId, 2, row.ConditionStat2, row.ConditionMin2, row.ConditionMax2, errors);
            ValidateStepNextReference(row.EventId, row.StepId, row.ConditionalNextStepId, stepKeys, "conditional_next_step_id", errors);
            ValidateStepNextReference(row.EventId, row.StepId, row.ConditionalFallbackStepId, stepKeys, "conditional_fallback_step_id", errors);
        }

        foreach (EventChoiceRow row in dataset.EventChoices)
        {
            ValidateReferences(row.InteractionIds.Select(id => (id, $"event_choices.csv -> interaction_ids ({row.EventId}/{row.StepId}/{row.ChoiceId})")), interactionIds, errors);
            if (!string.IsNullOrWhiteSpace(row.NextStepId))
            {
                ValidateOptionalReference(
                    CsvImportContext.BuildStepKey(row.EventId, row.NextStepId),
                    stepKeys,
                    $"event_choices.csv -> next_step_id ({row.EventId}/{row.StepId}/{row.ChoiceId})",
                    errors);
            }
        }

        foreach (EventCutsceneRuleRow row in dataset.EventCutsceneRules)
        {
            ValidateEnum<EWeekFlowCutsceneMoment>(row.Moment, $"event_cutscene_rules.csv -> moment ({row.Id})", errors);

            bool hasSequence = !string.IsNullOrWhiteSpace(row.SequenceId);
            bool hasSpecialPlayer = !string.IsNullOrWhiteSpace(row.SpecialPlayerId);
            if (hasSequence == hasSpecialPlayer)
            {
                errors.Add($"event_cutscene_rules.csv must set exactly one of sequence_id or special_player_id ({row.Id}).");
            }
        }

        foreach (CutsceneSequenceCommandRow row in dataset.CutsceneSequenceCommands)
        {
            if (string.IsNullOrWhiteSpace(row.SequenceId))
            {
                errors.Add($"Missing id: cutscene_sequences.csv -> sequence_id (order {row.Order})");
            }

            ValidateEnum<EDataCutsceneCommandType>(row.Command, $"cutscene_sequences.csv -> command ({row.SequenceId}/{row.Order})", errors);
            ValidateEnum<DG.Tweening.Ease>(row.Ease, $"cutscene_sequences.csv -> ease ({row.SequenceId}/{row.Order})", errors, allowBlank: true);
        }

        ValidateGroupedUniqueness(dataset.CardOptions, row => row.CardId, row => row.OptionOrder, "card_options.csv -> option_order", errors);
        ValidateGroupedUniqueness(dataset.WeekCards, row => row.WeekId, row => row.DisplayOrder, "week_cards.csv -> display_order", errors);
        ValidateGroupedUniqueness(dataset.EventChoices, row => CsvImportContext.BuildStepKey(row.EventId, row.StepId), row => row.ChoiceOrder, "event_choices.csv -> choice_order", errors);
        ValidateGroupedUniqueness(dataset.EventStepDialogueLines, row => CsvImportContext.BuildStepKey(row.EventId, row.StepId), row => row.LineOrder, "event_step_dialogue_lines.csv -> line_order", errors);
        ValidateGroupedUniqueness(dataset.EventChoiceDialogueLines, row => CsvImportContext.BuildChoiceKey(row.EventId, row.StepId, row.ChoiceId), row => row.LineOrder, "event_choice_dialogue_lines.csv -> line_order", errors);
        ValidateGroupedUniqueness(dataset.CutsceneSequenceCommands, row => row.SequenceId, row => row.Order, "cutscene_sequences.csv -> order", errors);
        ValidateUniqueKeys(dataset.EventSelectionRules, row => row.EventId, "event_selection_rules.csv -> event_id", errors);
        ValidateUniqueKeys(dataset.EventCutsceneRules, row => row.Id, "event_cutscene_rules.csv -> rule_id", errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("CSV validation failed.\n" + string.Join("\n", errors));
        }
    }

    private static void ValidateRequiredIds(IEnumerable<string> ids, string label, ICollection<string> errors)
    {
        foreach (IGrouping<string, string> group in ids
                     .Where(id => !string.IsNullOrWhiteSpace(id))
                     .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"Duplicate {label}: {group.Key}");
        }
    }

    private static void ValidateReferences(IEnumerable<(string Id, string Label)> references, ISet<string> validIds, ICollection<string> errors)
    {
        foreach ((string id, string label) in references)
        {
            ValidateOptionalReference(id, validIds, label, errors);
        }
    }

    private static void ValidateOptionalReference(string id, ISet<string> validIds, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        if (!validIds.Contains(id))
        {
            errors.Add($"Missing reference: {label} -> {id}");
        }
    }

    private static void ValidateEnum<TEnum>(string value, string label, ICollection<string> errors, bool allowBlank = false)
        where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!allowBlank)
            {
                errors.Add($"Missing enum value: {label}");
            }

            return;
        }

        if (!Enum.TryParse(value, true, out TEnum _))
        {
            errors.Add($"Invalid enum value: {label} -> {value}");
        }
    }

    private static void ValidateSelectionMode(string value, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        string normalizedValue = value.Trim();
        string[] validValues =
        {
            "none",
            "max_positive",
            "max_negative",
            "max_any",
            "min_any",
            "default",
        };

        if (!validValues.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Invalid enum value: {label} -> {value}");
        }
    }

    private static void ValidateStepConditionSlot(
        string eventId,
        string stepId,
        int slot,
        string statType,
        string minimumValue,
        string maximumValue,
        ICollection<string> errors)
    {
        string label = $"event_steps.csv -> condition_{slot} ({eventId}/{stepId})";
        ValidateEnum<EChildStatusType>(statType, $"{label}.stat", errors, allowBlank: true);

        bool hasBounds = !string.IsNullOrWhiteSpace(minimumValue) || !string.IsNullOrWhiteSpace(maximumValue);
        if (hasBounds && string.IsNullOrWhiteSpace(statType))
        {
            errors.Add($"Missing enum value: {label}.stat");
        }

        ValidateOptionalInt(minimumValue, $"{label}.min", errors);
        ValidateOptionalInt(maximumValue, $"{label}.max", errors);
    }

    private static void ValidateStepNextReference(
        string eventId,
        string stepId,
        string nextStepId,
        ISet<string> stepKeys,
        string columnName,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(nextStepId))
        {
            return;
        }

        ValidateOptionalReference(
            CsvImportContext.BuildStepKey(eventId, nextStepId),
            stepKeys,
            $"event_steps.csv -> {columnName} ({eventId}/{stepId})",
            errors);
    }

    private static void ValidateOptionalInt(string value, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!int.TryParse(value, out _))
        {
            errors.Add($"Invalid integer value: {label} -> {value}");
        }
    }

    private static void ValidateUniqueKeys<TRow, TKey>(
        IEnumerable<TRow> rows,
        Func<TRow, TKey> keySelector,
        string label,
        ICollection<string> errors)
    {
        foreach (IGrouping<TKey, TRow> keyGroup in rows
                     .GroupBy(keySelector)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"Duplicate key: {label} -> {keyGroup.Key}");
        }
    }

    private static void ValidateGroupedUniqueness<TRow, TOwner>(
        IEnumerable<TRow> rows,
        Func<TRow, TOwner> ownerSelector,
        Func<TRow, int> orderSelector,
        string label,
        ICollection<string> errors)
    {
        foreach (IGrouping<TOwner, TRow> ownerGroup in rows.GroupBy(ownerSelector))
        {
            foreach (IGrouping<int, TRow> orderGroup in ownerGroup.GroupBy(orderSelector).Where(group => group.Count() > 1))
            {
                errors.Add($"Duplicate order: {label} -> {ownerGroup.Key} / {orderGroup.Key}");
            }
        }
    }

    private enum EventKind
    {
        routine,
        story,
        private_dialogue,
    }
}
