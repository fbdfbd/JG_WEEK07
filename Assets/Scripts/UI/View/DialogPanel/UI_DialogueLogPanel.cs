using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_DialogueLogPanel : MonoBehaviour
{
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private UI_DialogueLogRowView _rowPrefab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TextMeshProUGUI _emptyText;

    private readonly List<UI_DialogueLogRowView> _rows = new();

    private WeekFlowDialogueLogService _dialogueLogService;

    public bool IsVisible => gameObject.activeSelf;

    private void Awake()
    {
        BindCloseButtonEvent();
    }

    private void OnDestroy()
    {
        UnbindCloseButtonEvent();
        UnbindDialogueLogService();
    }

    public void SetDialogueLogService(WeekFlowDialogueLogService dialogueLogService)
    {
        if (_dialogueLogService == dialogueLogService)
        {
            return;
        }

        UnbindDialogueLogService();
        _dialogueLogService = dialogueLogService;
        BindDialogueLogService();
        Refresh();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        ClearRows();

        if (_dialogueLogService == null || _contentRoot == null || _rowPrefab == null)
        {
            RefreshEmptyState();
            return;
        }

        IReadOnlyList<DialogueLogEntry> entries = _dialogueLogService.Entries;
        for (int index = 0; index < entries.Count; index++)
        {
            UI_DialogueLogRowView rowView = Instantiate(_rowPrefab, _contentRoot);
            rowView.Render(entries[index]);
            _rows.Add(rowView);
        }

        RefreshEmptyState();
    }

    private void BindCloseButtonEvent()
    {
        if (_closeButton == null)
        {
            return;
        }

        _closeButton.onClick.AddListener(HandleCloseButtonClicked);
    }

    private void UnbindCloseButtonEvent()
    {
        if (_closeButton == null)
        {
            return;
        }

        _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
    }

    private void HandleCloseButtonClicked()
    {
        Hide();
    }

    private void BindDialogueLogService()
    {
        if (_dialogueLogService == null)
        {
            return;
        }

        _dialogueLogService.Changed += HandleDialogueLogChanged;
    }

    private void UnbindDialogueLogService()
    {
        if (_dialogueLogService == null)
        {
            return;
        }

        _dialogueLogService.Changed -= HandleDialogueLogChanged;
    }

    private void HandleDialogueLogChanged()
    {
        if (!IsVisible)
        {
            return;
        }

        Refresh();
    }

    private void ClearRows()
    {
        for (int index = 0; index < _rows.Count; index++)
        {
            UI_DialogueLogRowView rowView = _rows[index];
            if (rowView == null)
            {
                continue;
            }

            Destroy(rowView.gameObject);
        }

        _rows.Clear();
    }

    private void RefreshEmptyState()
    {
        if (_emptyText == null)
        {
            return;
        }

        bool hasEntries = _dialogueLogService != null && _dialogueLogService.Entries.Count > 0;
        _emptyText.gameObject.SetActive(!hasEntries);

        if (!hasEntries)
        {
            _emptyText.text = "아직 표시된 대사가 없습니다.";
        }
    }
}
