using System;
using System.Collections.Generic;
using System.Linq;

public static class WeekNarrativeResolver
{
    private const string NarratorSpeakerId = "speaker_narrator";
    private static readonly EChildStatusType[] WeeklyResultSummaryStatTypes =
    {
        EChildStatusType.Trust,
        EChildStatusType.Curiosity,
        EChildStatusType.Anxiety,
        EChildStatusType.Obedience,
    };

    public static SO_InteractiveEventDefinition[] ResolvePendingEvents(
        SO_WeekDefinition weekDefinition,
        RuntimeChildState childState,
        RuntimeWeekResult weekResult)
    {
        return ResolveDayEvents(weekDefinition, childState, weekResult)
            .Concat(ResolveNightEvents(weekDefinition, childState, weekResult))
            .ToArray();
    }

    public static SO_InteractiveEventDefinition[] ResolveDayEvents(
        SO_WeekDefinition weekDefinition,
        RuntimeChildState childState,
        RuntimeWeekResult weekResult)
    {
        RuntimeInformationControlResult informationControlResult = weekResult?.InformationControlResult;
        IReadOnlyDictionary<string, RuntimeResolvedCardRecord> resolvedCardLookup = BuildResolvedCardLookup(weekResult?.ResolvedCards);
        List<SO_InteractiveEventDefinition> pendingEvents = new();
        pendingEvents.AddRange(ResolveRoutineEvents(weekDefinition?.DayFlow, childState, resolvedCardLookup));
        pendingEvents.AddRange(ResolveStoryEvents(weekDefinition?.DayFlow, childState, informationControlResult));
        return pendingEvents.ToArray();
    }

    public static SO_InteractiveEventDefinition[] ResolveNightEvents(
        SO_WeekDefinition weekDefinition,
        RuntimeChildState childState,
        RuntimeWeekResult weekResult)
    {
        RuntimeInformationControlResult informationControlResult = weekResult?.InformationControlResult;
        return ResolveNightDialogues(weekDefinition?.NightFlow, childState, informationControlResult).ToArray();
    }

    public static RuntimeResolvedCardRecord[] ResolveLinkedCards(
        SO_InteractiveEventDefinition eventDefinition,
        RuntimeWeekResult weekResult)
    {
        if (eventDefinition == null || weekResult?.ResolvedCards == null)
        {
            return Array.Empty<RuntimeResolvedCardRecord>();
        }

        return weekResult.ResolvedCards
            .Where(resolvedCard => resolvedCard != null)
            .Where(resolvedCard => resolvedCard.HasPendingEventReward)
            .Where(resolvedCard => IsCardLinkedToEvent(eventDefinition, resolvedCard))
            .ToArray();
    }

    public static InteractiveEventPresentation CreatePresentation(
        RuntimeInteractiveEventSession eventSession,
        RuntimeChildState childState,
        WeekUiTextProvider weekUiText)
    {
        if (eventSession?.EventDefinition == null || eventSession.CurrentStep == null)
        {
            return InteractiveEventPresentation.Empty;
        }

        SO_InteractiveEventStepDefinition step = eventSession.CurrentStep;
        InteractiveEventChoicePresentation[] choices = step.Choices?
            .Where(choice => choice != null)
            .Select(choice => new InteractiveEventChoicePresentation(
                choice.Label,
                BuildEffectSummary(choice.Interactions, weekUiText)))
            .ToArray()
            ?? Array.Empty<InteractiveEventChoicePresentation>();

        DialogueLinePresentation[] dialogueLines = BuildDialogueLines(
            step.DialogueLines,
            step.NemoLine);

        return new InteractiveEventPresentation(
            string.IsNullOrWhiteSpace(step.TitleOverride) ? eventSession.EventDefinition.Title : step.TitleOverride,
            step.BodyText,
            BuildEffectSummary(step.OnEnterInteractions, weekUiText),
            ResolveVisualState(step, childState),
            dialogueLines,
            choices,
            choices.Length == 0);
    }

