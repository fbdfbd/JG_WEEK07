using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;


public static class GameplayAnalyticsLogger
{
    private static readonly List<GameplayAnalyticsEvent> Events = new();
    private static string _sessionId;
    private static float _startedAt;
    private static string _rawPath;
    private static string _summaryPath;
    private static bool _sessionEnded;
    private static int _currentWeekIndex;
    private static float _lastSummaryUploadTime;
    private const float SummaryUploadInterval = 30f;

    public static void StartSession(SO_WeekDefinition currentWeek)
    {
        if (!string.IsNullOrEmpty(_sessionId) && !_sessionEnded)
        {
            return;
        }

        Events.Clear();
        _sessionId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";

        _startedAt = Time.realtimeSinceStartup;
        _sessionEnded = false;
        _currentWeekIndex = GetWeekIndex(currentWeek);
        _rawPath = GameplayAnalyticsExporter.BuildPath($"raw_{_sessionId}.jsonl");
        _summaryPath = GameplayAnalyticsExporter.BuildPath($"summary_{_sessionId}.json");

        Log("session_start", _currentWeekIndex)
            .Add("week_id", GetWeekId(currentWeek))
            .Add("week_title", GetWeekTitle(currentWeek));
    }

    public static void EndSession()
    {
        if (string.IsNullOrEmpty(_sessionId) || _sessionEnded)
        {
            return;
        }

        GameplayAnalyticsEvent lastEvent = Events.Count > 0 ? Events[^1] : null;
        Log("session_end", _currentWeekIndex)
            .Add("last_event_name", lastEvent != null ? lastEvent.EventName : string.Empty)
            .Add("last_week_index", lastEvent != null ? lastEvent.WeekIndex : _currentWeekIndex);

        _sessionEnded = true;
        WriteCurrentSummary();

        GameplayAnalyticsSummary summary = GameplayAnalyticsSummaryBuilder.Build(Events);
        string reportPath = GameplayAnalyticsExporter.BuildPath($"report_{_sessionId}.txt");

        GameplayAnalyticsExporter.WriteText(
            reportPath,
            GameplayAnalyticsSummaryBuilder.ToReport(summary));

        GameplayAnalyticsGoogleUploader.Upload(_summaryPath, _sessionId);


    }
    public static async Task EndSessionAndUpload()
    {
        if (string.IsNullOrEmpty(_sessionId) || _sessionEnded)
        {
            return;
        }

        GameplayAnalyticsEvent lastEvent = Events.Count > 0 ? Events[^1] : null;
        Log("session_end", _currentWeekIndex)
            .Add("last_event_name", lastEvent != null ? lastEvent.EventName : string.Empty)
            .Add("last_week_index", lastEvent != null ? lastEvent.WeekIndex : _currentWeekIndex);

        _sessionEnded = true;
        WriteCurrentSummary();

        GameplayAnalyticsSummary summary = GameplayAnalyticsSummaryBuilder.Build(Events);
        string reportPath = GameplayAnalyticsExporter.BuildPath($"report_{_sessionId}.txt");

        GameplayAnalyticsExporter.WriteText(
            reportPath,
            GameplayAnalyticsSummaryBuilder.ToReport(summary));

        await GameplayAnalyticsGoogleUploader.Upload(_summaryPath, _sessionId);
    }

    private static void WriteCurrentSummary()
    {
        if (string.IsNullOrEmpty(_summaryPath))
        {
            return;
        }

        GameplayAnalyticsSummary summary = GameplayAnalyticsSummaryBuilder.Build(Events);

        GameplayAnalyticsExporter.WriteText(
    _summaryPath,
    GameplayAnalyticsSummaryBuilder.ToJson(summary));

    if (Time.realtimeSinceStartup - _lastSummaryUploadTime >= SummaryUploadInterval)
    {
        _lastSummaryUploadTime = Time.realtimeSinceStartup;
        GameplayAnalyticsGoogleUploader.Upload(_summaryPath, _sessionId);
    }

    }


    public static void LogCardOptionClicked(
        SO_WeekDefinition week,
        SO_CardInfoDefinition card,
        int optionIndex)
    {
        CardOptionData option = GetOption(card, optionIndex);
        Log("card_option_clicked", GetWeekIndex(week))
            .Add("week_id", GetWeekId(week))
            .Add("card_id", GetCardId(card))
            .Add("card_type_id", GetCardTypeId(card))
            .Add("card_type_name", GetCardTypeName(card))
            .Add("card_title", GetCardTitle(card))
            .Add("option_index", optionIndex)
            .Add("semantic", option != null ? option.Semantic.ToString() : string.Empty);
    }

