using System.Collections.Generic;

public static class CardOptionViewUtility
{
    public const int InvalidOptionIndex = -1;

    public static int FindOptionIndex(IReadOnlyList<CardOptionData> options, ECardOptionSemantic semantic)
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

    public static bool IsValidOptionIndex(IReadOnlyList<CardOptionData> options, int optionIndex)
    {
        return options != null && optionIndex >= 0 && optionIndex < options.Count;
    }
}
