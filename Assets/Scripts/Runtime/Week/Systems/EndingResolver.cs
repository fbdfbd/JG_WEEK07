using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public static void LogDebugSnapshot(RuntimeChildState childState, string source)
    {
        if (childState == null)
        {
            Debug.Log($"[EndingDebug] {source} ChildState=null");
            return;
        }

        if (EndingContextBuilder.HasNoCharacterMet(childState))
        {
            Debug.Log(
                $"[EndingDebug] {source} NoCharacterMet | " +
                BuildStatSnapshot(childState));
            return;
        }

        EndingContext context = EndingContextBuilder.Build(childState);
        Debug.Log(
            $"[EndingDebug] {source} " +
            $"Direction={context.DirectionType}, Selected={context.CharacterType}, Meet={context.MeetCount}, " +
            $"Affinity={context.Affinity} | " +
            BuildStatSnapshot(childState));
    }

    public static EndingPresentation Resolve(
        RuntimeChildState childState,
        SO_EndingCatalog catalog = null)
    {
        if (catalog == null)
        {
            return CreateFallbackPresentation(
                MissingCatalogEndingId,
                "Ending Catalog Missing",
                "EndingCatalog가 연결되지 않았습니다.");
        }

        EndingContext context = EndingContextBuilder.Build(childState);
        bool hasNoCharacterMet = childState == null || EndingContextBuilder.HasNoCharacterMet(childState);
        SO_EndingLayerDefinition directionLayer = catalog.FindDirectionLayer(context);
        SO_CharacterEndingDefinition characterEnding = hasNoCharacterMet
            ? catalog.FindNoCharacterEnding(context.DirectionType)
            : catalog.FindCharacterEnding(context);
        if (characterEnding != null && directionLayer != null)
        {
            SO_AffinityEndingDefinition affinityEnding = catalog.FindAffinityEnding(context.Affinity);
            return CreatePresentation(directionLayer, characterEnding, affinityEnding);
        }

        SO_EndingLayerDefinition characterMeetLayer = null;
        if (!hasNoCharacterMet)
        {
            characterMeetLayer = catalog.FindCharacterMeetLayer(context);
            if (characterMeetLayer == null)
            {
                characterMeetLayer = ResolveFallbackCharacterMeetLayer(childState, catalog);
            }
        }

        if (directionLayer == null || characterMeetLayer == null)
        {
            if (hasNoCharacterMet)
            {
                return CreatePresentation(
                    catalog.NoCharacterMetEndingId,
                    catalog.NoCharacterMetEnding,
                    default);
            }

            return CreateFallbackPresentation(
                MissingMainEndingId,
                "Ending Not Found",
                $"엔딩 레이어를 찾지 못했습니다. Direction={context.DirectionType}, Character={context.CharacterType}, Meet={context.MeetCount}");
        }

        SO_EndingLayerDefinition affinityLayer = catalog.FindAffinityLayer(context.Affinity);
        return CreatePresentation(directionLayer, characterMeetLayer, affinityLayer);
    }

    private static SO_EndingLayerDefinition ResolveFallbackCharacterMeetLayer(
        RuntimeChildState childState,
        SO_EndingCatalog catalog)
    {
        EndingContext[] contexts = EndingContextBuilder.BuildMetCharacterContextsByPriority(childState);
        for (int i = 0; i < contexts.Length; i++)
        {
            SO_EndingLayerDefinition exactLayer = catalog.FindCharacterMeetLayer(contexts[i]);
            if (exactLayer != null)
            {
                return exactLayer;
            }
        }

        for (int i = 0; i < contexts.Length; i++)
        {
            SO_EndingLayerDefinition closestLayer = catalog.FindClosestCharacterMeetLayer(contexts[i]);
            if (closestLayer != null)
            {
                return closestLayer;
            }
        }

        return null;
    }

    private static EndingPresentation CreatePresentation(
        SO_EndingLayerDefinition directionLayer,
        SO_CharacterEndingDefinition characterEnding,
        SO_AffinityEndingDefinition affinityEnding)
    {
        EndingTextData directionText = directionLayer.Text;
        EndingTextData characterText = characterEnding.Text;
        EndingTextData affinityText = affinityEnding != null ? affinityEnding.Text : default;
        string endingId = affinityEnding != null
            ? $"{directionLayer.Id}+{characterEnding.Id}+{affinityEnding.Id}"
            : $"{directionLayer.Id}+{characterEnding.Id}";

        return CreatePresentation(endingId, directionText, characterText, affinityText);
    }

    private static EndingPresentation CreatePresentation(
        SO_EndingLayerDefinition directionLayer,
        SO_EndingLayerDefinition characterMeetLayer,
        SO_EndingLayerDefinition affinityLayer)
    {
        EndingTextData directionText = directionLayer.Text;
        EndingTextData characterText = characterMeetLayer.Text;
        EndingTextData affinityText = affinityLayer != null ? affinityLayer.Text : default;
        string endingId = affinityLayer != null
            ? $"{directionLayer.Id}+{characterMeetLayer.Id}+{affinityLayer.Id}"
            : $"{directionLayer.Id}+{characterMeetLayer.Id}";

        return CreatePresentation(endingId, directionText, characterText, affinityText);
    }

    private static EndingPresentation CreatePresentation(
        string endingId,
        EndingTextData mainText,
        EndingTextData affinityText)
    {
        List<string> detailLines = SplitBody(mainText.Body);
        AddEndingSection(detailLines, affinityText.Title, affinityText.Body);

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

    private static EndingPresentation CreatePresentation(
        string endingId,
        EndingTextData directionText,
        EndingTextData characterText,
        EndingTextData affinityText)
    {
        List<string> detailLines = SplitBody(directionText.Body);
        AddEndingSection(detailLines, characterText.Title, characterText.Body);
        AddEndingSection(detailLines, affinityText.Title, affinityText.Body);

        string title = FirstNotEmpty(directionText.Title, FirstNotEmpty(characterText.Title, "Ending"));
        string summary = FirstNotEmpty(directionText.Summary, FirstNotEmpty(characterText.Summary, affinityText.Summary));
        string closingLine = FirstNotEmpty(affinityText.ClosingLine, FirstNotEmpty(characterText.ClosingLine, directionText.ClosingLine));
        string reputationLine = FirstNotEmpty(characterText.ReputationLine, FirstNotEmpty(directionText.ReputationLine, affinityText.ReputationLine));
        ENemoVisualState visualState = directionText.VisualState;

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

    private static void AddEndingSection(List<string> lines, string title, string body)
    {
        List<string> bodyLines = SplitBody(body);
        if (bodyLines.Count <= 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            lines.Add(title.Trim());
        }

        lines.AddRange(bodyLines);
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

    private static string BuildStatSnapshot(RuntimeChildState childState)
    {
        return
            $"Meet(Max={childState.GetStat(EChildStatusType.Max)}, " +
            $"Rian={childState.GetStat(EChildStatusType.Rian)}, " +
            $"Yuffie={childState.GetStat(EChildStatusType.Yuffie)}, " +
            $"Millia={childState.GetStat(EChildStatusType.Millia)}) | " +
            $"DirectionStats(Trust={childState.GetStat(EChildStatusType.Trust)}, " +
            $"Anxiety={childState.GetStat(EChildStatusType.Anxiety)}, " +
            $"Obedience={childState.GetStat(EChildStatusType.Obedience)}, " +
            $"Curiosity={childState.GetStat(EChildStatusType.Curiosity)})";
    }
}
