using DG.Tweening;
using TMPro;
using UnityEngine;

public class UI_WeeklyResultLogEntryView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _contextText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _motionTarget;
    [SerializeField] private float _startYOffset = 18f;

    private Vector2 _baseAnchoredPosition;

    private void Awake()
    {
        ResolveReferences();
        _baseAnchoredPosition = _motionTarget != null ? _motionTarget.anchoredPosition : Vector2.zero;
    }

    public void Render(WeeklyResultLogEntryPresentation presentation)
    {
        SetText(_titleText, presentation.Title);
        SetText(_contextText, presentation.Context);
    }

    public void ShowInstant()
    {
        ResolveReferences();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_motionTarget != null)
        {
            _motionTarget.anchoredPosition = _baseAnchoredPosition;
            _motionTarget.localScale = Vector3.one;
        }
    }

    public Tween PlayShow(float duration)
    {
        ResolveReferences();
        ShowInstant();

        if (_canvasGroup == null && _motionTarget == null)
        {
            return DOVirtual.DelayedCall(0f, () => { });
        }

        Sequence sequence = DOTween.Sequence();
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            sequence.Join(_canvasGroup.DOFade(1f, duration));
        }

        if (_motionTarget != null)
        {
            _motionTarget.anchoredPosition = _baseAnchoredPosition + Vector2.down * _startYOffset;
            _motionTarget.localScale = Vector3.one * 0.96f;
            sequence.Join(_motionTarget.DOAnchorPos(_baseAnchoredPosition, duration).SetEase(Ease.OutCubic));
            sequence.Join(_motionTarget.DOScale(1f, duration).SetEase(Ease.OutBack));
        }

        return sequence;
    }

    private void ResolveReferences()
    {
        if (_canvasGroup == null)
        {
            TryGetComponent(out _canvasGroup);
        }

        if (_motionTarget == null)
        {
            _motionTarget = transform as RectTransform;
        }
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
