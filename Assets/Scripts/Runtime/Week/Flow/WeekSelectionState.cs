using System;
using System.Collections.Generic;
using System.Linq;

public sealed class WeekSelectionState
{
    private readonly Dictionary<SO_CardInfoDefinition, int> _selectedOptionIndexByCard = new();

    public void ApplyWeekEntries(IReadOnlyList<WeekCardEntryData> weekCardEntries)
    {
        HashSet<SO_CardInfoDefinition> validCards = new(
            weekCardEntries?
                .Where(entry => entry?.Card != null)
                .Select(entry => entry.Card)
                ?? Enumerable.Empty<SO_CardInfoDefinition>());

        List<SO_CardInfoDefinition> cardsToRemove = _selectedOptionIndexByCard.Keys
            .Where(cardDefinition => !validCards.Contains(cardDefinition))
            .ToList();

        foreach (SO_CardInfoDefinition cardDefinition in cardsToRemove)
        {
            _selectedOptionIndexByCard.Remove(cardDefinition);
        }

        foreach (WeekCardEntryData weekCardEntry in weekCardEntries ?? Array.Empty<WeekCardEntryData>())
        {
            if (weekCardEntry?.Card == null)
            {
                continue;
            }

            if (!_selectedOptionIndexByCard.ContainsKey(weekCardEntry.Card))
            {
                _selectedOptionIndexByCard.Add(weekCardEntry.Card, 0);
            }
        }
    }

    public void ResetAllSelections(IReadOnlyList<WeekCardEntryData> weekCardEntries)
    {
        ApplyWeekEntries(weekCardEntries);

        foreach (WeekCardEntryData weekCardEntry in weekCardEntries ?? Array.Empty<WeekCardEntryData>())
        {
            if (weekCardEntry?.Card == null)
            {
                continue;
            }

            _selectedOptionIndexByCard[weekCardEntry.Card] = 0;
        }
    }

    public int GetSelectedOptionIndex(SO_CardInfoDefinition cardDefinition)
    {
        if (cardDefinition == null)
        {
            return 0;
        }

        if (_selectedOptionIndexByCard.TryGetValue(cardDefinition, out int selectedOptionIndex))
        {
            return selectedOptionIndex;
        }

        return 0;
    }

    public bool TrySetSelectedOptionIndex(
        SO_CardInfoDefinition cardDefinition,
        int selectedOptionIndex)
    {
        if (cardDefinition == null)
        {
            return false;
        }

        int clampedOptionIndex = ClampOptionIndex(cardDefinition, selectedOptionIndex);
        _selectedOptionIndexByCard[cardDefinition] = clampedOptionIndex;
        return true;
    }

    public int SelectAllBySemantic(
        IReadOnlyList<WeekCardEntryData> weekCardEntries,
        ECardOptionSemantic semantic)
    {
        ApplyWeekEntries(weekCardEntries);

        int selectedCount = 0;

        foreach (WeekCardEntryData weekCardEntry in weekCardEntries ?? Array.Empty<WeekCardEntryData>())
        {
            SO_CardInfoDefinition cardDefinition = weekCardEntry?.Card;
            if (cardDefinition == null)
            {
                continue;
            }

            int optionIndex = FindOptionIndex(cardDefinition.Options, semantic);
            if (optionIndex < 0)
            {
                continue;
            }

            _selectedOptionIndexByCard[cardDefinition] = optionIndex;
            selectedCount++;
        }

        return selectedCount;
    }

    public RuntimeWeekSelection[] BuildSelections(IReadOnlyList<WeekCardEntryData> weekCardEntries)
    {
        List<RuntimeWeekSelection> selections = new();

        foreach (WeekCardEntryData weekCardEntry in weekCardEntries ?? Array.Empty<WeekCardEntryData>())
        {
            if (weekCardEntry?.Card == null)
            {
                continue;
            }

            selections.Add(new RuntimeWeekSelection(
                weekCardEntry.Card,
                GetSelectedOptionIndex(weekCardEntry.Card)));
        }

        return selections.ToArray();
    }

