using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_WeeklyTalk", menuName = "Scriptable Objects/SO_WeeklyTalk")]
public class SO_WeeklyTalk : ScriptableObject
{
    [SerializeField] private string _weekId = string.Empty;
    [SerializeField] private WeeklyTalkEntryData[] _entries = System.Array.Empty<WeeklyTalkEntryData>();

    [Header("Legacy")]
    [SerializeField, TextArea(3, 4)] private string _context = string.Empty;
    [SerializeField] private NemoEmotionState _nemostate = NemoEmotionState.None;
    [SerializeField] private SO_CardInteraction_StatDelta _CardInteraction_StatDelta;

    public string WeekId => _weekId;
    public WeeklyTalkEntryData[] Entries => _entries;

    public string Context => _context;
    public NemoEmotionState NemoState => _nemostate;
    public SO_CardInteraction_StatDelta StatDelta => _CardInteraction_StatDelta;

    public WeeklyTalkEntryData CreateLegacyEntry()
    {
        return WeeklyTalkEntryData.CreateLegacy(_context, _nemostate, _CardInteraction_StatDelta);
    }
}

public enum NemoEmotionState
{
    Happy,
    Sad,
    Heart,
    Melancholy,
    None
}

public enum WeeklyTalkStatDirection
{
    Clever,
    Innocent,
    Cautious,
    Curious,
    Stable,
    Anxious,
    Defiant,
    Compliant
}

[Serializable]
public class WeeklyTalkEntryData
{
    private static readonly SO_CardInteractionDefinition[] EmptyInteractions = Array.Empty<SO_CardInteractionDefinition>();

    [SerializeField] private string _id = string.Empty;
    [SerializeField] private WeeklyTalkStatDirection _direction = WeeklyTalkStatDirection.Anxious;
    [SerializeField] private int _variantOrder;
    [SerializeField] private int _priority;
    [SerializeField] private int _weight = 1;
    [SerializeField] private float _displaySeconds = 3.5f;
    [SerializeField] private float _cooldownSeconds;
    [SerializeField] private bool _allowAuto = true;
    [SerializeField] private bool _allowClick = true;
    [SerializeField] private WeeklyTalkConditionData _conditions = new();
    [SerializeField, TextArea(3, 4)] private string _context = string.Empty;
    [SerializeField] private NemoEmotionState _nemoState = NemoEmotionState.None;
    [SerializeField] private SO_CardInteractionDefinition[] _interactions = EmptyInteractions;

    public string Id => _id;
    public WeeklyTalkStatDirection Direction => _direction;
    public int VariantOrder => _variantOrder;
    public int Priority => _priority;
    public int Weight => _weight;
    public float DisplaySeconds => _displaySeconds;
    public float CooldownSeconds => _cooldownSeconds;
    public bool AllowAuto => _allowAuto;
    public bool AllowClick => _allowClick;
    public WeeklyTalkConditionData Conditions => _conditions;
    public string Context => _context;
    public NemoEmotionState NemoState => _nemoState;
    public SO_CardInteractionDefinition[] Interactions => _interactions ?? EmptyInteractions;
    public bool HasContent => !string.IsNullOrWhiteSpace(_context) || Interactions.Length > 0 || _nemoState != NemoEmotionState.None;

    public bool IsAvailable(RuntimeChildState childState)
    {
        return _conditions == null || _conditions.IsSatisfied(childState);
    }

    public static WeeklyTalkEntryData CreateLegacy(
        string context,
        NemoEmotionState nemoState,
        SO_CardInteractionDefinition interaction)
    {
        return new WeeklyTalkEntryData
        {
            _id = "legacy",
            _context = context,
            _nemoState = nemoState,
            _interactions = interaction != null
                ? new[] { interaction }
                : EmptyInteractions,
        };
    }
}

[Serializable]
public class WeeklyTalkConditionData
{
    [SerializeField] private SO_FlagDefinition[] _requiredFlags = Array.Empty<SO_FlagDefinition>();
    [SerializeField] private SO_FlagDefinition[] _blockedFlags = Array.Empty<SO_FlagDefinition>();
    [SerializeField] private WeekStatRequirementData[] _statRequirements = Array.Empty<WeekStatRequirementData>();

    public SO_FlagDefinition[] RequiredFlags => _requiredFlags;
    public SO_FlagDefinition[] BlockedFlags => _blockedFlags;
    public WeekStatRequirementData[] StatRequirements => _statRequirements;

    public bool IsSatisfied(RuntimeChildState childState)
    {
        if (childState == null)
        {
            return false;
        }

        return HasRequiredFlags(childState) &&
               HasNoBlockedFlags(childState) &&
               MeetsStatRequirements(childState);
    }

    private bool HasRequiredFlags(RuntimeChildState childState)
    {
        return _requiredFlags == null || _requiredFlags.All(childState.HasFlag);
    }

    private bool HasNoBlockedFlags(RuntimeChildState childState)
    {
        return _blockedFlags == null || _blockedFlags.All(flag => !childState.HasFlag(flag));
    }

    private bool MeetsStatRequirements(RuntimeChildState childState)
    {
        if (_statRequirements == null)
        {
            return true;
        }

        return _statRequirements.All(requirement =>
        {
            int value = childState.GetStat(requirement.StatType);
            bool meetsMinimum = !requirement.UseMinimum || value >= requirement.MinimumValue;
            bool meetsMaximum = !requirement.UseMaximum || value <= requirement.MaximumValue;
            return meetsMinimum && meetsMaximum;
        });
    }
}
