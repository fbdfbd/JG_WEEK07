using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class CsvDataset
{
    public IReadOnlyList<FlagRow> Flags { get; private set; }
    public IReadOnlyList<CardTypeRow> CardTypes { get; private set; }
    public IReadOnlyList<SpeakerRow> Speakers { get; private set; }
    public IReadOnlyList<InteractionRow> Interactions { get; private set; }
    public IReadOnlyList<CardRow> Cards { get; private set; }
    public IReadOnlyList<CardOptionRow> CardOptions { get; private set; }
    public IReadOnlyList<WeekRow> Weeks { get; private set; }
    public IReadOnlyList<WeekCardRow> WeekCards { get; private set; }
    public IReadOnlyList<EventRow> Events { get; private set; }
    public IReadOnlyList<EventFlagConditionRow> EventFlagConditions { get; private set; }
    public IReadOnlyList<EventStatConditionRow> EventStatConditions { get; private set; }
    public IReadOnlyList<EventInformationConditionRow> EventInformationConditions { get; private set; }
    public IReadOnlyList<EventSelectionRuleRow> EventSelectionRules { get; private set; }
    public IReadOnlyList<EventStepRow> EventSteps { get; private set; }
    public IReadOnlyList<EventStepDialogueLineRow> EventStepDialogueLines { get; private set; }
    public IReadOnlyList<EventChoiceRow> EventChoices { get; private set; }
    public IReadOnlyList<EventChoiceDialogueLineRow> EventChoiceDialogueLines { get; private set; }
    public IReadOnlyList<EventCutsceneRuleRow> EventCutsceneRules { get; private set; }
    public IReadOnlyList<CutsceneSequenceCommandRow> CutsceneSequenceCommands { get; private set; }

    public static CsvDataset Load(CsvImportSettings settings)
    {
        string csvRootPath = Path.GetFullPath(settings.CsvRootPath);
        CsvDataset dataset = new();
        dataset.Flags = LoadTable(csvRootPath, "flags.csv", record => new FlagRow(
            record["flag_id"],
            record["display_name"],
            record["description"]));
        dataset.CardTypes = LoadTable(csvRootPath, "card_types.csv", record => new CardTypeRow(
            record["card_type_id"],
            record["display_name"]));
        dataset.Speakers = LoadTable(csvRootPath, "speakers.csv", record => new SpeakerRow(
            record["speaker_id"],
            record["display_name"],
            record["default_visual_state"]));
        dataset.Interactions = LoadTable(csvRootPath, "interactions.csv", record => new InteractionRow(
            record["interaction_id"],
            record["kind"],
            record["display_name"],
            record["stat_type"],
            record.GetInt("amount"),
            record["flag_id"],
            record["condition_stat"],
            record.GetInt("min_value"),
            record["target_stat"],
            record["reaction_text"],
            record.GetMultiValue("child_interaction_ids")));
        dataset.Cards = LoadTable(csvRootPath, "cards.csv", record => new CardRow(
            record["card_id"],
            record["card_type_id"],
            record["title"],
            record["original_text"]));
        dataset.CardOptions = LoadTable(csvRootPath, "card_options.csv", record => new CardOptionRow(
            record["card_id"],
            record.GetInt("option_order"),
            record["semantic"],
            record["label"],
            record["presented_text"],
            record.GetMultiValue("interaction_ids")));
        dataset.Weeks = LoadTable(csvRootPath, "weeks.csv", record => new WeekRow(
            record["week_id"],
            record.GetInt("week_index"),
            record["title"],
            record["summary"],
            record["preturn_title"],
            record["preturn_summary"],
            record.GetMultiValue("on_week_start_interaction_ids"),
            record.GetMultiValue("on_week_end_interaction_ids")));
        dataset.WeekCards = LoadTable(csvRootPath, "week_cards.csv", record => new WeekCardRow(
            record["week_id"],
            record.GetInt("display_order"),
            record["card_id"],
            record.GetBool("is_required", true)));
        dataset.Events = LoadTable(csvRootPath, "events.csv", record => new EventRow(
            record["event_id"],
            record["week_id"],
            record["event_kind"],
            record.GetInt("event_order"),
            record["title"],
            record.GetInt("priority"),
            record["first_step_id"],
            record.GetMultiValue("on_completed_interaction_ids"),
            record.GetMultiValue("related_information_type_ids"),
            record.GetMultiValue("preferred_semantics"),
            record["linked_card_id"]));
        dataset.EventFlagConditions = LoadTable(csvRootPath, "event_flag_conditions.csv", record => new EventFlagConditionRow(
            record["event_id"],
            record["mode"],
            record["flag_id"]));
        dataset.EventStatConditions = LoadTable(csvRootPath, "event_stat_conditions.csv", record => new EventStatConditionRow(
            record["event_id"],
            record["stat_type"],
            record.GetBool("use_minimum", true),
            record.GetInt("minimum_value"),
            record.GetBool("use_maximum"),
            record.GetInt("maximum_value")));
        dataset.EventInformationConditions = LoadTable(csvRootPath, "event_information_conditions.csv", record => new EventInformationConditionRow(
            record["event_id"],
            record["information_type_id"],
            record.GetBool("use_semantic_filter"),
            record["semantic"],
            record.GetInt("minimum_count", 1)));
        dataset.EventSelectionRules = LoadTable(csvRootPath, "event_selection_rules.csv", record => new EventSelectionRuleRow(
            record["event_id"],
            record["selector_stat"],
            record["selector_mode"],
            record.GetInt("threshold"),
            record.GetBool("is_default")));
        dataset.EventSteps = LoadTable(csvRootPath, "event_steps.csv", record => new EventStepRow(
            record["event_id"],
            record["step_id"],
            record["title_override"],
            record["body_text"],
            record.GetBool("use_custom_visual_state"),
            record["visual_state"],
            record.GetMultiValue("on_enter_interaction_ids"),
            record["default_next_step_id"],
            record["condition_stat_1"],
            record["condition_min_1"],
            record["condition_max_1"],
            record["condition_stat_2"],
            record["condition_min_2"],
            record["condition_max_2"],
            record["conditional_next_step_id"],
            record["conditional_fallback_step_id"]));
        dataset.EventStepDialogueLines = LoadTable(csvRootPath, "event_step_dialogue_lines.csv", record => new EventStepDialogueLineRow(
            record["event_id"],
            record["step_id"],
            record.GetInt("line_order"),
            record["speaker_id"],
            record["text"]));
        dataset.EventChoices = LoadTable(csvRootPath, "event_choices.csv", record => new EventChoiceRow(
            record["event_id"],
            record["step_id"],
            record.GetInt("choice_order"),
            record["choice_id"],
            record["label"],
            record.GetMultiValue("interaction_ids"),
            record["next_step_id"]));
        dataset.EventChoiceDialogueLines = LoadTable(csvRootPath, "event_choice_dialogue_lines.csv", record => new EventChoiceDialogueLineRow(
            record["event_id"],
            record["step_id"],
            record["choice_id"],
            record.GetInt("line_order"),
            record["speaker_id"],
            record["text"]));
        dataset.EventCutsceneRules = LoadOptionalTable(csvRootPath, "event_cutscene_rules.csv", record => new EventCutsceneRuleRow(
            record["rule_id"],
            record.GetBool("enabled", true),
            record["week_id"],
            record["event_id"],
            record["moment"],
            record["sequence_id"],
            record["special_player_id"]));
        dataset.CutsceneSequenceCommands = LoadOptionalTable(csvRootPath, "cutscene_sequences.csv", record => new CutsceneSequenceCommandRow(
            record["sequence_id"],
            record.GetInt("order"),
            record["command"],
            record["target_key"],
            record["value1"],
            record["value2"],
            record["value3"],
            record.GetFloat("duration"),
            record["ease"],
            record.GetBool("blocking", true)));
        return dataset;
    }

    private static IReadOnlyList<TRow> LoadTable<TRow>(string csvRootPath, string fileName, Func<CsvRecord, TRow> factory)
    {
        CsvTable table = CsvTableParser.ParseFile(Path.Combine(csvRootPath, fileName));
        return table.Rows.Select(factory).ToArray();
    }

    private static IReadOnlyList<TRow> LoadOptionalTable<TRow>(string csvRootPath, string fileName, Func<CsvRecord, TRow> factory)
    {
        string path = Path.Combine(csvRootPath, fileName);
        if (!File.Exists(path))
        {
            return Array.Empty<TRow>();
        }

        CsvTable table = CsvTableParser.ParseFile(path);
        return table.Rows.Select(factory).ToArray();
    }
}