    public static InteractiveEventChoiceResultPresentation CreateChoiceResultPresentation(
        InteractiveEventChoiceData selectedChoice,
        WeekUiTextProvider weekUiText)
    {
        return new InteractiveEventChoiceResultPresentation(
            BuildDialogueLines(
                selectedChoice?.ResponseDialogueLines,
                selectedChoice?.ResponseLine),
            BuildEffectSummary(selectedChoice?.Interactions, weekUiText));
    }

    public static WeeklyResultLogPresentation CreateWeeklyResultLogPresentation(
        IReadOnlyList<SO_EventResultDefinition> resultLogs)
    {
        WeeklyResultLogEntryPresentation[] entries = resultLogs?
            .Where(resultLog => resultLog != null)
            .Select(resultLog => new WeeklyResultLogEntryPresentation(
                resultLog.EventId,
                resultLog.Title,
                resultLog.Context))
            .ToArray()
            ?? Array.Empty<WeeklyResultLogEntryPresentation>();

        return new WeeklyResultLogPresentation(entries);
    }

    public static WeeklyResultStatDeltaPresentation[] CreateWeeklyResultStatSummary(
        RuntimeChildState childState,
        IReadOnlyDictionary<EChildStatusType, int> beforeStats,
        WeekUiTextProvider weekUiText)
    {
        if (childState == null || beforeStats == null)
        {
            return Array.Empty<WeeklyResultStatDeltaPresentation>();
        }

        List<WeeklyResultStatDeltaPresentation> summary = new();
        for (int index = 0; index < WeeklyResultSummaryStatTypes.Length; index++)
        {
            EChildStatusType statType = WeeklyResultSummaryStatTypes[index];
            int beforeValue = beforeStats.TryGetValue(statType, out int value)
                ? value
                : RuntimeChildState.DefaultStatValue;
            int delta = childState.GetStat(statType) - beforeValue;
            if (delta <= 0)
            {
                continue;
            }

            summary.Add(new WeeklyResultStatDeltaPresentation(
                statType,
                GetStatLabel(statType, weekUiText),
                delta));
        }

        return summary.ToArray();
    }

    public static WeeklyResultLogPresentation CreateWeeklyResultLogPresentation(
        IReadOnlyList<RuntimeWeeklyResultLogRecord> history,
        string selectedWeekId)
    {
        WeeklyResultLogWeekPresentation[] weeks = history?
            .Where(record => record != null)
            .Select(record => new WeeklyResultLogWeekPresentation(
                record.WeekId,
                record.WeekIndex,
                record.WeekTitle,
                CreateWeeklyResultLogEntries(record.ResultLogs),
                record.StatSummary))
            .ToArray()
            ?? Array.Empty<WeeklyResultLogWeekPresentation>();

        WeeklyResultLogWeekPresentation selectedWeek = SelectWeeklyResultLogWeek(weeks, selectedWeekId);
        return new WeeklyResultLogPresentation(
            selectedWeek.Entries,
            weeks,
            selectedWeek.WeekId,
            selectedWeek.StatSummary);
    }

    private static WeeklyResultLogEntryPresentation[] CreateWeeklyResultLogEntries(
        IReadOnlyList<SO_EventResultDefinition> resultLogs)
    {
        return resultLogs?
            .Where(resultLog => resultLog != null)
            .Select(resultLog => new WeeklyResultLogEntryPresentation(
                resultLog.EventId,
                resultLog.Title,
                resultLog.Context))
            .ToArray()
            ?? Array.Empty<WeeklyResultLogEntryPresentation>();
    }

    private static WeeklyResultLogWeekPresentation SelectWeeklyResultLogWeek(
        IReadOnlyList<WeeklyResultLogWeekPresentation> weeks,
        string selectedWeekId)
    {
        if (weeks == null || weeks.Count == 0)
        {
            return new WeeklyResultLogWeekPresentation(
                string.Empty,
                0,
                string.Empty,
                Array.Empty<WeeklyResultLogEntryPresentation>(),
                Array.Empty<WeeklyResultStatDeltaPresentation>());
        }

        for (int index = 0; index < weeks.Count; index++)
        {
            if (string.Equals(weeks[index].WeekId, selectedWeekId, StringComparison.OrdinalIgnoreCase))
            {
                return weeks[index];
            }
        }

        return weeks[weeks.Count - 1];
    }

