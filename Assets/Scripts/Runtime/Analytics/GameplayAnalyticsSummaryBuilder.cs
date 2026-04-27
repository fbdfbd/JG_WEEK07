using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public sealed class GameplayAnalyticsSummary
{
    public string SessionId;
    public DateTime StartedAt;
    public DateTime EndedAt;
    public float ElapsedSeconds;
    public bool EndingReached;
    public string EndingId;
    public GameplayAnalyticsDropoff Dropoff;
    public List<GameplayAnalyticsWeekSummary> Weeks = new();
}

public sealed class GameplayAnalyticsDropoff
{
    public int WeekIndex;
    public string EventName;
    public string EventId;
    public string StepId;
}

public sealed class GameplayAnalyticsWeekSummary
{
    public int WeekIndex;
    public float PlayTimeSeconds;
    public bool Resolved;
    public bool EndingReached;
    public string EndingId;
    public GameplayAnalyticsDropoff LastProgress;
    public Dictionary<string, int> CardSemantics = new();
    public Dictionary<string, int> ClickedCardSemantics = new();
    public Dictionary<string, int> InteractiveChoices = new();
    public Dictionary<string, int> StatDelta = new();
    public List<GameplayAnalyticsCardChoiceSummary> SelectedCards = new();
    public List<GameplayAnalyticsCardChoiceSummary> ClickedCards = new();
    internal float FirstElapsedSeconds = -1f;
    internal float LastElapsedSeconds;
}

public sealed class GameplayAnalyticsCardChoiceSummary
{
    public int WeekIndex;
    public string WeekId;
    public string CardId;
    public string CardTypeId;
    public string CardTypeName;
    public string CardTitle;
    public int OptionIndex;
    public string Semantic;
}

public static class GameplayAnalyticsSummaryBuilder
{
    public static GameplayAnalyticsSummary Build(IReadOnlyList<GameplayAnalyticsEvent> events)
    {
        GameplayAnalyticsSummary summary = new();
        if (events == null || events.Count == 0)
        {
            return summary;
        }

        summary.SessionId = events[0].SessionId;
        summary.StartedAt = events[0].Timestamp;
        summary.EndedAt = events[^1].Timestamp;
        summary.ElapsedSeconds = events[^1].ElapsedSeconds;
        summary.EndingReached = events.Any(logEvent => logEvent.EventName == "ending_reached");
        summary.EndingId = ResolveEndingId(events);
        summary.Dropoff = ResolveDropoff(events);

        Dictionary<int, GameplayAnalyticsWeekSummary> weeks = new();
        foreach (GameplayAnalyticsEvent logEvent in events)
        {
            if (logEvent.WeekIndex <= 0)
            {
                continue;
            }

            GameplayAnalyticsWeekSummary week = GetWeek(weeks, logEvent.WeekIndex);
            if (week.FirstElapsedSeconds < 0f)
            {
                week.FirstElapsedSeconds = logEvent.ElapsedSeconds;
            }

            week.LastElapsedSeconds = logEvent.ElapsedSeconds;
            week.LastProgress = IsProgressEvent(logEvent)
                ? CreateDropoff(logEvent)
                : week.LastProgress;

            switch (logEvent.EventName)
            {
                case "card_option_clicked":
                    AddSemantic(week.ClickedCardSemantics, logEvent);
                    week.ClickedCards.Add(CreateCardChoiceSummary(logEvent));
                    break;
                case "card_option_selected":
                    AddSemantic(week.CardSemantics, logEvent);
                    week.SelectedCards.Add(CreateCardChoiceSummary(logEvent));
                    break;
                case "interactive_choice_selected":
                    AddCount(week.InteractiveChoices, BuildChoiceKey(logEvent));
                    break;
                case "stat_changed":
                    AddStatDelta(week.StatDelta, logEvent);
                    break;
                case "week_resolved":
                    week.Resolved = true;
                    break;
                case "ending_reached":
                    week.EndingReached = true;
                    logEvent.TryGetString("ending_id", out week.EndingId);
                    break;
            }
        }

        foreach (GameplayAnalyticsWeekSummary week in weeks.Values)
        {
            week.PlayTimeSeconds = Math.Max(0f, week.LastElapsedSeconds - week.FirstElapsedSeconds);
        }

        summary.Weeks = weeks.Values.OrderBy(week => week.WeekIndex).ToList();
        return summary;
    }

