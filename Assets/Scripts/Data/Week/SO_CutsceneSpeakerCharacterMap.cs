using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CutsceneSpeakerCharacterMap_",
    menuName = "Scriptable Objects/Week/CutsceneSpeakerCharacterMap")]
public class SO_CutsceneSpeakerCharacterMap : ScriptableObject
{
    [SerializeField] private CutsceneSpeakerCharacterEntry[] _entries = Array.Empty<CutsceneSpeakerCharacterEntry>();

    public bool TryGetCharacterType(string speakerId, out CutsceneCharacterType characterType)
    {
        characterType = CutsceneCharacterType.None;
        if (string.IsNullOrWhiteSpace(speakerId) || _entries == null)
        {
            return false;
        }

        for (int index = 0; index < _entries.Length; index++)
        {
            CutsceneSpeakerCharacterEntry entry = _entries[index];
            if (entry == null || !entry.IsMatch(speakerId))
            {
                continue;
            }

            characterType = entry.CharacterType;
            return true;
        }

        return false;
    }
}

[Serializable]
public class CutsceneSpeakerCharacterEntry
{
    [SerializeField] private string _speakerId = string.Empty;
    [SerializeField] private CutsceneCharacterType _characterType = CutsceneCharacterType.None;

    public CutsceneCharacterType CharacterType => _characterType;

    public bool IsMatch(string speakerId)
    {
        return !string.IsNullOrWhiteSpace(_speakerId)
            && string.Equals(_speakerId, speakerId, StringComparison.OrdinalIgnoreCase);
    }
}
