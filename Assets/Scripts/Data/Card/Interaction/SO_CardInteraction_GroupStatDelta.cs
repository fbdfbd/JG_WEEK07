using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CardInteraction_Group",
    menuName = "Scriptable Objects/Card/Interaction/GroupDelta")]
public class SO_CardInteraction_GroupStatDelta : SO_CardInteractionDefinition
{
    [SerializeField] private SO_CardInteractionDefinition[] _interactions;

    public override void Apply(RuntimeChildState childState)
    {
        foreach (var interaction in _interactions)
        {
            interaction?.Apply(childState);
        }
    }

    public override IEnumerable<string> GetDisplayNames()
    {
        if (!string.IsNullOrWhiteSpace(SerializedDisplayName))
        {
            yield return SerializedDisplayName;
            yield break;
        }

        if (_interactions == null)
        {
            yield break;
        }

        for (int index = 0; index < _interactions.Length; index++)
        {
            SO_CardInteractionDefinition interaction = _interactions[index];
            if (interaction == null)
            {
                continue;
            }

            foreach (string displayName in interaction.GetDisplayNames())
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    yield return displayName;
                }
            }
        }
    }
}
