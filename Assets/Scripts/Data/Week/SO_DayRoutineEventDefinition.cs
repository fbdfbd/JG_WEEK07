using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DayRoutineEvent_",
    menuName = "Scriptable Objects/Week/DayRoutineEventDefinition")]
public class SO_DayRoutineEventDefinition : SO_InteractiveEventDefinition
{
    [SerializeField] private SO_CardInfoTypeDefinition[] _relatedInformationTypes = Array.Empty<SO_CardInfoTypeDefinition>();
    [SerializeField] private ECardOptionSemantic[] _preferredSemantics = Array.Empty<ECardOptionSemantic>();
    [SerializeField] private SO_CardInfoDefinition _linkedCard;
    [SerializeField] private EventSelectionRuleData _selectionRule = new();

    public SO_CardInfoTypeDefinition[] RelatedInformationTypes => _relatedInformationTypes;
    public ECardOptionSemantic[] PreferredSemantics => _preferredSemantics;
    public SO_CardInfoDefinition LinkedCard => _linkedCard;
    public EventSelectionRuleData SelectionRule => _selectionRule;
}

[Serializable]
public class EventSelectionRuleData
{
    [SerializeField] private EEventSelectionMode _mode = EEventSelectionMode.None;
    [SerializeField] private EChildStatusType _selectorStat;
    [SerializeField] private int _threshold;
    [SerializeField] private bool _isDefault;

    public EEventSelectionMode Mode => _mode;
    public EChildStatusType SelectorStat => _selectorStat;
    public int Threshold => _threshold;
    public bool IsDefault => _isDefault;
}

public enum EEventSelectionMode
{
    None,
    MaxPositive,
    MaxNegative,
    MaxAny,
    MinAny,
    Default,
}
