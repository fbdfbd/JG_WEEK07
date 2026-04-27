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

    public SO_CharacterEndingDefinition FindClosestCharacterEnding(EndingContext context)
    {
        SO_CharacterEndingDefinition bestEnding = null;
        int bestDistance = int.MaxValue;
        bool bestMoodMatches = false;

        for (int i = 0; i < _characterEndings.Length; i++)
        {
            SO_CharacterEndingDefinition ending = _characterEndings[i];
            if (ending == null || !ending.MatchesCharacter(context.CharacterType))
            {
                continue;
            }

            int distance = ending.GetMeetCountDistance(context.MeetCount);
            bool moodMatches = ending.MatchesMood(context.MoodType);
            if (IsBetterClosestEnding(ending, bestEnding, distance, bestDistance, moodMatches, bestMoodMatches))
            {
                bestEnding = ending;
                bestDistance = distance;
                bestMoodMatches = moodMatches;
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
        bool candidateMoodMatches,
        bool currentMoodMatches)
    {
        if (current == null)
        {
            return true;
        }

        if (candidateMoodMatches != currentMoodMatches)
        {
            return candidateMoodMatches;
        }

        if (candidateDistance != currentDistance)
        {
            return candidateDistance < currentDistance;
        }

        if (candidate.MaxMeetCount != current.MaxMeetCount)
        {
            return candidate.MaxMeetCount > current.MaxMeetCount;
        }

        if (candidate.MinMeetCount != current.MinMeetCount)
        {
            return candidate.MinMeetCount > current.MinMeetCount;
        }

        return candidate.Priority > current.Priority;
    }
}

