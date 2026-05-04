using System.Collections.Generic;
using UnityEngine;

public abstract class SO_CardInteractionDefinition : ScriptableObject
{
    [SerializeField] private string _displayName = string.Empty;

    public string DisplayName => ResolveDisplayName();

    public abstract void Apply(RuntimeChildState childState);

    public virtual IEnumerable<string> GetDisplayNames()
    {
        string displayName = ResolveDisplayName();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            yield return displayName;
        }
    }

    protected virtual string ResolveDisplayName()
    {
        return _displayName;
    }

    protected string SerializedDisplayName => _displayName;
}
