using System.Collections.Generic;
using UnityEngine;

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

    private RuntimeChildState _childState;
    private int _nextToastIndex;
    private readonly Queue<string> _pendingToastMessages = new();
    private readonly Queue<string> _readyToastMessages = new();
    private WeekUiTextProvider _weekUiText;
    private Coroutine _toastSequenceCoroutine;

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
        _weekFlowController.FlowPresentationCompleted += HandleFlowPresentationCompleted;
        BindChildState(_weekFlowController.CurrentChildState);
    }

    private void OnDisable()
    {
        if (_weekFlowController != null)
        {
            _weekFlowController.ChildStateSourceChanged -= HandleChildStateSourceChanged;
            _weekFlowController.FlowPresentationCompleted -= HandleFlowPresentationCompleted;
        }

        BindChildState(null);
        StopToastSequence();
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

    private void HandleFlowPresentationCompleted()
    {
        ShowQueuedToastsImmediately();
    }

    public void ShowQueuedToastsImmediately()
    {
        while (_pendingToastMessages.Count > 0)
        {
            _readyToastMessages.Enqueue(_pendingToastMessages.Dequeue());
        }

        if (_toastSequenceCoroutine == null && _readyToastMessages.Count > 0)
        {
            _toastSequenceCoroutine = StartCoroutine(ProcessToastSequence());
        }
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

    private bool PlayToast(string message, System.Action onCompleted)
    {
        if (string.IsNullOrWhiteSpace(message) || !TryGetNextToastItem(out UI_ChildStateToastItem toastItem))
        {
            return false;
        }

        UI_ChildStateToastItem.PlaybackProfile playbackProfile = ResolvePlaybackProfile(toastItem);
        toastItem.Play(message, playbackProfile, onCompleted);
        return true;
    }

    private UI_ChildStateToastItem.PlaybackProfile ResolvePlaybackProfile(UI_ChildStateToastItem toastItem)
    {
        UI_ChildStateToastItem.PlaybackProfile defaultProfile = toastItem.GetDefaultPlaybackProfile();
        if (_readyToastMessages.Count == 0 || _burstSpeedMultiplier <= 1f)
        {
            return defaultProfile;
        }

        float speedMultiplier = _burstSpeedMultiplier;
        return new UI_ChildStateToastItem.PlaybackProfile(
            defaultProfile.FadeInDuration / speedMultiplier,
            defaultProfile.MoveDuration / speedMultiplier,
            defaultProfile.MoveDistance);
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

    private void StopToastSequence()
    {
        if (_toastSequenceCoroutine == null)
        {
            return;
        }

        StopCoroutine(_toastSequenceCoroutine);
        _toastSequenceCoroutine = null;
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
