using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_ExcursionCardGroupView : MonoBehaviour, IWeekCardGroupCollectionView
{
    [SerializeField] private string _displayName = "Excursion";
    [SerializeField] private string[] _targetCardTypeIds = { "card_type_excursion" };
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private UI_ExcursionCardItemView _itemPrefab;
    [SerializeField] private ScrollRect _scrollRect;

    private readonly List<UI_ExcursionCardItemView> _items = new();

    public event Action<SO_CardInfoDefinition, int> OptionSelected;

    public string DisplayName => _displayName;

    private void OnDestroy()
    {
        foreach (UI_ExcursionCardItemView item in _items)
        {
            if (item != null)
            {
                item.OptionSelected -= HandleItemOptionSelected;
            }
        }
    }

    public bool CanRender(IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups)
    {
        return groups != null && groups.Any(IsTargetGroup);
    }

    public bool CanRender(WeekSelectionCategoryGroupPresentation group)
    {
        return IsTargetGroup(group);
    }

    public void Render(IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups)
    {
        gameObject.SetActive(true);

        WeekSelectionEntryPresentation[] entries = groups == null
            ? Array.Empty<WeekSelectionEntryPresentation>()
            : groups
                .Where(IsTargetGroup)
                .SelectMany(group => group.Entries ?? Array.Empty<WeekSelectionEntryPresentation>())
                .ToArray();

        EnsureItemCount(entries.Length);

        for (int i = 0; i < _items.Count; i++)
        {
            UI_ExcursionCardItemView item = _items[i];
            if (item == null)
            {
                continue;
            }

            bool isActive = i < entries.Length;
            item.gameObject.SetActive(isActive);

            if (isActive)
            {
                item.Render(entries[i]);
            }
        }

    }

    public void ResetScrollPosition()
    {
        if (_scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        _scrollRect.StopMovement();
        _scrollRect.verticalNormalizedPosition = 1f;
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private bool IsTargetGroup(WeekSelectionCategoryGroupPresentation group)
    {
        if (group.CardType == null || string.IsNullOrWhiteSpace(group.CardType.Id) || _targetCardTypeIds == null)
        {
            return false;
        }

        return _targetCardTypeIds.Any(targetCardTypeId =>
            string.Equals(group.CardType.Id, targetCardTypeId, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureItemCount(int count)
    {
        if (_contentRoot == null || _itemPrefab == null)
        {
            return;
        }

        while (_items.Count < count)
        {
            UI_ExcursionCardItemView item = Instantiate(_itemPrefab, _contentRoot);
            item.OptionSelected += HandleItemOptionSelected;
            _items.Add(item);
        }
    }

    private void HandleItemOptionSelected(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        OptionSelected?.Invoke(cardDefinition, optionIndex);
    }
}
