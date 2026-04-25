using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterEnding_",
    menuName = "Scriptable Objects/Ending/CharacterEnding")]
public class SO_CharacterEndingDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private EEndingCharacterType _characterType;
    [SerializeField] private int _minMeetCount;
    [SerializeField] private int _maxMeetCount;
    [SerializeField] private EEndingMoodType _moodType;
    [SerializeField] private EndingTextData _text;

    public string Id => _id;
    public EEndingCharacterType CharacterType => _characterType;
    public int MinMeetCount => _minMeetCount;
    public int MaxMeetCount => _maxMeetCount;
    public EEndingMoodType MoodType => _moodType;
    public EndingTextData Text => _text;

    public bool Matches(EndingContext context)
    {
        return _characterType == context.CharacterType
            && _moodType == context.MoodType
            && context.MeetCount >= _minMeetCount
            && context.MeetCount <= _maxMeetCount;
    }
}

