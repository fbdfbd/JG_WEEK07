using System;

public sealed class EventCutsceneRuleResolver
{
    public bool TryResolve(
        WeekFlowCutsceneRequest request,
        SO_EventCutsceneRuleCatalog catalog,
        out EventCutsceneRuleData rule)
    {
        rule = null;
        if (catalog == null || catalog.Rules == null || catalog.Rules.Length == 0)
        {
            return false;
        }

        int bestScore = int.MinValue;
        for (int index = 0; index < catalog.Rules.Length; index++)
        {
            EventCutsceneRuleData candidate = catalog.Rules[index];
            if (!IsMatch(candidate, request, out int score))
            {
                continue;
            }

            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            rule = candidate;
        }

        return rule != null && (rule.UsesSequence || rule.UsesSpecialPlayer);
    }

    private static bool IsMatch(EventCutsceneRuleData rule, WeekFlowCutsceneRequest request, out int score)
    {
        score = 0;
        if (rule == null || !rule.Enabled)
        {
            return false;
        }

        if (request.Moment != EWeekFlowCutsceneMoment.EventEnter &&
            request.Moment != EWeekFlowCutsceneMoment.EventExit)
        {
            return false;
        }

        if (rule.Moment != request.Moment)
        {
            return false;
        }

        if (!IsStringFieldMatch(rule.EventId, request.EventId, 10, ref score))
        {
            return false;
        }

        if (!IsStringFieldMatch(rule.WeekId, request.WeekId, 5, ref score))
        {
            return false;
        }

        return true;
    }

    private static bool IsStringFieldMatch(string expected, string actual, int scoreValue, ref int score)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        score += scoreValue;
        return true;
    }
}