public sealed class FlagRow
{
    public FlagRow(string id, string displayName, string description)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
}

public sealed class CardTypeRow
{
    public CardTypeRow(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }
    public string DisplayName { get; }
}

public sealed class SpeakerRow
{
    public SpeakerRow(string id, string displayName, string defaultVisualState)
    {
        Id = id;
        DisplayName = displayName;
        DefaultVisualState = defaultVisualState;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string DefaultVisualState { get; }
}

public sealed class InteractionRow
{
    public InteractionRow(
        string id,
        string kind,
        string displayName,
        string statType,
        int amount,
        string flagId,
        string conditionStat,
        int minValue,
        string targetStat,
        string reactionText,
        string[] childInteractionIds)
    {
        Id = id;
        Kind = kind;
        DisplayName = displayName;
        StatType = statType;
        Amount = amount;
        FlagId = flagId;
        ConditionStat = conditionStat;
        MinValue = minValue;
        TargetStat = targetStat;
        ReactionText = reactionText;
        ChildInteractionIds = childInteractionIds;
    }

    public string Id { get; }
    public string Kind { get; }
    public string DisplayName { get; }
    public string StatType { get; }
    public int Amount { get; }
    public string FlagId { get; }
    public string ConditionStat { get; }
    public int MinValue { get; }
    public string TargetStat { get; }
    public string ReactionText { get; }
    public string[] ChildInteractionIds { get; }
}

public sealed class CardRow
{
    public CardRow(string id, string cardTypeId, string title, string originalText)
    {
        Id = id;
        CardTypeId = cardTypeId;
        Title = title;
        OriginalText = originalText;
    }

