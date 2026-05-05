public static class EndingCharacterResolver
{
    public static readonly EEndingCharacterType[] TiePriority =
    {
        EEndingCharacterType.Max,
        EEndingCharacterType.Rian,
        EEndingCharacterType.Yuffie,
        EEndingCharacterType.Millia,
    };

    public static bool HasAnyMet(RuntimeChildState childState)
    {
        if (childState == null)
        {
            return false;
        }

        for (int i = 0; i < TiePriority.Length; i++)
        {
            if (GetMeetCount(childState, TiePriority[i]) > 0)
            {
                return true;
            }
        }

        return false;
    }

    public static EEndingCharacterType Resolve(RuntimeChildState childState)
    {
        int highest = 0;
        EEndingCharacterType selected = TiePriority[0];

        for (int i = 0; i < TiePriority.Length; i++)
        {
            EEndingCharacterType candidate = TiePriority[i];
            int value = GetMeetCount(childState, candidate);
            if (value > highest)
            {
                highest = value;
                selected = candidate;
            }
        }

        return selected;
    }

    public static int GetMeetCount(RuntimeChildState childState, EEndingCharacterType characterType)
    {
        if (childState == null)
        {
            return 0;
        }

        return childState.GetStat(ToStatType(characterType));
    }

    private static EChildStatusType ToStatType(EEndingCharacterType characterType)
    {
        return characterType switch
        {
            EEndingCharacterType.Rian => EChildStatusType.Rian,
            EEndingCharacterType.Max => EChildStatusType.Max,
            EEndingCharacterType.Millia => EChildStatusType.Millia,
            EEndingCharacterType.Yuffie => EChildStatusType.Yuffie,
            _ => EChildStatusType.Max,
        };
    }
}
