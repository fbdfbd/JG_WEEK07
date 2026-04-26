using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UI_ExcursionCardGroupView : MonoBehaviour, IWeekCardGroupView
{
    [SerializeField] private string _targetCardTypeId = "card_type_excursion";
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private UI_ExcursionCardItemView _itemPrefab;

    private readonly List<UI_ExcursionCardItemView> _items = new();

    public event Action<SO_CardInfoDefinition, int> OptionSelected;

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

    public bool CanRender(WeekSelectionCategoryGroupPresentation group)
    {
        return group.CardType != null &&
               string.Equals(group.CardType.Id, _targetCardTypeId, StringComparison.OrdinalIgnoreCase);
    }

    public void Render(WeekSelectionCategoryGroupPresentation group)
    {
        gameObject.SetActive(true);

        IReadOnlyList<WeekSelectionEntryPresentation> entries =
            group.Entries ?? Array.Empty<WeekSelectionEntryPresentation>();

        EnsureItemCount(entries.Count);

        for (int i = 0; i < _items.Count; i++)
        {
            UI_ExcursionCardItemView item = _items[i];
            if (item == null)
            {
                continue;
            }

            bool isActive = i < entries.Count;
            item.gameObject.SetActive(isActive);

            if (isActive)
            {
                item.Render(entries[i]);
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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
