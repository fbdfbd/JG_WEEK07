using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        if (_leftLabelText != null)
        {
            _leftLabelText.text = leftLabel;
        }

        if (_rightLabelText != null)
        {
            _rightLabelText.text = rightLabel;
        }

        int centerValue = (minValue + maxValue) / 2;
        int delta = value - centerValue;
        int leftValue = delta < 0 ? Mathf.Abs(delta) : 0;
        int rightValue = delta > 0 ? delta : 0;

        if (_leftValueText != null)
        {
            _leftValueText.text = leftValue.ToString();
        }

        if (_rightValueText != null)
        {
            _rightValueText.text = rightValue.ToString();
        }

        if (_slider == null)
        {
            return;
        }

        _slider.minValue = minValue;
        _slider.maxValue = maxValue;
        _slider.value = value;
    }
}
