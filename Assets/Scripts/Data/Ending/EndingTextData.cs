using UnityEngine;

[System.Serializable]
public struct EndingTextData
{
    public EndingTextData(
        string title,
        string body,
        string summary,
        string closingLine,
        string reputationLine,
        ENemoVisualState visualState)
    {
        _title = title;
        _body = body;
        _summary = summary;
        _closingLine = closingLine;
        _reputationLine = reputationLine;
        _visualState = visualState;
    }

    [SerializeField] private string _title;
    [TextArea(3, 12)]
    [SerializeField] private string _body;
    [SerializeField] private string _summary;
    [SerializeField] private string _closingLine;
    [SerializeField] private string _reputationLine;
    [SerializeField] private ENemoVisualState _visualState;

    public string Title => _title;
    public string Body => _body;
    public string Summary => _summary;
    public string ClosingLine => _closingLine;
    public string ReputationLine => _reputationLine;
    public ENemoVisualState VisualState => _visualState;
}

