using DG.Tweening;
using TMPro;
using UnityEngine;

public class UI_DialogView : MonoBehaviour
{
    [SerializeField] private GameObject _nameTagPanel;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _mainContentText;
    [SerializeField] private float _charactersPerSecond = 40f;

    private Tween _typingTween;
    private string _currentContent = string.Empty;
    private bool _isNarration;

    public event System.Action TypingCompleted;

    public bool IsTyping => _typingTween != null && _typingTween.IsActive() && _typingTween.IsPlaying();

    private void OnDestroy()
    {
        StopTypingTween();
    }

    public void SetDialogue(string speakerName, string content)
    {
        _currentContent = string.IsNullOrEmpty(content) ? string.Empty : content;
        _isNarration = string.IsNullOrEmpty(speakerName);

        UpdateSpeakerName(speakerName);
        PlayTypewriter();
    }

    public bool CompleteTypingImmediately()
    {
        if (!IsTyping)
        {
            return false;
        }

        StopTypingTween();
        ApplyVisibleCharacterCount(int.MaxValue);
        return true;
    }

    private void UpdateSpeakerName(string speakerName)
    {
        if (_nameTagPanel == null)
        {
            return;
        }

        bool hasSpeakerName = !string.IsNullOrEmpty(speakerName);
        _nameTagPanel.SetActive(hasSpeakerName);

        if (hasSpeakerName && _nameText != null)
        {
            _nameText.text = speakerName;
        }
    }

    private void PlayTypewriter()
    {
        if (_mainContentText == null)
        {
            return;
        }

        StopTypingTween();

        _mainContentText.fontStyle = _isNarration ? FontStyles.Italic : FontStyles.Normal;
        _mainContentText.text = _currentContent;
        _mainContentText.maxVisibleCharacters = 0;
        _mainContentText.ForceMeshUpdate();

        int characterCount = _mainContentText.textInfo.characterCount;
        if (characterCount <= 0)
        {
            _mainContentText.maxVisibleCharacters = int.MaxValue;
            TypingCompleted?.Invoke();
            return;
        }

        float duration = Mathf.Max(0.05f, characterCount / Mathf.Max(1f, _charactersPerSecond));
        _typingTween = DOTween.To(
            GetVisibleCharacterCount,
            ApplyVisibleCharacterCount,
            characterCount,
            duration);

        _typingTween.SetEase(Ease.Linear);
        _typingTween.OnComplete(HandleTypingCompleted);
    }

    private int GetVisibleCharacterCount()
    {
        if (_mainContentText == null)
        {
            return 0;
        }

        return _mainContentText.maxVisibleCharacters;
    }

    private void ApplyVisibleCharacterCount(int visibleCharacterCount)
    {
        if (_mainContentText == null)
        {
            return;
        }

        _mainContentText.maxVisibleCharacters = visibleCharacterCount;
    }

    private void HandleTypingCompleted()
    {
        ApplyVisibleCharacterCount(int.MaxValue);
        _typingTween = null;
        TypingCompleted?.Invoke();
    }

    private void StopTypingTween()
    {
        if (_typingTween == null)
        {
            return;
        }

        if (_typingTween.IsActive())
        {
            _typingTween.Kill();
        }

        _typingTween = null;
    }
}
