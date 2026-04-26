using TMPro;
using UnityEngine;

public class UI_TopView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dayText;
    // [SerializeField] private TextMeshProUGUI _titleText;    // 주차 제목 (필요 시 연결)
    // [SerializeField] private TextMeshProUGUI _summaryText;  // 주차 요약 (필요 시 연결)

    public void RenderHeader(WeekHeaderPresentation presentation)
    {
        if (_dayText != null)
        {
            _dayText.text = string.IsNullOrWhiteSpace(presentation.Title)
                ? presentation.WeekLabel
                : presentation.Title;
        }

        //if (_titleText != null)
        //{
        //    _titleText.text = presentation.Title;
        //}

        //if (_summaryText != null)
        //{
        //    _summaryText.text = presentation.Summary;
        //}
    }
}
