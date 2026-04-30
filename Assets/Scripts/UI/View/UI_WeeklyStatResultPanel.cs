using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeeklyStatResultPanel : MonoBehaviour
{
    [Serializable]
    private class StatBarSlot
    {
        public EChildStatusType StatType;
        public GameObject Root = null;
        public UI_CharacterStatusBar StatusBar = null;
    }

    private readonly struct ActiveStatChange
    {
        public ActiveStatChange(StatBarSlot slot, WeeklyStatChangePresentation change)
        {
            Slot = slot;
            Change = change;
        }

        public StatBarSlot Slot { get; }
        public WeeklyStatChangePresentation Change { get; }
    }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private StatBarSlot[] _statBars =
    {
        new() { StatType = EChildStatusType.Trust },
        new() { StatType = EChildStatusType.Curiosity },
        new() { StatType = EChildStatusType.Obedience },
        new() { StatType = EChildStatusType.Anxiety },
    };
    [SerializeField] private Button _advanceButton;
    [SerializeField] private float _panelFadeDuration = 0.15f;
    [SerializeField] private float _barDuration = 0.45f;
    [SerializeField] private float _barInterval = 0.08f;

    private readonly List<ActiveStatChange> _activeChanges = new();
    private WeeklyStatResultPresentation _presentation;
    private Sequence _playSequence;
    private bool _isPlaying;
    private bool _isReadyToClose;

    public event Action ContinueRequested;
    public bool IsVisible => gameObject.activeSelf;

    private void Awake()
    {
        ResolveReferences();
        BindAdvanceButton();
        Hide();
    }

    private void OnDestroy()
    {
        KillPlaySequence(false);
        UnbindAdvanceButton();
    }

    public void Show(WeeklyStatResultPresentation presentation)
    {
        ResolveReferences();
        KillPlaySequence(false);
        HideAllSlots();
        _activeChanges.Clear();

        _presentation = presentation;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SetCanvasVisible(true);

        PrepareSlots();

        _isPlaying = _activeChanges.Count > 0;
        _isReadyToClose = !_isPlaying;

        if (_isPlaying)
        {
            PlayBars();
        }
    }

    public void Hide()
    {
        KillPlaySequence(false);
        _isPlaying = false;
        _isReadyToClose = false;
        SetCanvasVisible(false);
        gameObject.SetActive(false);
    }

    public bool TryAdvance()
    {
        if (!IsVisible)
        {
            return false;
        }

        if (_isPlaying)
        {
            SkipAnimation();
            return true;
        }

        if (_isReadyToClose)
        {
            Hide();
            ContinueRequested?.Invoke();
            return true;
        }

        return false;
    }

    private void PrepareSlots()
    {
        if (_presentation.Changes == null || _statBars == null)
        {
            return;
        }

        for (int index = 0; index < _presentation.Changes.Count; index++)
        {
            WeeklyStatChangePresentation change = _presentation.Changes[index];
            StatBarSlot slot = FindSlot(change.StatType);
            if (slot?.StatusBar == null)
            {
                continue;
            }

            SetSlotVisible(slot, true);
            slot.StatusBar.Render(
                change.LeftLabel,
                change.RightLabel,
                change.BeforeValue,
                change.MinValue,
                change.MaxValue,
                change.RenderMode);

            _activeChanges.Add(new ActiveStatChange(slot, change));
        }
    }

    private void PlayBars()
    {
        _playSequence = DOTween.Sequence();
        _playSequence.AppendInterval(Mathf.Max(0f, _panelFadeDuration));

        for (int index = 0; index < _activeChanges.Count; index++)
        {
            ActiveStatChange activeChange = _activeChanges[index];
            float barDuration = Mathf.Max(0f, _barDuration);
            _playSequence.Append(activeChange.Slot.StatusBar.PlayValue(
                activeChange.Change.LeftLabel,
                activeChange.Change.RightLabel,
                activeChange.Change.BeforeValue,
                activeChange.Change.AfterValue,
                activeChange.Change.MinValue,
                activeChange.Change.MaxValue,
                activeChange.Change.RenderMode,
                barDuration));
            _playSequence.AppendInterval(Mathf.Max(0f, _barInterval));
        }

        _playSequence.OnComplete(MarkReadyToClose);
    }

    private void SkipAnimation()
    {
        KillPlaySequence(false);
        for (int index = 0; index < _activeChanges.Count; index++)
        {
            ActiveStatChange activeChange = _activeChanges[index];
            activeChange.Slot.StatusBar.Render(
                activeChange.Change.LeftLabel,
                activeChange.Change.RightLabel,
                activeChange.Change.AfterValue,
                activeChange.Change.MinValue,
                activeChange.Change.MaxValue,
                activeChange.Change.RenderMode);
        }

        MarkReadyToClose();
    }

    private void MarkReadyToClose()
    {
        _isPlaying = false;
        _isReadyToClose = true;
    }

    private void HideAllSlots()
    {
        if (_statBars == null)
        {
            return;
        }

        for (int index = 0; index < _statBars.Length; index++)
        {
            SetSlotVisible(_statBars[index], false);
        }
    }

    private void KillPlaySequence(bool complete)
    {
        if (_playSequence == null)
        {
            return;
        }

        _playSequence.Kill(complete);
        _playSequence = null;
    }

    private void SetCanvasVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void ResolveReferences()
    {
        if (_canvasGroup == null)
        {
            TryGetComponent(out _canvasGroup);
        }
    }

    private void BindAdvanceButton()
    {
        if (_advanceButton != null)
        {
            _advanceButton.onClick.AddListener(HandleAdvanceButtonClicked);
        }
    }

    private void UnbindAdvanceButton()
    {
        if (_advanceButton != null)
        {
            _advanceButton.onClick.RemoveListener(HandleAdvanceButtonClicked);
        }
    }

    private void HandleAdvanceButtonClicked()
    {
        TryAdvance();
    }

    private StatBarSlot FindSlot(EChildStatusType statType)
    {
        if (_statBars == null)
        {
            return null;
        }

        for (int index = 0; index < _statBars.Length; index++)
        {
            StatBarSlot slot = _statBars[index];
            if (slot != null && slot.StatType == statType)
            {
                return slot;
            }
        }

        return null;
    }

    private static void SetSlotVisible(StatBarSlot slot, bool visible)
    {
        if (slot == null)
        {
            return;
        }

        GameObject target = slot.Root != null
            ? slot.Root
            : slot.StatusBar != null ? slot.StatusBar.gameObject : null;
        if (target != null)
        {
            target.SetActive(visible);
        }
    }
}
