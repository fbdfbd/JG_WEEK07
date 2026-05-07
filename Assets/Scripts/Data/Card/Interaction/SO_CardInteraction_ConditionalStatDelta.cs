using UnityEngine;

[CreateAssetMenu(
    fileName = "Interaction_ConditionalStatDelta",
    menuName = "Scriptable Objects/Card/Interaction/ConditionalStatDelta")]
public class SO_CardInteraction_ConditionalStatDelta : SO_CardInteractionDefinition
{
    [SerializeField] private EChildStatusType _conditionStat;
    [SerializeField] private int _minValue;

    [SerializeField] private EChildStatusType _targetStat;
    [SerializeField] private int _amount;
    [SerializeField] private string _toastMessage;

    public override void Apply(RuntimeChildState childState)
    {
        if (childState.GetStat(_conditionStat) >= _minValue)
        {
            childState.AddStat(_targetStat, _amount, _toastMessage);
        }
    }

    protected override string ResolveDisplayName()
    {
        return !string.IsNullOrWhiteSpace(SerializedDisplayName)
            ? SerializedDisplayName
            : !string.IsNullOrWhiteSpace(_toastMessage)
                ? _toastMessage
                : base.ResolveDisplayName();
    }
}
