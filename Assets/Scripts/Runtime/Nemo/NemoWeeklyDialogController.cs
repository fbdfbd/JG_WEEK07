using DG.Tweening;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class NemoWeeklyDialogController : MonoBehaviour
{
    [SerializeField] private SO_WeeklyTalkCatalog _weeklyTalkCatalog;
    [SerializeField] private WeekFlowController _weekFlowController;
    [SerializeField] private UI_ChildStateToastManager _toastManager;

    [SerializeField] private UI_WeeklyDialogPanel _dialogPanel;
    [SerializeField] private GameObject _interactionPanel;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private TextMeshProUGUI _talkText; 
    [SerializeField] private float textDuration = 0.5f;

    [SerializeField] private List<ParticleSystem> _particle;

    private bool _weeklyDialogFinised = true;
    private string _previousWeekId = string.Empty;
    private string _weekId = string.Empty;
    private SO_WeeklyTalk _currentTalk;
    private WeeklyTalkEntryData _currentEntry;
    private Tween _typingTween;

    private void OnEnable()
    {
        if (_weekFlowController != null)
        {
            _weekFlowController.WeekChanged += HandleWeekChanged;
        }
    }

    private void OnDisable()
    {
        if (_weekFlowController != null)
        {
            _weekFlowController.WeekChanged -= HandleWeekChanged;
        }
    }

    private void Start()
    {
        ResolveToastManager();
        Refresh();
    }

    private void OnDestroy()
    {
        StopTypingTween();
    }

    private void Refresh()
    {
        string nextWeekId = _weekFlowController != null
            ? _weekFlowController.CurrentWeekDefinition?.Id ?? string.Empty
            : string.Empty;

        bool hasWeekChanged = !string.Equals(_previousWeekId, nextWeekId, StringComparison.Ordinal);

        _weekId = nextWeekId;

        if (hasWeekChanged)
        {
            _weeklyDialogFinised = true;
        }

        _currentTalk = _weeklyTalkCatalog != null
            ? _weeklyTalkCatalog.GetByWeekId(_weekId)
            : null;
        _currentEntry = ResolveCurrentEntry();

        _previousWeekId = _weekId;
    }

    private void TextRefresh()
    {
        if (_talkText == null)
        {
            return;
        }

        StopTypingTween();

        if (_currentEntry == null || string.IsNullOrWhiteSpace(_currentEntry.Context))
        {
            _talkText.text = string.Empty;
            return;
        }

        _talkText.text = _currentEntry.Context;
        _talkText.maxVisibleCharacters = 0;
        _talkText.ForceMeshUpdate();

        int characterCount = _talkText.textInfo.characterCount;
        if (characterCount <= 0)
        {
            _talkText.maxVisibleCharacters = int.MaxValue;
            return;
        }

        _typingTween = DOTween.To(
                GetVisibleCharacterCount,
                ApplyVisibleCharacterCount,
                characterCount,
                textDuration)
            .SetEase(Ease.Linear)
            .OnComplete(HandleTypingCompleted);
    }

    private void HandleWeekChanged(SO_WeekDefinition _)
    {
        Refresh();
    }

    private void PlayCurrentTalkParticle()
    {
        if (_currentEntry == null || _particle == null)
        {
            return;
        }

        int particleIndex = (int)_currentEntry.NemoState;
        if (particleIndex < 0 || particleIndex >= _particle.Count)
        {
            return;
        }

        ParticleSystem targetParticle = _particle[particleIndex];
        if (targetParticle == null)
        {
            return;
        }

        targetParticle.Play();
    }

    private void ApplyCurrentTalkInteractions()
    {
        if (_currentEntry == null)
        {
            return;
        }

        RuntimeChildState childState = _weekFlowController != null ? _weekFlowController.CurrentChildState : null;
        if (childState == null)
        {
            return;
        }

        GameplayInteractionExecutor.ApplyAll(_currentEntry.Interactions, childState);
    }

    private WeeklyTalkEntryData ResolveCurrentEntry()
    {
        RuntimeChildState childState = _weekFlowController != null ? _weekFlowController.CurrentChildState : null;
        return NemoWeeklyTalkResolver.Resolve(_currentTalk, childState);
    }

    private int GetVisibleCharacterCount()
    {
        return _talkText != null ? _talkText.maxVisibleCharacters : 0;
    }

    private void ApplyVisibleCharacterCount(int visibleCharacterCount)
    {
        if (_talkText == null)
        {
            return;
        }

        _talkText.maxVisibleCharacters = visibleCharacterCount;
    }

    private void HandleTypingCompleted()
    {
        ApplyVisibleCharacterCount(int.MaxValue);
        _typingTween = null;
    }

    private void StopTypingTween()
    {
        if (_typingTween == null)
        {
            return;
        }

        if (_typingTween.IsActive())
        {
            _typingTween.Kill();
        }

        _typingTween = null;
    }

    public bool IsWeeklyDialogFinished()
    {
        return _weeklyDialogFinised;
    }

    public void OnClickTalkButton()
    {
        if (_weeklyDialogFinised)
        {
            _currentEntry = ResolveCurrentEntry();
            _dialogPanel.Show();
            PlayCurrentTalkParticle();
            TextRefresh();
            ApplyCurrentTalkInteractions();
            _toastManager?.ShowQueuedToastsImmediately();
            _weeklyDialogFinised = false;
        }

        _interactionPanel.SetActive(false);
    }

    public void OnClickeExitButton()
    {
        NemoEntity.Instance.ResumeRoutine();
        _dialogPanel.Hide();
    }
    public void OnClickFlagButton()
    {

    }

    private void ResolveToastManager()
    {
        if (_toastManager != null)
        {
            return;
        }

        _toastManager = FindAnyObjectByType<UI_ChildStateToastManager>();
    }
}

public static class NemoWeeklyTalkResolver
{
    public static WeeklyTalkEntryData Resolve(
        SO_WeeklyTalk talk,
        RuntimeChildState childState)
    {
        if (talk == null)
        {
            return null;
        }

        WeeklyTalkEntryData matchedEntry = talk.Entries?
            .Where(entry => entry != null && entry.HasContent && entry.IsAvailable(childState))
            .OrderByDescending(entry => entry.Priority)
            .FirstOrDefault();

        if (matchedEntry != null)
        {
            return matchedEntry;
        }

        return talk.CreateLegacyEntry();
    }
}