    public static DialogueLinePresentation GetPrimaryDialogueLine(
        IReadOnlyList<DialogueLinePresentation> dialogueLines,
        string fallbackSpeakerName = NemoFeedbackResolver.DefaultSpeakerName)
    {
        if (dialogueLines != null)
        {
            foreach (DialogueLinePresentation dialogueLine in dialogueLines)
            {
                if (!dialogueLine.HasContent)
                {
                    continue;
                }

                string speakerName = string.IsNullOrWhiteSpace(dialogueLine.SpeakerName)
                    ? fallbackSpeakerName
                    : dialogueLine.SpeakerName;
                return new DialogueLinePresentation(speakerName, dialogueLine.Text);
            }
        }

        return new DialogueLinePresentation(fallbackSpeakerName, string.Empty);
    }

    public static ENemoVisualState GetVisualStateForCurrentState(RuntimeChildState childState)
    {
        return GetVisualStateForStat(GetDominantStat(childState));
    }

    public static ENemoVisualState GetVisualStateForStat(EChildStatusType dominantStat)
    {
        return dominantStat switch
        {
            EChildStatusType.Trust => ENemoVisualState.Trusting,
            EChildStatusType.Affinity => ENemoVisualState.Trusting,
            EChildStatusType.Curiosity => ENemoVisualState.Curious,
            EChildStatusType.Anxiety => ENemoVisualState.Anxious,
            EChildStatusType.Obedience => ENemoVisualState.Obedient,
            _ => ENemoVisualState.Neutral,
        };
    }

    public static EChildStatusType GetDominantStat(RuntimeChildState childState)
    {
        return Enum.GetValues(typeof(EChildStatusType))
            .Cast<EChildStatusType>()
            .OrderByDescending(childState.GetStat)
            .First();
    }

    private static IEnumerable<SO_InteractiveEventDefinition> ResolveRoutineEvents(
        SO_WeekDayFlowDefinition dayFlow,
        RuntimeChildState childState,
        IReadOnlyDictionary<string, RuntimeResolvedCardRecord> resolvedCardLookup)
    {
        return dayFlow?.RoutineEvents?
            .Where(routineEvent => routineEvent != null)
            .Select((routineEvent, sourceIndex) => new RoutineEventCandidate(
                routineEvent,
                ResolveMatchedRoutineCard(routineEvent, resolvedCardLookup),
                sourceIndex))
            .Where(item => IsRoutineEventAvailable(item.Event, item.MatchedCard, childState))
            .GroupBy(item => item.MatchedCard.CardDefinition.Id)
            .OrderBy(group => group.Min(item => item.SourceIndex))
            .Select(group => SelectRoutineEventWinner(group, childState))
            .Select(item => item.Event)
            ?? Enumerable.Empty<SO_InteractiveEventDefinition>();
    }

    private static IEnumerable<SO_InteractiveEventDefinition> ResolveStoryEvents(
        SO_WeekDayFlowDefinition dayFlow,
        RuntimeChildState childState,
        RuntimeInformationControlResult informationControlResult)
    {
        return ResolveEligibleEvents(dayFlow?.StoryEvents, childState, informationControlResult);
    }

    private static IEnumerable<SO_InteractiveEventDefinition> ResolveNightDialogues(
        SO_WeekNightFlowDefinition nightFlow,
        RuntimeChildState childState,
        RuntimeInformationControlResult informationControlResult)
    {
        return ResolveEligibleEvents(nightFlow?.Dialogues, childState, informationControlResult);
    }

