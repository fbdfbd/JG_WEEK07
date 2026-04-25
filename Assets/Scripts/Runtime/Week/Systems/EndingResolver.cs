using System;
using System.Collections.Generic;
using System.Linq;

public readonly struct EndingPresentation
{
    public EndingPresentation(
        string endingId,
        string title,
        IReadOnlyList<string> detailLines,
        string summary,
        string closingLine,
        string reputationLine,
        ENemoVisualState visualState)
    {
        EndingId = endingId;
        Title = title;
        DetailLines = detailLines;
        Summary = summary;
        ClosingLine = closingLine;
        ReputationLine = reputationLine;
        VisualState = visualState;
    }

    public string EndingId { get; }
    public string Title { get; }
    public IReadOnlyList<string> DetailLines { get; }
    public string Summary { get; }
    public string ClosingLine { get; }
    public string ReputationLine { get; }
    public ENemoVisualState VisualState { get; }
}

public static class EndingResolver
{
    private const string MissingCatalogEndingId = "ending_catalog_missing";
    private const string MissingMainEndingId = "ending_main_missing";

    public static EndingPresentation Resolve(RuntimeChildState childState, SO_EndingCatalog catalog = null)
    {
        if (catalog == null)
        {
            return CreateFallbackPresentation(
                MissingCatalogEndingId,
                "Ending Catalog Missing",
                "EndingCatalog이 연결되지 않았습니다.");
        }

        if (childState == null || EndingContextBuilder.HasNoCharacterMet(childState))
        {
            return CreatePresentation(
                catalog.NoCharacterMetEndingId,
                catalog.NoCharacterMetEnding,
                default);
        }

        EndingContext context = EndingContextBuilder.Build(childState);
        SO_CharacterEndingDefinition mainEnding = catalog.FindCharacterEnding(context);
        if (mainEnding == null)
        {
            return CreateFallbackPresentation(
                MissingMainEndingId,
                "Ending Not Found",
                $"엔딩 데이터를 찾지 못했습니다. Character={context.CharacterType}, Meet={context.MeetCount}, Mood={context.MoodType}");
        }

        SO_AffinityEndingDefinition affinityEnding = catalog.FindAffinityEnding(context.Affinity);
        return CreatePresentation(mainEnding, affinityEnding);
    }

    private static EndingPresentation CreatePresentation(
        SO_CharacterEndingDefinition mainEnding,
        SO_AffinityEndingDefinition affinityEnding)
    {
        EndingTextData mainText = mainEnding.Text;
        EndingTextData affinityText = affinityEnding != null ? affinityEnding.Text : default;
        return CreatePresentation(mainEnding.Id, mainText, affinityText);
    }

    private static EndingPresentation CreatePresentation(
        string endingId,
        EndingTextData mainText,
        EndingTextData affinityText)
    {
        List<string> detailLines = SplitBody(mainText.Body);
        detailLines.AddRange(SplitBody(affinityText.Body));

        string title = FirstNotEmpty(mainText.Title, "Ending");
        string summary = FirstNotEmpty(mainText.Summary, affinityText.Summary);
        string closingLine = FirstNotEmpty(affinityText.ClosingLine, mainText.ClosingLine);
        string reputationLine = FirstNotEmpty(mainText.ReputationLine, affinityText.ReputationLine);
        ENemoVisualState visualState = mainText.VisualState;

        return new EndingPresentation(
            endingId,
            title,
            detailLines,
            summary,
            closingLine,
            reputationLine,
            visualState);
    }

    private static EndingPresentation CreateFallbackPresentation(string endingId, string title, string body)
    {
        return new EndingPresentation(
            endingId,
            title,
            SplitBody(body),
            body,
            string.Empty,
            string.Empty,
            ENemoVisualState.Neutral);
    }

    private static List<string> SplitBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return new List<string>();
        }

        return body
            .Split(new[] { "\r\n\r\n", "\n\n", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static string FirstNotEmpty(string first, string second)
    {
        return string.IsNullOrWhiteSpace(first) ? second : first;
    }
}

