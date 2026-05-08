using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PoCFlow_",
    menuName = "Scriptable Objects/PoC/Flow Definition")]
public sealed class SO_PoCFlowDefinition : ScriptableObject
{
    [Header("Intro")]
    [SerializeField] private PoCTextPageData[] _introPages = Array.Empty<PoCTextPageData>();

    [Header("Days")]
    [SerializeField] private PoCDayData[] _days = Array.Empty<PoCDayData>();

    public PoCTextPageData[] IntroPages => _introPages;
    public PoCDayData[] Days => _days;
}

[Serializable]
public sealed class PoCTextPageData
{
    [SerializeField] private string _title = string.Empty;
    [SerializeField, TextArea(3, 10)] private string _body = string.Empty;

    public string Title => _title;
    public string Body => _body;
    public bool HasContent => !string.IsNullOrWhiteSpace(_title) || !string.IsNullOrWhiteSpace(_body);
}

[Serializable]
public sealed class PoCDayData
{
    [SerializeField] private string _id = string.Empty;
    [SerializeField] private string _label = string.Empty;
    [SerializeField] private PoCReportData _report = new();

    [Header("Main Choice")]
    [SerializeField] private string _choiceTitle = string.Empty;
    [SerializeField, TextArea(2, 6)] private string _choiceBody = string.Empty;
    [SerializeField] private PoCMainChoiceData[] _mainChoices = Array.Empty<PoCMainChoiceData>();

    [Header("Day Event")]
    [SerializeField] private PoCDayEventData _dayEvent = new();

    [Header("Night Dialogue")]
    [SerializeField] private PoCNightDialogueRuleData[] _nightDialogueRules = Array.Empty<PoCNightDialogueRuleData>();

    public string Id => _id;
    public string Label => _label;
    public PoCReportData Report => _report;
    public string ChoiceTitle => _choiceTitle;
    public string ChoiceBody => _choiceBody;
    public PoCMainChoiceData[] MainChoices => _mainChoices;
    public PoCDayEventData DayEvent => _dayEvent;
    public PoCNightDialogueRuleData[] NightDialogueRules => _nightDialogueRules;
    public bool HasChoices => _mainChoices != null && _mainChoices.Length > 0;
    public bool HasDayEvent => _dayEvent != null && _dayEvent.HasContent;
    public bool HasNightDialogue => _nightDialogueRules != null && _nightDialogueRules.Length > 0;
}

[Serializable]
public sealed class PoCReportData
{
    [SerializeField] private PoCTextPageData _fallbackPage = new();
    [SerializeField] private PoCReportRuleData[] _rules = Array.Empty<PoCReportRuleData>();

    public PoCTextPageData FallbackPage => _fallbackPage;
    public PoCReportRuleData[] Rules => _rules;
}

[Serializable]
public sealed class PoCReportRuleData
{
    [SerializeField] private int _priority;
    [SerializeField] private string _requiredChoiceId = string.Empty;
    [SerializeField] private string[] _requiredFlags = Array.Empty<string>();
    [SerializeField] private string[] _forbiddenFlags = Array.Empty<string>();
    [SerializeField] private PoCStatRequirementData[] _statRequirements = Array.Empty<PoCStatRequirementData>();
    [SerializeField] private PoCTextPageData _page = new();

    public int Priority => _priority;
    public string RequiredChoiceId => _requiredChoiceId;
    public string[] RequiredFlags => _requiredFlags;
    public string[] ForbiddenFlags => _forbiddenFlags;
    public PoCStatRequirementData[] StatRequirements => _statRequirements;
    public PoCTextPageData Page => _page;

    public bool Matches(PoCRuntimeState state)
    {
        if (state == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_requiredChoiceId) &&
            !state.HasSelectedChoice(_requiredChoiceId))
        {
            return false;
        }

        if (!PoCRuleMatchUtility.MatchesFlags(state, _requiredFlags, _forbiddenFlags))
        {
            return false;
        }

