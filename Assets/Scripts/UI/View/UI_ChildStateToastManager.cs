using System.Collections.Generic;
using UnityEngine;

public enum EChildStateToastFlushMode
{
    Sequential,
    Burst,
    Clear
}

public class UI_ChildStateToastManager : MonoBehaviour
{
    [SerializeField] private WeekFlowController _weekFlowController;
    [SerializeField] private SO_WeekUiTextCatalog _weekUiTextCatalog;
    [SerializeField] private UI_ChildStateToastItem[] _toastItems;
    [SerializeField] private EChildStatusType[] _toastStatTypes =
    {
        EChildStatusType.Trust,
        EChildStatusType.Curiosity,
        EChildStatusType.Anxiety,
        EChildStatusType.Obedience,
    };
    [SerializeField] private float _burstSpeedMultiplier = 1.35f;
    [SerializeField] private EChildStateToastFlushMode _defaultFlowCompletedMode = EChildStateToastFlushMode.Sequential;
    [SerializeField] private EChildStateToastFlushMode _weekAdvancedMode = EChildStateToastFlushMode.Burst;
    [SerializeField] private EChildStateToastFlushMode _endingMode = EChildStateToastFlushMode.Burst;
    [SerializeField] private float _burstStaggerInterval = 0.06f;
    [SerializeField] private float _burstMinimumSpeedMultiplier = 2.75f;
    [SerializeField] private float _toastWaitTimeoutSeconds = 3f;

    private RuntimeChildState _childState;
    private int _nextToastIndex;
    private readonly Queue<string> _pendingToastMessages = new();
    private readonly Queue<string> _readyToastMessages = new();
    private WeekUiTextProvider _weekUiText;
    private Coroutine _toastSequenceCoroutine;
    private Coroutine _burstToastCoroutine;

    private void Awake()
    {
        if (_weekFlowController == null)
        {
            _weekFlowController = FindAnyObjectByType<WeekFlowController>();
        }

        _weekUiText = new WeekUiTextProvider(_weekUiTextCatalog);
    }

    private void OnEnable()
    {
        if (_weekFlowController == null)
        {
            return;
        }

        _weekFlowController.ChildStateSourceChanged += HandleChildStateSourceChanged;
        _weekFlowController.FlowPresentationCompletedWithContext += HandleFlowPresentationCompleted;
        BindChildState(_weekFlowController.CurrentChildState);
    }

    private void OnDisable()
    {
        if (_weekFlowController != null)
        {
            _weekFlowController.ChildStateSourceChanged -= HandleChildStateSourceChanged;
            _weekFlowController.FlowPresentationCompletedWithContext -= HandleFlowPresentationCompleted;
        }

        BindChildState(null);
        StopAllPlayback();
        _pendingToastMessages.Clear();
        _readyToastMessages.Clear();
    }

    private void HandleChildStateSourceChanged(RuntimeChildState childState)
    {
        BindChildState(childState);
    }

    private void BindChildState(RuntimeChildState childState)
    {
        if (ReferenceEquals(_childState, childState))
        {
            return;
        }

        if (_childState != null)
        {
            _childState.StatChanged -= HandleStatChanged;
        }

        _childState = childState;

        if (_childState != null)
        {
            _childState.StatChanged += HandleStatChanged;
        }
    }

    private void HandleStatChanged(StatChangeInfo changeInfo)
    {
        if (changeInfo.Delta == 0
            || !CanShowToast(changeInfo.StatType)
            || _toastItems == null
            || _toastItems.Length == 0)
        {
            return;
        }

        _pendingToastMessages.Enqueue(BuildStatMessage(changeInfo));
    }

    private bool CanShowToast(EChildStatusType statType)
    {
        if (_toastStatTypes == null || _toastStatTypes.Length == 0)
        {
            return true;
        }

        return System.Array.Exists(_toastStatTypes, type => type == statType);
    }

    private void HandleFlowPresentationCompleted(WeekFlowPresentationContext context)
    {
        ShowQueuedToasts(ResolveFlushMode(context));
    }

    public void ShowQueuedToastsImmediately()
    {
        ShowQueuedToasts(EChildStateToastFlushMode.Sequential);
    }

    public System.Collections.IEnumerator ShowQueuedToastsAndWait(EChildStateToastFlushMode flushMode)
    {
        ShowQueuedToasts(flushMode);

        float timeoutAt = Time.unscaledTime + Mathf.Max(0f, _toastWaitTimeoutSeconds);
        while ((_toastSequenceCoroutine != null || _burstToastCoroutine != null)
            && Time.unscaledTime < timeoutAt)
        {
            yield return null;
        }

        if (_toastSequenceCoroutine != null || _burstToastCoroutine != null)
        {
            StopAllPlayback();
        }
    }

    public void ShowQueuedToasts(EChildStateToastFlushMode flushMode)
    {
        while (_pendingToastMessages.Count > 0)
        {
            _readyToastMessages.Enqueue(_pendingToastMessages.Dequeue());
        }

        if (_readyToastMessages.Count == 0)
        {
            return;
        }

        if (flushMode == EChildStateToastFlushMode.Clear)
        {
            StopAllPlayback();
            _readyToastMessages.Clear();
            return;
        }

        if (flushMode == EChildStateToastFlushMode.Burst)
        {
            StartBurstSequence();
            return;
        }

        if (_toastSequenceCoroutine == null && _burstToastCoroutine == null)
        {
            _toastSequenceCoroutine = StartCoroutine(ProcessToastSequence());
        }
    }