    private static IEnumerable<SO_InteractiveEventDefinition> ResolveEligibleEvents(
        IEnumerable<SO_InteractiveEventDefinition> events,
        RuntimeChildState childState,
        RuntimeInformationControlResult informationControlResult)
    {
        return events?
            .Where(eventDefinition => IsEventAvailable(eventDefinition, childState, informationControlResult))
            .OrderByDescending(eventDefinition => eventDefinition.Priority)
            ?? Enumerable.Empty<SO_InteractiveEventDefinition>();
    }

    private static bool IsEventAvailable(
        SO_InteractiveEventDefinition eventDefinition,
        RuntimeChildState childState,
        RuntimeInformationControlResult informationControlResult)
    {
        if (eventDefinition?.FirstStep == null)
        {
            return false;
        }

        WeekEventConditionData conditions = eventDefinition.Conditions;
        return WeekEventConditionEvaluator.MeetsAllConditions(childState, informationControlResult, conditions);
    }

    private static bool IsCardLinkedToEvent(
        SO_InteractiveEventDefinition eventDefinition,
        RuntimeResolvedCardRecord resolvedCard)
    {
        if (eventDefinition == null || resolvedCard?.CardDefinition?.CardType == null || resolvedCard.SelectedOption == null)
        {
            return false;
        }

        if (eventDefinition is SO_DayRoutineEventDefinition routineEvent)
        {
            return MatchesRoutineLinkedCard(routineEvent, resolvedCard);
        }

        return MatchesInformationRequirements(eventDefinition.Conditions, resolvedCard);
    }

    private static bool IsRoutineEventAvailable(
        SO_DayRoutineEventDefinition routineEvent,
        RuntimeResolvedCardRecord resolvedCard,
        RuntimeChildState childState)
    {
        if (routineEvent?.FirstStep == null || resolvedCard?.SelectedOption == null)
        {
            return false;
        }

        WeekEventConditionData conditions = routineEvent.Conditions;
        return WeekEventConditionEvaluator.MeetsStateConditions(childState, conditions) &&
               MatchesRoutineEvent(routineEvent, resolvedCard);
    }

    private static bool MatchesRoutineEvent(
        SO_DayRoutineEventDefinition routineEvent,
        RuntimeResolvedCardRecord resolvedCard)
    {
        if (routineEvent?.RelatedInformationTypes == null || routineEvent.RelatedInformationTypes.Length == 0)
        {
            return false;
        }

        bool matchesCardType = routineEvent.RelatedInformationTypes.Contains(resolvedCard.CardDefinition.CardType);
        if (!matchesCardType)
        {
            return false;
        }

        if (routineEvent.PreferredSemantics == null || routineEvent.PreferredSemantics.Length == 0)
        {
            return true;
        }

        return routineEvent.PreferredSemantics.Contains(resolvedCard.SelectedOption.Semantic);
    }

    private static bool MatchesRoutineLinkedCard(
        SO_DayRoutineEventDefinition routineEvent,
        RuntimeResolvedCardRecord resolvedCard)
    {
        SO_CardInfoDefinition linkedCard = routineEvent?.LinkedCard;
        return linkedCard != null &&
               string.Equals(resolvedCard?.CardDefinition?.Id, linkedCard.Id, StringComparison.OrdinalIgnoreCase) &&
               MatchesRoutineEvent(routineEvent, resolvedCard);
    }

    private static bool MatchesInformationRequirements(
        WeekEventConditionData conditions,
        RuntimeResolvedCardRecord resolvedCard)
    {
        if (conditions?.InformationRequirements == null || conditions.InformationRequirements.Length == 0)
        {
            return false;
        }

        return conditions.InformationRequirements.Any(requirement =>
            requirement.InformationType == resolvedCard.CardDefinition.CardType &&
            (!requirement.UseSemanticFilter || requirement.Semantic == resolvedCard.SelectedOption.Semantic));
    }

