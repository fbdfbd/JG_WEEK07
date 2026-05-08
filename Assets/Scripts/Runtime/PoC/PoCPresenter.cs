using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PoCPresenter
{
    private const string NarratorSpeakerId = "speaker_narrator";

    private readonly PoCRuntimeState _state;

    public PoCPresenter(PoCRuntimeState state)
    {
        _state = state;
    }

    public PoCTextPresentation BuildText(PoCTextPageData page)
    {
        return page == null
            ? PoCTextPresentation.Empty
            : new PoCTextPresentation(page.Title, page.Body);
    }

    public PoCTextPresentation BuildReport(PoCDayData day)
    {
        PoCTextPageData page = ResolveReportPage(day?.Report);
        return BuildText(page);
    }

    public PoCMainChoicePresentation BuildMainChoice(PoCDayData day)
    {
        PoCMainChoiceOptionPresentation[] options = day?.MainChoices?
            .Where(choice => choice != null && !string.IsNullOrWhiteSpace(choice.Id))
            .Select(choice => new PoCMainChoiceOptionPresentation(
                choice.Id,
                choice.Label,
                choice.Description,
                BuildStatChangeSummary(choice.StatChanges)))
            .ToArray()
            ?? Array.Empty<PoCMainChoiceOptionPresentation>();

        return new PoCMainChoicePresentation(
            day != null ? day.ChoiceTitle : string.Empty,
            day != null ? day.ChoiceBody : string.Empty,
            options);
    }

    public PoCDayEventPresentation BuildDayEvent(PoCDayEventResolvedData dayEvent)
    {
        return !dayEvent.HasContent
            ? PoCDayEventPresentation.Empty
            : new PoCDayEventPresentation(
                dayEvent.Title,
                dayEvent.Body,
                BuildStatChangeSummary(dayEvent.StatChanges));
    }

    public IReadOnlyList<PoCStatPresentation> BuildPublicStats()
    {
        return PoCRuntimeState.PublicStatTypes
            .Select(type => new PoCStatPresentation(type, GetStatLabel(type), _state.GetStat(type)))
            .ToArray();
    }

    public InteractiveEventPresentation BuildNightDialogue(RuntimeInteractiveEventSession session)
    {
        if (session?.EventDefinition == null || session.CurrentStep == null)
        {
            return InteractiveEventPresentation.Empty;
        }

        SO_InteractiveEventStepDefinition step = session.CurrentStep;
        InteractiveEventChoicePresentation[] choices = step.Choices?
            .Where(choice => choice != null)
            .Select(choice => new InteractiveEventChoicePresentation(choice.Label, string.Empty))
            .ToArray()
            ?? Array.Empty<InteractiveEventChoicePresentation>();

        return new InteractiveEventPresentation(
            string.IsNullOrWhiteSpace(step.TitleOverride) ? session.EventDefinition.Title : step.TitleOverride,
            step.BodyText,
            string.Empty,
            step.UseCustomVisualState ? step.VisualState : ENemoVisualState.Neutral,
            BuildDialogueLines(step.DialogueLines, step.NemoLine),
            choices,
            choices.Length == 0);
    }

    public InteractiveEventChoiceResultPresentation BuildNightChoiceResult(InteractiveEventChoiceData choice)
    {
        if (choice == null)
        {
            return new InteractiveEventChoiceResultPresentation(Array.Empty<DialogueLinePresentation>(), string.Empty);
        }

        return new InteractiveEventChoiceResultPresentation(
            BuildDialogueLines(choice.ResponseDialogueLines, choice.ResponseLine),
            string.Empty);
    }

    private PoCTextPageData ResolveReportPage(PoCReportData report)
    {
        if (report == null)
        {
            return null;
        }

        PoCReportRuleData rule = report.Rules?
            .Where(candidate => candidate != null && candidate.Matches(_state))
            .OrderByDescending(candidate => candidate.Priority)
            .FirstOrDefault(candidate => candidate.Page != null && candidate.Page.HasContent);

        return rule != null ? rule.Page : report.FallbackPage;
    }

    private static DialogueLinePresentation[] BuildDialogueLines(
        IReadOnlyList<DialogueLineData> dialogueLines,
        string fallbackLine)
    {
        DialogueLinePresentation[] lines = dialogueLines?
            .Where(line => line != null && !string.IsNullOrWhiteSpace(line.Text))
            .Select(line => new DialogueLinePresentation(ResolveSpeakerName(line.Speaker), line.Text))
            .ToArray()
            ?? Array.Empty<DialogueLinePresentation>();

        if (lines.Length > 0)
        {
            return lines;
        }

        return string.IsNullOrWhiteSpace(fallbackLine)
            ? Array.Empty<DialogueLinePresentation>()
            : new[] { new DialogueLinePresentation(string.Empty, fallbackLine) };
    }

    private static string ResolveSpeakerName(SO_DialogueSpeakerDefinition speaker)
    {
        if (speaker == null || string.Equals(speaker.Id, NarratorSpeakerId, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(speaker.DisplayName))
        {
            return speaker.DisplayName;
        }

        return !string.IsNullOrWhiteSpace(speaker.name) ? speaker.name : string.Empty;
    }

    private static string BuildStatChangeSummary(IReadOnlyList<PoCStatChangeData> changes)
    {
        string[] parts = changes?
            .Where(change => change != null && change.Delta != 0)
            .Select(change => $"{GetStatLabel(change.StatType)} {(change.Delta > 0 ? "+" : string.Empty)}{change.Delta}")
            .ToArray()
            ?? Array.Empty<string>();

        return parts.Length == 0 ? string.Empty : string.Join("  ", parts);
    }

    private static string GetStatLabel(CharacterStatType statType)
    {
        return statType switch
        {
            CharacterStatType.Stability => "안정도",
            CharacterStatType.Autonomy => "자율성",
            CharacterStatType.Trust => "신뢰",
            CharacterStatType.ControlAwareness => "통제 인식",
            CharacterStatType.InstitutionEvaluation => "기관 평가",
            _ => statType.ToString()
        };
    }
}

public readonly struct PoCTextPresentation
{
    public static PoCTextPresentation Empty => new(string.Empty, string.Empty);

    public PoCTextPresentation(string title, string body)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
    }

    public string Title { get; }
    public string Body { get; }
}

