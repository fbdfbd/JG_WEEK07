using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeekFlowViewBase : MonoBehaviour
{
    public event Action RunWeekRequested;
    public event Action ResetSelectionsRequested;
    public event Action ResetChildStateRequested;
    public event Action WeekFeedbackClosed;
    public event Action InteractiveEventContinueRequested;
    public event Action InteractiveEventSkipRequested;
    public event Action<int> InteractiveEventChoiceSelected;
    public event Action<SO_CardInfoDefinition, int> CardOptionSelected;
    public event Action<ECardOptionSemantic> AllCardSemanticSelected;

    protected void RaiseRunWeekRequested()
    {
        RunWeekRequested?.Invoke();
    }

    protected void RaiseResetSelectionsRequested()
    {
        ResetSelectionsRequested?.Invoke();
    }

    protected void RaiseResetChildStateRequested()
    {
        ResetChildStateRequested?.Invoke();
    }

    protected void RaiseWeekFeedbackClosed()
    {
        WeekFeedbackClosed?.Invoke();
    }

    protected void RaiseInteractiveEventContinueRequested()
    {
        InteractiveEventContinueRequested?.Invoke();
    }

    protected void RaiseInteractiveEventSkipRequested()
    {
        InteractiveEventSkipRequested?.Invoke();
    }

    protected void RaiseInteractiveEventChoiceSelected(int choiceIndex)
    {
        InteractiveEventChoiceSelected?.Invoke(choiceIndex);
    }

    protected void RaiseCardOptionSelected(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        CardOptionSelected?.Invoke(cardDefinition, optionIndex);
    }

    protected void RaiseAllCardSemanticSelected(ECardOptionSemantic semantic)
    {
        AllCardSemanticSelected?.Invoke(semantic);
    }

    public virtual void RenderWeekHeader(WeekHeaderPresentation presentation) { }
    public virtual void RenderSelections(IReadOnlyList<WeekSelectionEntryPresentation> presentations) { }

    public virtual void RenderSelectionGroups(IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups)
    {
        if (groups == null || groups.Count == 0)
        {
            RenderSelections(Array.Empty<WeekSelectionEntryPresentation>());
            return;
        }

        List<WeekSelectionEntryPresentation> flattened = new();

        foreach (WeekSelectionCategoryGroupPresentation group in groups)
        {
            if (group.Entries == null || group.Entries.Count == 0)
            {
                continue;
            }

            flattened.AddRange(group.Entries);
        }

        RenderSelections(flattened);
    }


    public virtual void RenderChildState(ChildStatePresentation presentation) { }
    public virtual void RenderStatusMessage(string statusMessage) { }
    public virtual void PresentNemoFeedback(NemoFeedbackPresentation presentation) { }
    public virtual void ShowWeekFeedback(WeekFeedbackPresentation presentation) { }
    public virtual void ShowInteractiveEvent(InteractiveEventPresentation presentation) { }
    public virtual void ShowInteractiveEventResult(InteractiveEventChoiceResultPresentation presentation) { }
    public virtual void ShowEnding(EndingPresentation presentation) { }
    public virtual void ShowEndingFollowUp() { }
    public virtual void HideTransientViews() { }
    public virtual void SetMainCanvasVisible(bool visible) { }
    public virtual WeekFlowCutsceneBridgeBase GetCutsceneBridge() { return null; }
    public virtual void SetFlowScreenContext(WeekFlowScreen screen, RuntimeChildState childState, RuntimeWeekResult lastWeekResult) { }
    public virtual IEnumerator PlayCurrentDialogueCutscene() { yield break; }
    public virtual IEnumerator PlayFlowTransition(WeekFlowTransitionContext context) { yield break; }
    public virtual IEnumerator PlayWeekEntryIntro(SO_WeekDefinition weekDefinition) { yield break; }
}

public readonly struct WeekHeaderPresentation
{
    public WeekHeaderPresentation(string weekLabel, string title, string summary)
    {
        WeekLabel = weekLabel;
        Title = title;
        Summary = summary;
    }

    public string WeekLabel { get; }
    public string Title { get; }
    public string Summary { get; }
}

public readonly struct WeekSelectionEntryPresentation
{
    public WeekSelectionEntryPresentation(
        SO_CardInfoDefinition cardDefinition,
        string typeName,
        string title,
        string originalText,
        int selectedOptionIndex,
        IReadOnlyList<CardOptionData> options)
    {
        CardDefinition = cardDefinition;
        TypeName = typeName;
        Title = title;
        OriginalText = originalText;
        SelectedOptionIndex = selectedOptionIndex;
        Options = options;
    }

    public SO_CardInfoDefinition CardDefinition { get; }
    public string TypeName { get; }
    public string Title { get; }
    public string OriginalText { get; }
    public int SelectedOptionIndex { get; }
    public IReadOnlyList<CardOptionData> Options { get; }
}

public readonly struct WeekStatPresentation
{
    public WeekStatPresentation(EChildStatusType statType, string label, int value)
    {
        StatType = statType;
        Label = label;
        Value = value;
    }

    public EChildStatusType StatType { get; }
    public string Label { get; }
    public int Value { get; }
}

public readonly struct ChildStatePresentation
{
    public ChildStatePresentation(
        IReadOnlyList<WeekStatPresentation> stats,
        IReadOnlyList<string> flags,
        IReadOnlyList<string> reactionLogs)
    {
        Stats = stats;
        Flags = flags;
        ReactionLogs = reactionLogs;
    }

    public IReadOnlyList<WeekStatPresentation> Stats { get; }
    public IReadOnlyList<string> Flags { get; }
    public IReadOnlyList<string> ReactionLogs { get; }
}

public readonly struct DialogueLinePresentation
{
    public DialogueLinePresentation(string speakerName, string text)
    {
        SpeakerName = speakerName;
        Text = text;
    }

    public string SpeakerName { get; }
    public string Text { get; }
    public bool HasContent => !string.IsNullOrWhiteSpace(Text);
}

public readonly struct InteractiveEventChoiceResultPresentation
{
    public InteractiveEventChoiceResultPresentation(
        IReadOnlyList<DialogueLinePresentation> dialogueLines,
        string effectSummaryLine)
    {
        DialogueLines = dialogueLines;
        EffectSummaryLine = effectSummaryLine;
    }

    public IReadOnlyList<DialogueLinePresentation> DialogueLines { get; }
    public string EffectSummaryLine { get; }
}

public readonly struct WeekSelectionCategoryGroupPresentation
{
    public WeekSelectionCategoryGroupPresentation(
        SO_CardInfoTypeDefinition cardType,
        string typeName,
        IReadOnlyList<WeekSelectionEntryPresentation> entries)
    {
        CardType = cardType;
        TypeName = typeName;
        Entries = entries;
    }

    public SO_CardInfoTypeDefinition CardType { get; }
    public string TypeName { get; }
    public IReadOnlyList<WeekSelectionEntryPresentation> Entries { get; }
}
