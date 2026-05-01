using System;
using System.Collections.Generic;
using System.Linq;

public sealed class WeekRunner
{
    public RuntimeWeekResult RunWeek(
        SO_WeekDefinition weekDefinition,
        RuntimeChildState childState,
        IReadOnlyList<RuntimeWeekSelection> selections,
        IReadOnlyList<WeekCardEntryData> weekEntries)
    {
        if (weekDefinition == null)
        {
            throw new ArgumentNullException(nameof(weekDefinition));
        }

        if (childState == null)
        {
            throw new ArgumentNullException(nameof(childState));
        }

        if (selections == null)
        {
            throw new ArgumentNullException(nameof(selections));
        }

        RuntimeWeekContext context = new(weekDefinition, childState);
        Dictionary<SO_CardInfoDefinition, RuntimeWeekSelection> selectionMap = BuildSelectionMap(selections);

        GameplayInteractionExecutor.ApplyAll(weekDefinition.OnWeekStartInteractions, childState);

        foreach (SO_WeekRuleDefinition weekRule in weekDefinition.WeekRules)
        {
            weekRule?.OnWeekStart(context);
        }

        foreach (WeekCardEntryData cardEntry in (weekEntries ?? Array.Empty<WeekCardEntryData>()).OrderBy(entry => entry.DisplayOrder))
        {
            ResolveCardEntry(cardEntry, selectionMap, context);
        }

        foreach (SO_WeekRuleDefinition weekRule in weekDefinition.WeekRules)
        {
            weekRule?.OnWeekEnd(context);
        }

        RuntimeChildState eventResolutionChildState = childState.CreateCopy();
        ApplyDeferredCardRewards(context.ResolvedCards, eventResolutionChildState);

        GameplayInteractionExecutor.ApplyAll(weekDefinition.OnWeekEndInteractions, childState);
        GameplayInteractionExecutor.ApplyAll(weekDefinition.OnWeekEndInteractions, eventResolutionChildState);

        return new RuntimeWeekResult(
            weekDefinition,
            context.ResolvedCards,
            context.WeekLogs,
            WeekInformationControlResolver.Resolve(context.ResolvedCards),
            eventResolutionChildState);
    }

    private static Dictionary<SO_CardInfoDefinition, RuntimeWeekSelection> BuildSelectionMap(
        IReadOnlyList<RuntimeWeekSelection> selections)
    {
        Dictionary<SO_CardInfoDefinition, RuntimeWeekSelection> map = new();

        foreach (RuntimeWeekSelection selection in selections)
        {
            if (selection?.CardDefinition == null)
            {
                continue;
            }

            map[selection.CardDefinition] = selection;
        }

        return map;
    }

    private static void ResolveCardEntry(
        WeekCardEntryData cardEntry,
        IReadOnlyDictionary<SO_CardInfoDefinition, RuntimeWeekSelection> selectionMap,
        RuntimeWeekContext context)
    {
        if (cardEntry?.Card == null)
        {
            return;
        }

        if (!selectionMap.TryGetValue(cardEntry.Card, out RuntimeWeekSelection selection))
        {
            if (cardEntry.IsRequired)
            {
                throw new InvalidOperationException(
                    $"Required card '{cardEntry.Card.name}' is missing a runtime selection.");
            }

            return;
        }

        CardOptionData selectedOption = selection.GetSelectedOption();

        foreach (SO_WeekRuleDefinition weekRule in context.WeekDefinition.WeekRules)
        {
            weekRule?.BeforeResolveCard(context, cardEntry.Card, selectedOption);
        }

        foreach (SO_WeekRuleDefinition weekRule in context.WeekDefinition.WeekRules)
        {
            weekRule?.AfterResolveCard(context, cardEntry.Card, selectedOption);
        }

        context.AddResolvedCard(
            new RuntimeResolvedCardRecord(cardEntry.Card, selection.SelectedOptionIndex, selectedOption));
    }

    private static void ApplyDeferredCardRewards(
        IReadOnlyList<RuntimeResolvedCardRecord> resolvedCards,
        RuntimeChildState childState)
    {
        if (resolvedCards == null || childState == null)
        {
            return;
        }

        foreach (RuntimeResolvedCardRecord resolvedCard in resolvedCards)
        {
            resolvedCard?.ApplyInteractionsTo(childState);
        }
    }
}

public static class WeekInformationControlResolver
{
    public static RuntimeInformationControlResult Resolve(
        IReadOnlyList<RuntimeResolvedCardRecord> resolvedCards)
    {
        RuntimeInformationSelectionRecord[] selections = resolvedCards?
            .Where(resolvedCard => resolvedCard?.CardDefinition != null && resolvedCard.SelectedOption != null)
            .Select(resolvedCard => new RuntimeInformationSelectionRecord(
                resolvedCard.CardDefinition,
                resolvedCard.CardDefinition.CardType,
                resolvedCard.SelectedOption.Semantic))
            .ToArray()
            ?? Array.Empty<RuntimeInformationSelectionRecord>();

        return new RuntimeInformationControlResult(selections);
    }
}
