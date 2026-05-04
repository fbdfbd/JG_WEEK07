using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UI_WeeklyResultLogSummaryView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _summaryText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _motionTarget;
    [SerializeField] private float _startYOffset = 18f;

    private Vector2 _baseAnchoredPosition;

    private void Awake()
    {
        ResolveReferences();
        _baseAnchoredPosition = _motionTarget != null ? _motionTarget.anchoredPosition : Vector2.zero;
    }

    public void Render(IReadOnlyList<WeeklyResultStatDeltaPresentation> statSummary)
    {
        if (_summaryText != null)
        {
            _summaryText.text = BuildSummaryText(statSummary);
        }
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

    private static string BuildSummaryText(IReadOnlyList<WeeklyResultStatDeltaPresentation> statSummary)
    {
        if (statSummary == null || statSummary.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        for (int index = 0; index < statSummary.Count; index++)
        {
            WeeklyResultStatDeltaPresentation entry = statSummary[index];
            if (entry.Delta == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(" / ");
            }

            builder.Append(entry.Label);
            builder.Append(" +");
            builder.Append(Mathf.Abs(entry.Delta));
        }

        return builder.ToString();
    }
}