    public WeekSelectionCategoryGroupPresentation[] BuildSelectionGroupPresentations(
    IReadOnlyList<WeekCardEntryData> weekCardEntries,
    string unknownCardTypeLabel)
    {
        WeekSelectionEntryPresentation[] selectionPresentations =
            BuildSelectionPresentations(weekCardEntries, unknownCardTypeLabel);

        if (selectionPresentations.Length == 0)
        {
            return Array.Empty<WeekSelectionCategoryGroupPresentation>();
        }

        List<SO_CardInfoTypeDefinition> orderedTypes = new();
        List<string> orderedTypeNames = new();
        List<List<WeekSelectionEntryPresentation>> groupedEntries = new();

        foreach (WeekSelectionEntryPresentation presentation in selectionPresentations)
        {
            SO_CardInfoTypeDefinition cardType =
                presentation.CardDefinition != null ? presentation.CardDefinition.CardType : null;

            int groupIndex = orderedTypes.FindIndex(existingType => existingType == cardType);

            if (groupIndex < 0)
            {
                orderedTypes.Add(cardType);
                orderedTypeNames.Add(
                    string.IsNullOrWhiteSpace(presentation.TypeName)
                        ? unknownCardTypeLabel
                        : presentation.TypeName);
                groupedEntries.Add(new List<WeekSelectionEntryPresentation>());
                groupIndex = groupedEntries.Count - 1;
            }

            groupedEntries[groupIndex].Add(presentation);
        }

        WeekSelectionCategoryGroupPresentation[] groups =
            new WeekSelectionCategoryGroupPresentation[groupedEntries.Count];

        for (int i = 0; i < groupedEntries.Count; i++)
        {
            groups[i] = new WeekSelectionCategoryGroupPresentation(
                orderedTypes[i],
                orderedTypeNames[i],
                groupedEntries[i]);
        }

        return groups;
    }

    public WeekSelectionEntryPresentation[] BuildSelectionPresentations(
        IReadOnlyList<WeekCardEntryData> weekCardEntries,
        string unknownCardTypeLabel)
    {
        List<WeekSelectionEntryPresentation> selectionPresentations = new();

        foreach (WeekCardEntryData weekCardEntry in weekCardEntries ?? Array.Empty<WeekCardEntryData>())
        {
            if (weekCardEntry?.Card == null)
            {
                continue;
            }

            SO_CardInfoDefinition cardDefinition = weekCardEntry.Card;
            selectionPresentations.Add(new WeekSelectionEntryPresentation(
                cardDefinition,
                cardDefinition.CardType != null ? cardDefinition.CardType.DisplayName : unknownCardTypeLabel,
                cardDefinition.Title,
                cardDefinition.OriginalText,
                GetSelectedOptionIndex(cardDefinition),
                cardDefinition.Options ?? Array.Empty<CardOptionData>()));
        }

        return selectionPresentations.ToArray();
    }

    private static int ClampOptionIndex(
        SO_CardInfoDefinition cardDefinition,
        int selectedOptionIndex)
    {
        CardOptionData[] options = cardDefinition.Options ?? Array.Empty<CardOptionData>();
        if (options.Length == 0)
        {
            return 0;
        }

        if (selectedOptionIndex < 0)
        {
            return 0;
        }

        if (selectedOptionIndex >= options.Length)
        {
            return options.Length - 1;
        }

        return selectedOptionIndex;
    }

    private static int FindOptionIndex(
        IReadOnlyList<CardOptionData> options,
        ECardOptionSemantic semantic)
    {
        if (options == null)
        {
            return -1;
        }

        for (int i = 0; i < options.Count; i++)
        {
            CardOptionData option = options[i];
            if (option != null && option.Semantic == semantic)
            {
                return i;
            }
        }

        return -1;
    }
}
