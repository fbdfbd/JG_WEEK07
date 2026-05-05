using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public sealed class UI_TabSlideImage : MonoBehaviour
{
    private float _closedX = 2362f;
    private float _openedX = 1470f;
    [SerializeField] private float _duration = 0.45f;
    [SerializeField] private Ease _ease = Ease.OutCubic;

    private RectTransform _rectTransform;
    private Tween _slideTween;
    private bool _isOpened;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _isOpened = Mathf.Approximately(_rectTransform.anchoredPosition.x, _openedX);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame)
        {
            return;
        }

        Toggle();
    }

    public void Toggle()
    {
        if (_isOpened)
        {
            Close();
            return;
        }

        Open();
    }

    public void Open()
    {
        _isOpened = true;
        SlideTo(_openedX);
    }

    public void Close()
    {
        _isOpened = false;
        SlideTo(_closedX);
    }

    private void SlideTo(float targetX)
    {
        _slideTween?.Kill();
        _slideTween = _rectTransform
            .DOAnchorPosX(targetX, _duration)
            .SetEase(_ease);
    }


    private void OnDisable()
    {
        _slideTween?.Complete();
        _slideTween = null;
    }

}
