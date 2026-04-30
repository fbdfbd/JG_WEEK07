using System;
using System.Collections.Generic;

public static class WeeklyStatResultResolver
{
    private static readonly EChildStatusType[] VisibleStatTypes =
    {
        EChildStatusType.Trust,
        EChildStatusType.Curiosity,
        EChildStatusType.Anxiety,
        EChildStatusType.Obedience,
    };

    public static WeeklyStatResultPresentation Resolve(
        RuntimeChildState childState,
        IReadOnlyDictionary<EChildStatusType, int> beforeStats,
        WeekUiTextProvider textProvider)
    {
        if (childState == null || beforeStats == null)
        {
            return new WeeklyStatResultPresentation(Array.Empty<WeeklyStatChangePresentation>());
        }

        List<WeeklyStatChangePresentation> changes = new();
        for (int index = 0; index < VisibleStatTypes.Length; index++)
        {
            EChildStatusType statType = VisibleStatTypes[index];
            int beforeValue = beforeStats.TryGetValue(statType, out int value)
                ? value
                : RuntimeChildState.DefaultStatValue;
            int afterValue = childState.GetStat(statType);

            changes.Add(new WeeklyStatChangePresentation(
                statType,
                GetStatLabel(statType, textProvider),
                GetLeftLabel(statType),
                GetRightLabel(statType),
                beforeValue,
                afterValue,
                RuntimeChildState.MinStatValue,
                RuntimeChildState.MaxStatValue,
                ECharacterStatusBarRenderMode.Bipolar));
        }

        return new WeeklyStatResultPresentation(changes);
    }

    private static string GetStatLabel(EChildStatusType statType, WeekUiTextProvider textProvider)
    {
        return textProvider != null ? textProvider.GetStatLabel(statType) : statType.ToString();
    }

    private static string GetLeftLabel(EChildStatusType statType)
    {
        return statType switch
        {
            EChildStatusType.Trust => "순진",
            EChildStatusType.Curiosity => "호기심",
            EChildStatusType.Anxiety => "순응",
            EChildStatusType.Obedience => "안정",
            _ => string.Empty,
        };
    }

    private static string GetRightLabel(EChildStatusType statType)
    {
        return statType switch
        {
            EChildStatusType.Trust => "영민",
            EChildStatusType.Curiosity => "신중",
            EChildStatusType.Anxiety => "반항",
            EChildStatusType.Obedience => "불안",
            _ => string.Empty,
        };
    }
}
