using UnityEngine;

[CreateAssetMenu(
    fileName = "AffinityEnding_",
    menuName = "Scriptable Objects/Ending/AffinityEnding")]
public class SO_AffinityEndingDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private int _priority;
    [SerializeField] private int _minAffinity = int.MinValue;
    [SerializeField] private int _maxAffinity = int.MaxValue;
    [SerializeField] private EndingTextData _text;

    public string Id => _id;
    public int Priority => _priority;
    public EndingTextData Text => _text;

    public bool Matches(int affinity)
    {
        return affinity >= _minAffinity && affinity <= _maxAffinity;
    }
}

