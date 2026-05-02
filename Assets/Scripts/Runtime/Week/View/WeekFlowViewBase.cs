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
    public event Action WeeklyResultLogContinueRequested;
    public event Action WeeklyStatResultContinueRequested;
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

    protected void RaiseWeeklyResultLogContinueRequested()
    {
        WeeklyResultLogContinueRequested?.Invoke();
    }

    protected void RaiseWeeklyStatResultContinueRequested()
    {
        WeeklyStatResultContinueRequested?.Invoke();
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
    public virtual void RenderDayFlowProgress(DayFlowProgressSnapshot presentation) { }
    public virtual void RenderStatusMessage(string statusMessage) { }
    public virtual void PresentNemoFeedback(NemoFeedbackPresentation presentation) { }
    public virtual void ShowWeekFeedback(WeekFeedbackPresentation presentation) { }
    public virtual void ShowInteractiveEvent(InteractiveEventPresentation presentation) { }
    public virtual void ShowInteractiveEventResult(InteractiveEventChoiceResultPresentation presentation) { }
    public virtual void ShowWeeklyResultLog(WeeklyResultLogPresentation presentation) { }
    public virtual void ShowWeeklyStatResult(WeeklyStatResultPresentation presentation) { }
    public virtual void ShowEnding(EndingPresentation presentation) { }
    public virtual void ShowEndingFollowUp() { }
    public virtual void HideTransientViews() { }
    public virtual void SetMainCanvasVisible(bool visible) { }
    public virtual void AppendDialogueLogEntries(IReadOnlyList<DialogueLogEntry> entries) { }
    public virtual WeekFlowCutsceneBridgeBase GetCutsceneBridge() { return null; }
    public virtual void SetFlowScreenContext(WeekFlowScreen screen, RuntimeChildState childState, RuntimeWeekResult lastWeekResult) { }
    public virtual IEnumerator PlayCurrentDialogueCutscene() { yield break; }
    public virtual IEnumerator PlayFlowTransition(WeekFlowTransitionContext context) { yield break; }
    public virtual IEnumerator PlayWeekEntryIntro(SO_WeekDefinition weekDefinition) { yield break; }
}