        for (int index = 0; index < (_statRequirements?.Length ?? 0); index++)
        {
            PoCStatRequirementData requirement = _statRequirements[index];
            if (requirement != null && !requirement.Matches(state))
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public sealed class PoCMainChoiceData
{
    [SerializeField] private string _id = string.Empty;
    [SerializeField] private string _label = string.Empty;
    [SerializeField, TextArea(2, 6)] private string _description = string.Empty;
    [SerializeField] private string[] _setFlags = Array.Empty<string>();
    [SerializeField] private PoCStatChangeData[] _statChanges = Array.Empty<PoCStatChangeData>();

    public string Id => _id;
    public string Label => _label;
    public string Description => _description;
    public string[] SetFlags => _setFlags;
    public PoCStatChangeData[] StatChanges => _statChanges;
}

[Serializable]
public sealed class PoCDayEventData
{
    [SerializeField] private string _title = string.Empty;
    [SerializeField, TextArea(3, 10)] private string _body = string.Empty;
    [SerializeField] private PoCStatChangeData[] _statChanges = Array.Empty<PoCStatChangeData>();
    [SerializeField] private PoCDayEventRuleData[] _rules = Array.Empty<PoCDayEventRuleData>();

    public string Title => _title;
    public string Body => _body;
    public PoCStatChangeData[] StatChanges => _statChanges;
    public PoCDayEventRuleData[] Rules => _rules;
    public bool HasContent => !string.IsNullOrWhiteSpace(_title)
        || !string.IsNullOrWhiteSpace(_body)
        || HasRuleContent;

    public PoCDayEventResolvedData Resolve(PoCRuntimeState state)
    {
        PoCDayEventRuleData rule = _rules?
            .Where(candidate => candidate != null && candidate.Matches(state))
            .OrderByDescending(candidate => candidate.Priority)
            .FirstOrDefault(candidate => candidate.Event != null && candidate.Event.HasContent);

        return rule != null
            ? rule.Event.ToResolvedData()
            : new PoCDayEventResolvedData(_title, _body, _statChanges);
    }

    private bool HasRuleContent
    {
        get
        {
            for (int index = 0; index < (_rules?.Length ?? 0); index++)
            {
                PoCDayEventRuleData rule = _rules[index];
                if (rule?.Event != null && rule.Event.HasContent)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

[Serializable]
public sealed class PoCDayEventRuleData
{
    [SerializeField] private int _priority;
    [SerializeField] private string _requiredChoiceId = string.Empty;
    [SerializeField] private string[] _requiredFlags = Array.Empty<string>();
    [SerializeField] private string[] _forbiddenFlags = Array.Empty<string>();
    [SerializeField] private PoCStatRequirementData[] _statRequirements = Array.Empty<PoCStatRequirementData>();
    [SerializeField] private PoCDayEventContentData _event = new();

    public int Priority => _priority;
    public string RequiredChoiceId => _requiredChoiceId;
    public string[] RequiredFlags => _requiredFlags;
    public string[] ForbiddenFlags => _forbiddenFlags;
    public PoCStatRequirementData[] StatRequirements => _statRequirements;
    public PoCDayEventContentData Event => _event;

    public bool Matches(PoCRuntimeState state)
    {
        if (state == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_requiredChoiceId) &&
            !state.HasSelectedChoice(_requiredChoiceId))
        {
            return false;
        }

        if (!PoCRuleMatchUtility.MatchesFlags(state, _requiredFlags, _forbiddenFlags))
        {
            return false;
        }

        for (int index = 0; index < (_statRequirements?.Length ?? 0); index++)
        {
            PoCStatRequirementData requirement = _statRequirements[index];
            if (requirement != null && !requirement.Matches(state))
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public sealed class PoCDayEventContentData
{
    [SerializeField] private string _title = string.Empty;
    [SerializeField, TextArea(3, 10)] private string _body = string.Empty;
    [SerializeField] private PoCStatChangeData[] _statChanges = Array.Empty<PoCStatChangeData>();

    public string Title => _title;
    public string Body => _body;
    public PoCStatChangeData[] StatChanges => _statChanges;
    public bool HasContent => !string.IsNullOrWhiteSpace(_title) || !string.IsNullOrWhiteSpace(_body);

    public PoCDayEventResolvedData ToResolvedData()
    {
        return new PoCDayEventResolvedData(_title, _body, _statChanges);
    }
}

public readonly struct PoCDayEventResolvedData
{
    public PoCDayEventResolvedData(
        string title,
        string body,
        IReadOnlyList<PoCStatChangeData> statChanges)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        StatChanges = statChanges ?? Array.Empty<PoCStatChangeData>();
    }

    public string Title { get; }
    public string Body { get; }
    public IReadOnlyList<PoCStatChangeData> StatChanges { get; }
    public bool HasContent => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Body);
}

[Serializable]
public sealed class PoCNightDialogueRuleData
{
    [SerializeField] private SO_InteractiveEventDefinition _dialogue;
    [SerializeField] private int _priority;
    [SerializeField] private string _requiredChoiceId = string.Empty;
    [SerializeField] private string[] _requiredFlags = Array.Empty<string>();
    [SerializeField] private string[] _forbiddenFlags = Array.Empty<string>();
    [SerializeField] private PoCStatRequirementData[] _statRequirements = Array.Empty<PoCStatRequirementData>();

    public SO_InteractiveEventDefinition Dialogue => _dialogue;
    public int Priority => _priority;
    public string RequiredChoiceId => _requiredChoiceId;
    public string[] RequiredFlags => _requiredFlags;
    public string[] ForbiddenFlags => _forbiddenFlags;
    public PoCStatRequirementData[] StatRequirements => _statRequirements;

    public bool Matches(PoCRuntimeState state)
    {
        if (_dialogue == null || state == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_requiredChoiceId) &&
            !state.HasSelectedChoice(_requiredChoiceId))
        {
            return false;
        }

        if (!PoCRuleMatchUtility.MatchesFlags(state, _requiredFlags, _forbiddenFlags))
        {
            return false;
        }

        for (int index = 0; index < (_statRequirements?.Length ?? 0); index++)
        {
            PoCStatRequirementData requirement = _statRequirements[index];
            if (requirement != null && !requirement.Matches(state))
            {
                return false;
            }
        }

        return true;
    }
}

public static class PoCRuleMatchUtility
{
    public static bool MatchesFlags(
        PoCRuntimeState state,
        IReadOnlyList<string> requiredFlags,
        IReadOnlyList<string> forbiddenFlags)
    {
        if (state == null)
        {
            return false;
        }

        for (int index = 0; index < (requiredFlags?.Count ?? 0); index++)
        {
            string flag = requiredFlags[index];
            if (!string.IsNullOrWhiteSpace(flag) && !state.HasFlag(flag))
            {
                return false;
            }
        }

        for (int index = 0; index < (forbiddenFlags?.Count ?? 0); index++)
        {
            string flag = forbiddenFlags[index];
            if (!string.IsNullOrWhiteSpace(flag) && state.HasFlag(flag))
            {
                return false;
            }
        }

        return true;
    }
}

[Serializable]
public sealed class PoCStatChangeData
{
    [SerializeField] private CharacterStatType _statType;
    [SerializeField] private int _delta;

    public CharacterStatType StatType => _statType;
    public int Delta => _delta;
}

[Serializable]
public sealed class PoCStatRequirementData
{
    [SerializeField] private CharacterStatType _statType;
    [SerializeField] private bool _useMinimum = true;
    [SerializeField] private int _minimumValue;
    [SerializeField] private bool _useMaximum;
    [SerializeField] private int _maximumValue;

    public CharacterStatType StatType => _statType;
    public bool UseMinimum => _useMinimum;
    public int MinimumValue => _minimumValue;
    public bool UseMaximum => _useMaximum;
    public int MaximumValue => _maximumValue;

    public bool Matches(PoCRuntimeState state)
    {
        if (state == null)
        {
            return false;
        }

        int value = state.GetStat(_statType);
        bool meetsMinimum = !_useMinimum || value >= _minimumValue;
        bool meetsMaximum = !_useMaximum || value <= _maximumValue;
        return meetsMinimum && meetsMaximum;
    }
}
