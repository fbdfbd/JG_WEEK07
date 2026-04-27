using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeekEntryIntroCatalog",
    menuName = "Scriptable Objects/Week/WeekEntryIntroCatalog")]
public class SO_WeekEntryIntroCatalog : ScriptableObject
{
    [SerializeField] private WeekEntryIntroEntry[] _entries = Array.Empty<WeekEntryIntroEntry>();

    public bool TryGet(string weekId, out WeekEntryIntroEntry entry)
    {
        foreach (WeekEntryIntroEntry item in _entries)
        {
            if (item == null || !item.IsForWeek(weekId))
            {
                continue;
            }

            entry = item;
            return true;
        }

        entry = null;
        return false;
    }
}

[Serializable]
public class WeekEntryIntroEntry
{
    [SerializeField] private string _weekId = string.Empty;
    [SerializeField] private string _title = string.Empty;
    [SerializeField] private float _fadeInSeconds = 0.4f;
    [SerializeField] private float _fadeOutSeconds = 0.4f;
    [SerializeField] private bool _waitForClick = true;

    public string WeekId => _weekId;
    public string Title => _title;
    public float FadeInSeconds => Mathf.Max(0f, _fadeInSeconds);
    public float FadeOutSeconds => Mathf.Max(0f, _fadeOutSeconds);
    public bool WaitForClick => _waitForClick;

    public bool IsForWeek(string weekId)
    {
        return !string.IsNullOrWhiteSpace(_weekId)
            && string.Equals(_weekId, weekId, StringComparison.OrdinalIgnoreCase);
    }
}
