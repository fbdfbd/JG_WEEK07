using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class CsvTableParser
{
    public static CsvTable ParseFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"CSV file not found: {path}");
        }

        return Parse(File.ReadAllText(path, Encoding.UTF8));
    }

    public static CsvTable Parse(string text)
    {
        List<List<string>> rawRows = ParseRows(text ?? string.Empty);
        if (rawRows.Count == 0)
        {
            throw new InvalidOperationException("CSV is empty.");
        }

        string[] headers = rawRows[0].Select(NormalizeCell).ToArray();
        List<CsvRecord> rows = new();
        for (int index = 1; index < rawRows.Count; index++)
        {
            List<string> rawRow = rawRows[index];
            if (rawRow.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new CsvRecord(headers, rawRow.Select(NormalizeCell).ToArray(), index + 1));
        }

        return new CsvTable(headers, rows);
    }

    private static List<List<string>> ParseRows(string text)
    {
        List<List<string>> rows = new();
        List<string> currentRow = new();
        StringBuilder currentCell = new();
        bool inQuotes = false;

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (inQuotes)
            {
                if (character == '"')
                {
                    bool isEscapedQuote = index + 1 < text.Length && text[index + 1] == '"';
                    if (isEscapedQuote)
                    {
                        currentCell.Append('"');
                        index++;
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                currentCell.Append(character);
                continue;
            }

            switch (character)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    break;
                default:
                    currentCell.Append(character);
                    break;
            }
        }

        currentRow.Add(currentCell.ToString());
        rows.Add(currentRow);
        return rows;
    }

    private static string NormalizeCell(string value)
    {
        return value?.Trim() ?? string.Empty;
    }
}

public sealed class CsvTable
{
    public CsvTable(string[] headers, IReadOnlyList<CsvRecord> rows)
    {
        Headers = headers;
        Rows = rows;
    }

    public string[] Headers { get; }
    public IReadOnlyList<CsvRecord> Rows { get; }
}

public sealed class CsvRecord
{
    private readonly Dictionary<string, string> _values;

    public CsvRecord(string[] headers, string[] values, int rowNumber)
    {
        RowNumber = rowNumber;
        _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < headers.Length; index++)
        {
            string value = index < values.Length ? values[index] : string.Empty;
            _values[headers[index]] = value;
        }
    }

    public int RowNumber { get; }

    public string this[string header] => _values.TryGetValue(header, out string value) ? value : string.Empty;

    public int GetInt(string header, int defaultValue = 0)
    {
        string rawValue = this[header];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (!int.TryParse(rawValue, out int parsedValue))
        {
            throw new InvalidOperationException($"Row {RowNumber}: '{header}' must be an integer.");
        }

        return parsedValue;
    }

    public bool GetBool(string header, bool defaultValue = false)
    {
        string rawValue = this[header];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (!bool.TryParse(rawValue, out bool parsedValue))
        {
            throw new InvalidOperationException($"Row {RowNumber}: '{header}' must be true or false.");
        }

        return parsedValue;
    }

    public float GetFloat(string header, float defaultValue = 0f)
    {
        string rawValue = this[header];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (!float.TryParse(rawValue, out float parsedValue))
        {
            throw new InvalidOperationException($"Row {RowNumber}: '{header}' must be a number.");
        }

        return parsedValue;
    }

    public string[] GetMultiValue(string header)
    {
        string rawValue = this[header];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Array.Empty<string>();
        }

        return rawValue
            .Split('|')
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }
}
