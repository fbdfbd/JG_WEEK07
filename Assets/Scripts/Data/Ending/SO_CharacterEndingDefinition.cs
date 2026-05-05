using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterEnding_",
    menuName = "Scriptable Objects/Ending/CharacterEnding")]
public class SO_CharacterEndingDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private int _priority;
    [SerializeField] private bool _isNoCharacterEnding;
    [SerializeField] private EEndingCharacterType _characterType;
    [SerializeField] private int _minMeetCount;
    [SerializeField] private int _maxMeetCount;
    [SerializeField] private EEndingDirectionType _directionType;
    [SerializeField] private EndingTextData _text;

    public string Id => _id;
    public int Priority => _priority;
    public bool IsNoCharacterEnding => _isNoCharacterEnding;
    public EEndingCharacterType CharacterType => _characterType;
    public int MinMeetCount => _minMeetCount;
    public int MaxMeetCount => _maxMeetCount;
    public EEndingDirectionType DirectionType => _directionType;
    public EndingTextData Text => _text;

    public bool Matches(EndingContext context)
    {
        return !_isNoCharacterEnding
            && _characterType == context.CharacterType
            && _directionType == context.DirectionType
            && context.MeetCount >= _minMeetCount
            && context.MeetCount <= EffectiveMaxMeetCount;
    }

    public bool MatchesNoCharacter(EEndingDirectionType directionType)
    {
        return _isNoCharacterEnding
            && _directionType == directionType;
    }

    public bool MatchesCharacter(EEndingCharacterType characterType)
    {
        return !_isNoCharacterEnding
            && _characterType == characterType;
    }

    public bool MatchesDirection(EEndingDirectionType directionType)
    {
        return _directionType == directionType;
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

