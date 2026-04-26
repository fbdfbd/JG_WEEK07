using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_ExcursionCardItemView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Header("Options")]
    [SerializeField] private Button _directButton;
    [SerializeField] private Button _blockedButton;
    [SerializeField] private TextMeshProUGUI _directButtonLabel;
    [SerializeField] private TextMeshProUGUI _blockedButtonLabel;

    private WeekSelectionEntryPresentation _entry;
    private int _directOptionIndex = CardOptionViewUtility.InvalidOptionIndex;
    private int _blockedOptionIndex = CardOptionViewUtility.InvalidOptionIndex;

    public event Action<SO_CardInfoDefinition, int> OptionSelected;

    private void Awake()
    {
        if (_directButton != null)
        {
            _directButton.onClick.AddListener(HandleDirectButtonClicked);
        }

        if (_blockedButton != null)
        {
            _blockedButton.onClick.AddListener(HandleBlockedButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (_directButton != null)
        {
            _directButton.onClick.RemoveListener(HandleDirectButtonClicked);
        }

        if (_blockedButton != null)
        {
            _blockedButton.onClick.RemoveListener(HandleBlockedButtonClicked);
        }
    }

    public void Render(WeekSelectionEntryPresentation entry)
    {
        _entry = entry;

        if (_titleText != null)
        {
            _titleText.text = entry.Title;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = entry.OriginalText;
        }

        _directOptionIndex = CardOptionViewUtility.FindOptionIndex(entry.Options, ECardOptionSemantic.Direct);
        _blockedOptionIndex = CardOptionViewUtility.FindOptionIndex(entry.Options, ECardOptionSemantic.Blocked);

        ApplyButtonState(_directButton, _directButtonLabel, _directOptionIndex);
        ApplyButtonState(_blockedButton, _blockedButtonLabel, _blockedOptionIndex);
    }

    private void ApplyButtonState(Button button, TextMeshProUGUI label, int optionIndex)
    {
        if (button == null)
        {
            return;
        }

        bool hasOption = CardOptionViewUtility.IsValidOptionIndex(_entry.Options, optionIndex);
        button.gameObject.SetActive(hasOption);

        if (!hasOption)
        {
            return;
        }

        CardOptionData option = _entry.Options[optionIndex];
        if (label != null && option != null && !string.IsNullOrWhiteSpace(option.Label))
        {
            label.text = option.Label;
        }

        button.interactable = optionIndex != _entry.SelectedOptionIndex;
    }

    private void HandleDirectButtonClicked()
    {
        SelectOption(_directOptionIndex);
    }

    private void HandleBlockedButtonClicked()
    {
        SelectOption(_blockedOptionIndex);
    }

    private void SelectOption(int optionIndex)
    {
        if (_entry.CardDefinition == null ||
            !CardOptionViewUtility.IsValidOptionIndex(_entry.Options, optionIndex))
        {
            return;
        }

        OptionSelected?.Invoke(_entry.CardDefinition, optionIndex);
    }
}
