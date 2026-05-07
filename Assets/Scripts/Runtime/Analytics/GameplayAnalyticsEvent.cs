using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public sealed class GameplayAnalyticsEvent
{
    private readonly Dictionary<string, object> _fields = new();
    private readonly Action<GameplayAnalyticsEvent> _changed;

    public GameplayAnalyticsEvent(
        string sessionId,
        string eventName,
        float elapsedSeconds,
        int weekIndex,
        Action<GameplayAnalyticsEvent> changed = null)
    {
        SessionId = sessionId;
        EventName = eventName;
        Timestamp = DateTime.Now;
        ElapsedSeconds = elapsedSeconds;
        WeekIndex = weekIndex;
        _changed = changed;
    }

    public string SessionId { get; }
    public string EventName { get; }
    public DateTime Timestamp { get; }
    public float ElapsedSeconds { get; }
    public int WeekIndex { get; }
    public IReadOnlyDictionary<string, object> Fields => _fields;

    public GameplayAnalyticsEvent Add(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key) && value != null)
        {
            _fields[key] = value;
            _changed?.Invoke(this);
        }

        return this;
    }

    public string ToJsonLine()
    {
        StringBuilder builder = new();
        builder.Append('{');
        AppendJsonProperty(builder, "session_id", SessionId);
        builder.Append(',');
        AppendJsonProperty(builder, "event_name", EventName);
        builder.Append(',');
        AppendJsonProperty(builder, "timestamp", Timestamp.ToString("O", CultureInfo.InvariantCulture));
        builder.Append(',');
        AppendJsonProperty(builder, "elapsed_seconds", Math.Round(ElapsedSeconds, 3));
        builder.Append(',');
        AppendJsonProperty(builder, "week_index", WeekIndex);

        foreach (KeyValuePair<string, object> field in _fields)
        {
            builder.Append(',');
            AppendJsonProperty(builder, field.Key, field.Value);
        }

        builder.Append('}');
        return builder.ToString();
    }

    public bool TryGetString(string key, out string value)
    {
        if (_fields.TryGetValue(key, out object rawValue) && rawValue != null)
        {
            value = rawValue.ToString();
            return true;
        }

        value = string.Empty;
        return false;
    }

    public bool TryGetInt(string key, out int value)
    {
        if (_fields.TryGetValue(key, out object rawValue))
        {
            if (rawValue is int intValue)
            {
                value = intValue;
                return true;
            }

            if (int.TryParse(rawValue.ToString(), out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    public static void AppendJsonProperty(StringBuilder builder, string key, object value)
    {
        builder.Append('"');
        builder.Append(Escape(key));
        builder.Append("\":");
        AppendJsonValue(builder, value);
    }

    public static void AppendJsonValue(StringBuilder builder, object value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;
            case bool boolValue:
                builder.Append(boolValue ? "true" : "false");
                break;
            case int or long or float or double or decimal:
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            default:
                builder.Append('"');
                builder.Append(Escape(value.ToString()));
                builder.Append('"');
                break;
        }
    }

    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}
