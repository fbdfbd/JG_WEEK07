public static class EndingContextBuilder
{
    public static bool HasNoCharacterMet(RuntimeChildState childState)
    {
        return !EndingCharacterResolver.HasAnyMet(childState);
    }

    public static EndingContext Build(RuntimeChildState childState)
    {
        EEndingCharacterType characterType = EndingCharacterResolver.Resolve(childState);
        return Build(childState, characterType);
    }

    public static EndingContext[] BuildMetCharacterContextsByPriority(RuntimeChildState childState)
    {
        if (childState == null)
        {
            return System.Array.Empty<EndingContext>();
        }

        EndingContext[] contexts = new EndingContext[EndingCharacterResolver.TiePriority.Length];
        int count = 0;
        for (int i = 0; i < EndingCharacterResolver.TiePriority.Length; i++)
        {
            EEndingCharacterType characterType = EndingCharacterResolver.TiePriority[i];
            int meetCount = EndingCharacterResolver.GetMeetCount(childState, characterType);
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

    public static EndingContext BuildForCharacter(
        RuntimeChildState childState,
        EEndingCharacterType characterType)
    {
        return Build(childState, characterType);
    }

    public static EEndingMoodType BuildLegacyMoodType(
        RuntimeChildState childState,
        EEndingCharacterType characterType)
    {
        return ResolveMoodType(childState, characterType);
    }

    private static EndingContext Build(
        RuntimeChildState childState,
        EEndingCharacterType characterType)
    {
        int meetCount = EndingCharacterResolver.GetMeetCount(childState, characterType);
        EEndingDirectionType directionType = EndingDirectionResolver.Resolve(childState);
        int affinity = childState != null ? childState.GetStat(EChildStatusType.Affinity) : 0;

        return new EndingContext(characterType, meetCount, directionType, affinity);
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

    private static EEndingMoodType ResolveMoodType(RuntimeChildState childState, EEndingCharacterType characterType)
    {
        return characterType switch
        {
            EEndingCharacterType.Rian => ResolveSignedMood(
                childState,
                characterType,
                EEndingMoodType.Anxiety,
                EEndingMoodType.Stability),
            EEndingCharacterType.Max => ResolveSignedMood(
                childState,
                characterType,
                EEndingMoodType.Submission,
                EEndingMoodType.Rebellion),
            EEndingCharacterType.Millia => ResolveSignedMood(
                childState,
                characterType,
                EEndingMoodType.Curious,
                EEndingMoodType.Cautious),
            EEndingCharacterType.Yuffie => ResolveSignedMood(
                childState,
                characterType,
                EEndingMoodType.Submission,
                EEndingMoodType.Caution),
            _ => EEndingMoodType.Anxiety,
        };
    }

    private static EEndingMoodType ResolveSignedMood(
        RuntimeChildState childState,
        EEndingCharacterType characterType,
        EEndingMoodType zeroOrPositiveMood,
        EEndingMoodType negativeMood)
    {
        return GetMoodDecisionValue(childState, characterType) < 0
            ? negativeMood
            : zeroOrPositiveMood;
    }

}

