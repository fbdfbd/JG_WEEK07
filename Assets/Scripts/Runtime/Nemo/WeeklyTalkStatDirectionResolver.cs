using UnityEngine;

public static class WeeklyTalkStatDirectionResolver
{
    private static readonly EChildStatusType[] CandidateStats =
    {
        EChildStatusType.Trust,
        EChildStatusType.Curiosity,
        EChildStatusType.Anxiety,
        EChildStatusType.Obedience
    };

    public static WeeklyTalkStatDirection Resolve(RuntimeChildState childState)
    {
        EChildStatusType dominantStat = EChildStatusType.Anxiety;
        int dominantValue = childState != null
            ? childState.GetStat(EChildStatusType.Anxiety)
            : RuntimeChildState.DefaultStatValue;

        if (childState != null)
        {
            foreach (EChildStatusType statType in CandidateStats)
            {
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

    private static WeeklyTalkStatDirection ToDirection(EChildStatusType statType, int value)
    {
        bool isPositive = value >= 0;

        return statType switch
        {
            EChildStatusType.Trust => isPositive ? WeeklyTalkStatDirection.Innocent : WeeklyTalkStatDirection.Clever,
            EChildStatusType.Curiosity => isPositive ? WeeklyTalkStatDirection.Curious : WeeklyTalkStatDirection.Cautious,
            EChildStatusType.Anxiety => isPositive ? WeeklyTalkStatDirection.Anxious : WeeklyTalkStatDirection.Stable,
            EChildStatusType.Obedience => isPositive ? WeeklyTalkStatDirection.Compliant : WeeklyTalkStatDirection.Defiant,
            _ => WeeklyTalkStatDirection.Anxious
        };
    }
}