    public static void LogCardOptionSelected(
        SO_WeekDefinition week,
        RuntimeResolvedCardRecord resolvedCard)
    {
        if (resolvedCard == null)
        {
            return;
        }

        Log("card_option_selected", GetWeekIndex(week))
            .Add("week_id", GetWeekId(week))
            .Add("card_id", GetCardId(resolvedCard.CardDefinition))
            .Add("card_type_id", GetCardTypeId(resolvedCard.CardDefinition))
            .Add("card_type_name", GetCardTypeName(resolvedCard.CardDefinition))
            .Add("card_title", GetCardTitle(resolvedCard.CardDefinition))
            .Add("option_index", resolvedCard.SelectedOptionIndex)
            .Add("semantic", resolvedCard.SelectedOption != null ? resolvedCard.SelectedOption.Semantic.ToString() : string.Empty);
    }

    public static void LogCheckID()
    {
        Debug.LogError("폴더Id");
    }
    public static void LogWeekResolved(SO_WeekDefinition week, RuntimeWeekResult result)
    {
        Log("week_resolved", GetWeekIndex(week))
            .Add("week_id", GetWeekId(week))
            .Add("week_title", GetWeekTitle(week))
            .Add("resolved_card_count", result?.ResolvedCards?.Count ?? 0);
    }



    public static void LogEventStepShown(WeekFlowScreen screen)
    {
        if (screen == null || screen.ScreenType != EWeekFlowScreenType.EventStep)
        {
            return;
        }

        Log("event_step_shown", GetWeekIndex(screen.WeekDefinition))
            .Add("week_id", GetWeekId(screen.WeekDefinition))
            .Add("event_id", GetEventId(screen.EventDefinition))
            .Add("event_title", GetEventTitle(screen.EventDefinition))
            .Add("step_id", GetStepId(screen.StepDefinition))
            .Add("step_title", GetStepTitle(screen.StepDefinition));
    }

    public static void LogSkipButtonClicked(WeekFlowScreen screen)
    {
        if (screen == null || screen.ScreenType != EWeekFlowScreenType.EventStep)
        {
            return;
        }

        Log("skip_button_clicked", GetWeekIndex(screen.WeekDefinition))
            .Add("week_id", GetWeekId(screen.WeekDefinition))
            .Add("event_id", GetEventId(screen.EventDefinition))
            .Add("event_title", GetEventTitle(screen.EventDefinition))
            .Add("step_id", GetStepId(screen.StepDefinition))
            .Add("step_title", GetStepTitle(screen.StepDefinition))
            .Add("screen_type", screen.ScreenType.ToString())
            .Add("source_class", nameof(UI_DialogueScreenView))
            .Add("source_method", "HandleSkipEventButtonClicked");
    }

    public static void LogInteractiveEventSkipped(
        SO_WeekDefinition week,
        SO_InteractiveEventDefinition eventDefinition,
        SO_InteractiveEventStepDefinition step)
    {
        Log("interactive_event_skipped", GetWeekIndex(week))
            .Add("week_id", GetWeekId(week))
            .Add("event_id", GetEventId(eventDefinition))
            .Add("event_title", GetEventTitle(eventDefinition))
            .Add("step_id", GetStepId(step))
            .Add("step_title", GetStepTitle(step))
            .Add("source_class", nameof(WeekFlowNarrativeHandler))
            .Add("source_method", "SkipCurrentInteractiveEvent");
    }

    public static void LogInteractiveChoiceSelected(
        SO_WeekDefinition week,
        SO_InteractiveEventDefinition eventDefinition,
        SO_InteractiveEventStepDefinition step,
        int choiceIndex,
        InteractiveEventChoiceData choice)
    {
        Log("interactive_choice_selected", GetWeekIndex(week))
            .Add("week_id", GetWeekId(week))
            .Add("event_id", GetEventId(eventDefinition))
            .Add("event_title", GetEventTitle(eventDefinition))
            .Add("step_id", GetStepId(step))
            .Add("step_title", GetStepTitle(step))
            .Add("choice_index", choiceIndex)
            .Add("choice_label", choice != null ? choice.Label : string.Empty);
    }

    public static void LogStatChanged(SO_WeekDefinition week, StatChangeInfo changeInfo)
    {
        Log("stat_changed", GetWeekIndex(week))
            .Add("week_id", GetWeekId(week))
            .Add("stat", changeInfo.StatType.ToString())
            .Add("previous", changeInfo.PreviousValue)
            .Add("current", changeInfo.CurrentValue)
            .Add("delta", changeInfo.Delta);
    }

    public static void LogEndingReached(SO_WeekDefinition week, EndingPresentation ending)
    {
        Log("ending_reached", GetWeekIndex(week))
            .Add("week_id", GetWeekId(week))
            .Add("ending_id", ending.EndingId)
            .Add("ending_title", ending.Title)
            .Add("ending_visual_state", ending.VisualState.ToString());
    }

    public static void LogWeekResultPanelVisibilityChanged(
        bool visible,
        WeeklyResultLogPresentation presentation)
    {
        string selectedWeekId = ResolveWeeklyResultSelectedWeekId(presentation);
        int selectedWeekIndex = ResolveWeeklyResultSelectedWeekIndex(presentation, selectedWeekId);
        int entryCount = ResolveWeeklyResultEntryCount(presentation, selectedWeekId);

        Log(visible ? "week_result_panel_opened" : "week_result_panel_closed",
                selectedWeekIndex > 0 ? selectedWeekIndex : _currentWeekIndex)
            .Add("panel_name", "WeekResultPanel")
            .Add("visible", visible)
            .Add("selected_week_id", selectedWeekId)
            .Add("entry_count", entryCount);
    }

