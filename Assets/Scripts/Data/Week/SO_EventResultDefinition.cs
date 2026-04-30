using UnityEngine;

[CreateAssetMenu(
    fileName = "EventResult_",
    menuName = "Scriptable Objects/Week/EventResultDefinition")]
public class SO_EventResultDefinition : ScriptableObject
{
    [SerializeField] private string _eventId = string.Empty;
    [SerializeField] private string _title = string.Empty;
    [SerializeField, TextArea(2, 6)] private string _context = string.Empty;

    public string EventId => _eventId;
    public string Title => _title;
    public string Context => _context;
}