    public static string ToJson(GameplayAnalyticsSummary summary)
    {
        StringBuilder builder = new();
        builder.AppendLine("{");
        AppendIndentedProperty(builder, 1, "session_id", summary.SessionId, true);
        AppendIndentedProperty(builder, 1, "started_at", summary.StartedAt.ToString("O", CultureInfo.InvariantCulture), true);
        AppendIndentedProperty(builder, 1, "ended_at", summary.EndedAt.ToString("O", CultureInfo.InvariantCulture), true);
        AppendIndentedProperty(builder, 1, "elapsed_seconds", Math.Round(summary.ElapsedSeconds, 3), true);
        AppendIndentedProperty(builder, 1, "ending_reached", summary.EndingReached, true);
        AppendIndentedProperty(builder, 1, "ending_id", summary.EndingId, true);
        AppendDropoff(builder, 1, "dropoff", summary.Dropoff, true);
        builder.AppendLine("  \"weeks\": [");

        for (int index = 0; index < summary.Weeks.Count; index++)
        {
            GameplayAnalyticsWeekSummary week = summary.Weeks[index];
            builder.AppendLine("    {");
            AppendIndentedProperty(builder, 3, "week_index", week.WeekIndex, true);
            AppendIndentedProperty(builder, 3, "play_time_seconds", Math.Round(week.PlayTimeSeconds, 3), true);
            AppendIndentedProperty(builder, 3, "resolved", week.Resolved, true);
            AppendDictionary(builder, 3, "card_semantics", week.CardSemantics, true);
            AppendDictionary(builder, 3, "clicked_card_semantics", week.ClickedCardSemantics, true);
            AppendCardChoices(builder, 3, "selected_cards", week.SelectedCards, true);
            AppendCardChoices(builder, 3, "clicked_cards", week.ClickedCards, true);
            AppendDictionary(builder, 3, "interactive_choices", week.InteractiveChoices, true);
            AppendDictionary(builder, 3, "stat_delta", week.StatDelta, true);
            AppendIndentedProperty(builder, 3, "ending_reached", week.EndingReached, true);
            AppendIndentedProperty(builder, 3, "ending_id", week.EndingId, true);
            AppendDropoff(builder, 3, "last_progress", week.LastProgress, false);
            builder.Append("    }");
            builder.AppendLine(index < summary.Weeks.Count - 1 ? "," : string.Empty);
        }

        builder.AppendLine("  ]");
        builder.AppendLine("}");
        return builder.ToString();
    }

    public static string ToReport(GameplayAnalyticsSummary summary)
    {
        StringBuilder builder = new();
        builder.AppendLine($"Session: {summary.SessionId}");
        builder.AppendLine($"Total Play Time: {summary.ElapsedSeconds:0.###}s");
        builder.AppendLine($"Ending Reached: {(summary.EndingReached ? "Yes" : "No")}");
        if (!string.IsNullOrWhiteSpace(summary.EndingId))
        {
            builder.AppendLine($"Ending ID: {summary.EndingId}");
        }
        builder.AppendLine();
        builder.AppendLine("Dropoff:");
        AppendDropoffReport(builder, summary.Dropoff);

        foreach (GameplayAnalyticsWeekSummary week in summary.Weeks)
        {
            builder.AppendLine();
            builder.AppendLine($"[Week {week.WeekIndex}]");
            builder.AppendLine($"Play Time: {week.PlayTimeSeconds:0.###}s");
            builder.AppendLine($"Resolved: {(week.Resolved ? "Yes" : "No")}");
            AppendCountsReport(builder, "Final Card Choices", week.CardSemantics);
            AppendCountsReport(builder, "Clicked Card Choices", week.ClickedCardSemantics);
            AppendCountsReport(builder, "Interactive Choices", week.InteractiveChoices);
            AppendCountsReport(builder, "Stat Delta", week.StatDelta, signedValues: true);
            builder.AppendLine($"Ending Reached: {(week.EndingReached ? "Yes" : "No")}");
            if (!string.IsNullOrWhiteSpace(week.EndingId))
            {
                builder.AppendLine($"Ending ID: {week.EndingId}");
            }
        }

        return builder.ToString();
    }

