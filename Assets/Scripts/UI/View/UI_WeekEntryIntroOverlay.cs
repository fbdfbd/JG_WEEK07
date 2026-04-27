using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeekEntryIntroOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Button _clickArea;

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

    public IEnumerator Play(WeekEntryIntroEntry entry)
    {
        if (entry == null || _canvasGroup == null)
        {
            yield break;
        }

        gameObject.SetActive(true);
        SetTitle(entry.Title);
        SetClicked(false);
        SetClickEnabled(false);

        yield return FadeTo(1f, entry.FadeInSeconds);

        if (entry.WaitForClick)
        {
            SetClickEnabled(true);
            yield return new WaitUntil(() => _isClicked);
            SetClickEnabled(false);
        }

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
