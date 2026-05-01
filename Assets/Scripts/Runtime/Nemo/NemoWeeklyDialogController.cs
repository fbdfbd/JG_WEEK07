using DG.Tweening;
using TMPro;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class NemoWeeklyDialogController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SO_WeeklyTalkCatalog _weeklyTalkCatalog;
    [SerializeField] private WeekFlowController _weekFlowController;

    [Header("Runtime")]
    [SerializeField] private NemoEntity _nemoEntity;
    [SerializeField] private UI_ChildStateToastManager _toastManager;

    [Header("Bubble")]
    [SerializeField] private UI_WeeklyDialogPanel _dialogPanel;
    [SerializeField] private TextMeshProUGUI _talkText;
    [SerializeField] private RectTransform _bubbleRect;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private Transform _bubbleAnchor;
    [SerializeField] private Vector3 _bubbleWorldOffset = new(0f, 1.8f, 0f);
    [SerializeField] private bool _followAnchor = true;
    [SerializeField] private float textDuration = 0.5f;

    [Header("Ambient Timing")]
    [SerializeField] private bool _autoTalkEnabled = true;
    [SerializeField] private float _minAutoDelaySeconds = 4f;
    [SerializeField] private float _maxAutoDelaySeconds = 8f;
    [SerializeField] private float _defaultDisplaySeconds = 3.5f;
    [SerializeField] private float _defaultCooldownSeconds = 4f;
    [SerializeField] private float _clickDisplaySeconds = 3.5f;

    [SerializeField] private List<ParticleSystem> _particle;

    private readonly Dictionary<string, int> _sequenceIndices = new(StringComparer.Ordinal);

    private string _weekId = string.Empty;
    private SO_WeeklyTalk _currentTalk;
    private WeeklyTalkEntryData _currentEntry;
    private Tween _typingTween;
    private Coroutine _autoTalkCoroutine;
    private Coroutine _hideCoroutine;
    private bool _isSpeaking;

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

        StopAutoTalkLoop();
    }

    private void Start()
    {
        ResolveReferences();
        Refresh();
        HideBubbleImmediate();
        StartAutoTalkLoop();
    }

    private void LateUpdate()
    {
        if (_followAnchor)
        {
            UpdateBubblePosition();
        }
    }

    private void OnDestroy()
    {
        StopTypingTween();
    }

    private void Refresh()
    {
        _weekId = _weekFlowController != null
            ? _weekFlowController.CurrentWeekDefinition?.Id ?? string.Empty
            : string.Empty;

        _currentTalk = _weeklyTalkCatalog != null
            ? _weeklyTalkCatalog.GetByWeekId(_weekId)
            : null;

        _sequenceIndices.Clear();
    }

    private void HandleWeekChanged(SO_WeekDefinition _)
    {
        Refresh();
    }

    public bool SpeakNow()
    {
        if (!CanSpeak())
        {
            return false;
        }

        WeeklyTalkEntryData entry = ResolveEntry(WeeklyTalkResolveMode.Click);
        if (entry == null)
        {
            return false;
        }

        Speak(entry, _clickDisplaySeconds > 0f ? _clickDisplaySeconds : ResolveDisplaySeconds(entry));
        return true;
    }

    public bool IsWeeklyDialogFinished()
    {
        return !_isSpeaking;
    }

    public void OnClickTalkButton()
    {
        SpeakNow();
    }

    public void OnClickeExitButton()
    {
        HideBubble();
        NemoEntity.Instance?.ResumeRoutine();
    }

    public void OnClickFlagButton()
    {
    }

    private IEnumerator AutoTalkLoop()
    {
        while (true)
        {
            float delay = UnityEngine.Random.Range(_minAutoDelaySeconds, _maxAutoDelaySeconds);
            yield return new WaitForSeconds(delay);

            if (!_autoTalkEnabled || !CanSpeak())
            {
                continue;
            }

            WeeklyTalkEntryData entry = ResolveEntry(WeeklyTalkResolveMode.Auto);
            if (entry == null)
            {
                continue;
            }

            Speak(entry, ResolveDisplaySeconds(entry));
            yield return new WaitForSeconds(ResolveDisplaySeconds(entry) + ResolveCooldownSeconds(entry));
        }
    }

    private WeeklyTalkEntryData ResolveEntry(WeeklyTalkResolveMode mode)
    {
        RuntimeChildState childState = _weekFlowController != null ? _weekFlowController.CurrentChildState : null;
        WeeklyTalkStatDirection direction = WeeklyTalkStatDirectionResolver.Resolve(childState);
        string sequenceKey = $"{mode}:{_weekId}:{direction}";
        _sequenceIndices.TryGetValue(sequenceKey, out int sequenceIndex);

        WeeklyTalkEntryData entry = NemoWeeklyTalkResolver.Resolve(_currentTalk, childState, mode, sequenceIndex);
        if (entry != null)
        {
            _sequenceIndices[sequenceKey] = sequenceIndex + 1;
        }

        return entry;
    }

    private void Speak(WeeklyTalkEntryData entry, float displaySeconds)
    {
        _currentEntry = entry;
        _isSpeaking = true;

        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }

        UpdateBubblePosition();
        SetBubbleText(entry.Context);
        _dialogPanel?.Show();
        PlayCurrentTalkParticle();
        ApplyCurrentTalkInteractions();
        _toastManager?.ShowQueuedToastsImmediately();

        _hideCoroutine = StartCoroutine(HideAfterDelay(displaySeconds));
    }

    private IEnumerator HideAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, seconds));
        HideBubble();
        _hideCoroutine = null;
    }

    private void SetBubbleText(string text)
    {
        _dialogPanel?.SetText(text);

        if (_talkText == null)
        {
            return;
        }

        StopTypingTween();
        _talkText.text = text ?? string.Empty;
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

    private void HideBubble()
    {
        StopTypingTween();
        _dialogPanel?.Hide();
        _isSpeaking = false;
        _currentEntry = null;
    }

    private void HideBubbleImmediate()
    {
        StopTypingTween();
        if (_dialogPanel != null)
        {
            _dialogPanel.gameObject.SetActive(false);
        }

        _isSpeaking = false;
        _currentEntry = null;
    }

    private bool CanSpeak()
    {
        ResolveReferences();

        if (_currentTalk == null)
        {
            Refresh();
        }

        return _currentTalk != null &&
               _dialogPanel != null &&
               IsNemoAvailableForAmbientTalk();
    }

    private bool IsNemoAvailableForAmbientTalk()
    {
        if (_nemoEntity == null)
        {
            return true;
        }

        return _nemoEntity.CurrentState == NemoEntity.NemoState.Routine;
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

    private void UpdateBubblePosition()
    {
        if (_bubbleRect == null || _canvasRect == null || _bubbleAnchor == null || Camera.main == null)
        {
            return;
        }

        Vector3 worldPosition = _bubbleAnchor.position + _bubbleWorldOffset;
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPosition,
                Camera.main,
                out Vector2 localPosition))
        {
            _bubbleRect.localPosition = localPosition;
        }
    }

    private float ResolveDisplaySeconds(WeeklyTalkEntryData entry)
    {
        return entry != null && entry.DisplaySeconds > 0f
            ? entry.DisplaySeconds
            : _defaultDisplaySeconds;
    }

    private float ResolveCooldownSeconds(WeeklyTalkEntryData entry)
    {
        return entry != null && entry.CooldownSeconds > 0f
            ? entry.CooldownSeconds
            : _defaultCooldownSeconds;
    }

    private int GetVisibleCharacterCount()
    {
        return _talkText != null ? _talkText.maxVisibleCharacters : 0;
    }

    private void ApplyVisibleCharacterCount(int visibleCharacterCount)
    {
        if (_talkText != null)
        {
            _talkText.maxVisibleCharacters = visibleCharacterCount;
        }
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

    private void StartAutoTalkLoop()
    {
        if (_autoTalkCoroutine == null)
        {
            _autoTalkCoroutine = StartCoroutine(AutoTalkLoop());
        }
    }

    private void StopAutoTalkLoop()
    {
        if (_autoTalkCoroutine != null)
        {
            StopCoroutine(_autoTalkCoroutine);
            _autoTalkCoroutine = null;
        }

        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
    }

    private void ResolveReferences()
    {
        if (_nemoEntity == null)
        {
            _nemoEntity = NemoEntity.Instance;
        }

        if (_toastManager == null)
        {
            _toastManager = FindAnyObjectByType<UI_ChildStateToastManager>();
        }

        if (_bubbleAnchor == null && _nemoEntity != null)
        {
            _bubbleAnchor = _nemoEntity.transform;
        }

        if (_bubbleRect == null && _dialogPanel != null)
        {
            _bubbleRect = _dialogPanel.GetComponent<RectTransform>();
        }
    }
}

