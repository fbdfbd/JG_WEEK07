using System;
using UnityEditor;

public static class CsvImportPipeline
{
    public static CsvImportReport Run(CsvImportSettings settings)
    {
        CsvDataset dataset = CsvDataset.Load(settings);
        CsvImportValidator.Validate(dataset);

        CsvImportReport report = new();
        CsvImportContext context = new(settings, dataset, report);

        if (settings.ValidateOnly)
        {
            return report;
        }

        try
        {
            CsvImportAssetUtility.BeginImportSession();
            AssetDatabase.StartAssetEditing();
            FlagCsvImporter.Import(context);
            CardTypeCsvImporter.Import(context);
            SpeakerCsvImporter.Import(context);
            InteractionCsvImporter.Import(context);
            CardCsvImporter.Import(context);
            WeekCsvImporter.Import(context);
            EventCsvImporter.Import(context);
            EventResultCsvImporter.Import(context);
            EventCutsceneCsvImporter.Import(context);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            CsvImportAssetUtility.EndImportSession();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return report;
    }
}