    private static GameplayAnalyticsWeekSummary GetWeek(
        Dictionary<int, GameplayAnalyticsWeekSummary> weeks,
        int weekIndex)
    {
        if (!weeks.TryGetValue(weekIndex, out GameplayAnalyticsWeekSummary week))
        {
            week = new GameplayAnalyticsWeekSummary { WeekIndex = weekIndex };
            weeks[weekIndex] = week;
        }

        return week;
    }

    private static GameplayAnalyticsDropoff ResolveDropoff(IReadOnlyList<GameplayAnalyticsEvent> events)
    {
        GameplayAnalyticsEvent dropoff = events.LastOrDefault(logEvent => logEvent.EventName == "event_step_shown")
            ?? events.LastOrDefault(logEvent => logEvent.EventName == "week_resolved")
            ?? events.LastOrDefault(logEvent => logEvent.EventName != "session_end");

        return CreateDropoff(dropoff);
    }

    private static string ResolveEndingId(IReadOnlyList<GameplayAnalyticsEvent> events)
    {
        GameplayAnalyticsEvent endingEvent = events.LastOrDefault(logEvent => logEvent.EventName == "ending_reached");
        return endingEvent != null && endingEvent.TryGetString("ending_id", out string endingId)
            ? endingId
            : string.Empty;
    }

    private static GameplayAnalyticsDropoff CreateDropoff(GameplayAnalyticsEvent logEvent)
    {
        if (logEvent == null)
        {
            return null;
        }

        logEvent.TryGetString("event_id", out string eventId);
        logEvent.TryGetString("step_id", out string stepId);
        return new GameplayAnalyticsDropoff
        {
            WeekIndex = logEvent.WeekIndex,
            EventName = logEvent.EventName,
            EventId = eventId,
            StepId = stepId,
        };
    }

    private static bool IsProgressEvent(GameplayAnalyticsEvent logEvent)
    {
        return logEvent.EventName == "event_step_shown" || logEvent.EventName == "week_resolved";
    }

    private static void AddSemantic(Dictionary<string, int> counts, GameplayAnalyticsEvent logEvent)
    {
        if (logEvent.TryGetString("semantic", out string semantic))
        {
            AddCount(counts, semantic);
        }
    }

    private static GameplayAnalyticsCardChoiceSummary CreateCardChoiceSummary(GameplayAnalyticsEvent logEvent)
    {
        logEvent.TryGetString("week_id", out string weekId);
        logEvent.TryGetString("card_id", out string cardId);
        logEvent.TryGetString("card_type_id", out string cardTypeId);
        logEvent.TryGetString("card_type_name", out string cardTypeName);
        logEvent.TryGetString("card_title", out string cardTitle);
        logEvent.TryGetInt("option_index", out int optionIndex);
        logEvent.TryGetString("semantic", out string semantic);

        return new GameplayAnalyticsCardChoiceSummary
        {
            WeekIndex = logEvent.WeekIndex,
            WeekId = weekId,
            CardId = cardId,
            CardTypeId = cardTypeId,
            CardTypeName = cardTypeName,
            CardTitle = cardTitle,
            OptionIndex = optionIndex,
            Semantic = semantic,
        };
    }

    private static void AddStatDelta(Dictionary<string, int> totals, GameplayAnalyticsEvent logEvent)
    {
        if (!logEvent.TryGetString("stat", out string stat) || !logEvent.TryGetInt("delta", out int delta))
        {
            return;
        }

        totals.TryGetValue(stat, out int current);
        totals[stat] = current + delta;
    }

    private static void AddCount(Dictionary<string, int> counts, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        counts.TryGetValue(key, out int current);
        counts[key] = current + 1;
    }

    private static string BuildChoiceKey(GameplayAnalyticsEvent logEvent)
    {
        logEvent.TryGetString("event_id", out string eventId);
        logEvent.TryGetString("step_id", out string stepId);
        logEvent.TryGetInt("choice_index", out int choiceIndex);
        return $"{eventId}/{stepId}/choice_{choiceIndex}";
    }

