using System;
using System.Collections.Generic;
using System.Linq;

public static class WeekCardEntryResolver
{
    public static WeekCardEntryData[] ResolveCurrentWeekEntries(
        SO_WeekDefinition currentWeekDefinition,
        RuntimeChildState childState)
    {
        WeekCardEntryData[] rawEntries = currentWeekDefinition?.PreTurn?.InformationCards
            ?? Array.Empty<WeekCardEntryData>();

        IndexedWeekCardEntry[] validEntries = rawEntries
            .Select((entry, sourceIndex) => new IndexedWeekCardEntry(entry, sourceIndex))
            .Where(item => item.Entry?.Card != null)
            .ToArray();

        if (validEntries.Length == 0)
        {
            return Array.Empty<WeekCardEntryData>();
        }

        SO_DayRoutineEventDefinition[] routineEvents = currentWeekDefinition?.DayFlow?.RoutineEvents
            ?? Array.Empty<SO_DayRoutineEventDefinition>();

        List<ResolvedWeekCardEntry> resolvedEntries = new();

        foreach (IndexedWeekCardEntry nullTypeEntry in validEntries.Where(item => item.Entry.Card.CardType == null))
        {
            resolvedEntries.Add(new ResolvedWeekCardEntry(nullTypeEntry.Entry, nullTypeEntry.Entry.DisplayOrder, nullTypeEntry.SourceIndex));
        }

        IEnumerable<IGrouping<SO_CardInfoTypeDefinition, IndexedWeekCardEntry>> typedGroups = validEntries
            .Where(item => item.Entry.Card.CardType != null)
            .GroupBy(item => item.Entry.Card.CardType);

        foreach (IGrouping<SO_CardInfoTypeDefinition, IndexedWeekCardEntry> group in typedGroups)
        {
            IndexedWeekCardEntry? selected = SelectBestCandidate(group, routineEvents, childState);
            if (!selected.HasValue)
            {
                continue;
            }

            int displayOrder = group.Min(item => item.Entry.DisplayOrder);
            int sourceIndex = group.Min(item => item.SourceIndex);
            resolvedEntries.Add(new ResolvedWeekCardEntry(selected.Value.Entry, displayOrder, sourceIndex));
        }

        return resolvedEntries
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.SourceIndex)
            .Select(item => item.Entry)
            .ToArray();
    }

    private static IndexedWeekCardEntry? SelectBestCandidate(
        IEnumerable<IndexedWeekCardEntry> candidates,
        IReadOnlyList<SO_DayRoutineEventDefinition> routineEvents,
        RuntimeChildState childState)
    {
        IndexedWeekCardEntry[] candidateArray = candidates?
            .OrderBy(item => item.Entry.DisplayOrder)
            .ThenBy(item => item.SourceIndex)
            .ToArray()
            ?? Array.Empty<IndexedWeekCardEntry>();

        if (candidateArray.Length == 0)
        {
            return null;
        }

        CandidateScore[] scoredCandidates = candidateArray
            .Select(candidate => BuildCandidateScore(candidate, routineEvents, childState))
            .ToArray();

        CandidateScore[] activeScoredCandidates = scoredCandidates
            .Where(score => score.HasActiveScore)
            .OrderByDescending(score => score.Score)
            .ThenByDescending(score => score.EventPriority)
            .ThenBy(score => score.EventSourceIndex)
            .ThenBy(score => score.Entry.Entry.DisplayOrder)
            .ThenBy(score => score.Entry.SourceIndex)
            .ToArray();

        if (activeScoredCandidates.Length > 0)
        {
            return activeScoredCandidates[0].Entry;
        }

        CandidateScore[] defaultCandidates = scoredCandidates
            .Where(score => score.IsDefault)
            .OrderByDescending(score => score.EventPriority)
            .ThenBy(score => score.EventSourceIndex)
            .ThenBy(score => score.Entry.Entry.DisplayOrder)
            .ThenBy(score => score.Entry.SourceIndex)
            .ToArray();

        if (defaultCandidates.Length > 0)
        {
            return defaultCandidates[0].Entry;
        }

        return candidateArray[0];
    }

    private static CandidateScore BuildCandidateScore(
        IndexedWeekCardEntry candidate,
        IReadOnlyList<SO_DayRoutineEventDefinition> routineEvents,
        RuntimeChildState childState)
    {
        SO_DayRoutineEventDefinition[] linkedEvents = GetLinkedRoutineEvents(candidate.Entry.Card, routineEvents)
            .ToArray();

        if (linkedEvents.Length == 0)
        {
            return CandidateScore.Unlinked(candidate);
        }

        SO_DayRoutineEventDefinition[] availableLinkedEvents = linkedEvents
            .Where(eventDefinition => eventDefinition.FirstStep != null)
            .Where(eventDefinition => WeekEventConditionEvaluator.MeetsStateConditions(childState, eventDefinition.Conditions))
            .ToArray();

        if (availableLinkedEvents.Length == 0)
        {
            return CandidateScore.Unavailable(candidate);
        }

        CandidateScore bestDefault = CandidateScore.Unavailable(candidate);

        CandidateScore bestActive = CandidateScore.Unavailable(candidate);
        SO_DayRoutineEventDefinition[] routineEventArray = routineEvents?.ToArray()
            ?? Array.Empty<SO_DayRoutineEventDefinition>();

        for (int eventIndex = 0; eventIndex < availableLinkedEvents.Length; eventIndex++)
        {
            SO_DayRoutineEventDefinition linkedEvent = availableLinkedEvents[eventIndex];
            int sourceIndex = Array.IndexOf(routineEventArray, linkedEvent);

            if (WeekEventConditionEvaluator.TryGetSelectionScore(linkedEvent.SelectionRule, childState, out int score))
            {
                CandidateScore active = CandidateScore.Active(candidate, score, linkedEvent.Priority, sourceIndex);
                if (!bestActive.HasActiveScore || IsBetterActiveScore(active, bestActive))
                {
                    bestActive = active;
                }

                continue;
            }

            if (WeekEventConditionEvaluator.IsDefaultSelection(linkedEvent.SelectionRule) &&
                (!bestDefault.IsDefault ||
                 linkedEvent.Priority > bestDefault.EventPriority ||
                 sourceIndex < bestDefault.EventSourceIndex))
            {
                bestDefault = CandidateScore.Default(candidate, linkedEvent.Priority, sourceIndex);
            }
        }

        if (bestActive.HasActiveScore)
        {
            return bestActive;
        }

        return bestDefault;
    }

    private static bool IsBetterActiveScore(CandidateScore candidate, CandidateScore current)
    {
        if (candidate.Score != current.Score)
        {
            return candidate.Score > current.Score;
        }

        if (candidate.EventPriority != current.EventPriority)
        {
            return candidate.EventPriority > current.EventPriority;
        }

        return candidate.EventSourceIndex < current.EventSourceIndex;
    }

    private static IEnumerable<SO_DayRoutineEventDefinition> GetLinkedRoutineEvents(
        SO_CardInfoDefinition card,
        IReadOnlyList<SO_DayRoutineEventDefinition> routineEvents)
    {
        string cardId = card?.Id;
        if (string.IsNullOrWhiteSpace(cardId) || routineEvents == null)
        {
            return Enumerable.Empty<SO_DayRoutineEventDefinition>();
        }

        return routineEvents.Where(eventDefinition =>
            eventDefinition?.LinkedCard != null &&
            string.Equals(eventDefinition.LinkedCard.Id, cardId, StringComparison.OrdinalIgnoreCase));
    }

    private readonly struct IndexedWeekCardEntry
    {
        public IndexedWeekCardEntry(WeekCardEntryData entry, int sourceIndex)
        {
            Entry = entry;
            SourceIndex = sourceIndex;
        }

        public WeekCardEntryData Entry { get; }
        public int SourceIndex { get; }
    }

    private readonly struct ResolvedWeekCardEntry
    {
        public ResolvedWeekCardEntry(WeekCardEntryData entry, int displayOrder, int sourceIndex)
        {
            Entry = entry;
            DisplayOrder = displayOrder;
            SourceIndex = sourceIndex;
        }

        public WeekCardEntryData Entry { get; }
        public int DisplayOrder { get; }
        public int SourceIndex { get; }
    }

    private readonly struct CandidateScore
    {
        private CandidateScore(
            IndexedWeekCardEntry entry,
            bool hasActiveScore,
            bool isDefault,
            int score,
            int eventPriority,
            int eventSourceIndex)
        {
            Entry = entry;
            HasActiveScore = hasActiveScore;
            IsDefault = isDefault;
            Score = score;
            EventPriority = eventPriority;
            EventSourceIndex = eventSourceIndex;
        }

        public IndexedWeekCardEntry Entry { get; }
        public bool HasActiveScore { get; }
        public bool IsDefault { get; }
        public int Score { get; }
        public int EventPriority { get; }
        public int EventSourceIndex { get; }

        public static CandidateScore Active(IndexedWeekCardEntry entry, int score, int eventPriority, int eventSourceIndex)
        {
            return new CandidateScore(entry, true, false, score, eventPriority, eventSourceIndex);
        }

        public static CandidateScore Default(IndexedWeekCardEntry entry, int eventPriority, int eventSourceIndex)
        {
            return new CandidateScore(entry, false, true, 0, eventPriority, eventSourceIndex);
        }

        public static CandidateScore Unlinked(IndexedWeekCardEntry entry)
        {
            return new CandidateScore(entry, false, true, 0, int.MinValue, int.MaxValue);
        }

        public static CandidateScore Unavailable(IndexedWeekCardEntry entry)
        {
            return new CandidateScore(entry, false, false, 0, int.MinValue, int.MaxValue);
        }
    }
}
