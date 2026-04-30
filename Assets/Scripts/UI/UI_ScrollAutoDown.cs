using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_ScrollAutoDown : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentRect;

    private Coroutine _heightWatchRoutine;
    private float _previousContentHeight;

    private void OnEnable()
    {
        StopHeightWatchRoutine();
        _heightWatchRoutine = StartCoroutine(WatchContentHeightAndScrollDown());
    }

    private void OnDisable()
    {
        StopHeightWatchRoutine();
    }

    private IEnumerator WatchContentHeightAndScrollDown()
    {
        // 활성화 / Instantiate 직후 레이아웃 반영 대기
        yield return null;

        ForceUpdateLayout();
        ScrollToBottom();

        _previousContentHeight = GetContentHeight();

        while (true)
        {
            yield return null;

            ForceUpdateLayout();

            float currentContentHeight = GetContentHeight();

            if (!Mathf.Approximately(currentContentHeight, _previousContentHeight))
            {
                _previousContentHeight = currentContentHeight;

                yield return null;

                ForceUpdateLayout();
                ScrollToBottom();
            }
        }
    }

    private float GetContentHeight()
    {
        return _contentRect.rect.height;
    }

    private void ForceUpdateLayout()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
        Canvas.ForceUpdateCanvases();
    }

    private void ScrollToBottom()
    {
        _scrollRect.StopMovement();
        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private void StopHeightWatchRoutine()
    {
        if (_heightWatchRoutine == null)
            return;

        StopCoroutine(_heightWatchRoutine);
        _heightWatchRoutine = null;
    }
}