public readonly struct EndingContext
{
    public EndingContext(
        EEndingCharacterType characterType,
        int meetCount,
        EEndingMoodType moodType,
        int affinity)
    {
        CharacterType = characterType;
        MeetCount = meetCount;
        MoodType = moodType;
        Affinity = affinity;
    }

    public EEndingCharacterType CharacterType { get; }
    public int MeetCount { get; }
    public EEndingMoodType MoodType { get; }
    public int Affinity { get; }
}

