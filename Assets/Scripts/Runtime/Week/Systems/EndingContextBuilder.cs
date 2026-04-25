public static class EndingContextBuilder
{
    public static bool HasNoCharacterMet(RuntimeChildState childState)
    {
        return GetMeetCount(childState, EEndingCharacterType.Rian) <= 0
            && GetMeetCount(childState, EEndingCharacterType.Max) <= 0
            && GetMeetCount(childState, EEndingCharacterType.Millia) <= 0
            && GetMeetCount(childState, EEndingCharacterType.Yuffie) <= 0;
    }

    public static EndingContext Build(RuntimeChildState childState)
    {
        EEndingCharacterType characterType = ResolveCharacterType(childState);
        int meetCount = GetMeetCount(childState, characterType);
        EEndingMoodType moodType = ResolveMoodType(childState, characterType);
        int affinity = childState.GetStat(EChildStatusType.Affinity);

        return new EndingContext(characterType, meetCount, moodType, affinity);
    }

    private static EEndingCharacterType ResolveCharacterType(RuntimeChildState childState)
    {
        EEndingCharacterType result = EEndingCharacterType.Rian;
        int highest = GetMeetCount(childState, result);

        ChooseIfHigher(childState, EEndingCharacterType.Max, ref result, ref highest);
        ChooseIfHigher(childState, EEndingCharacterType.Millia, ref result, ref highest);
        ChooseIfHigher(childState, EEndingCharacterType.Yuffie, ref result, ref highest);

        return result;
    }

    private static void ChooseIfHigher(
        RuntimeChildState childState,
        EEndingCharacterType candidate,
        ref EEndingCharacterType result,
        ref int highest)
    {
        int value = GetMeetCount(childState, candidate);
        if (value > highest)
        {
            highest = value;
            result = candidate;
        }
    }

    private static EEndingMoodType ResolveMoodType(
        RuntimeChildState childState,
        EEndingCharacterType characterType)
    {
        return characterType switch
        {
            EEndingCharacterType.Rian => ResolveRianMood(childState),
            EEndingCharacterType.Max => ResolveMaxMood(childState),
            EEndingCharacterType.Millia => ResolveMilliaMood(childState),
            EEndingCharacterType.Yuffie => ResolveYuffieMood(childState),
            _ => EEndingMoodType.Anxiety,
        };
    }

    private static EEndingMoodType ResolveRianMood(RuntimeChildState childState)
    {
        return childState.GetStat(EChildStatusType.Trust) > childState.GetStat(EChildStatusType.Anxiety)
            ? EEndingMoodType.Stability
            : EEndingMoodType.Anxiety;
    }

    private static EEndingMoodType ResolveMaxMood(RuntimeChildState childState)
    {
        return childState.GetStat(EChildStatusType.Trust) > childState.GetStat(EChildStatusType.Anxiety)
            ? EEndingMoodType.Rebellion
            : EEndingMoodType.Anxiety;
    }

    private static EEndingMoodType ResolveMilliaMood(RuntimeChildState childState)
    {
        return childState.GetStat(EChildStatusType.Trust) > childState.GetStat(EChildStatusType.Curiosity)
            ? EEndingMoodType.Good
            : EEndingMoodType.Bad;
    }

    private static EEndingMoodType ResolveYuffieMood(RuntimeChildState childState)
    {
        return childState.GetStat(EChildStatusType.Curiosity) > childState.GetStat(EChildStatusType.Obedience)
            ? EEndingMoodType.Caution
            : EEndingMoodType.Submission;
    }

    private static int GetMeetCount(RuntimeChildState childState, EEndingCharacterType characterType)
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
            _ => EChildStatusType.Rian,
        };
    }
}

