using System;
using System.Collections.Generic;
using System.Linq;

public static class WeekFlowQueryUtility
{
    public static WeekCardEntryData[] GetCurrentWeekEntries(SO_WeekDefinition currentWeekDefinition)
    {
        return GetCurrentWeekEntries(currentWeekDefinition, null);
    }

    public static WeekCardEntryData[] GetCurrentWeekEntries(
        SO_WeekDefinition currentWeekDefinition,
        RuntimeChildState childState)
    {
        if (currentWeekDefinition?.PreTurn == null || currentWeekDefinition.PreTurn.InformationCards == null)
        {
            return Array.Empty<WeekCardEntryData>();
        }

        return WeekCardEntryResolver.ResolveCurrentWeekEntries(currentWeekDefinition, childState);
    }

    public static Dictionary<EChildStatusType, int> CaptureCurrentStats(RuntimeChildState childState)
    {
        return RuntimeChildState.AllStatTypes.ToDictionary(statType => statType, childState.GetStat);
    }
}
