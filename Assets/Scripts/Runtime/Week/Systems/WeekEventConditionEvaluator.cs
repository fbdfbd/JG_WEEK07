using System.Linq;

public static class WeekEventConditionEvaluator
{
    public static bool MeetsStateConditions(
        RuntimeChildState childState,
        WeekEventConditionData conditions)
    {
        return HasRequiredFlags(childState, conditions) &&
               HasNoBlockedFlags(childState, conditions) &&
               MeetsStatRequirements(childState, conditions);
    }

    public static bool MeetsAllConditions(
        RuntimeChildState childState,
        RuntimeInformationControlResult informationControlResult,
        WeekEventConditionData conditions)
    {
        return MeetsStateConditions(childState, conditions) &&
               MeetsInformationRequirements(informationControlResult, conditions);
    }

    public static bool TryGetSelectionScore(
        EventSelectionRuleData rule,
        RuntimeChildState childState,
        out int score)
    {
        score = 0;
        if (rule == null || childState == null)
        {
            return false;
        }

        int value = childState.GetStat(rule.SelectorStat);
        switch (rule.Mode)
        {
            case EEventSelectionMode.MaxPositive:
                if (value <= rule.Threshold)
                {
                    return false;
                }

                score = value;
                return true;
            case EEventSelectionMode.MaxNegative:
                if (value >= rule.Threshold)
                {
                    return false;
                }

                score = rule.Threshold - value;
                return true;
            case EEventSelectionMode.MaxAny:
                score = value;
                return true;
            case EEventSelectionMode.MinAny:
                score = -value;
                return true;
            default:
                return false;
        }
    }

    public static bool IsDefaultSelection(EventSelectionRuleData rule)
    {
        return rule == null || rule.IsDefault || rule.Mode == EEventSelectionMode.Default || rule.Mode == EEventSelectionMode.None;
    }

    private static bool HasRequiredFlags(RuntimeChildState childState, WeekEventConditionData conditions)
    {
        if (conditions?.RequiredFlags == null || conditions.RequiredFlags.Length == 0)
        {
            return true;
        }

        return childState != null && conditions.RequiredFlags.All(childState.HasFlag);
    }

    private static bool HasNoBlockedFlags(RuntimeChildState childState, WeekEventConditionData conditions)
    {
        if (conditions?.BlockedFlags == null || conditions.BlockedFlags.Length == 0)
        {
            return true;
        }

        return childState != null && conditions.BlockedFlags.All(flagType => !childState.HasFlag(flagType));
    }

    private static bool MeetsStatRequirements(RuntimeChildState childState, WeekEventConditionData conditions)
    {
        if (conditions?.StatRequirements == null || conditions.StatRequirements.Length == 0)
        {
            return true;
        }

        if (childState == null)
        {
            return false;
        }

        return conditions.StatRequirements.All(requirement =>
        {
            int value = childState.GetStat(requirement.StatType);
            bool meetsMinimum = !requirement.UseMinimum || value >= requirement.MinimumValue;
            bool meetsMaximum = !requirement.UseMaximum || value <= requirement.MaximumValue;
            return meetsMinimum && meetsMaximum;
        });
    }

    private static bool MeetsInformationRequirements(
        RuntimeInformationControlResult informationControlResult,
        WeekEventConditionData conditions)
    {
        if (conditions?.InformationRequirements == null || conditions.InformationRequirements.Length == 0)
        {
            return true;
        }

        return conditions.InformationRequirements.All(requirement =>
        {
            ECardOptionSemantic? semanticFilter = requirement.UseSemanticFilter ? requirement.Semantic : null;
            int count = informationControlResult?.CountSelectionsForType(requirement.InformationType, semanticFilter) ?? 0;
            return count >= requirement.MinimumCount;
        });
    }
}
