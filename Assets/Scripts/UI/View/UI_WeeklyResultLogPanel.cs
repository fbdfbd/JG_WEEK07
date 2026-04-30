using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeeklyResultLogPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Transform _entryRoot;
    [SerializeField] private UI_WeeklyResultLogEntryView _entryPrefab;
    [SerializeField] private Button _advanceButton;
    [SerializeField] private float _panelFadeDuration = 0.15f;
    [SerializeField] private float _entryShowDuration = 0.28f;
    [SerializeField] private float _entryInterval = 0.08f;

    private readonly List<WeeklyResultLogEntryPresentation> _pendingEntries = new();
    private readonly List<UI_WeeklyResultLogEntryView> _entryViews = new();
    private Sequence _showSequence;
    private int _nextEntryIndex;
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
        KillShowSequence(false);
        UnbindAdvanceButton();
    }

    public void Show(WeeklyResultLogPresentation presentation)
    {
        ResolveReferences();
        KillShowSequence(false);
        ClearEntries();

        _pendingEntries.Clear();
        if (presentation.Entries != null)
        {
            _pendingEntries.AddRange(presentation.Entries);
        }

        gameObject.SetActive(true);
        SetCanvasVisible(true);

        _nextEntryIndex = 0;
        _isPlaying = _pendingEntries.Count > 0 && _entryPrefab != null && _entryRoot != null;
        _isReadyToClose = !_isPlaying;

        if (!_isPlaying)
        {
            ShowAllEntriesInstant();
            return;
        }

        PlayEntries();
    }

    public void Hide()
    {
        KillShowSequence(false);
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
            SkipEntryAnimation();
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

    private void PlayEntries()
    {
        _showSequence = DOTween.Sequence();
        _showSequence.AppendInterval(Mathf.Max(0f, _panelFadeDuration));

        while (_nextEntryIndex < _pendingEntries.Count)
        {
            int entryIndex = _nextEntryIndex++;
            _showSequence.AppendCallback(() => CreateEntry(_pendingEntries[entryIndex], false));
            _showSequence.AppendInterval(Mathf.Max(0f, _entryInterval));
        }

        _showSequence.OnComplete(MarkReadyToClose);
    }

    private void SkipEntryAnimation()
    {
        KillShowSequence(true);
        ShowAllEntriesInstant();
        MarkReadyToClose();
    }

    private void ShowAllEntriesInstant()
    {
        while (_nextEntryIndex < _pendingEntries.Count)
        {
            CreateEntry(_pendingEntries[_nextEntryIndex++], true);
        }
    }

    private void CreateEntry(WeeklyResultLogEntryPresentation entry, bool instant)
    {
        if (_entryPrefab == null || _entryRoot == null)
        {
            return;
        }

        UI_WeeklyResultLogEntryView entryView = Instantiate(_entryPrefab, _entryRoot);
        entryView.Render(entry);
        _entryViews.Add(entryView);

        if (instant)
        {
            entryView.ShowInstant();
            return;
        }

        entryView.PlayShow(Mathf.Max(0f, _entryShowDuration));
    }

    private void MarkReadyToClose()
    {
        _isPlaying = false;
        _isReadyToClose = true;
    }

    private void ClearEntries()
    {
        for (int index = 0; index < _entryViews.Count; index++)
        {
            if (_entryViews[index] != null)
            {
                Destroy(_entryViews[index].gameObject);
            }
        }

        _entryViews.Clear();
    }

    private void KillShowSequence(bool complete)
    {
        if (_showSequence == null)
        {
            return;
        }

        _showSequence.Kill(complete);
        _showSequence = null;
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
}