public readonly struct PoCMainChoicePresentation
{
    public PoCMainChoicePresentation(
        string title,
        string body,
        IReadOnlyList<PoCMainChoiceOptionPresentation> options)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        Options = options ?? Array.Empty<PoCMainChoiceOptionPresentation>();
    }

    public string Title { get; }
    public string Body { get; }
    public IReadOnlyList<PoCMainChoiceOptionPresentation> Options { get; }
}

public readonly struct PoCMainChoiceOptionPresentation
{
    public PoCMainChoiceOptionPresentation(
        string id,
        string label,
        string description,
        string effectSummary)
    {
        Id = id ?? string.Empty;
        Label = label ?? string.Empty;
        Description = description ?? string.Empty;
        EffectSummary = effectSummary ?? string.Empty;
    }

    public string Id { get; }
    public string Label { get; }
    public string Description { get; }
    public string EffectSummary { get; }
}

public readonly struct PoCDayEventPresentation
{
    public static PoCDayEventPresentation Empty => new(string.Empty, string.Empty, string.Empty);

    public PoCDayEventPresentation(string title, string body, string effectSummary)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        EffectSummary = effectSummary ?? string.Empty;
    }

    public string Title { get; }
    public string Body { get; }
    public string EffectSummary { get; }
}

public readonly struct PoCStatPresentation
{
    public PoCStatPresentation(CharacterStatType statType, string label, int value)
    {
        StatType = statType;
        Label = label ?? string.Empty;
        Value = value;
    }

    public CharacterStatType StatType { get; }
    public string Label { get; }
    public int Value { get; }
}