    private EChildStateToastFlushMode ResolveFlushMode(WeekFlowPresentationContext context)
    {
        if (context.NextScreen != null)
        {
            switch (context.NextScreen.ScreenType)
            {
                case EWeekFlowScreenType.WeeklyResultLog:
                    return EChildStateToastFlushMode.Clear;

                case EWeekFlowScreenType.WeeklyStatResult:
                    return EChildStateToastFlushMode.Clear;

                case EWeekFlowScreenType.Ending:
                case EWeekFlowScreenType.EndingFollowUp:
                    return _endingMode;
            }
        }

        if (context.DidChangeWeek && context.DidClearScreen)
        {
            return _weekAdvancedMode;
        }

        return _defaultFlowCompletedMode;
    }

    private System.Collections.IEnumerator ProcessToastSequence()
    {
        while (_readyToastMessages.Count > 0)
        {
            string message = _readyToastMessages.Dequeue();
            bool isToastCompleted = false;

            if (!PlayToast(message, () => isToastCompleted = true))
            {
                continue;
            }

            yield return new WaitUntil(() => isToastCompleted);
        }

        _toastSequenceCoroutine = null;
    }

    private void StartBurstSequence()
    {
        StopAllPlayback();
        _burstToastCoroutine = StartCoroutine(ProcessBurstSequence());
    }

    private System.Collections.IEnumerator ProcessBurstSequence()
    {
        while (_readyToastMessages.Count > 0)
        {
            string message = _readyToastMessages.Dequeue();
            if (PlayToast(message, null, true))
            {
                yield return new WaitForSeconds(Mathf.Max(0f, _burstStaggerInterval));
            }
        }

        yield return new WaitForSeconds(GetBurstCompletionDelay());
        _burstToastCoroutine = null;
    }

    private bool PlayToast(string message, System.Action onCompleted)
    {
        return PlayToast(message, onCompleted, false);
    }

    private bool PlayToast(string message, System.Action onCompleted, bool forceBurstProfile)
    {
        if (string.IsNullOrWhiteSpace(message) || !TryGetNextToastItem(out UI_ChildStateToastItem toastItem))
        {
            return false;
        }

        UI_ChildStateToastItem.PlaybackProfile playbackProfile = ResolvePlaybackProfile(toastItem, forceBurstProfile);
        toastItem.Play(message, playbackProfile, onCompleted);
        return true;
    }

    private UI_ChildStateToastItem.PlaybackProfile ResolvePlaybackProfile(UI_ChildStateToastItem toastItem, bool forceBurstProfile)
    {
        UI_ChildStateToastItem.PlaybackProfile defaultProfile = toastItem.GetDefaultPlaybackProfile();
        if (!forceBurstProfile && (_readyToastMessages.Count == 0 || _burstSpeedMultiplier <= 1f))
        {
            return defaultProfile;
        }

        float speedMultiplier = forceBurstProfile
            ? Mathf.Max(_burstSpeedMultiplier, _burstMinimumSpeedMultiplier)
            : _burstSpeedMultiplier;
        return new UI_ChildStateToastItem.PlaybackProfile(
            defaultProfile.FadeInDuration / speedMultiplier,
            defaultProfile.MoveDuration / speedMultiplier,
            defaultProfile.MoveDistance);
    }

    private float GetBurstCompletionDelay()
    {
        if (_toastItems == null || _toastItems.Length == 0)
        {
            return 0f;
        }

        for (int i = 0; i < _toastItems.Length; i++)
        {
            UI_ChildStateToastItem toastItem = _toastItems[i];
            if (toastItem == null)
            {
                continue;
            }

            UI_ChildStateToastItem.PlaybackProfile profile = ResolvePlaybackProfile(toastItem, true);
            return Mathf.Max(0f, profile.FadeInDuration + profile.MoveDuration + profile.MoveDuration);
        }

        return 0f;
    }

    private bool TryGetNextToastItem(out UI_ChildStateToastItem toastItem)
    {
        toastItem = null;

        if (_toastItems == null || _toastItems.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < _toastItems.Length; i++)
        {
            UI_ChildStateToastItem candidate = _toastItems[_nextToastIndex % _toastItems.Length];
            _nextToastIndex++;

            if (candidate == null)
            {
                continue;
            }

            toastItem = candidate;
            return true;
        }

        return false;
    }

    private void StopAllPlayback()
    {
        StopToastSequence();
        StopBurstSequence();
        HideToastItems();
    }

    private void StopToastSequence()
    {
        if (_toastSequenceCoroutine == null)
        {
            return;
        }

        StopCoroutine(_toastSequenceCoroutine);
        _toastSequenceCoroutine = null;
    }

    private void StopBurstSequence()
    {
        if (_burstToastCoroutine == null)
        {
            return;
        }

        StopCoroutine(_burstToastCoroutine);
        _burstToastCoroutine = null;
    }

    private void HideToastItems()
    {
        if (_toastItems == null)
        {
            return;
        }

        for (int i = 0; i < _toastItems.Length; i++)
        {
            _toastItems[i]?.StopAndHide();
        }
    }

    private string BuildStatMessage(StatChangeInfo changeInfo)
    {
        if (!string.IsNullOrWhiteSpace(changeInfo.ToastMessage))
        {
            return changeInfo.ToastMessage;
        }

        string label = _weekUiText != null
            ? _weekUiText.GetStatLabel(changeInfo.StatType)
            : changeInfo.StatType.ToString();
        string sign = changeInfo.Delta > 0 ? "+" : string.Empty;
        return $"{label} {sign}{changeInfo.Delta}";
    }
}