public enum WeeklyTalkResolveMode
{
    Auto,
    Click
}

public static class NemoWeeklyTalkResolver
{
    public static WeeklyTalkEntryData Resolve(
        SO_WeeklyTalk talk,
        RuntimeChildState childState,
        WeeklyTalkResolveMode mode,
        int sequenceIndex)
    {
        if (talk == null)
        {
            return null;
        }

        WeeklyTalkStatDirection direction = WeeklyTalkStatDirectionResolver.Resolve(childState);
        WeeklyTalkEntryData[] directionEntries = GetAvailableEntries(talk, childState, mode)
            .Where(entry => entry.Direction == direction)
            .ToArray();

        WeeklyTalkEntryData selectedEntry = SelectBySequence(directionEntries, sequenceIndex);
        if (selectedEntry != null)
        {
            return selectedEntry;
        }

        WeeklyTalkEntryData fallbackEntry = SelectBySequence(GetAvailableEntries(talk, childState, mode).ToArray(), sequenceIndex);
        if (fallbackEntry != null)
        {
            return fallbackEntry;
        }

        return talk.CreateLegacyEntry();
    }

    public static WeeklyTalkEntryData Resolve(
        SO_WeeklyTalk talk,
        RuntimeChildState childState)
    {
        return Resolve(talk, childState, WeeklyTalkResolveMode.Click, 0);
    }

    private static IEnumerable<WeeklyTalkEntryData> GetAvailableEntries(
        SO_WeeklyTalk talk,
        RuntimeChildState childState,
        WeeklyTalkResolveMode mode)
    {
        return talk.Entries?
                   .Where(entry => entry != null &&
                                   entry.HasContent &&
                                   entry.IsAvailable(childState) &&
                                   IsAllowedForMode(entry, mode))
                   .OrderByDescending(entry => entry.Priority)
                   .ThenBy(entry => entry.VariantOrder)
               ?? Enumerable.Empty<WeeklyTalkEntryData>();
    }

    private static bool IsAllowedForMode(WeeklyTalkEntryData entry, WeeklyTalkResolveMode mode)
    {
        return mode switch
        {
            WeeklyTalkResolveMode.Auto => entry.AllowAuto,
            WeeklyTalkResolveMode.Click => entry.AllowClick,
            _ => true
        };
    }

    private static WeeklyTalkEntryData SelectBySequence(WeeklyTalkEntryData[] entries, int sequenceIndex)
    {
        if (entries == null || entries.Length == 0)
        {
            return null;
        }

        int index = Mathf.Abs(sequenceIndex) % entries.Length;
        return entries[index];
    }
}
