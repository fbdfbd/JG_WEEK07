using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EndingCatalog",
    menuName = "Scriptable Objects/Ending/EndingCatalog")]
public class SO_EndingCatalog : ScriptableObject
{
    [SerializeField] private SO_EndingLayerDefinition[] _endingLayers = Array.Empty<SO_EndingLayerDefinition>();
    [SerializeField] private SO_CharacterEndingDefinition[] _characterEndings = Array.Empty<SO_CharacterEndingDefinition>();
    [SerializeField] private SO_AffinityEndingDefinition[] _affinityEndings = Array.Empty<SO_AffinityEndingDefinition>();

    [Header("Special")]
    [SerializeField] private string _noCharacterMetEndingId = "ending_no_character_met";
    [SerializeField] private EndingTextData _noCharacterMetEnding;

    public SO_EndingLayerDefinition[] EndingLayers => _endingLayers;
    public SO_CharacterEndingDefinition[] CharacterEndings => _characterEndings;
    public SO_AffinityEndingDefinition[] AffinityEndings => _affinityEndings;
    public string NoCharacterMetEndingId => _noCharacterMetEndingId;
    public EndingTextData NoCharacterMetEnding => _noCharacterMetEnding;

    public SO_EndingLayerDefinition FindDirectionLayer(EndingContext context)
    {
        SO_EndingLayerDefinition bestLayer = null;
        for (int i = 0; i < _endingLayers.Length; i++)
        {
            SO_EndingLayerDefinition layer = _endingLayers[i];
            if (layer != null && layer.MatchesDirection(context.DirectionType))
            {
                if (bestLayer == null || layer.Priority > bestLayer.Priority)
                {
                    bestLayer = layer;
                }
            }
        }

        return bestLayer;
    }

    public SO_EndingLayerDefinition FindCharacterMeetLayer(EndingContext context)
    {
        SO_EndingLayerDefinition bestLayer = null;
        for (int i = 0; i < _endingLayers.Length; i++)
        {
            SO_EndingLayerDefinition layer = _endingLayers[i];
            if (layer != null && layer.MatchesCharacterMeet(context))
            {
                if (bestLayer == null || layer.Priority > bestLayer.Priority)
                {
                    bestLayer = layer;
                }
            }
        }

        return bestLayer;
    }

    public SO_EndingLayerDefinition FindClosestCharacterMeetLayer(EndingContext context)
    {
        SO_EndingLayerDefinition bestLayer = null;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < _endingLayers.Length; i++)
        {
            SO_EndingLayerDefinition layer = _endingLayers[i];
            if (layer == null || !layer.MatchesCharacter(context.CharacterType))
            {
                continue;
            }

            int distance = layer.GetMeetCountDistance(context.MeetCount);
            if (IsBetterClosestLayer(layer, bestLayer, distance, bestDistance))
            {
                bestLayer = layer;
                bestDistance = distance;
            }
        }

        return bestLayer;
    }

    public SO_EndingLayerDefinition FindAffinityLayer(int affinity)
    {
        SO_EndingLayerDefinition bestLayer = null;
        for (int i = 0; i < _endingLayers.Length; i++)
        {
            SO_EndingLayerDefinition layer = _endingLayers[i];
            if (layer != null && layer.MatchesAffinity(affinity))
            {
                if (bestLayer == null || layer.Priority > bestLayer.Priority)
                {
                    bestLayer = layer;
                }
            }
        }

        return bestLayer;
    }

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

    public SO_CharacterEndingDefinition FindNoCharacterEnding(EEndingDirectionType directionType)
    {
        SO_CharacterEndingDefinition bestEnding = null;
        for (int i = 0; i < _characterEndings.Length; i++)
        {
            SO_CharacterEndingDefinition ending = _characterEndings[i];
            if (ending != null && ending.MatchesNoCharacter(directionType))
            {
                if (bestEnding == null || ending.Priority > bestEnding.Priority)
                {
                    bestEnding = ending;
                }
            }
        }

        return bestEnding;
    }

    public SO_CharacterEndingDefinition FindClosestCharacterEnding(EndingContext context)
    {
        SO_CharacterEndingDefinition bestEnding = null;
        int bestDistance = int.MaxValue;
        bool bestDirectionMatches = false;

        for (int i = 0; i < _characterEndings.Length; i++)
        {
            SO_CharacterEndingDefinition ending = _characterEndings[i];
            if (ending == null || !ending.MatchesCharacter(context.CharacterType))
            {
                continue;
            }

            int distance = ending.GetMeetCountDistance(context.MeetCount);
            bool directionMatches = ending.MatchesDirection(context.DirectionType);
            if (IsBetterClosestEnding(ending, bestEnding, distance, bestDistance, directionMatches, bestDirectionMatches))
            {
                bestEnding = ending;
                bestDistance = distance;
                bestDirectionMatches = directionMatches;
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

    private static bool IsBetterClosestEnding(
        SO_CharacterEndingDefinition candidate,
        SO_CharacterEndingDefinition current,
        int candidateDistance,
        int currentDistance,
        bool candidateDirectionMatches,
        bool currentDirectionMatches)
    {
        if (current == null)
        {
            return true;
        }

        if (candidateDirectionMatches != currentDirectionMatches)
        {
            return candidateDirectionMatches;
        }

        if (candidateDistance != currentDistance)
        {
            return candidateDistance < currentDistance;
        }

        if (candidate.Priority != current.Priority)
        {
            return candidate.Priority > current.Priority;
        }

        if (candidate.MaxMeetCount != current.MaxMeetCount)
        {
            return candidate.MaxMeetCount > current.MaxMeetCount;
        }

        if (candidate.MinMeetCount != current.MinMeetCount)
        {
            return candidate.MinMeetCount > current.MinMeetCount;
        }

        return false;
    }

    private static bool IsBetterClosestLayer(
        SO_EndingLayerDefinition candidate,
        SO_EndingLayerDefinition current,
        int candidateDistance,
        int currentDistance)
    {
        if (current == null)
        {
            return true;
        }

        if (candidateDistance != currentDistance)
        {
            return candidateDistance < currentDistance;
        }

        if (candidate.Priority != current.Priority)
        {
            return candidate.Priority > current.Priority;
        }

        if (candidate.MaxMeetCount != current.MaxMeetCount)
        {
            return candidate.MaxMeetCount > current.MaxMeetCount;
        }

        if (candidate.MinMeetCount != current.MinMeetCount)
        {
            return candidate.MinMeetCount > current.MinMeetCount;
        }

        return false;
    }
}

