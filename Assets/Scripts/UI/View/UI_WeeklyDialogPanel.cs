using UnityEngine;
using DG.Tweening;
using TMPro;

public class UI_WeeklyDialogPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dialogueText;
    [SerializeField] private float showDuration = 0.2f;
    [SerializeField] private float hideDuration = 0.15f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private RectTransform rectTr;

    private void Awake()
    {
        rectTr = GetComponent<RectTransform>();
    }

    public void SetText(string text)
    {
        if (_dialogueText != null)
        {
            _dialogueText.text = text ?? string.Empty;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);

        rectTr.DOKill();
        rectTr.localScale = Vector3.zero;
        rectTr.DOScale(Vector3.one, showDuration).SetEase(showEase);
    }

    public void Hide()
    {
        rectTr.DOKill();
        rectTr.DOScale(Vector3.zero, hideDuration)
            .SetEase(hideEase)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
