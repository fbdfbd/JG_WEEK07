using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

public class UI_CardView : MonoBehaviour
{
    [Header("Index Panel")]
    [SerializeField] private Button[] _indexButtons;
    [SerializeField] private TextMeshProUGUI[] _indexTexts;

    [Header("Color")]
    [SerializeField] private Color _indexDefaultColor;
    [SerializeField] private Color _indexHighlightColor;

    [Header("Content Panel - Title Panel")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _categoryText;

    [Header("Content Panel - Desc Panel")]
    [SerializeField] private TextMeshProUGUI _descText;
    [SerializeField] private TextMeshProUGUI _mainDescText;

    [Header("Content Panel - Options")]
    [SerializeField] private Button _modifiedButton;
    [SerializeField] private Button _blockedButton;
    [SerializeField] private Button _directButton;
    [SerializeField] private TextMeshProUGUI _modifiedButtonLabel;
    [SerializeField] private TextMeshProUGUI _blockedButtonLabel;
    [SerializeField] private TextMeshProUGUI _directButtonLabel;

    [Header("Content Panel - Batch Options")]
    [SerializeField] private Button _allDirectButton;
    [SerializeField] private Button _allBlockedButton;

    [Header("Content Panel - Navigation")]
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;

    [Header("Type Specific Views")]
    [Tooltip("Existing left/right card content root. Do not assign the CardPanel root that has UI_CardView on it.")]
    [SerializeField] private GameObject _defaultGroupRoot;
    [Tooltip("Optional type-specific group views, such as UI_ExcursionCardGroupView.")]
    [SerializeField] private MonoBehaviour[] _groupViewBehaviours;

    private const int InvalidOptionIndex = -1;

    private IReadOnlyList<WeekSelectionCategoryGroupPresentation> _currentGroups;
    private int _currentGroupIndex = 0;
    private int _currentCardIndexInGroup = 0;

    private int _directOptionIndex = InvalidOptionIndex;
    private int _selectedOptionIndex = InvalidOptionIndex;
    private int _modifiedOptionIndex = InvalidOptionIndex;
    private int _blockedOptionIndex = InvalidOptionIndex;
    private readonly List<IWeekCardGroupView> _groupViews = new();
    private IWeekCardGroupView _activeGroupView;

    // 상위 뷰로 전달할 이벤트
    public event Action<SO_CardInfoDefinition, int> OnCardOptionClicked;
    public event Action<ECardOptionSemantic> OnAllCardSemanticRequested;

    private void Awake()
    {
        CacheGroupViews();

        // 인덱스 버튼 바인딩
        for (int i = 0; i < _indexButtons.Length; i++)
        {
            int index = i;
            _indexButtons[i].onClick.AddListener(() => OnIndexButtonClicked(index));
        }

        // 옵션 버튼 바인딩
        if (_modifiedButton != null)
        {
            _modifiedButton.onClick.AddListener(OnModifiedButtonClicked);
        }

        if (_blockedButton != null)
        {
            _blockedButton.onClick.AddListener(OnBlockedButtonClicked);
        }

        if (_directButton != null)
        {
            _directButton.onClick.AddListener(OnDirectButtonClicked);
        }

        // 이전 / 다음 버튼 바인딩
        if (_allDirectButton != null)
        {
            _allDirectButton.onClick.AddListener(OnAllDirectButtonClicked);
        }

        if (_allBlockedButton != null)
        {
            _allBlockedButton.onClick.AddListener(OnAllBlockedButtonClicked);
        }

        if (_prevButton != null)
        {
            _prevButton.onClick.AddListener(OnPrevButtonClicked);
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    private void OnDestroy()
    {
        // 인덱스 버튼 리스너 정리
        for (int i = 0; i < _indexButtons.Length; i++)
        {
            if (_indexButtons[i] != null)
            {
                _indexButtons[i].onClick.RemoveAllListeners();
            }
        }

        // 옵션 버튼 리스너 정리
        if (_modifiedButton != null)
        {
            _modifiedButton.onClick.RemoveListener(OnModifiedButtonClicked);
        }

        if (_blockedButton != null)
        {
            _blockedButton.onClick.RemoveListener(OnBlockedButtonClicked);
        }

        if (_directButton != null)
        {
            _directButton.onClick.RemoveListener(OnDirectButtonClicked);
        }

        // 이전 / 다음 버튼 리스너 정리
        if (_allDirectButton != null)
        {
            _allDirectButton.onClick.RemoveListener(OnAllDirectButtonClicked);
        }

        if (_allBlockedButton != null)
        {
            _allBlockedButton.onClick.RemoveListener(OnAllBlockedButtonClicked);
        }

        if (_prevButton != null)
        {
            _prevButton.onClick.RemoveListener(OnPrevButtonClicked);
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(OnNextButtonClicked);
        }

        foreach (IWeekCardGroupView groupView in _groupViews)
        {
            groupView.OptionSelected -= HandleGroupViewOptionSelected;
        }
    }

    public void SetCardGroups(IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups)
    {
        SO_CardInfoDefinition previousCardDefinition = null;
        SO_CardInfoTypeDefinition previousCardType = null;
        int previousGroupIndex = _currentGroupIndex;
        int previousCardIndexInGroup = _currentCardIndexInGroup;

        if (TryGetCurrentCardData(out WeekSelectionEntryPresentation currentCardData))
        {
            previousCardDefinition = currentCardData.CardDefinition;
            previousCardType = currentCardData.CardDefinition != null
                ? currentCardData.CardDefinition.CardType
                : null;
        }
        else if (TryGetCurrentGroup(out WeekSelectionCategoryGroupPresentation currentGroup))
        {
            previousCardType = currentGroup.CardType;
        }

        _currentGroups = groups;
        RestoreCurrentPosition(previousCardDefinition, previousCardType, previousGroupIndex, previousCardIndexInGroup);

        for (int i = 0; i < _indexButtons.Length; i++)
        {
            if (groups != null && i < groups.Count)
            {
                _indexButtons[i].gameObject.SetActive(true);

                if (_indexTexts != null && i < _indexTexts.Length && _indexTexts[i] != null)
                {
                    _indexTexts[i].text = groups[i].TypeName;
                }
            }
            else
            {
                // 남는 인덱스 버튼은 안 보이게 숨김 처리
                _indexButtons[i].gameObject.SetActive(false);
            }
        }

        UpdateCurrentGroupedCard();
    }

    private void RestoreCurrentPosition(
        SO_CardInfoDefinition previousCardDefinition,
        SO_CardInfoTypeDefinition previousCardType,
        int previousGroupIndex,
        int previousCardIndexInGroup)
    {
        if (_currentGroups == null || _currentGroups.Count == 0)
        {
            _currentGroupIndex = 0;
            _currentCardIndexInGroup = 0;
            return;
        }

        if (TryRestoreCardPosition(previousCardDefinition))
        {
            return;
        }

        if (TryRestoreGroupPosition(previousCardType))
        {
            return;
        }

        _currentGroupIndex = Mathf.Clamp(previousGroupIndex, 0, _currentGroups.Count - 1);
        int cardCount = _currentGroups[_currentGroupIndex].Entries != null
            ? _currentGroups[_currentGroupIndex].Entries.Count
            : 0;
        _currentCardIndexInGroup = cardCount > 0
            ? Mathf.Clamp(previousCardIndexInGroup, 0, cardCount - 1)
            : 0;
    }

    private bool TryRestoreCardPosition(SO_CardInfoDefinition previousCardDefinition)
    {
        if (previousCardDefinition == null || _currentGroups == null)
        {
            return false;
        }

        for (int groupIndex = 0; groupIndex < _currentGroups.Count; groupIndex++)
        {
            IReadOnlyList<WeekSelectionEntryPresentation> entries = _currentGroups[groupIndex].Entries;
            if (entries == null)
            {
                continue;
            }

            for (int cardIndex = 0; cardIndex < entries.Count; cardIndex++)
            {
                if (entries[cardIndex].CardDefinition != previousCardDefinition)
                {
                    continue;
                }

                _currentGroupIndex = groupIndex;
                _currentCardIndexInGroup = cardIndex;
                return true;
            }
        }

        return false;
    }

    private bool TryRestoreGroupPosition(SO_CardInfoTypeDefinition previousCardType)
    {
        if (_currentGroups == null)
        {
            return false;
        }

        for (int groupIndex = 0; groupIndex < _currentGroups.Count; groupIndex++)
        {
            if (_currentGroups[groupIndex].CardType != previousCardType)
            {
                continue;
            }

            _currentGroupIndex = groupIndex;
            _currentCardIndexInGroup = 0;
            return true;
        }

        return false;
    }

    // [로컬 UI 상태 변경] 인덱스 버튼 클릭 시 현재 그룹과 첫 카드로 이동
    private void OnIndexButtonClicked(int index)
    {
        if (_currentGroups == null || index < 0 || index >= _currentGroups.Count)
        {
            return;
        }

        _currentGroupIndex = index;
        _currentCardIndexInGroup = 0;

        UpdateCurrentGroupedCard();
    }

    // [로컬 UI 상태 변경] 이전 카드로 이동
    private void OnPrevButtonClicked()
    {
        if (_currentCardIndexInGroup <= 0)
        {
            return;
        }

        _currentCardIndexInGroup--;
        UpdateCurrentGroupedCard();
    }

    // [로컬 UI 상태 변경] 다음 카드로 이동
    private void OnNextButtonClicked()
    {
        if (!TryGetCurrentGroup(out WeekSelectionCategoryGroupPresentation currentGroup))
        {
            return;
        }

        if (_currentCardIndexInGroup >= currentGroup.Entries.Count - 1)
        {
            return;
        }

        _currentCardIndexInGroup++;
        UpdateCurrentGroupedCard();
    }

    // 수정 옵션 버튼 클릭 처리
    private void OnModifiedButtonClicked()
    {
        SelectSemanticOption(_modifiedOptionIndex);
    }

    // 차단 옵션 버튼 클릭 처리
    private void OnBlockedButtonClicked()
    {
        SelectSemanticOption(_blockedOptionIndex);
    }

    private void OnDirectButtonClicked()
    {
        SelectSemanticOption(_directOptionIndex);
    }

    private void OnAllDirectButtonClicked()
    {
        OnAllCardSemanticRequested?.Invoke(ECardOptionSemantic.Direct);
    }

    private void OnAllBlockedButtonClicked()
    {
        OnAllCardSemanticRequested?.Invoke(ECardOptionSemantic.Blocked);
    }

    // 유저가 선택한 semantic 옵션을 현재 카드에 반영
    private void SelectSemanticOption(int optionIndex)
    {
        if (!TryGetCurrentCardData(out WeekSelectionEntryPresentation currentCardData))
        {
            return;
        }

        if (!IsValidOptionIndex(currentCardData.Options, optionIndex))
        {
            return;
        }

        _selectedOptionIndex = optionIndex;

        // 버튼 상태 / 설명 텍스트 / 상위 이벤트까지 한 번에 갱신
        UpdateSemanticButtons();
        RenderDescription(currentCardData);
        RaiseCardOptionSelectedEvent(optionIndex);
    }

    private void UpdateCurrentGroupedCard()
    {
        // 방어 코드: 현재 그룹 또는 카드 데이터가 비어있으면 화면 초기화 후 리턴
        if (!TryGetCurrentGroup(out WeekSelectionCategoryGroupPresentation currentGroup))
        {
            HideTypeSpecificViews();
            SetDefaultGroupVisible(true);
            ClearCardDisplay();
            return;
        }

        if (TryRenderTypeSpecificView(currentGroup))
        {
            return;
        }

        SetDefaultGroupVisible(true);

        if (!TryGetCurrentCardData(out WeekSelectionEntryPresentation currentCardData))
        {
            ClearCardDisplay();
            return;
        }

        // 텍스트 렌더링
        if (_categoryText != null)
        {
            _categoryText.text = currentGroup.TypeName;
        }

        if (_titleText != null)
        {
            _titleText.text = currentCardData.Title;
        }

        // 현재 카드 기준으로 semantic 옵션 인덱스 캐싱
        CacheSemanticOptionIndices(currentCardData);

        // 현재 카드가 이미 선택 중인 옵션을 갖고 있다면 로컬 선택 상태 동기화
        SyncSelectedOptionIndex(currentCardData);

        // 인덱스 버튼 색상 갱신
        UpdateIndexButtonColors();

        // 옵션 버튼 / 네비게이션 버튼 / 설명 텍스트 렌더링
        UpdateSemanticButtons();
        UpdateNavigationButtons(currentGroup);
        RenderDescription(currentCardData);
    }

    private void CacheGroupViews()
    {
        _groupViews.Clear();

        if (_groupViewBehaviours == null)
        {
            return;
        }

        foreach (MonoBehaviour behaviour in _groupViewBehaviours)
        {
            if (!(behaviour is IWeekCardGroupView groupView))
            {
                continue;
            }

            _groupViews.Add(groupView);
            groupView.OptionSelected -= HandleGroupViewOptionSelected;
            groupView.OptionSelected += HandleGroupViewOptionSelected;
            groupView.Hide();
        }
    }

    private bool TryRenderTypeSpecificView(WeekSelectionCategoryGroupPresentation currentGroup)
    {
        IWeekCardGroupView nextView = FindGroupView(currentGroup);
        if (nextView == null)
        {
            HideTypeSpecificViews();
            _activeGroupView = null;
            return false;
        }

        SetDefaultGroupVisible(!CanDisableDefaultGroupRoot(nextView));
        ClearCardDisplay();
        UpdateIndexButtonColors();

        foreach (IWeekCardGroupView groupView in _groupViews)
        {
            if (groupView != nextView)
            {
                groupView.Hide();
            }
        }

        _activeGroupView = nextView;
        _activeGroupView.Render(currentGroup);
        return true;
    }

    private IWeekCardGroupView FindGroupView(WeekSelectionCategoryGroupPresentation currentGroup)
    {
        foreach (IWeekCardGroupView groupView in _groupViews)
        {
            if (groupView.CanRender(currentGroup))
            {
                return groupView;
            }
        }

        return null;
    }

    private void HideTypeSpecificViews()
    {
        foreach (IWeekCardGroupView groupView in _groupViews)
        {
            groupView.Hide();
        }

        _activeGroupView = null;
    }

    private void SetDefaultGroupVisible(bool visible)
    {
        if (_defaultGroupRoot != null)
        {
            _defaultGroupRoot.SetActive(visible);
        }
    }

    private bool CanDisableDefaultGroupRoot(IWeekCardGroupView nextView)
    {
        if (_defaultGroupRoot == null || nextView == null)
        {
            return false;
        }

        if (nextView is MonoBehaviour behaviour && behaviour != null)
        {
            return !behaviour.transform.IsChildOf(_defaultGroupRoot.transform);
        }

        return true;
    }

    private void HandleGroupViewOptionSelected(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        OnCardOptionClicked?.Invoke(cardDefinition, optionIndex);
    }

    // 선택된 인덱스 버튼만 하이라이트 색으로, 나머지는 디폴트 색으로 갱신
    private void UpdateIndexButtonColors()
    {
        if (_indexButtons == null)
        {
            return;
        }

        for (int i = 0; i < _indexButtons.Length; i++)
        {
            Button button = _indexButtons[i];
            if (button == null)
            {
                continue;
            }

            bool isSelected = button.gameObject.activeSelf &&
                              _currentGroups != null &&
                              i == _currentGroupIndex &&
                              i < _currentGroups.Count;

            Color targetColor = isSelected ? _indexHighlightColor : _indexDefaultColor;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = targetColor;
            }
        }
    }

    // 현재 카드의 옵션 목록에서 semantic 별 인덱스를 캐싱
    private void CacheSemanticOptionIndices(WeekSelectionEntryPresentation currentCardData)
    {
        _directOptionIndex = FindOptionIndex(currentCardData.Options, ECardOptionSemantic.Direct);
        _modifiedOptionIndex = FindOptionIndex(currentCardData.Options, ECardOptionSemantic.Modified);
        _blockedOptionIndex = FindOptionIndex(currentCardData.Options, ECardOptionSemantic.Blocked);
    }

    // 현재 카드의 선택 상태를 로컬 UI 상태와 동기화
    private void SyncSelectedOptionIndex(WeekSelectionEntryPresentation currentCardData)
    {
        _selectedOptionIndex = IsValidOptionIndex(currentCardData.Options, currentCardData.SelectedOptionIndex)
            ? currentCardData.SelectedOptionIndex
            : InvalidOptionIndex;
    }

    // semantic 옵션 버튼들의 표시 / interactable 상태 갱신
    private void UpdateSemanticButtons()
    {
        if (!TryGetCurrentCardData(out WeekSelectionEntryPresentation currentCardData))
        {
            ApplyButtonState(_directButton, _directButtonLabel, null, InvalidOptionIndex);
            ApplyButtonState(_modifiedButton, _modifiedButtonLabel, null, InvalidOptionIndex);
            ApplyButtonState(_blockedButton, _blockedButtonLabel, null, InvalidOptionIndex);
            return;
        }

        ApplyButtonState(_directButton, _directButtonLabel, currentCardData.Options, _directOptionIndex);
        ApplyButtonState(_modifiedButton, _modifiedButtonLabel, currentCardData.Options, _modifiedOptionIndex);
        ApplyButtonState(_blockedButton, _blockedButtonLabel, currentCardData.Options, _blockedOptionIndex);
    }

    // 버튼이 가리키는 옵션이 있으면 표시, 현재 선택된 옵션이면 비활성화
    private void ApplyButtonState(Button button, TextMeshProUGUI buttonLabel, IReadOnlyList<CardOptionData> options, int optionIndex)
    {
        if (button == null)
        {
            return;
        }

        bool hasOption = IsValidOptionIndex(options, optionIndex);
        button.gameObject.SetActive(hasOption);

        if (!hasOption)
        {
            return;
        }

        if (buttonLabel != null)
        {
            CardOptionData option = options[optionIndex];
            if (option != null && !string.IsNullOrWhiteSpace(option.Label))
            {
                buttonLabel.text = option.Label;
            }
        }

        button.interactable = optionIndex != _selectedOptionIndex;
    }

    // 현재 카드 위치에 따라 이전 / 다음 버튼 상태 갱신
    private void UpdateNavigationButtons(WeekSelectionCategoryGroupPresentation currentGroup)
    {
        if (_prevButton != null)
        {
            // 첫 번째 카드면 이전 버튼 비활성화
            _prevButton.interactable = _currentCardIndexInGroup > 0;
        }

        if (_nextButton != null)
        {
            // 마지막 카드면 다음 버튼 비활성화
            _nextButton.interactable = _currentCardIndexInGroup < currentGroup.Entries.Count - 1;
        }
    }

    // 선택된 옵션이 있으면 해당 문구를, 없으면 원문 설명을 출력
    private void RenderDescription(WeekSelectionEntryPresentation currentCardData)
    {
        if (_descText == null)
        {
            return;
        }

        if (IsValidOptionIndex(currentCardData.Options, _selectedOptionIndex))
        {
            // 현재 선택된 옵션의 PresentedText를 설명 영역에 렌더링
            CardOptionData selectedOption = currentCardData.Options[_selectedOptionIndex];
            _mainDescText.text = currentCardData.OriginalText;
            if (selectedOption != null && !string.IsNullOrWhiteSpace(selectedOption.PresentedText))
            {
                _descText.text = selectedOption.PresentedText;
                
                return;
            }
        }

        // 선택된 옵션이 없으면 원본 텍스트 표시
        _descText.text = currentCardData.OriginalText;
    }

    // 카드 표시 영역 초기화
    private void ClearCardDisplay()
    {
        _selectedOptionIndex = InvalidOptionIndex;
        _modifiedOptionIndex = InvalidOptionIndex;
        _blockedOptionIndex = InvalidOptionIndex;
        _directOptionIndex = InvalidOptionIndex;

        if (_categoryText != null)
        {
            _categoryText.text = string.Empty;
        }

        if (_titleText != null)
        {
            _titleText.text = string.Empty;
        }

        if (_descText != null)
        {
            _descText.text = string.Empty;
        }

        if (_modifiedButton != null)
        {
            _modifiedButton.gameObject.SetActive(false);
        }

        if (_blockedButton != null)
        {
            _blockedButton.gameObject.SetActive(false);
        }

        if (_prevButton != null)
        {
            _prevButton.interactable = false;
        }

        if (_nextButton != null)
        {
            _nextButton.interactable = false;
        }

        if (_directButton != null)
        {
            _directButton.gameObject.SetActive(false);
        }

        UpdateIndexButtonColors();
    }

    // 현재 선택된 그룹을 안전하게 가져오기
    private bool TryGetCurrentGroup(out WeekSelectionCategoryGroupPresentation currentGroup)
    {
        currentGroup = default;

        if (_currentGroups == null || _currentGroups.Count == 0)
        {
            return false;
        }

        if (_currentGroupIndex < 0 || _currentGroupIndex >= _currentGroups.Count)
        {
            return false;
        }

        currentGroup = _currentGroups[_currentGroupIndex];
        return currentGroup.Entries != null;
    }

    // 현재 선택된 카드 데이터를 안전하게 가져오기
    private bool TryGetCurrentCardData(out WeekSelectionEntryPresentation currentCardData)
    {
        currentCardData = default;

        if (!TryGetCurrentGroup(out WeekSelectionCategoryGroupPresentation currentGroup))
        {
            return false;
        }

        if (currentGroup.Entries == null || currentGroup.Entries.Count == 0)
        {
            return false;
        }

        if (_currentCardIndexInGroup < 0 || _currentCardIndexInGroup >= currentGroup.Entries.Count)
        {
            return false;
        }

        currentCardData = currentGroup.Entries[_currentCardIndexInGroup];

        // 유효한 카드 정의가 있을 때만 true 반환
        return currentCardData.CardDefinition != null;
    }

    // 특정 semantic 을 가진 옵션의 인덱스를 찾아 반환
    private int FindOptionIndex(IReadOnlyList<CardOptionData> options, ECardOptionSemantic semantic)
    {
        if (options == null)
        {
            return InvalidOptionIndex;
        }

        for (int i = 0; i < options.Count; i++)
        {
            CardOptionData option = options[i];
            if (option != null && option.Semantic == semantic)
            {
                return i;
            }
        }

        return InvalidOptionIndex;
    }

    // 옵션 인덱스가 유효한 범위인지 검사
    private bool IsValidOptionIndex(IReadOnlyList<CardOptionData> options, int optionIndex)
    {
        return options != null && optionIndex >= 0 && optionIndex < options.Count;
    }

    // 유저가 누른 특정 옵션의 인덱스를 상위로 전달
    private void RaiseCardOptionSelectedEvent(int clickedOptionIndex)
    {
        if (!TryGetCurrentCardData(out WeekSelectionEntryPresentation currentCardData))
        {
            return;
        }

        // 현재 보고 있는 정확한 카드의 Definition을 상위로 쏴준다.
        OnCardOptionClicked?.Invoke(currentCardData.CardDefinition, clickedOptionIndex);
    }
}