    public string Id { get; }
    public string CardTypeId { get; }
    public string Title { get; }
    public string OriginalText { get; }
}

public sealed class CardOptionRow
{
    public CardOptionRow(
        string cardId,
        int optionOrder,
        string semantic,
        string label,
        string presentedText,
        string[] interactionIds)
    {
        CardId = cardId;
        OptionOrder = optionOrder;
        Semantic = semantic;
        Label = label;
        PresentedText = presentedText;
        InteractionIds = interactionIds;
    }

    public string CardId { get; }
    public int OptionOrder { get; }
    public string Semantic { get; }
    public string Label { get; }
    public string PresentedText { get; }
    public string[] InteractionIds { get; }
}

public sealed class WeekRow
{
    public WeekRow(
        string id,
        int weekIndex,
        string title,
        string summary,
        string preTurnTitle,
        string preTurnSummary,
        string[] onWeekStartInteractionIds,
        string[] onWeekEndInteractionIds)
    {
        Id = id;
        WeekIndex = weekIndex;
        Title = title;
        Summary = summary;
        PreTurnTitle = preTurnTitle;
        PreTurnSummary = preTurnSummary;
        OnWeekStartInteractionIds = onWeekStartInteractionIds;
        OnWeekEndInteractionIds = onWeekEndInteractionIds;
    }

    public string Id { get; }
    public int WeekIndex { get; }
    public string Title { get; }
    public string Summary { get; }
    public string PreTurnTitle { get; }
    public string PreTurnSummary { get; }
    public string[] OnWeekStartInteractionIds { get; }
    public string[] OnWeekEndInteractionIds { get; }
}

public sealed class WeekCardRow
{
    public WeekCardRow(string weekId, int displayOrder, string cardId, bool isRequired)
    {
        WeekId = weekId;
        DisplayOrder = displayOrder;
        CardId = cardId;
        IsRequired = isRequired;
    }

