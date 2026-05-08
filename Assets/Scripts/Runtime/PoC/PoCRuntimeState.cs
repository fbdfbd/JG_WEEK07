using System;
using System.Collections.Generic;

public sealed class PoCRuntimeState
{
    public const int DefaultStatValue = 0;
    public const int MinStatValue = -100;
    public const int MaxStatValue = 100;

    public static readonly CharacterStatType[] PublicStatTypes =
    {
        CharacterStatType.Stability,
        CharacterStatType.Autonomy,
        CharacterStatType.Trust
    };

    private readonly Dictionary<CharacterStatType, int> _stats = new();
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _selectedChoiceIds = new(StringComparer.OrdinalIgnoreCase);

    public event Action StatsChanged;
    public event Action FlagsChanged;

    public PoCPhase Phase { get; private set; }
    public string SelectedMainChoiceId { get; private set; } = string.Empty;
    public RuntimeInteractiveEventSession NightSession { get; private set; }

    public IReadOnlyCollection<string> Flags => _flags;
    public IReadOnlyCollection<string> SelectedChoiceIds => _selectedChoiceIds;

    public void SetPhase(PoCPhase phase)
    {
        Phase = phase;
    }

    public int GetStat(CharacterStatType statType)
    {
        return _stats.TryGetValue(statType, out int value) ? value : DefaultStatValue;
    }

    public void AddStat(CharacterStatType statType, int delta)
    {
        if (delta == 0)
        {
            return;
        }

        int nextValue = Math.Clamp(GetStat(statType) + delta, MinStatValue, MaxStatValue);
        _stats[statType] = nextValue;
        StatsChanged?.Invoke();
    }

    public void ApplyStatChanges(IReadOnlyList<PoCStatChangeData> changes)
    {
        if (changes == null)
        {
            return;
        }

        for (int index = 0; index < changes.Count; index++)
        {
            PoCStatChangeData change = changes[index];
            if (change != null)
            {
                AddStat(change.StatType, change.Delta);
            }
        }
    }

    public void SelectMainChoice(string choiceId)
    {
        SelectedMainChoiceId = choiceId ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(choiceId))
        {
            _selectedChoiceIds.Add(choiceId);
        }
    }

    public bool HasSelectedChoice(string choiceId)
    {
        return !string.IsNullOrWhiteSpace(choiceId) && _selectedChoiceIds.Contains(choiceId);
    }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && _flags.Contains(flag);
    }

    public void SetFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag) || !_flags.Add(flag))
        {
            return;
        }

        FlagsChanged?.Invoke();
    }

    public void BeginNightDialogue(SO_InteractiveEventDefinition dialogue)
    {
        NightSession = dialogue != null ? new RuntimeInteractiveEventSession(dialogue) : null;
    }

    public void ClearNightDialogue()
    {
        NightSession = null;
    }
}
