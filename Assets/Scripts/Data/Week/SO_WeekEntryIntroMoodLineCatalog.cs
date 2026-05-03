using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeekEntryIntroMoodLineCatalog",
    menuName = "Scriptable Objects/Week/Week Entry Intro Mood Line Catalog")]
public class SO_WeekEntryIntroMoodLineCatalog : ScriptableObject
{
    [SerializeField] private WeekEntryIntroMoodLineSet[] _weekLineSets = Array.Empty<WeekEntryIntroMoodLineSet>();

    public bool TryResolve(
        string weekId,
        RuntimeChildState childState,
        out string line,
        bool useMoodThresholdCorrection = true)
    {
        line = string.Empty;

        if (childState == null)
        {
            return false;
        }

        if (!TryGetWeekLineSet(weekId, out WeekEntryIntroMoodLineSet weekLineSet))
        {
            return false;
        }

        return weekLineSet.TryResolve(childState, out line, useMoodThresholdCorrection);
    }

    private bool TryGetWeekLineSet(string weekId, out WeekEntryIntroMoodLineSet weekLineSet)
    {
        for (int i = 0; i < _weekLineSets.Length; i++)
        {
            WeekEntryIntroMoodLineSet candidate = _weekLineSets[i];
            if (candidate != null && candidate.IsForWeek(weekId))
            {
                weekLineSet = candidate;
                return true;
            }
        }

        weekLineSet = null;
        return false;
    }
}

[Serializable]
public class WeekEntryIntroMoodLineSet
{
    [SerializeField] private string _weekId = string.Empty;
    [TextArea(2, 4)]
    [SerializeField] private string _noCharacterMetLine = string.Empty;
    [SerializeField] private WeekEntryIntroCharacterMoodLine[] _characterMoodLines = Array.Empty<WeekEntryIntroCharacterMoodLine>();

    public string WeekId => _weekId;
    public string NoCharacterMetLine => _noCharacterMetLine;
    public WeekEntryIntroCharacterMoodLine[] CharacterMoodLines => _characterMoodLines;

    public bool IsForWeek(string weekId)
    {
        return !string.IsNullOrWhiteSpace(_weekId)
            && string.Equals(_weekId, weekId, StringComparison.OrdinalIgnoreCase);
    }

    public bool TryResolve(
        RuntimeChildState childState,
        out string line,
        bool useMoodThresholdCorrection = true)
    {
        line = string.Empty;

        if (childState == null)
        {
            return false;
        }

        if (EndingContextBuilder.HasNoCharacterMet(childState))
        {
            line = _noCharacterMetLine;
            return !string.IsNullOrWhiteSpace(line);
        }

        EndingContext context = EndingContextBuilder.Build(childState, useMoodThresholdCorrection);
        return TryGetCharacterMoodLine(context.CharacterType, context.MoodType, out line);
    }

    private bool TryGetCharacterMoodLine(
        EEndingCharacterType characterType,
        EEndingMoodType moodType,
        out string line)
    {
        for (int i = 0; i < _characterMoodLines.Length; i++)
        {
            WeekEntryIntroCharacterMoodLine entry = _characterMoodLines[i];
            if (entry != null && entry.Matches(characterType, moodType))
            {
                line = entry.ContextLine;
                return !string.IsNullOrWhiteSpace(line);
            }
        }

        line = string.Empty;
        return false;
    }
}

[Serializable]
public class WeekEntryIntroCharacterMoodLine
{
    [SerializeField] private EEndingCharacterType _characterType;
    [SerializeField] private EEndingMoodType _resolvedMoodType;
    [TextArea(2, 4)]
    [SerializeField] private string _contextLine = string.Empty;

    public EEndingCharacterType CharacterType => _characterType;
    public EEndingMoodType ResolvedMoodType => _resolvedMoodType;
    public string ContextLine => _contextLine;

    public bool Matches(EEndingCharacterType characterType, EEndingMoodType moodType)
    {
        return _characterType == characterType
            && _resolvedMoodType == moodType;
    }
}
