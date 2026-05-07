using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EventCutsceneRuleCatalog_",
    menuName = "Scriptable Objects/Week/EventCutsceneRuleCatalog")]
public class SO_EventCutsceneRuleCatalog : ScriptableObject
{
    [SerializeField] private EventCutsceneRuleData[] _rules = Array.Empty<EventCutsceneRuleData>();

    public EventCutsceneRuleData[] Rules => _rules;
}

[Serializable]
public class EventCutsceneRuleData
{
    [SerializeField] private string _ruleId = string.Empty;
    [SerializeField] private bool _enabled = true;
    [SerializeField] private string _weekId = string.Empty;
    [SerializeField] private string _eventId = string.Empty;
    [SerializeField] private EWeekFlowCutsceneMoment _moment = EWeekFlowCutsceneMoment.EventEnter;
    [SerializeField] private string _sequenceId = string.Empty;
    [SerializeField] private string _specialPlayerId = string.Empty;

    public string RuleId => _ruleId;
    public bool Enabled => _enabled;
    public string WeekId => _weekId;
    public string EventId => _eventId;
    public EWeekFlowCutsceneMoment Moment => _moment;
    public string SequenceId => _sequenceId;
    public string SpecialPlayerId => _specialPlayerId;

    public bool UsesSequence => !string.IsNullOrWhiteSpace(_sequenceId);
    public bool UsesSpecialPlayer => !string.IsNullOrWhiteSpace(_specialPlayerId);
}
