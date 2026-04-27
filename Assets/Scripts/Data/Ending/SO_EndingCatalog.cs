using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EndingCatalog",
    menuName = "Scriptable Objects/Ending/EndingCatalog")]
public class SO_EndingCatalog : ScriptableObject
{
    [SerializeField] private SO_CharacterEndingDefinition[] _characterEndings = Array.Empty<SO_CharacterEndingDefinition>();
    [SerializeField] private SO_AffinityEndingDefinition[] _affinityEndings = Array.Empty<SO_AffinityEndingDefinition>();

    [Header("Special")]
    [SerializeField] private string _noCharacterMetEndingId = "ending_no_character_met";
    [SerializeField] private EndingTextData _noCharacterMetEnding;

    public SO_CharacterEndingDefinition[] CharacterEndings => _characterEndings;
    public SO_AffinityEndingDefinition[] AffinityEndings => _affinityEndings;
    public string NoCharacterMetEndingId => _noCharacterMetEndingId;
    public EndingTextData NoCharacterMetEnding => _noCharacterMetEnding;

    public SO_CharacterEndingDefinition FindCharacterEnding(EndingContext context)
    {
        SO_CharacterEndingDefinition bestEnding = null;
        for (int i = 0; i < _characterEndings.Length; i++)
        {
            SO_CharacterEndingDefinition ending = _characterEndings[i];
            if (ending != null && ending.Matches(context))
            {
                if (bestEnding == null || ending.Priority > bestEnding.Priority)
                {
                    bestEnding = ending;
                }
            }
        }

        return bestEnding;
    }

    public SO_AffinityEndingDefinition FindAffinityEnding(int affinity)
    {
        SO_AffinityEndingDefinition bestEnding = null;
        for (int i = 0; i < _affinityEndings.Length; i++)
        {
            SO_AffinityEndingDefinition ending = _affinityEndings[i];
            if (ending != null && ending.Matches(affinity))
            {
                if (bestEnding == null || ending.Priority > bestEnding.Priority)
                {
                    bestEnding = ending;
                }
            }
        }

        return bestEnding;
    }
}