public readonly struct DayFlowProgressSnapshot
{
    public DayFlowProgressSnapshot(int totalCount, int currentIndex, int completedCount, bool isActive)
    {
        TotalCount = Mathf.Max(0, totalCount);
        CurrentIndex = currentIndex;
        CompletedCount = Mathf.Clamp(completedCount, 0, TotalCount);
        IsActive = isActive && TotalCount > 0 && CurrentIndex >= 0 && CurrentIndex < TotalCount;
    }

    public int TotalCount { get; }
    public int CurrentIndex { get; }
    public int CompletedCount { get; }
    public bool IsActive { get; }

    public float IndicatorT
    {
        get
        {
            if (!IsActive)
            {
                return 0f;
            }

            return TotalCount <= 1 ? 0.5f : CurrentIndex / (float)(TotalCount - 1);
        }
    }

    public float FillAmount => TotalCount <= 0 ? 0f : Mathf.Clamp01((CompletedCount + (IsActive ? 1f : 0f)) / TotalCount);

    public static DayFlowProgressSnapshot Hidden => new(0, -1, 0, false);
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
        : this(
            cardDefinition,
            typeName,
            title,
            originalText,
            selectedOptionIndex,
            options,
            false,
            0)
    {
    }

    public WeekSelectionEntryPresentation(
        SO_CardInfoDefinition cardDefinition,
        string typeName,
        string title,
        string originalText,
        int selectedOptionIndex,
        IReadOnlyList<CardOptionData> options,
        bool hasCategoryStatValue,
        int categoryStatValue)
    {
        CardDefinition = cardDefinition;
        TypeName = typeName;
        Title = title;
        OriginalText = originalText;
        SelectedOptionIndex = selectedOptionIndex;
        Options = options;
        HasCategoryStatValue = hasCategoryStatValue;
        CategoryStatValue = categoryStatValue;
    }

    public SO_CardInfoDefinition CardDefinition { get; }
    public string TypeName { get; }
    public string Title { get; }
    public string OriginalText { get; }
    public int SelectedOptionIndex { get; }
    public IReadOnlyList<CardOptionData> Options { get; }
    public bool HasCategoryStatValue { get; }
    public int CategoryStatValue { get; }
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

public readonly struct WeeklyStatResultPresentation
{
    public WeeklyStatResultPresentation(IReadOnlyList<WeeklyStatChangePresentation> changes)
    {
        Changes = changes ?? Array.Empty<WeeklyStatChangePresentation>();
    }

    public IReadOnlyList<WeeklyStatChangePresentation> Changes { get; }
    public bool HasChanges => Changes != null && Changes.Count > 0;
}

public readonly struct WeeklyStatChangePresentation
{
    public WeeklyStatChangePresentation(
        EChildStatusType statType,
        string label,
        string leftLabel,
        string rightLabel,
        int beforeValue,
        int afterValue,
        int minValue,
        int maxValue,
        ECharacterStatusBarRenderMode renderMode)
    {
        StatType = statType;
        Label = label ?? string.Empty;
        LeftLabel = leftLabel ?? string.Empty;
        RightLabel = rightLabel ?? string.Empty;
        BeforeValue = beforeValue;
        AfterValue = afterValue;
        MinValue = minValue;
        MaxValue = maxValue;
        RenderMode = renderMode;
    }

    public EChildStatusType StatType { get; }
    public string Label { get; }
    public string LeftLabel { get; }
    public string RightLabel { get; }
    public int BeforeValue { get; }
    public int AfterValue { get; }
    public int Delta => AfterValue - BeforeValue;
    public int MinValue { get; }
    public int MaxValue { get; }
    public ECharacterStatusBarRenderMode RenderMode { get; }
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

public readonly struct WeeklyResultLogPresentation
{
    public WeeklyResultLogPresentation(IReadOnlyList<WeeklyResultLogEntryPresentation> entries)
        : this(entries, Array.Empty<WeeklyResultLogWeekPresentation>(), string.Empty, Array.Empty<WeeklyResultStatDeltaPresentation>())
    {
    }

    public WeeklyResultLogPresentation(
        IReadOnlyList<WeeklyResultLogEntryPresentation> entries,
        IReadOnlyList<WeeklyResultLogWeekPresentation> weeks,
        string selectedWeekId)
        : this(entries, weeks, selectedWeekId, Array.Empty<WeeklyResultStatDeltaPresentation>())
    {
    }

    public WeeklyResultLogPresentation(
        IReadOnlyList<WeeklyResultLogEntryPresentation> entries,
        IReadOnlyList<WeeklyResultLogWeekPresentation> weeks,
        string selectedWeekId,
        IReadOnlyList<WeeklyResultStatDeltaPresentation> statSummary)
    {
        Entries = entries ?? Array.Empty<WeeklyResultLogEntryPresentation>();
        Weeks = weeks ?? Array.Empty<WeeklyResultLogWeekPresentation>();
        SelectedWeekId = selectedWeekId ?? string.Empty;
        StatSummary = statSummary ?? Array.Empty<WeeklyResultStatDeltaPresentation>();
    }

    public IReadOnlyList<WeeklyResultLogEntryPresentation> Entries { get; }
    public IReadOnlyList<WeeklyResultLogWeekPresentation> Weeks { get; }
    public string SelectedWeekId { get; }
    public IReadOnlyList<WeeklyResultStatDeltaPresentation> StatSummary { get; }
    public bool HasEntries => Entries != null && Entries.Count > 0;
    public bool HasWeeks => Weeks != null && Weeks.Count > 0;
    public bool HasStatSummary => StatSummary != null && StatSummary.Count > 0;
}

public readonly struct WeeklyResultLogWeekPresentation
{
    public WeeklyResultLogWeekPresentation(
        string weekId,
        int weekIndex,
        string title,
        IReadOnlyList<WeeklyResultLogEntryPresentation> entries)
        : this(weekId, weekIndex, title, entries, Array.Empty<WeeklyResultStatDeltaPresentation>())
    {
    }

    public WeeklyResultLogWeekPresentation(
        string weekId,
        int weekIndex,
        string title,
        IReadOnlyList<WeeklyResultLogEntryPresentation> entries,
        IReadOnlyList<WeeklyResultStatDeltaPresentation> statSummary)
    {
        WeekId = weekId ?? string.Empty;
        WeekIndex = weekIndex;
        Title = title ?? string.Empty;
        Entries = entries ?? Array.Empty<WeeklyResultLogEntryPresentation>();
        StatSummary = statSummary ?? Array.Empty<WeeklyResultStatDeltaPresentation>();
    }

    public string WeekId { get; }
    public int WeekIndex { get; }
    public string Title { get; }
    public IReadOnlyList<WeeklyResultLogEntryPresentation> Entries { get; }
    public IReadOnlyList<WeeklyResultStatDeltaPresentation> StatSummary { get; }
    public string Label => WeekIndex > 0 ? $"WEEK {WeekIndex}" : Title;
}

public readonly struct WeeklyResultStatDeltaPresentation
{
    public WeeklyResultStatDeltaPresentation(EChildStatusType statType, string label, int delta)
    {
        StatType = statType;
        Label = label ?? string.Empty;
        Delta = Math.Max(0, delta);
    }

    public EChildStatusType StatType { get; }
    public string Label { get; }
    public int Delta { get; }
}

public readonly struct WeeklyResultLogEntryPresentation
{
    public WeeklyResultLogEntryPresentation(string eventId, string title, string context)
    {
        EventId = eventId;
        Title = title;
        Context = context;
    }

    public string EventId { get; }
    public string Title { get; }
    public string Context { get; }
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
