using System;
using UnityEngine;

public sealed class WeekFlowCutsceneTargetRegistry : MonoBehaviour
{
    [Serializable]
    private class TargetEntry
    {
        public string key = string.Empty;
        public GameObject gameObject;
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;

        [NonSerialized] public Vector2 defaultAnchoredPosition;
        [NonSerialized] public float defaultAlpha = 1f;
        [NonSerialized] public bool cached;
    }

    [SerializeField] private TargetEntry[] _targets = Array.Empty<TargetEntry>();

    private void Awake()
    {
        CacheDefaults();
    }

    private void OnValidate()
    {
        CacheDefaults();
    }

    public bool TryGetGameObject(string key, out GameObject target)
    {
        TargetEntry entry = FindEntry(key);
        target = entry?.gameObject;
        return target != null;
    }

    public bool TryGetRectTransform(string key, out RectTransform target, out Vector2 defaultAnchoredPosition)
    {
        TargetEntry entry = FindEntry(key);
        target = entry?.rectTransform;
        defaultAnchoredPosition = entry != null ? entry.defaultAnchoredPosition : default;
        return target != null;
    }

    public bool TryGetCanvasGroup(string key, out CanvasGroup target, out float defaultAlpha)
    {
        TargetEntry entry = FindEntry(key);
        target = entry?.canvasGroup;
        defaultAlpha = entry != null ? entry.defaultAlpha : 1f;
        return target != null;
    }

    public void RestoreDefaults()
    {
        CacheDefaults();
        if (_targets == null)
        {
            return;
        }

        for (int index = 0; index < _targets.Length; index++)
        {
            TargetEntry entry = _targets[index];
            if (entry == null)
            {
                continue;
            }

            if (entry.rectTransform != null)
            {
                entry.rectTransform.anchoredPosition = entry.defaultAnchoredPosition;
            }

            if (entry.canvasGroup != null)
            {
                entry.canvasGroup.alpha = entry.defaultAlpha;
            }
        }
    }

    private void CacheDefaults()
    {
        if (_targets == null)
        {
            return;
        }

        for (int index = 0; index < _targets.Length; index++)
        {
            TargetEntry entry = _targets[index];
            if (entry == null || entry.cached)
            {
                continue;
            }

            if (entry.gameObject == null)
            {
                if (entry.rectTransform != null)
                {
                    entry.gameObject = entry.rectTransform.gameObject;
                }
                else if (entry.canvasGroup != null)
                {
                    entry.gameObject = entry.canvasGroup.gameObject;
                }
            }

            if (entry.rectTransform == null && entry.gameObject != null)
            {
                entry.rectTransform = entry.gameObject.GetComponent<RectTransform>();
            }

            if (entry.canvasGroup == null && entry.gameObject != null)
            {
                entry.canvasGroup = entry.gameObject.GetComponent<CanvasGroup>();
            }

            if (entry.rectTransform != null)
            {
                entry.defaultAnchoredPosition = entry.rectTransform.anchoredPosition;
            }

            if (entry.canvasGroup != null)
            {
                entry.defaultAlpha = entry.canvasGroup.alpha;
            }

            entry.cached = true;
        }
    }

    private TargetEntry FindEntry(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || _targets == null)
        {
            return null;
        }

        CacheDefaults();
        for (int index = 0; index < _targets.Length; index++)
        {
            TargetEntry entry = _targets[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            if (string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }
}
