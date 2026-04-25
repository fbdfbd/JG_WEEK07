using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuntimeChildState
{
    public const int MinStatValue = -15;
    public const int MaxStatValue = 15;
    public const int DefaultStatValue = 0;
    private static readonly EChildStatusType[] CachedStatTypes = Enum.GetValues(typeof(EChildStatusType)).Cast<EChildStatusType>().ToArray();

    private readonly Dictionary<EChildStatusType, int> _stats = new();
    private readonly HashSet<SO_FlagDefinition> _flags = new();
    private readonly List<string> _reactionLogs = new();

    public event Action<StatChangeInfo> StatChanged;
    public event Action<FlagChangeInfo> FlagChanged;

    public RuntimeChildState()
    {
        InitializeDefaultStats();
    }

    public IReadOnlyCollection<SO_FlagDefinition> Flags => _flags;
    public IReadOnlyList<string> ReactionLogs => _reactionLogs;
    public static IReadOnlyList<EChildStatusType> AllStatTypes => CachedStatTypes;

    public int GetStat(EChildStatusType statType)
    {
        if (_stats.TryGetValue(statType, out int value))
        {
            return value;
        }

        return DefaultStatValue;
    }

    public void SetStat(EChildStatusType statType, int value, string toastMessage = null)
    {
        int previousValue = GetStat(statType);
        int currentValue = ClampStat(value);
        if (previousValue == currentValue)
        {
            return;
        }

        _stats[statType] = currentValue;
        StatChanged?.Invoke(new StatChangeInfo(statType, previousValue, currentValue, toastMessage));
    }

    public void AddStat(EChildStatusType statType, int amount, string toastMessage = null)
    {
        SetStat(statType, GetStat(statType) + amount, toastMessage);
    }

    public bool HasFlag(SO_FlagDefinition flagDefinition)
    {
        return flagDefinition != null && _flags.Contains(flagDefinition);
    }

    public bool HasFlag(string flagId)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return false;
        }

        return _flags.Any(flagDefinition => flagDefinition != null && flagDefinition.Id == flagId);
    }

    public void SetFlag(SO_FlagDefinition flagDefinition)
    {
        if (flagDefinition == null)
        {
            return;
        }

        if (_flags.Add(flagDefinition))
        {
            FlagChanged?.Invoke(new FlagChangeInfo(flagDefinition, true));
        }
    }

    public void RemoveFlag(SO_FlagDefinition flagDefinition)
    {
        if (flagDefinition == null)
        {
            return;
        }

        if (_flags.Remove(flagDefinition))
        {
            FlagChanged?.Invoke(new FlagChangeInfo(flagDefinition, false));
        }
    }

    public void AddReactionLog(string reactionText)
    {
        if (string.IsNullOrWhiteSpace(reactionText))
        {
            return;
        }

        _reactionLogs.Add(reactionText);
    }

    public void ClearReactionLogs()
    {
        _reactionLogs.Clear();
    }

    public RuntimeChildState CreateCopy()
    {
        RuntimeChildState copy = new();
        copy._stats.Clear();

        foreach (KeyValuePair<EChildStatusType, int> statEntry in _stats)
        {
            copy._stats[statEntry.Key] = statEntry.Value;
        }

        copy._flags.Clear();
        foreach (SO_FlagDefinition flag in _flags)
        {
            copy._flags.Add(flag);
        }

        copy._reactionLogs.Clear();
        copy._reactionLogs.AddRange(_reactionLogs);

        return copy;
    }

    private void InitializeDefaultStats()
    {
        foreach (EChildStatusType statType in AllStatTypes)
        {
            _stats[statType] = DefaultStatValue;
        }
    }

    private static int ClampStat(int value)
    {
        if (value < MinStatValue)
        {
            return MinStatValue;
        }

        if (value > MaxStatValue)
        {
            return MaxStatValue;
        }

        return value;
    }
}

public readonly struct StatChangeInfo
{
    public StatChangeInfo(EChildStatusType statType, int previousValue, int currentValue, string toastMessage = null)
    {
        StatType = statType;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        ToastMessage = toastMessage;
    }

    public EChildStatusType StatType { get; }
    public int PreviousValue { get; }
    public int CurrentValue { get; }
    public string ToastMessage { get; }
    public int Delta => CurrentValue - PreviousValue;
}

public readonly struct FlagChangeInfo
{
    public FlagChangeInfo(SO_FlagDefinition flagDefinition, bool added)
    {
        FlagDefinition = flagDefinition;
        Added = added;
    }

    public SO_FlagDefinition FlagDefinition { get; }
    public bool Added { get; }
}

public enum EChildStatusType
{
    Trust,
    Affinity,
    Curiosity,
    Anxiety,
    Obedience,
    Rian,
    Max,
    Millia,
    Yuffie
}
