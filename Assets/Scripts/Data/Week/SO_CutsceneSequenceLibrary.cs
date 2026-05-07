using System;
using DG.Tweening;
using UnityEngine;

public enum EDataCutsceneCommandType
{
    None,
    ShowBackground,
    HideBackground,
    ShowCharacter,
    HideCharacters,
    FadeCanvas,
    MoveRect,
    SetActive,
    Wait
}

[CreateAssetMenu(
    fileName = "CutsceneSequenceLibrary_",
    menuName = "Scriptable Objects/Week/CutsceneSequenceLibrary")]
public class SO_CutsceneSequenceLibrary : ScriptableObject
{
    [SerializeField] private CutsceneSequenceData[] _sequences = Array.Empty<CutsceneSequenceData>();

    public CutsceneSequenceData[] Sequences => _sequences;

    public bool TryGetSequence(string sequenceId, out CutsceneSequenceData sequence)
    {
        sequence = null;
        if (string.IsNullOrWhiteSpace(sequenceId) || _sequences == null)
        {
            return false;
        }

        for (int index = 0; index < _sequences.Length; index++)
        {
            CutsceneSequenceData candidate = _sequences[index];
            if (candidate == null || !candidate.IsMatch(sequenceId))
            {
                continue;
            }

            sequence = candidate;
            return true;
        }

        return false;
    }
}

[Serializable]
public class CutsceneSequenceData
{
    [SerializeField] private string _sequenceId = string.Empty;
    [SerializeField] private CutsceneCommandData[] _commands = Array.Empty<CutsceneCommandData>();

    public string SequenceId => _sequenceId;
    public CutsceneCommandData[] Commands => _commands;

    public bool IsMatch(string sequenceId)
    {
        return !string.IsNullOrWhiteSpace(_sequenceId)
            && string.Equals(_sequenceId, sequenceId, StringComparison.OrdinalIgnoreCase);
    }
}

[Serializable]
public class CutsceneCommandData
{
    [SerializeField] private EDataCutsceneCommandType _commandType;
    [SerializeField] private string _targetKey = string.Empty;
    [SerializeField] private string _value1 = string.Empty;
    [SerializeField] private string _value2 = string.Empty;
    [SerializeField] private string _value3 = string.Empty;
    [SerializeField] private float _duration;
    [SerializeField] private Ease _ease = Ease.OutCubic;
    [SerializeField] private bool _blocking = true;

    public EDataCutsceneCommandType CommandType => _commandType;
    public string TargetKey => _targetKey;
    public string Value1 => _value1;
    public string Value2 => _value2;
    public string Value3 => _value3;
    public float Duration => _duration;
    public Ease Ease => _ease;
    public bool Blocking => _blocking;
}
