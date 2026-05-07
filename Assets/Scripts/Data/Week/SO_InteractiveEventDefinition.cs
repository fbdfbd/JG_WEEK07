using System;
using UnityEngine;

public abstract class SO_InteractiveEventDefinition : ScriptableObject
{
    [SerializeField] private string _id = string.Empty;
    [SerializeField] private string _title = string.Empty;
    [SerializeField] private int _priority;
    [SerializeField] private WeekEventConditionData _conditions = new();
    [SerializeField] private SO_InteractiveEventStepDefinition _firstStep;
    [SerializeField] private SO_EventResultDefinition _result;
    [SerializeField] private SO_CardInteractionDefinition[] _onCompletedInteractions = Array.Empty<SO_CardInteractionDefinition>();
    [SerializeField] private SO_WeekFlowCinematicProfile _cinematicProfile;

    public string Id => _id;
    public string Title => _title;
    public int Priority => _priority;
    public WeekEventConditionData Conditions => _conditions;
    public SO_InteractiveEventStepDefinition FirstStep => _firstStep;
    public SO_EventResultDefinition Result => _result;
    public SO_CardInteractionDefinition[] OnCompletedInteractions => _onCompletedInteractions;
    public SO_WeekFlowCinematicProfile CinematicProfile => _cinematicProfile;
}

[Serializable]
public class WeekEventConditionData
{
    [SerializeField] private SO_FlagDefinition[] _requiredFlags = Array.Empty<SO_FlagDefinition>();
    [SerializeField] private SO_FlagDefinition[] _blockedFlags = Array.Empty<SO_FlagDefinition>();
    [SerializeField] private WeekStatRequirementData[] _statRequirements = Array.Empty<WeekStatRequirementData>();
    [SerializeField] private InformationTypeConditionData[] _informationRequirements = Array.Empty<InformationTypeConditionData>();

    public SO_FlagDefinition[] RequiredFlags => _requiredFlags;
    public SO_FlagDefinition[] BlockedFlags => _blockedFlags;
    public WeekStatRequirementData[] StatRequirements => _statRequirements;
    public InformationTypeConditionData[] InformationRequirements => _informationRequirements;
}

[Serializable]
public class WeekStatRequirementData
{
    [SerializeField] private EChildStatusType _statType;
    [SerializeField] private bool _useMinimum = true;
    [SerializeField] private int _minimumValue = RuntimeChildState.DefaultStatValue;
    [SerializeField] private bool _useMaximum;
    [SerializeField] private int _maximumValue = RuntimeChildState.MaxStatValue;

    public EChildStatusType StatType => _statType;
    public bool UseMinimum => _useMinimum;
    public int MinimumValue => _minimumValue;
    public bool UseMaximum => _useMaximum;
    public int MaximumValue => _maximumValue;
}

[Serializable]
public class InformationTypeConditionData
{
    [SerializeField] private SO_CardInfoTypeDefinition _informationType;
    [SerializeField] private bool _useSemanticFilter;
    [SerializeField] private ECardOptionSemantic _semantic;
    [SerializeField] private int _minimumCount = 1;

    public SO_CardInfoTypeDefinition InformationType => _informationType;
    public bool UseSemanticFilter => _useSemanticFilter;
    public ECardOptionSemantic Semantic => _semantic;
    public int MinimumCount => _minimumCount;
}
