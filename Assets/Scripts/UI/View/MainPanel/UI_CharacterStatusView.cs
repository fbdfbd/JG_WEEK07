using System.Collections.Generic;
using UnityEngine;

public class UI_CharacterStatusView : MonoBehaviour
{
    [System.Serializable]
    private class StatusDisplayRule
    {
        public EChildStatusType StatType;
        public string LeftLabel;
        public string RightLabel;
    }

    [Header("Stat Panels")]
    [SerializeField] private StatusDisplayRule[] _displayRules =
    {
        new() { StatType = EChildStatusType.Trust, LeftLabel = "innocent / 순진", RightLabel = "clever / 영민" },
        new() { StatType = EChildStatusType.Curiosity, LeftLabel = "curious / 호기심", RightLabel = "cautious / 신중" },
        new() { StatType = EChildStatusType.Anxiety, LeftLabel = "compliant / 순응", RightLabel = "defiant / 반항" },
        new() { StatType = EChildStatusType.Obedience, LeftLabel = "stable / 안정", RightLabel = "anxious / 불안" },
    };

    [SerializeField] private UI_CharacterStatusBar[] _statusBars;

    public void RenderStatus(ChildStatePresentation presentation)
    {
        if (_displayRules == null || _statusBars == null)
        {
            return;
        }

        int count = Mathf.Min(_displayRules.Length, _statusBars.Length);

        for (int i = 0; i < count; i++)
        {
            UI_CharacterStatusBar statusBar = _statusBars[i];
            if (statusBar == null)
            {
                continue;
            }

            StatusDisplayRule rule = _displayRules[i];
            int statValue = FindStatValue(presentation.Stats, rule.StatType);

            statusBar.gameObject.SetActive(true);
            statusBar.Render(
                rule.LeftLabel,
                rule.RightLabel,
                statValue,
                RuntimeChildState.MinStatValue,
                RuntimeChildState.MaxStatValue);
        }

        for (int i = count; i < _statusBars.Length; i++)
        {
            if (_statusBars[i] != null)
            {
                _statusBars[i].gameObject.SetActive(false);
            }
        }
    }

    private static int FindStatValue(IReadOnlyList<WeekStatPresentation> stats, EChildStatusType statType)
    {
        if (stats == null)
        {
            return RuntimeChildState.DefaultStatValue;
        }

        foreach (WeekStatPresentation stat in stats)
        {
            if (stat.StatType == statType)
            {
                return stat.Value;
            }
        }

        return RuntimeChildState.DefaultStatValue;
    }
}
