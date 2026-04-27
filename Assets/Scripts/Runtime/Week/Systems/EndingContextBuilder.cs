public static class EndingContextBuilder
{
    private const int PositiveEndingThreshold = 5;
    private const int NegativeEndingThreshold = -5;

    private static readonly EEndingCharacterType[] CharacterTiePriority =
    {
        EEndingCharacterType.Max,
        EEndingCharacterType.Rian,
        EEndingCharacterType.Yuffie,
        EEndingCharacterType.Millia,
    };

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

    public static EndingContext[] BuildMetCharacterContextsByPriority(RuntimeChildState childState)
    {
        if (childState == null)
        {
            return System.Array.Empty<EndingContext>();
        }

        EndingContext[] contexts = new EndingContext[CharacterTiePriority.Length];
        int count = 0;
        for (int i = 0; i < CharacterTiePriority.Length; i++)
        {
            EEndingCharacterType characterType = CharacterTiePriority[i];
            int meetCount = GetMeetCount(childState, characterType);
            if (meetCount <= 0)
            {
                continue;
            }

            contexts[count] = Build(childState, characterType);
            count++;
        }

        if (count == contexts.Length)
        {
            return contexts;
        }

        EndingContext[] trimmedContexts = new EndingContext[count];
        System.Array.Copy(contexts, trimmedContexts, count);
        return trimmedContexts;
    }

    private static EndingContext Build(RuntimeChildState childState, EEndingCharacterType characterType)
    {
        int meetCount = GetMeetCount(childState, characterType);
        EEndingMoodType moodType = ResolveMoodType(childState, characterType);
        int affinity = childState.GetStat(EChildStatusType.Affinity);

        return new EndingContext(characterType, meetCount, moodType, affinity);
    }

    private static EEndingCharacterType ResolveCharacterType(RuntimeChildState childState)
    {
        int highest = GetHighestMeetCount(childState);

        for (int i = 0; i < CharacterTiePriority.Length; i++)
        {
            EEndingCharacterType candidate = CharacterTiePriority[i];
            if (GetMeetCount(childState, candidate) == highest
                && GetMoodDecisionMagnitude(childState, candidate) > 0)
            {
                return candidate;
            }
        }

        for (int i = 0; i < CharacterTiePriority.Length; i++)
        {
            EEndingCharacterType candidate = CharacterTiePriority[i];
            if (GetMeetCount(childState, candidate) == highest)
            {
                return candidate;
            }
        }

        return EEndingCharacterType.Rian;
    }

    private static int GetHighestMeetCount(RuntimeChildState childState)
    {
        int highest = 0;
        for (int i = 0; i < CharacterTiePriority.Length; i++)
        {
            int value = GetMeetCount(childState, CharacterTiePriority[i]);
            if (value > highest)
            {
                highest = value;
            }
        }

        return highest;
    }

    private static int GetMoodDecisionMagnitude(
        RuntimeChildState childState,
        EEndingCharacterType characterType)
    {
        return System.Math.Abs(GetMoodDecisionValue(childState, characterType));
    }

    private static int GetMoodDecisionValue(
        RuntimeChildState childState,
        EEndingCharacterType characterType)
    {
        if (childState == null)
        {
            return RuntimeChildState.DefaultStatValue;
        }

        return characterType switch
        {
            EEndingCharacterType.Rian => childState.GetStat(EChildStatusType.Anxiety),
            EEndingCharacterType.Max => childState.GetStat(EChildStatusType.Obedience),
            EEndingCharacterType.Millia => childState.GetStat(EChildStatusType.Curiosity),
            EEndingCharacterType.Yuffie => childState.GetStat(EChildStatusType.Trust),
            _ => RuntimeChildState.DefaultStatValue,
        };
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
        return childState.GetStat(EChildStatusType.Anxiety) <= NegativeEndingThreshold
            ? EEndingMoodType.Stability
            : EEndingMoodType.Anxiety;
    }

    private static EEndingMoodType ResolveMaxMood(RuntimeChildState childState)
    {
        return childState.GetStat(EChildStatusType.Obedience) <= NegativeEndingThreshold
            ? EEndingMoodType.Rebellion
            : EEndingMoodType.Submission;
    }

    private static EEndingMoodType ResolveMilliaMood(RuntimeChildState childState)
    {
        return childState.GetStat(EChildStatusType.Curiosity) <= NegativeEndingThreshold
            ? EEndingMoodType.Cautious
            : EEndingMoodType.Curious;
    }

    private static EEndingMoodType ResolveYuffieMood(RuntimeChildState childState)
    {
        return childState.GetStat(EChildStatusType.Trust) <= NegativeEndingThreshold
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