    private static void AppendIndentedProperty(StringBuilder builder, int indentLevel, string key, object value, bool comma)
    {
        builder.Append(' ', indentLevel * 2);
        GameplayAnalyticsEvent.AppendJsonProperty(builder, key, value);
        builder.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendDictionary(
        StringBuilder builder,
        int indentLevel,
        string key,
        Dictionary<string, int> values,
        bool comma)
    {
        builder.Append(' ', indentLevel * 2);
        builder.Append('"').Append(GameplayAnalyticsEvent.Escape(key)).AppendLine("\": {");

        KeyValuePair<string, int>[] items = values.OrderBy(pair => pair.Key).ToArray();
        for (int index = 0; index < items.Length; index++)
        {
            builder.Append(' ', (indentLevel + 1) * 2);
            GameplayAnalyticsEvent.AppendJsonProperty(builder, items[index].Key, items[index].Value);
            builder.AppendLine(index < items.Length - 1 ? "," : string.Empty);
        }

        builder.Append(' ', indentLevel * 2);
        builder.Append('}');
        builder.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendCardChoices(
        StringBuilder builder,
        int indentLevel,
        string key,
        IReadOnlyList<GameplayAnalyticsCardChoiceSummary> values,
        bool comma)
    {
        builder.Append(' ', indentLevel * 2);
        builder.Append('"').Append(GameplayAnalyticsEvent.Escape(key)).AppendLine("\": [");

        for (int index = 0; index < values.Count; index++)
        {
            GameplayAnalyticsCardChoiceSummary value = values[index];
            builder.Append(' ', (indentLevel + 1) * 2);
            builder.AppendLine("{");
            AppendIndentedProperty(builder, indentLevel + 2, "week_index", value.WeekIndex, true);
            AppendIndentedProperty(builder, indentLevel + 2, "week_id", value.WeekId, true);
            AppendIndentedProperty(builder, indentLevel + 2, "card_id", value.CardId, true);
            AppendIndentedProperty(builder, indentLevel + 2, "card_type_id", value.CardTypeId, true);
            AppendIndentedProperty(builder, indentLevel + 2, "card_type_name", value.CardTypeName, true);
            AppendIndentedProperty(builder, indentLevel + 2, "card_title", value.CardTitle, true);
            AppendIndentedProperty(builder, indentLevel + 2, "option_index", value.OptionIndex, true);
            AppendIndentedProperty(builder, indentLevel + 2, "semantic", value.Semantic, false);
            builder.Append(' ', (indentLevel + 1) * 2);
            builder.Append('}');
            builder.AppendLine(index < values.Count - 1 ? "," : string.Empty);
        }

        builder.Append(' ', indentLevel * 2);
        builder.Append(']');
        builder.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendDropoff(
        StringBuilder builder,
        int indentLevel,
        string key,
        GameplayAnalyticsDropoff dropoff,
        bool comma)
    {
        builder.Append(' ', indentLevel * 2);
        builder.Append('"').Append(GameplayAnalyticsEvent.Escape(key)).Append("\": ");
        if (dropoff == null)
        {
            builder.Append("null");
            builder.AppendLine(comma ? "," : string.Empty);
            return;
        }

        builder.AppendLine("{");
        AppendIndentedProperty(builder, indentLevel + 1, "week_index", dropoff.WeekIndex, true);
        AppendIndentedProperty(builder, indentLevel + 1, "event_name", dropoff.EventName, true);
        AppendIndentedProperty(builder, indentLevel + 1, "event_id", dropoff.EventId, true);
        AppendIndentedProperty(builder, indentLevel + 1, "step_id", dropoff.StepId, false);
        builder.Append(' ', indentLevel * 2);
        builder.Append('}');
        builder.AppendLine(comma ? "," : string.Empty);
    }

    private static void AppendDropoffReport(StringBuilder builder, GameplayAnalyticsDropoff dropoff)
    {
        if (dropoff == null)
        {
            builder.AppendLine("- None");
            return;
        }

        builder.AppendLine($"- Week {dropoff.WeekIndex}");
        builder.AppendLine($"- {dropoff.EventName}");
        if (!string.IsNullOrWhiteSpace(dropoff.EventId) || !string.IsNullOrWhiteSpace(dropoff.StepId))
        {
            builder.AppendLine($"- {dropoff.EventId} / {dropoff.StepId}");
        }
    }

    private static void AppendCountsReport(
        StringBuilder builder,
        string title,
        Dictionary<string, int> values,
        bool signedValues = false)
    {
        builder.AppendLine($"{title}:");
        if (values.Count == 0)
        {
            builder.AppendLine("- None");
            return;
        }

        foreach (KeyValuePair<string, int> pair in values.OrderBy(pair => pair.Key))
        {
            string value = signedValues && pair.Value > 0 ? $"+{pair.Value}" : pair.Value.ToString();
            builder.AppendLine($"- {pair.Key}: {value}");
        }
    }
}