    public string WeekId { get; }
    public int DisplayOrder { get; }
    public string CardId { get; }
    public bool IsRequired { get; }
}

public sealed class EventRow
{
    public EventRow(
        string id,
        string weekId,
        string eventKind,
        int eventOrder,
        string title,
        int priority,
        string firstStepId,
        string[] onCompletedInteractionIds,
        string[] relatedInformationTypeIds,
        string[] preferredSemantics,
        string linkedCardId)
    {
        Id = id;
        WeekId = weekId;
        EventKind = eventKind;
        EventOrder = eventOrder;
        Title = title;
        Priority = priority;
        FirstStepId = firstStepId;
        OnCompletedInteractionIds = onCompletedInteractionIds;
        RelatedInformationTypeIds = relatedInformationTypeIds;
        PreferredSemantics = preferredSemantics;
        LinkedCardId = linkedCardId;
    }

    public string Id { get; }
    public string WeekId { get; }
    public string EventKind { get; }
    public int EventOrder { get; }
    public string Title { get; }
    public int Priority { get; }
    public string FirstStepId { get; }
    public string[] OnCompletedInteractionIds { get; }
    public string[] RelatedInformationTypeIds { get; }
    public string[] PreferredSemantics { get; }
    public string LinkedCardId { get; }
}

public sealed class EventFlagConditionRow
{
    public EventFlagConditionRow(string eventId, string mode, string flagId)
    {
        EventId = eventId;
        Mode = mode;
        FlagId = flagId;
    }

    public string EventId { get; }
    public string Mode { get; }
    public string FlagId { get; }
}

public sealed class EventStatConditionRow
{
    public EventStatConditionRow(
        string eventId,
        string statType,
        bool useMinimum,
        int minimumValue,
        bool useMaximum,
        int maximumValue)
    {
        EventId = eventId;
        StatType = statType;
        UseMinimum = useMinimum;
        MinimumValue = minimumValue;
        UseMaximum = useMaximum;
        MaximumValue = maximumValue;
    }

    public string EventId { get; }
    public string StatType { get; }
    public bool UseMinimum { get; }
    public int MinimumValue { get; }
    public bool UseMaximum { get; }
    public int MaximumValue { get; }
}

public sealed class EventInformationConditionRow
{
    public EventInformationConditionRow(
        string eventId,
        string informationTypeId,
        bool useSemanticFilter,
        string semantic,
        int minimumCount)
    {
        EventId = eventId;
        InformationTypeId = informationTypeId;
        UseSemanticFilter = useSemanticFilter;
        Semantic = semantic;
        MinimumCount = minimumCount;
    }

    public string EventId { get; }
    public string InformationTypeId { get; }
    public bool UseSemanticFilter { get; }
    public string Semantic { get; }
    public int MinimumCount { get; }
}

public sealed class EventSelectionRuleRow
{
    public EventSelectionRuleRow(
        string eventId,
        string selectorStat,
        string selectorMode,
        int threshold,
        bool isDefault)
    {
        EventId = eventId;
        SelectorStat = selectorStat;
        SelectorMode = selectorMode;
        Threshold = threshold;
        IsDefault = isDefault;
    }

    public string EventId { get; }
    public string SelectorStat { get; }
    public string SelectorMode { get; }
    public int Threshold { get; }
    public bool IsDefault { get; }
}

public sealed class EventStepRow
{
    public EventStepRow(
        string eventId,
        string stepId,
        string titleOverride,
        string bodyText,
        bool useCustomVisualState,
        string visualState,
        string[] onEnterInteractionIds,
        string defaultNextStepId,
        string conditionStat1,
        string conditionMin1,
        string conditionMax1,
        string conditionStat2,
        string conditionMin2,
        string conditionMax2,
        string conditionalNextStepId,
        string conditionalFallbackStepId)
    {
        EventId = eventId;
        StepId = stepId;
        TitleOverride = titleOverride;
        BodyText = bodyText;
        UseCustomVisualState = useCustomVisualState;
        VisualState = visualState;
        OnEnterInteractionIds = onEnterInteractionIds;
        DefaultNextStepId = defaultNextStepId;
        ConditionStat1 = conditionStat1;
        ConditionMin1 = conditionMin1;
        ConditionMax1 = conditionMax1;
        ConditionStat2 = conditionStat2;
        ConditionMin2 = conditionMin2;
        ConditionMax2 = conditionMax2;
        ConditionalNextStepId = conditionalNextStepId;
        ConditionalFallbackStepId = conditionalFallbackStepId;
    }

