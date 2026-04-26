using System;

public interface IWeekCardGroupView
{
    event Action<SO_CardInfoDefinition, int> OptionSelected;

    bool CanRender(WeekSelectionCategoryGroupPresentation group);
    void Render(WeekSelectionCategoryGroupPresentation group);
    void Hide();
}