    private static RoutineEventCandidate SelectRoutineEventWinner(
        IEnumerable<RoutineEventCandidate> candidates,
        RuntimeChildState childState)
    {
        RoutineEventCandidate[] candidateArray = candidates?
            .Where(candidate => candidate.Event != null)
            .ToArray()
            ?? Array.Empty<RoutineEventCandidate>();

        if (candidateArray.Length == 0)
        {
            return default;
        }

        RoutineEventCandidate[] scoredCandidates = candidateArray
            .Select(candidate => new
            {
                Candidate = candidate,
                HasScore = WeekEventConditionEvaluator.TryGetSelectionScore(candidate.Event?.SelectionRule, childState, out int score),
                Score = score,
            })
            .Where(item => item.HasScore)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Candidate.Event.Priority)
            .ThenBy(item => item.Candidate.SourceIndex)
            .Select(item => item.Candidate)
            .ToArray();

        if (scoredCandidates.Length > 0)
        {
            return scoredCandidates[0];
        }

        RoutineEventCandidate[] defaultCandidates = candidateArray
            .Where(candidate => WeekEventConditionEvaluator.IsDefaultSelection(candidate.Event?.SelectionRule))
            .OrderByDescending(candidate => candidate.Event.Priority)
            .ThenBy(candidate => candidate.SourceIndex)
            .ToArray();

        if (defaultCandidates.Length > 0)
        {
            return defaultCandidates[0];
        }

        return candidateArray
            .OrderByDescending(candidate => candidate.Event.Priority)
            .ThenBy(candidate => candidate.SourceIndex)
            .First();
    }

    private static RuntimeResolvedCardRecord ResolveMatchedRoutineCard(
        SO_DayRoutineEventDefinition routineEvent,
        IReadOnlyDictionary<string, RuntimeResolvedCardRecord> resolvedCardLookup)
    {
        string linkedCardId = routineEvent?.LinkedCard?.Id;
        if (!string.IsNullOrWhiteSpace(linkedCardId) &&
            resolvedCardLookup != null &&
            resolvedCardLookup.TryGetValue(linkedCardId, out RuntimeResolvedCardRecord resolvedCard))
        {
            return resolvedCard;
        }

        return ResolveBlockedRoutineFallbackCard(routineEvent, resolvedCardLookup);
    }

    private static RuntimeResolvedCardRecord ResolveBlockedRoutineFallbackCard(
        SO_DayRoutineEventDefinition routineEvent,
        IReadOnlyDictionary<string, RuntimeResolvedCardRecord> resolvedCardLookup)
    {
        if (routineEvent?.PreferredSemantics == null ||
            !routineEvent.PreferredSemantics.Contains(ECardOptionSemantic.Blocked) ||
            resolvedCardLookup == null)
        {
            return null;
        }

        return resolvedCardLookup.Values
            .Where(resolvedCard => resolvedCard?.SelectedOption?.Semantic == ECardOptionSemantic.Blocked)
            .FirstOrDefault(resolvedCard => MatchesRoutineEvent(routineEvent, resolvedCard));
    }

    private static IReadOnlyDictionary<string, RuntimeResolvedCardRecord> BuildResolvedCardLookup(
        IReadOnlyList<RuntimeResolvedCardRecord> resolvedCards)
    {
        Dictionary<string, RuntimeResolvedCardRecord> lookup = new(StringComparer.OrdinalIgnoreCase);

        foreach (RuntimeResolvedCardRecord resolvedCard in resolvedCards ?? Array.Empty<RuntimeResolvedCardRecord>())
        {
            string cardId = resolvedCard?.CardDefinition?.Id;
            if (string.IsNullOrWhiteSpace(cardId))
            {
                continue;
            }

            lookup[cardId] = resolvedCard;
        }

        return lookup;
    }

    private readonly struct RoutineEventCandidate
    {
        public RoutineEventCandidate(
            SO_DayRoutineEventDefinition eventDefinition,
            RuntimeResolvedCardRecord matchedCard,
            int sourceIndex)
        {
            Event = eventDefinition;
            MatchedCard = matchedCard;
            SourceIndex = sourceIndex;
        }

        public SO_DayRoutineEventDefinition Event { get; }
        public RuntimeResolvedCardRecord MatchedCard { get; }
        public int SourceIndex { get; }
    }

    private static ENemoVisualState ResolveVisualState(
        SO_InteractiveEventStepDefinition step,
        RuntimeChildState childState)
    {
        return step.UseCustomVisualState
            ? step.VisualState
            : GetVisualStateForCurrentState(childState);
    }

    private static string BuildEffectSummary(
        IReadOnlyList<SO_CardInteractionDefinition> interactions,
        WeekUiTextProvider weekUiText)
    {
        string[] effectNames = interactions?
            .Where(interaction => interaction != null)
            .Select(interaction => interaction.name)
            .Where(effectName => !string.IsNullOrWhiteSpace(effectName))
            .ToArray()
            ?? Array.Empty<string>();

        return effectNames.Length == 0
            ? string.Empty
            : string.Join(" / ", effectNames);
    }

    private static string GetStatLabel(EChildStatusType statType, WeekUiTextProvider weekUiText)
    {
        return weekUiText != null ? weekUiText.GetStatLabel(statType) : statType.ToString();
    }

    private static DialogueLinePresentation[] BuildDialogueLines(
        IReadOnlyList<DialogueLineData> dialogueLines,
        string legacyFallbackLine)
    {
        DialogueLinePresentation[] lines = dialogueLines?
            .Where(dialogueLine => dialogueLine != null && !string.IsNullOrWhiteSpace(dialogueLine.Text))
            .Select(dialogueLine => new DialogueLinePresentation(
                ResolveSpeakerName(dialogueLine.Speaker),
                dialogueLine.Text))
            .ToArray()
            ?? Array.Empty<DialogueLinePresentation>();

        if (lines.Length > 0)
        {
            return lines;
        }

        return string.IsNullOrWhiteSpace(legacyFallbackLine)
            ? Array.Empty<DialogueLinePresentation>()
            : new[] { new DialogueLinePresentation(NemoFeedbackResolver.DefaultSpeakerName, legacyFallbackLine) };
    }

    private static string ResolveSpeakerName(SO_DialogueSpeakerDefinition speaker)
    {
        if (speaker == null)
        {
            return NemoFeedbackResolver.DefaultSpeakerName;
        }

        if (string.Equals(speaker.Id, NarratorSpeakerId, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(speaker.DisplayName))
        {
            return speaker.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(speaker.name))
        {
            return speaker.name;
        }

        return NemoFeedbackResolver.DefaultSpeakerName;
    }
}

