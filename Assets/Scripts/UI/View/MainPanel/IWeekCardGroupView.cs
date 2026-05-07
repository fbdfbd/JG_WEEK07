using System;
using System.Collections.Generic;

public interface IWeekCardGroupView
{
    event Action<SO_CardInfoDefinition, int> OptionSelected;

    bool CanRender(WeekSelectionCategoryGroupPresentation group);
    void Render(WeekSelectionCategoryGroupPresentation group);
    void Hide();
}

public interface IWeekCardGroupCollectionView
{
    event Action<SO_CardInfoDefinition, int> OptionSelected;

    string DisplayName { get; }
    bool CanRender(IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups);
    bool CanRender(WeekSelectionCategoryGroupPresentation group);
    void Render(IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups);
    void Hide();
}
