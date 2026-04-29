using DG.Tweening;
using UnityEngine;

public class UI_WeekFlowTransitionPlayer : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _transitionTarget;
    [SerializeField] private CanvasGroup _inputBlocker;
    [SerializeField] private Vector2 _slideOffset = new(80f, 0f);
    [SerializeField] private float _punchStrength = 0.12f;

    private Tween _activeTween;
    private Vector2 _defaultAnchoredPosition;
    private Vector3 _defaultScale;
    private bool _isInitialized;

    public bool IsPlaying => _activeTween != null && _activeTween.IsActive() && _activeTween.IsPlaying();

    private void Awake()
    {
        CacheDefaultValues();
        SetInputBlocked(false);
    }

    private void OnDestroy()
    {
        StopActiveTween(false);
    }

    public System.Collections.IEnumerator Play(WeekFlowTransitionContext context)
    {
        if (context == null || context.Cue == null || context.Cue.Style == EWeekFlowCueStyle.None)
        {
            yield break;
        }

        CacheDefaultValues();
        StopActiveTween(false);

        if (context.Cue.Duration <= 0f)
        {
            FinalizeTransitionState(context);
            SetInputBlocked(false);
            yield break;
        }

        SetInputBlocked(context.Cue.BlockInput);

        _activeTween = BuildTween(context);
        if (_activeTween == null)
        {
            SetInputBlocked(false);
            yield break;
        }

        while (IsPlaying)
        {
            yield return null;
        }

        FinalizeTransitionState(context);
        StopActiveTween(false);
        SetInputBlocked(false);
    }

    public bool TrySkipCurrent()
    {
        if (!IsPlaying)
        {
            return false;
        }

        StopActiveTween(true);
        SetInputBlocked(false);
        return true;
    }

    private void CacheDefaultValues()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_transitionTarget != null)
        {
            _defaultAnchoredPosition = _transitionTarget.anchoredPosition;
            _defaultScale = _transitionTarget.localScale;
        }

        _isInitialized = true;
    }

    private Tween BuildTween(WeekFlowTransitionContext context)
    {
        switch (context.Cue.Style)
        {
            case EWeekFlowCueStyle.Fade:
                return BuildFadeTween(context);

            case EWeekFlowCueStyle.Slide:
                return BuildSlideTween(context);

            case EWeekFlowCueStyle.Punch:
                return BuildPunchTween(context);

            case EWeekFlowCueStyle.Typewriter:
                return BuildFadeTween(context);

            case EWeekFlowCueStyle.Custom:
                return BuildFadeTween(context);
        }

        return null;
    }

    private Tween BuildFadeTween(WeekFlowTransitionContext context)
    {
        if (_canvasGroup == null)
        {
            return null;
        }

        float startAlpha = context.Phase == EWeekFlowTransitionPhase.Enter ? 0f : 1f;
        float endAlpha = context.Phase == EWeekFlowTransitionPhase.Enter ? 1f : 0f;

        _canvasGroup.alpha = startAlpha;
        return _canvasGroup.DOFade(endAlpha, context.Cue.Duration).SetEase(context.Cue.Ease);
    }

    private Tween BuildSlideTween(WeekFlowTransitionContext context)
    {
        if (_transitionTarget == null)
        {
            return null;
        }

        Vector2 startPosition = context.Phase == EWeekFlowTransitionPhase.Enter
            ? _defaultAnchoredPosition + _slideOffset
            : _defaultAnchoredPosition;

        Vector2 endPosition = context.Phase == EWeekFlowTransitionPhase.Enter
            ? _defaultAnchoredPosition
            : _defaultAnchoredPosition - _slideOffset;

        _transitionTarget.anchoredPosition = startPosition;
        return _transitionTarget.DOAnchorPos(endPosition, context.Cue.Duration).SetEase(context.Cue.Ease);
    }

    private Tween BuildPunchTween(WeekFlowTransitionContext context)
    {
        if (_transitionTarget == null)
        {
            return null;
        }

        _transitionTarget.localScale = _defaultScale;

        if (context.Phase == EWeekFlowTransitionPhase.Exit)
        {
            return _transitionTarget
                .DOScale(_defaultScale * (1f - _punchStrength), context.Cue.Duration)
                .SetEase(context.Cue.Ease);
        }

        Vector3 punch = Vector3.one * _punchStrength;
        return _transitionTarget.DOPunchScale(punch, context.Cue.Duration, 1, 0.5f);
    }

    private void FinalizeTransitionState(WeekFlowTransitionContext context)
    {
        if (_canvasGroup != null && context.Cue.Style == EWeekFlowCueStyle.Fade)
        {
            _canvasGroup.alpha = context.Phase == EWeekFlowTransitionPhase.Enter ? 1f : 0f;
        }

        if (_transitionTarget == null)
        {
            return;
        }

        _transitionTarget.anchoredPosition = _defaultAnchoredPosition;
        _transitionTarget.localScale = _defaultScale;
    }

    private void StopActiveTween(bool complete)
    {
        if (_activeTween == null)
        {
            return;
        }

        if (_activeTween.IsActive())
        {
            _activeTween.Kill(complete);
        }

        _activeTween = null;
    }

    private void SetInputBlocked(bool shouldBlock)
    {
        if (_inputBlocker == null)
        {
            return;
        }

        _inputBlocker.alpha = 0f;
        _inputBlocker.blocksRaycasts = shouldBlock;
        _inputBlocker.interactable = shouldBlock;
    }
}
