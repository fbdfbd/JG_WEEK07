using System;
using System.Linq;

public sealed class PoCNightDialogueResolver
{
    public SO_InteractiveEventDefinition Resolve(
        PoCNightDialogueRuleData[] rules,
        PoCRuntimeState state)
    {
        return rules?
            .Where(rule => rule != null && rule.Matches(state))
            .OrderByDescending(rule => rule.Priority)
            .Select(rule => rule.Dialogue)
            .FirstOrDefault(dialogue => dialogue != null && dialogue.FirstStep != null);
    }
}