    private static GameplayAnalyticsEvent Log(string eventName, int weekIndex)
    {
        if (string.IsNullOrEmpty(_sessionId))
        {
            StartSession(null);
        }

        _currentWeekIndex = weekIndex > 0 ? weekIndex : _currentWeekIndex;
        GameplayAnalyticsEvent logEvent = new(
            _sessionId,
            eventName,
            Time.realtimeSinceStartup - _startedAt,
            weekIndex);

        Events.Add(logEvent);
        File.AppendAllText(_rawPath, logEvent.ToJsonLine() + Environment.NewLine);
        WriteCurrentSummary();
        return logEvent;

    }

    private static int GetWeekIndex(SO_WeekDefinition week)
    {
        return week != null ? week.WeekIndex : _currentWeekIndex;
    }

    private static string GetWeekId(SO_WeekDefinition week)
    {
        return week != null ? week.Id : string.Empty;
    }

    private static string GetWeekTitle(SO_WeekDefinition week)
    {
        return week != null ? week.Title : string.Empty;
    }

    private static string GetCardId(SO_CardInfoDefinition card)
    {
        return card != null && !string.IsNullOrWhiteSpace(card.Id) ? card.Id : card != null ? card.name : string.Empty;
    }

    private static string GetCardTitle(SO_CardInfoDefinition card)
    {
        return card != null ? card.Title : string.Empty;
    }

    private static string GetCardTypeId(SO_CardInfoDefinition card)
    {
        return card?.CardType != null && !string.IsNullOrWhiteSpace(card.CardType.Id)
            ? card.CardType.Id
            : string.Empty;
    }

    private static string GetCardTypeName(SO_CardInfoDefinition card)
    {
        return card?.CardType != null ? card.CardType.DisplayName : string.Empty;
    }

    private static CardOptionData GetOption(SO_CardInfoDefinition card, int optionIndex)
    {
        CardOptionData[] options = card != null ? card.Options : null;
        return options != null && optionIndex >= 0 && optionIndex < options.Length
            ? options[optionIndex]
            : null;
    }

    private static string ResolveWeeklyResultSelectedWeekId(WeeklyResultLogPresentation presentation)
    {
        if (!string.IsNullOrWhiteSpace(presentation.SelectedWeekId))
        {
            return presentation.SelectedWeekId;
        }

        return presentation.HasWeeks
            ? presentation.Weeks[presentation.Weeks.Count - 1].WeekId
            : string.Empty;
    }

    private static int ResolveWeeklyResultSelectedWeekIndex(
        WeeklyResultLogPresentation presentation,
        string selectedWeekId)
    {
        if (!presentation.HasWeeks)
        {
            return 0;
        }

        for (int index = 0; index < presentation.Weeks.Count; index++)
        {
            WeeklyResultLogWeekPresentation week = presentation.Weeks[index];
            if (string.Equals(week.WeekId, selectedWeekId, StringComparison.OrdinalIgnoreCase))
            {
                return week.WeekIndex;
            }
        }

        return presentation.Weeks[presentation.Weeks.Count - 1].WeekIndex;
    }

    private static int ResolveWeeklyResultEntryCount(
        WeeklyResultLogPresentation presentation,
        string selectedWeekId)
    {
        if (!presentation.HasWeeks)
        {
            return presentation.Entries?.Count ?? 0;
        }

        for (int index = 0; index < presentation.Weeks.Count; index++)
        {
            WeeklyResultLogWeekPresentation week = presentation.Weeks[index];
            if (string.Equals(week.WeekId, selectedWeekId, StringComparison.OrdinalIgnoreCase))
            {
                return week.Entries?.Count ?? 0;
            }
        }

        WeeklyResultLogWeekPresentation fallbackWeek = presentation.Weeks[presentation.Weeks.Count - 1];
        return fallbackWeek.Entries?.Count ?? 0;
    }

    private static string GetEventId(SO_InteractiveEventDefinition eventDefinition)
    {
        return eventDefinition != null && !string.IsNullOrWhiteSpace(eventDefinition.Id)
            ? eventDefinition.Id
            : eventDefinition != null ? eventDefinition.name : string.Empty;
    }

    private static string GetEventTitle(SO_InteractiveEventDefinition eventDefinition)
    {
        return eventDefinition != null ? eventDefinition.Title : string.Empty;
    }

    private static string GetStepId(SO_InteractiveEventStepDefinition step)
    {
        return step != null ? step.name : string.Empty;
    }

    private static string GetStepTitle(SO_InteractiveEventStepDefinition step)
    {
        return step != null ? step.TitleOverride : string.Empty;
    }
}
