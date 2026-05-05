public readonly struct EndingContext
{
    public EndingContext(
        EEndingCharacterType characterType,
        int meetCount,
        EEndingDirectionType directionType,
        int affinity)
    {
        CharacterType = characterType;
        MeetCount = meetCount;
        DirectionType = directionType;
        Affinity = affinity;
    }

    public EEndingCharacterType CharacterType { get; }
    public int MeetCount { get; }
    public EEndingDirectionType DirectionType { get; }
    public int Affinity { get; }
}

