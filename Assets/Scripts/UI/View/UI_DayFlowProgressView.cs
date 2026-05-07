
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


public sealed class UI_DayFlowProgressView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _contentRoot;

    [Header("Progress")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private RectTransform _indicator;
    [SerializeField] private RectTransform[] _dotSlots = System.Array.Empty<RectTransform>();

    [Header("Animation")]
    [SerializeField] private bool _animateFirstRender;
    [SerializeField] private float _moveDuration = 0.35f;
    [SerializeField] private float _fillDuration = 0.35f;
    [SerializeField] private Ease _moveEase = Ease.OutCubic;
    [SerializeField] private Ease _fillEase = Ease.OutCubic;

    private Sequence _sequence;
    private int _lastTotalCount = -1;
    private int _lastCurrentIndex = -1;
    private bool _hasRenderedActiveProgress;

    private void Awake()
    {
        ResolveReferences();
        HideInstant();
    }

    private void OnDestroy()
    {
        KillSequence(false);
    }

    public void Render(DayFlowProgressSnapshot progress)
    {
        ResolveReferences();

        int visibleCount = GetVisibleCount(progress.TotalCount);
        if (!progress.IsActive || visibleCount <= 0)
        {
            HideInstant();
            return;
        }

        bool layoutChanged = visibleCount != _lastTotalCount;
        SetVisible(true);
        RenderDots(visibleCount);

        int targetIndex = Mathf.Clamp(progress.CurrentIndex, 0, visibleCount - 1);
        Vector2 targetPosition = GetIndicatorPosition(targetIndex);
        float targetFillAmount = ResolveFillAmount(progress, visibleCount);
        bool shouldAnimate = _hasRenderedActiveProgress || _animateFirstRender;

        if (layoutChanged || !shouldAnimate)
        {
            KillSequence(false);
            SetIndicatorPosition(targetPosition);
            SetFillAmount(targetFillAmount);
        }
        else if (targetIndex != _lastCurrentIndex)
        {
            PlayProgressTween(targetPosition, targetFillAmount);
        }
        else
        {
            SetFillAmount(targetFillAmount);
        }

        _lastTotalCount = visibleCount;
        _lastCurrentIndex = targetIndex;
        _hasRenderedActiveProgress = true;
    }

    private int GetVisibleCount(int requestedCount)
    {
        if (_dotSlots == null || _dotSlots.Length == 0)
        {
            return 0;
        }

        return Mathf.Clamp(requestedCount, 0, _dotSlots.Length);
    }

    private void RenderDots(int visibleCount)
    {
        if (_dotSlots == null)
        {
            return;
        }

        for (int index = 0; index < _dotSlots.Length; index++)
        {
            RectTransform dotSlot = _dotSlots[index];
            if (dotSlot != null)
            {
                dotSlot.gameObject.SetActive(index < visibleCount);
            }
        }
    }

    private void PlayProgressTween(Vector2 targetPosition, float targetFillAmount)
    {
        KillSequence(false);

        _sequence = DOTween.Sequence();
        if (_indicator != null)
        {
            _sequence.Join(_indicator
                .DOAnchorPos(targetPosition, Mathf.Max(0f, _moveDuration))
                .SetEase(_moveEase));
        }

        if (_fillImage != null)
        {
            _sequence.Join(_fillImage
                .DOFillAmount(targetFillAmount, Mathf.Max(0f, _fillDuration))
                .SetEase(_fillEase));
        }
    }

    private Vector2 GetIndicatorPosition(int dotIndex)
    {
        RectTransform dotSlot = _dotSlots[dotIndex];
        if (_indicator == null || dotSlot == null)
        {
            return Vector2.zero;
        }

        if (_indicator.parent == dotSlot.parent)
        {
            return dotSlot.anchoredPosition;
        }

        RectTransform indicatorParent = _indicator.parent as RectTransform;
        if (indicatorParent == null)
        {
            return dotSlot.anchoredPosition;
        }

        Vector3 localPosition = indicatorParent.InverseTransformPoint(dotSlot.position);
        return new Vector2(localPosition.x, localPosition.y);
    }

    private float ResolveFillAmount(DayFlowProgressSnapshot progress, int visibleCount)
    {
        if (visibleCount <= 0)
        {
            return 0f;
        }

        int currentIndex = Mathf.Clamp(progress.CurrentIndex, 0, visibleCount - 1);
        return Mathf.Clamp01((currentIndex + 1f) / visibleCount);
    }

    private void SetIndicatorPosition(Vector2 position)
    {
        if (_indicator != null)
        {
            _indicator.anchoredPosition = position;
        }
    }

    private void SetFillAmount(float fillAmount)
    {
        if (_fillImage != null)
        {
            _fillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_contentRoot != null)
        {
            _contentRoot.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_indicator != null)
        {
            _indicator.gameObject.SetActive(visible);
        }
    }

    private void HideInstant()
    {
        KillSequence(false);
        RenderDots(0);
        SetFillAmount(0f);
        SetVisible(false);
        _lastTotalCount = -1;
        _lastCurrentIndex = -1;
        _hasRenderedActiveProgress = false;
    }

    private void KillSequence(bool complete)
    {
        if (_sequence == null)
        {
            return;
        }

        _sequence.Kill(complete);
        _sequence = null;
    }

    private void ResolveReferences()
    {
        if (_canvasGroup == null)
        {
            TryGetComponent(out _canvasGroup);
        }
    }
}