public readonly struct InteractiveEventPresentation
{
    public static InteractiveEventPresentation Empty => new(
        string.Empty,
        string.Empty,
        string.Empty,
        ENemoVisualState.Neutral,
        Array.Empty<DialogueLinePresentation>(),
        Array.Empty<InteractiveEventChoicePresentation>(),
        false);

    public InteractiveEventPresentation(
        string title,
        string bodyText,
        string effectSummaryLine,
        ENemoVisualState visualState,
        IReadOnlyList<DialogueLinePresentation> dialogueLines,
        IReadOnlyList<InteractiveEventChoicePresentation> choices,
        bool canContinue)
    {
        Title = title;
        BodyText = bodyText;
        EffectSummaryLine = effectSummaryLine;
        VisualState = visualState;
        DialogueLines = dialogueLines;
        Choices = choices;
        CanContinue = canContinue;
    }

    public string Title { get; }
    public string BodyText { get; }
    public string EffectSummaryLine { get; }
    public ENemoVisualState VisualState { get; }
    public IReadOnlyList<DialogueLinePresentation> DialogueLines { get; }
    public IReadOnlyList<InteractiveEventChoicePresentation> Choices { get; }
    public bool CanContinue { get; }
    public bool HasContent => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(BodyText);
}

public readonly struct InteractiveEventChoicePresentation
{
    public InteractiveEventChoicePresentation(
        string label,
        string effectSummaryLine)
    {
        Label = label;
        EffectSummaryLine = effectSummaryLine;
    }

    public string Label { get; }
    public string EffectSummaryLine { get; }
}
