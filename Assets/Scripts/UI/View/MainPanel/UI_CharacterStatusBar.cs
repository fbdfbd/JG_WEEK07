using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ECharacterStatusBarRenderMode
{
    Bipolar,
    PositiveOnly
}

public class UI_CharacterStatusBar : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI _leftLabelText;
    [SerializeField] private TextMeshProUGUI _rightLabelText;
    [SerializeField] private TextMeshProUGUI _leftValueText;
    [SerializeField] private TextMeshProUGUI _rightValueText;

    [Header("Slider")]
    [SerializeField] private Slider _slider;

    public void Render(string leftLabel, string rightLabel, int value, int minValue, int maxValue)
    {
        Render(leftLabel, rightLabel, value, minValue, maxValue, ECharacterStatusBarRenderMode.Bipolar);
    }

    public void Render(
        string leftLabel,
        string rightLabel,
        int value,
        int minValue,
        int maxValue,
        ECharacterStatusBarRenderMode renderMode)
    {
        if (_leftLabelText != null)
        {
            _leftLabelText.text = leftLabel;
        }

        if (_rightLabelText != null)
        {
            _rightLabelText.text = rightLabel;
        }

        RenderValues(value, minValue, maxValue, renderMode);

        if (_slider == null)
        {
            return;
        }

        _slider.minValue = minValue;
        _slider.maxValue = maxValue;
        _slider.value = value;
    }

    private void RenderValues(int value, int minValue, int maxValue, ECharacterStatusBarRenderMode renderMode)
    {
        if (renderMode == ECharacterStatusBarRenderMode.PositiveOnly)
        {
            SetValueTexts(minValue.ToString(), Mathf.Clamp(value, minValue, maxValue).ToString());
            return;
        }

        int centerValue = (minValue + maxValue) / 2;
        int delta = value - centerValue;
        int leftValue = delta < 0 ? Mathf.Abs(delta) : 0;
        int rightValue = delta > 0 ? delta : 0;
        SetValueTexts(leftValue.ToString(), rightValue.ToString());
    }

    private void SetValueTexts(string leftValue, string rightValue)
    {
        if (_leftValueText != null)
        {
            _leftValueText.text = leftValue;
        }

        if (_rightValueText != null)
        {
            _rightValueText.text = rightValue;
        }
    }
}
