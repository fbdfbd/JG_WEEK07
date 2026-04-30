using System;
using System.Linq;

public static class EventResultCsvImporter
{
    public static void Import(CsvImportContext context)
    {
        if (context.Dataset.EventResults == null || context.Dataset.EventResults.Count == 0)
        {
            return;
        }

        foreach (EventResultRow row in context.Dataset.EventResults)
        {
            EventRow eventRow = context.Dataset.Events.First(eventData =>
                string.Equals(eventData.Id, row.EventId, StringComparison.OrdinalIgnoreCase));

            string path = CsvImportAssetUtility.CombineAssetPath(
                context.Settings.OutputRootPath,
                "Week",
                eventRow.WeekId,
                "EventResults",
                $"{row.EventId}_result.asset");

            SO_EventResultDefinition result = CsvImportAssetUtility.LoadOrCreateAsset<SO_EventResultDefinition>(
                path,
                $"{row.EventId}_result",
                context.Report);

            CsvImportAssetUtility.SetField(result, "_eventId", row.EventId);
            CsvImportAssetUtility.SetField(result, "_title", row.Title);
            CsvImportAssetUtility.SetField(result, "_context", row.Context);
            CsvImportAssetUtility.MarkDirty(result);

            SO_InteractiveEventDefinition eventDefinition = context.EventsById[row.EventId];
            CsvImportAssetUtility.SetField(eventDefinition, "_result", result);
            CsvImportAssetUtility.MarkDirty(eventDefinition);

            context.EventResultsByEventId[row.EventId] = result;
        }
    }
}
