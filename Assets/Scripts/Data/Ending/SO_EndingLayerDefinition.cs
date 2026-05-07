using UnityEngine;

[CreateAssetMenu(
    fileName = "EndingLayer_",
    menuName = "Scriptable Objects/Ending/EndingLayer")]
public class SO_EndingLayerDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private int _priority;
    [SerializeField] private EEndingLayerCategory _category;
    [SerializeField] private EEndingDirectionType _directionType;
    [SerializeField] private EEndingCharacterType _characterType;
    [SerializeField] private int _minMeetCount;
    [SerializeField] private int _maxMeetCount = -1;
    [SerializeField] private int _minAffinity = int.MinValue;
    [SerializeField] private int _maxAffinity = int.MaxValue;
    [SerializeField] private EndingTextData _text;

    public string Id => _id;
    public int Priority => _priority;
    public EEndingLayerCategory Category => _category;
    public EEndingDirectionType DirectionType => _directionType;
    public EEndingCharacterType CharacterType => _characterType;
    public int MinMeetCount => _minMeetCount;
    public int MaxMeetCount => _maxMeetCount;
    public EndingTextData Text => _text;

    public bool MatchesDirection(EEndingDirectionType directionType)
    {
        return _category == EEndingLayerCategory.DirectionBase
            && _directionType == directionType;
    }

    public bool MatchesCharacterMeet(EndingContext context)
    {
        return _category == EEndingLayerCategory.CharacterMeetLayer
            && _characterType == context.CharacterType
            && context.MeetCount >= _minMeetCount
            && context.MeetCount <= EffectiveMaxMeetCount;
    }

    public bool MatchesAffinity(int affinity)
    {
        return _category == EEndingLayerCategory.AffinityLayer
            && affinity >= _minAffinity
            && affinity <= _maxAffinity;
    }

    public bool MatchesCharacter(EEndingCharacterType characterType)
    {
        return _category == EEndingLayerCategory.CharacterMeetLayer
            && _characterType == characterType;
    }

    public int GetMeetCountDistance(int meetCount)
    {
        if (meetCount < _minMeetCount)
        {
            return _minMeetCount - meetCount;
        }

        int maxMeetCount = EffectiveMaxMeetCount;
        if (meetCount > maxMeetCount)
        {
            return meetCount - maxMeetCount;
        }

        return 0;
    }

    private int EffectiveMaxMeetCount => _maxMeetCount < 0 ? int.MaxValue : _maxMeetCount;
}
