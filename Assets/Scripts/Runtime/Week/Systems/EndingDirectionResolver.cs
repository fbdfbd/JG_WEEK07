using UnityEngine;

public static class EndingDirectionResolver
{
    private static readonly EChildStatusType[] CandidateStats =
    {
        EChildStatusType.Trust,
        EChildStatusType.Curiosity,
        EChildStatusType.Anxiety,
        EChildStatusType.Obedience
    };

    public static EEndingDirectionType Resolve(RuntimeChildState childState)
    {
        EChildStatusType dominantStat = EChildStatusType.Anxiety;
        int dominantValue = childState != null
            ? childState.GetStat(EChildStatusType.Anxiety)
            : RuntimeChildState.DefaultStatValue;

        if (childState != null)
        {
            for (int i = 0; i < CandidateStats.Length; i++)
            {
                EChildStatusType statType = CandidateStats[i];
                int value = childState.GetStat(statType);
                if (Mathf.Abs(value) > Mathf.Abs(dominantValue))
                {
                    dominantStat = statType;
                    dominantValue = value;
                }
            }
        }

        return ToDirection(dominantStat, dominantValue);
    }

    public static EEndingDirectionType Resolve(
        int trust,
        int curiosity,
        int anxiety,
        int obedience)
    {
        EChildStatusType dominantStat = EChildStatusType.Trust;
        int dominantValue = trust;

        ResolveDominantStat(EChildStatusType.Curiosity, curiosity, ref dominantStat, ref dominantValue);
        ResolveDominantStat(EChildStatusType.Anxiety, anxiety, ref dominantStat, ref dominantValue);
        ResolveDominantStat(EChildStatusType.Obedience, obedience, ref dominantStat, ref dominantValue);

        return ToDirection(dominantStat, dominantValue);
    }

    private static void ResolveDominantStat(
        EChildStatusType statType,
        int value,
        ref EChildStatusType dominantStat,
        ref int dominantValue)
    {
        if (Mathf.Abs(value) > Mathf.Abs(dominantValue))
        {
            dominantStat = statType;
            dominantValue = value;
        }
    }

    private static EEndingDirectionType ToDirection(EChildStatusType statType, int value)
    {
        bool isPositive = value >= 0;

        return statType switch
        {
            EChildStatusType.Trust => isPositive ? EEndingDirectionType.Innocent : EEndingDirectionType.Clever,
            EChildStatusType.Curiosity => isPositive ? EEndingDirectionType.Curious : EEndingDirectionType.Cautious,
            EChildStatusType.Anxiety => isPositive ? EEndingDirectionType.Anxious : EEndingDirectionType.Stable,
            EChildStatusType.Obedience => isPositive ? EEndingDirectionType.Compliant : EEndingDirectionType.Defiant,
            _ => EEndingDirectionType.Anxious
        };
    }
}
