using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeeklyResultLogPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Transform _indexRoot;
    [SerializeField] private Button _indexButtonPrefab;
    [SerializeField] private string _indexButtonLabelFormat = "{0}";
    [SerializeField] private Transform _entryRoot;
    [SerializeField] private UI_WeeklyResultLogEntryView _entryPrefab;
    [SerializeField] private Button _advanceButton;
    [SerializeField] private float _panelFadeDuration = 0.15f;
    [SerializeField] private float _entryShowDuration = 0.28f;
    [SerializeField] private float _entryInterval = 0.08f;

    private readonly List<WeeklyResultLogEntryPresentation> _pendingEntries = new();
    private readonly List<Button> _indexButtons = new();
    private readonly List<UI_WeeklyResultLogEntryView> _entryViews = new();
    private WeeklyResultLogPresentation _presentation;
    private string _selectedWeekId = string.Empty;
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
        ClearIndexButtons();
        ClearEntries();

        _presentation = presentation;
        _selectedWeekId = ResolveInitialWeekId(presentation);
        CreateIndexButtons();
        SetPendingEntries(GetSelectedEntries());

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

    private void SelectWeek(string weekId)
    {
        if (string.IsNullOrWhiteSpace(weekId) ||
            string.Equals(_selectedWeekId, weekId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        KillShowSequence(false);
        ClearEntries();

        _selectedWeekId = weekId;
        SetIndexButtonSelection();
        SetPendingEntries(GetSelectedEntries());
        _nextEntryIndex = 0;
        _isPlaying = false;
        _isReadyToClose = true;
        ShowAllEntriesInstant();
    }

    private void CreateIndexButtons()
    {
        if (!_presentation.HasWeeks || _indexRoot == null || _indexButtonPrefab == null)
        {
            return;
        }

        for (int index = 0; index < _presentation.Weeks.Count; index++)
        {
            WeeklyResultLogWeekPresentation week = _presentation.Weeks[index];
            Button button = Instantiate(_indexButtonPrefab, _indexRoot);
            string weekId = week.WeekId;

            TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (labelText != null)
            {
                labelText.text = FormatIndexLabel(week);
            }

            button.interactable = !IsSelectedWeek(weekId);
            button.onClick.AddListener(() => SelectWeek(weekId));
            _indexButtons.Add(button);
        }
    }

    private void SetIndexButtonSelection()
    {
        if (!_presentation.HasWeeks)
        {
            return;
        }

        for (int index = 0; index < _indexButtons.Count && index < _presentation.Weeks.Count; index++)
        {
            if (_indexButtons[index] != null)
            {
                _indexButtons[index].interactable = !IsSelectedWeek(_presentation.Weeks[index].WeekId);
            }
        }
    }

    private IReadOnlyList<WeeklyResultLogEntryPresentation> GetSelectedEntries()
    {
        if (!_presentation.HasWeeks)
        {
            return _presentation.Entries;
        }

        for (int index = 0; index < _presentation.Weeks.Count; index++)
        {
            WeeklyResultLogWeekPresentation week = _presentation.Weeks[index];
            if (IsSelectedWeek(week.WeekId))
            {
                return week.Entries;
            }
        }

        return _presentation.Entries;
    }

    private void SetPendingEntries(IReadOnlyList<WeeklyResultLogEntryPresentation> entries)
    {
        _pendingEntries.Clear();
        if (entries != null)
        {
            _pendingEntries.AddRange(entries);
        }
    }

    private bool IsSelectedWeek(string weekId)
    {
        return string.Equals(_selectedWeekId, weekId, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveInitialWeekId(WeeklyResultLogPresentation presentation)
    {
        if (!string.IsNullOrWhiteSpace(presentation.SelectedWeekId))
        {
            return presentation.SelectedWeekId;
        }

        if (presentation.HasWeeks)
        {
            return presentation.Weeks[presentation.Weeks.Count - 1].WeekId;
        }

        return string.Empty;
    }

    private string FormatIndexLabel(WeeklyResultLogWeekPresentation week)
    {
        string format = string.IsNullOrWhiteSpace(_indexButtonLabelFormat)
            ? "{0}"
            : _indexButtonLabelFormat;

        return string.Format(format, week.WeekIndex);
    }

    private void ClearIndexButtons()
    {
        for (int index = 0; index < _indexButtons.Count; index++)
        {
            if (_indexButtons[index] != null)
            {
                Destroy(_indexButtons[index].gameObject);
            }
        }

        _indexButtons.Clear();
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