    public string EventId { get; }
    public string StepId { get; }
    public string TitleOverride { get; }
    public string BodyText { get; }
    public bool UseCustomVisualState { get; }
    public string VisualState { get; }
    public string[] OnEnterInteractionIds { get; }
    public string DefaultNextStepId { get; }
    public string ConditionStat1 { get; }
    public string ConditionMin1 { get; }
    public string ConditionMax1 { get; }
    public string ConditionStat2 { get; }
    public string ConditionMin2 { get; }
    public string ConditionMax2 { get; }
    public string ConditionalNextStepId { get; }
    public string ConditionalFallbackStepId { get; }
}

public sealed class EventStepDialogueLineRow
{
    public EventStepDialogueLineRow(string eventId, string stepId, int lineOrder, string speakerId, string text)
    {
        EventId = eventId;
        StepId = stepId;
        LineOrder = lineOrder;
        SpeakerId = speakerId;
        Text = text;
    }

    public string EventId { get; }
    public string StepId { get; }
    public int LineOrder { get; }
    public string SpeakerId { get; }
    public string Text { get; }
}

public sealed class EventChoiceRow
{
    public EventChoiceRow(
        string eventId,
        string stepId,
        int choiceOrder,
        string choiceId,
        string label,
        string[] interactionIds,
        string nextStepId)
    {
        EventId = eventId;
        StepId = stepId;
        ChoiceOrder = choiceOrder;
        ChoiceId = choiceId;
        Label = label;
        InteractionIds = interactionIds;
        NextStepId = nextStepId;
    }

    public string EventId { get; }
    public string StepId { get; }
    public int ChoiceOrder { get; }
    public string ChoiceId { get; }
    public string Label { get; }
    public string[] InteractionIds { get; }
    public string NextStepId { get; }
}

public sealed class EventChoiceDialogueLineRow
{
    public EventChoiceDialogueLineRow(
        string eventId,
        string stepId,
        string choiceId,
        int lineOrder,
        string speakerId,
        string text)
    {
        EventId = eventId;
        StepId = stepId;
        ChoiceId = choiceId;
        LineOrder = lineOrder;
        SpeakerId = speakerId;
        Text = text;
    }

    public string EventId { get; }
    public string StepId { get; }
    public string ChoiceId { get; }
    public int LineOrder { get; }
    public string SpeakerId { get; }
    public string Text { get; }
}

public sealed class EventCutsceneRuleRow
{
    public EventCutsceneRuleRow(
        string id,
        bool enabled,
        string weekId,
        string eventId,
        string moment,
        string sequenceId,
        string specialPlayerId)
    {
        Id = id;
        Enabled = enabled;
        WeekId = weekId;
        EventId = eventId;
        Moment = moment;
        SequenceId = sequenceId;
        SpecialPlayerId = specialPlayerId;
    }

    public string Id { get; }
    public bool Enabled { get; }
    public string WeekId { get; }
    public string EventId { get; }
    public string Moment { get; }
    public string SequenceId { get; }
    public string SpecialPlayerId { get; }
}

public sealed class CutsceneSequenceCommandRow
{
    public CutsceneSequenceCommandRow(
        string sequenceId,
        int order,
        string command,
        string targetKey,
        string value1,
        string value2,
        string value3,
        float duration,
        string ease,
        bool blocking)
    {
        SequenceId = sequenceId;
        Order = order;
        Command = command;
        TargetKey = targetKey;
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
        Duration = duration;
        Ease = ease;
        Blocking = blocking;
    }

    public string SequenceId { get; }
    public int Order { get; }
    public string Command { get; }
    public string TargetKey { get; }
    public string Value1 { get; }
    public string Value2 { get; }
    public string Value3 { get; }
    public float Duration { get; }
    public string Ease { get; }
    public bool Blocking { get; }
}
