using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_EndingLetterView : MonoBehaviour
{
    [SerializeField] private Button _paperButton;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _bodyText;

    private readonly List<string> _lines = new();
    private readonly StringBuilder _shownText = new();

    private WeekFlowDialogueLogService _dialogueLogService;
    private string _logTitle = string.Empty;
    private int _nextLineIndex;

    public event Action ContinueRequested;

    public bool IsVisible => gameObject.activeInHierarchy;

    private void Awake()
    {
        BindPaperButton();
    }

    private void OnDestroy()
    {
        UnbindPaperButton();
    }

    public void SetDialogueLogService(WeekFlowDialogueLogService dialogueLogService)
    {
        _dialogueLogService = dialogueLogService;
    }

    public void Show(EndingPresentation presentation)
    {
        gameObject.SetActive(true);

        _logTitle = presentation.Title;
        _nextLineIndex = 0;
        _shownText.Clear();

        SetTitle(presentation.Title);
        BuildLines(presentation);
        RefreshBodyText();
        ResetScrollPosition();
        TryAdvance();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _lines.Clear();
        _shownText.Clear();
        _nextLineIndex = 0;
        _logTitle = string.Empty;
        RefreshBodyText();
    }

    public bool TryAdvance()
    {
        if (!IsVisible)
        {
            return false;
        }

        if (_nextLineIndex >= _lines.Count)
        {
            ContinueRequested?.Invoke();
            return true;
        }

        ShowNextLine();
        return true;
    }

    private void BindPaperButton()
    {
        if (_paperButton == null)
        {
            return;
        }

        _paperButton.onClick.AddListener(HandlePaperClicked);
    }

    private void UnbindPaperButton()
    {
        if (_paperButton == null)
        {
            return;
        }

        _paperButton.onClick.RemoveListener(HandlePaperClicked);
    }

    private void HandlePaperClicked()
    {
        TryAdvance();
    }

    private void SetTitle(string title)
    {
        if (_titleText == null)
        {
            return;
        }

        _titleText.text = title;
        _titleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
    }

    private void BuildLines(EndingPresentation presentation)
    {
        _lines.Clear();

        AddLine(presentation.Summary);
        AddDetailLines(presentation.DetailLines);
        AddLine(presentation.ReputationLine);
        AddLine(presentation.ClosingLine);
    }

    private void AddLines(IReadOnlyList<string> lines)
    {
        if (lines == null)
        {
            return;
        }

        for (int index = 0; index < lines.Count; index++)
        {
            AddLine(lines[index]);
        }
    }

    private void AddDetailLines(IReadOnlyList<string> lines)
    {
        if (lines == null)
        {
            return;
        }

        for (int index = 0; index < lines.Count; index++)
        {
            string line = index == 1 ? $"<i>{lines[index]}</i>" : lines[index];
            AddLine(line);
        }
    }

    private void AddLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _lines.Add(line.Trim());
    }

    private void ShowNextLine()
    {
        string line = _lines[_nextLineIndex];
        _nextLineIndex++;

        if (_shownText.Length > 0)
        {
            _shownText.AppendLine();
            _shownText.AppendLine();
        }

        _shownText.Append(line);
        RefreshBodyText(true);
        AppendLineToLog(line);
    }

    private void RefreshBodyText(bool scrollToBottom = false)
    {
        if (_bodyText == null)
        {
            return;
        }

        _bodyText.text = _shownText.ToString();

        if (scrollToBottom)
        {
            ScrollToBottom();
        }
    }

    private void ResetScrollPosition()
    {
        if (_scrollRect == null)
        {
            return;
        }

        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ScrollToBottom()
    {
        if (_scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        if (_scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
        }

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private void AppendLineToLog(string line)
    {
        if (_dialogueLogService == null)
        {
            return;
        }

        _dialogueLogService.Append(new DialogueLogEntry(
            EDialogueLogSource.Ending,
            _logTitle,
            string.Empty,
            line));
    }
}