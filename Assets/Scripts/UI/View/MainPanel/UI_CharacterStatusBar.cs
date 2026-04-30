using DG.Tweening;
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
    [System.Serializable]
    private class DirectionalSlider
    {
        [SerializeField] private Slider _slider;

        public void Render(int amount, int maxAmount)
        {
            Render((float)amount, maxAmount);
        }

        public void Render(float amount, float maxAmount)
        {
            if (_slider == null)
            {
                return;
            }

            float safeMaxAmount = Mathf.Max(0f, maxAmount);
            _slider.minValue = 0;
            _slider.maxValue = safeMaxAmount;
            _slider.value = Mathf.Clamp(amount, 0, safeMaxAmount);
        }
    }

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI _leftLabelText;
    [SerializeField] private TextMeshProUGUI _rightLabelText;
    [SerializeField] private TextMeshProUGUI _leftValueText;
    [SerializeField] private TextMeshProUGUI _rightValueText;

    [Header("Sliders")]
    [SerializeField] private DirectionalSlider _leftFill;
    [SerializeField] private DirectionalSlider _rightFill;

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
        RenderSliders(value, minValue, maxValue, renderMode);
    }

    public Tween PlayValue(
        string leftLabel,
        string rightLabel,
        int fromValue,
        int toValue,
        int minValue,
        int maxValue,
        ECharacterStatusBarRenderMode renderMode,
        float duration)
    {
        int safeDurationValue = Mathf.RoundToInt(Mathf.Max(0f, duration) * 1000f);
        if (safeDurationValue <= 0 || fromValue == toValue)
        {
            Render(leftLabel, rightLabel, toValue, minValue, maxValue, renderMode);
            return DOVirtual.DelayedCall(0f, () => { });
        }

        Render(leftLabel, rightLabel, fromValue, minValue, maxValue, renderMode);
        return DOTween.To(
            () => (float)fromValue,
            value => RenderAnimatedValue(leftLabel, rightLabel, value, minValue, maxValue, renderMode),
            toValue,
            duration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => Render(leftLabel, rightLabel, toValue, minValue, maxValue, renderMode));
    }

    private void RenderAnimatedValue(
        string leftLabel,
        string rightLabel,
        float value,
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

        int roundedValue = Mathf.RoundToInt(value);
        RenderValues(roundedValue, minValue, maxValue, renderMode);
        RenderSliders(value, minValue, maxValue, renderMode);
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

    private void RenderSliders(int value, int minValue, int maxValue, ECharacterStatusBarRenderMode renderMode)
    {
        RenderSliders((float)value, minValue, maxValue, renderMode);
    }

    private void RenderSliders(float value, int minValue, int maxValue, ECharacterStatusBarRenderMode renderMode)
    {
        float clampedValue = Mathf.Clamp(value, minValue, maxValue);

        if (renderMode == ECharacterStatusBarRenderMode.PositiveOnly)
        {
            RenderDirectionalFills(0f, clampedValue - minValue, maxValue - minValue);
            return;
        }

        int centerValue = (minValue + maxValue) / 2;
        float leftAmount = Mathf.Max(0f, centerValue - clampedValue);
        float rightAmount = Mathf.Max(0f, clampedValue - centerValue);
        int leftMaxAmount = centerValue - minValue;
        int rightMaxAmount = maxValue - centerValue;

        RenderDirectionalFills(leftAmount, rightAmount, leftMaxAmount, rightMaxAmount);
    }

    private void RenderDirectionalFills(int leftAmount, int rightAmount, int maxAmount)
    {
        RenderDirectionalFills(leftAmount, rightAmount, maxAmount, maxAmount);
    }

    private void RenderDirectionalFills(float leftAmount, float rightAmount, float maxAmount)
    {
        RenderDirectionalFills(leftAmount, rightAmount, maxAmount, maxAmount);
    }

    private void RenderDirectionalFills(int leftAmount, int rightAmount, int leftMaxAmount, int rightMaxAmount)
    {
        RenderDirectionalFills((float)leftAmount, rightAmount, leftMaxAmount, rightMaxAmount);
    }

    private void RenderDirectionalFills(float leftAmount, float rightAmount, float leftMaxAmount, float rightMaxAmount)
    {
        _leftFill?.Render(leftAmount, leftMaxAmount);
        _rightFill?.Render(rightAmount, rightMaxAmount);
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
