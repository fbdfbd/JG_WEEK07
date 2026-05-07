using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeekEntryIntroOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _contextText;
    [SerializeField] private Button _clickArea;
    [SerializeField] private float _closeSecond = 3.5f;


    private bool _isClicked;
    private Tween _fadeTween;

    private void Awake()
    {
        HideImmediate();
    }

    private void OnDisable()
    {
        RemoveClickListener();
        StopFade(false);
    }

    private void OnDestroy()
    {
        RemoveClickListener();
        StopFade(false);
    }

    public IEnumerator Play(WeekEntryIntroEntry entry, string contextLine = null)
    {
        if (entry == null || _canvasGroup == null)
        {
            yield break;
        }

        gameObject.SetActive(true);
        SetTitle(entry.Title);
        SetContext(contextLine);
        SetClicked(false);
        SetClickEnabled(false);

        yield return FadeTo(1f, entry.FadeInSeconds);

        yield return new WaitForSecondsRealtime(_closeSecond);

        yield return FadeTo(0f, entry.FadeOutSeconds);
        HideImmediate();


        yield return FadeTo(0f, entry.FadeOutSeconds);
        HideImmediate();
    }

    private void SetTitle(string title)
    {
        if (_titleText == null)
        {
            return;
        }

        _titleText.text = title;
    }

    private void SetContext(string contextLine)
    {
        if (_contextText == null)
        {
            return;
        }

        _contextText.text = string.IsNullOrWhiteSpace(contextLine)
            ? string.Empty
            : contextLine.Trim();
    }

    private IEnumerator FadeTo(float alpha, float seconds)
    {
        StopFade(false);

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        _fadeTween = _canvasGroup
            .DOFade(alpha, seconds)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        yield return _fadeTween.WaitForCompletion();
        _fadeTween = null;
    }

    private void HandleClick()
    {
        SetClicked(true);
    }

    private void SetClicked(bool isClicked)
    {
        _isClicked = isClicked;
    }

    private void SetClickEnabled(bool isEnabled)
    {
        if (_clickArea == null)
        {
            return;
        }

        RemoveClickListener();
        _clickArea.interactable = isEnabled;

        if (isEnabled)
        {
            _clickArea.onClick.AddListener(HandleClick);
        }
    }

    private void RemoveClickListener()
    {
        if (_clickArea == null)
        {
            return;
        }

        _clickArea.onClick.RemoveListener(HandleClick);
    }

    private void HideImmediate()
    {
        StopFade(false);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    private void StopFade(bool complete)
    {
        if (_fadeTween == null)
        {
            return;
        }

        if (_fadeTween.IsActive())
        {
            _fadeTween.Kill(complete);
        }

        _fadeTween = null;
    }
}
