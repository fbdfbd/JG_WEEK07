using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UI_ChildStateToastItem : MonoBehaviour
{
    [Serializable]
    public struct PlaybackProfile
    {
        public float FadeInDuration;
        public float MoveDuration;
        public float MoveDistance;

        public PlaybackProfile(float fadeInDuration, float moveDuration, float moveDistance)
        {
            FadeInDuration = fadeInDuration;
            MoveDuration = moveDuration;
            MoveDistance = moveDistance;
        }
    }

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private float _fadeInDuration = 0.12f;
    [SerializeField] private float _moveDuration = 0.55f;
    [SerializeField] private float _moveDistance = 50f;

    private Tween _playingTween;
    private Vector2 _defaultPosition;
    private bool _initialized;

    private void Awake()
    {
        CacheReferences();
        HideImmediate();
    }

    private void OnDestroy()
    {
        StopCurrentTween();
    }

    public void Play(string message, Action onCompleted = null)
    {
        Play(message, GetDefaultPlaybackProfile(), onCompleted);
    }

    public void Play(string message, PlaybackProfile playbackProfile, Action onCompleted = null)
    {
        CacheReferences();
        StopCurrentTween();

        if (_messageText != null)
        {
            _messageText.text = message;
        }

        gameObject.SetActive(true);

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _defaultPosition;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        Sequence sequence = DOTween.Sequence();

        if (_canvasGroup != null)
        {
            sequence.Append(_canvasGroup.DOFade(1f, playbackProfile.FadeInDuration));
        }

        if (_rectTransform != null)
        {
            sequence.Join(_rectTransform.DOAnchorPosY(
                _defaultPosition.y + playbackProfile.MoveDistance,
                playbackProfile.MoveDuration).SetEase(Ease.OutQuad));
        }

        if (_canvasGroup != null)
        {
            sequence.Append(_canvasGroup.DOFade(0f, playbackProfile.MoveDuration).SetEase(Ease.InQuad));
        }

        sequence.OnComplete(() =>
        {
            HideImmediate();
            onCompleted?.Invoke();
        });
        _playingTween = sequence;
    }

    public PlaybackProfile GetDefaultPlaybackProfile()
    {
        return new PlaybackProfile(_fadeInDuration, _moveDuration, _moveDistance);
    }

    public void StopAndHide()
    {
        CacheReferences();
        StopCurrentTween();
        HideImmediate();
    }

    private void CacheReferences()
    {
        if (_initialized)
        {
            return;
        }

        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_rectTransform != null)
        {
            _defaultPosition = _rectTransform.anchoredPosition;
        }

        _initialized = true;
    }

    private void HideImmediate()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _defaultPosition;
        }

        gameObject.SetActive(false);
        _playingTween = null;
    }

    private void StopCurrentTween()
    {
        if (_playingTween == null)
        {
            return;
        }

        if (_playingTween.IsActive())
        {
            _playingTween.Kill();
        }

        _playingTween = null;
    }
}
