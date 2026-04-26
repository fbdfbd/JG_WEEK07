using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BackgroundType
{
    BedRoom,
    BallRoom,
    DiningRoom,
    DrawingRoom,
    Garden,
    Lake,
    Study,
    Hallway,
    Basement,
    GardenCrapeMyrtle,
    GardenDoor,
    Hunter,
    ForestEnterance,
    FestivalEntrance,
    FestivalRianBackStreet,
    FestivalShop,
    FestivalStreet,
    FestivalTheater,
    FestivalTheaterSeat,
    ImperialBallEntrance,
    ImperialBall,
    ImperialBallNobleSeat,
    ImperialPalaceHallway,
    GardenDeclarationStage
}

public enum BackgroundTransitionMode
{
    None,
    Slide,
    Pop,
}

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager I { get; private set; }

    [Serializable]
    private class BackgroundEntry
    {
        public BackgroundType type;
        public Image targetImage;

        [Tooltip("Use the same key for visually identical backgrounds. If empty, the sprite reference is used.")]
        public string visualKey;

        [NonSerialized] public RectTransform rect;
        [NonSerialized] public Vector2 initialAnchoredPosition;
    }

    [Header("Backgrounds")]
    [SerializeField] private BackgroundEntry[] backgrounds;
    [SerializeField] private BackgroundType defaultBackground;

    [Header("Transition")]
    [SerializeField] private BackgroundTransitionMode transitionMode = BackgroundTransitionMode.Slide;
    [SerializeField] private UINoBackgroundTransition noTransition;
    [SerializeField] private UIBackgroundSlideTransition slideTransition;
    [SerializeField] private UIBackgroundPopTransition popTransition;

    private readonly Dictionary<BackgroundType, BackgroundEntry> backgroundMap = new();

    private BackgroundEntry currentEntry;
    private BackgroundType currentBackground;
    private string currentVisualToken;
    private bool isTransitioning;
    private Coroutine pendingHideRoutine;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        BuildMap();
        Initialize(defaultBackground);
    }

    public void ShowBackground(BackgroundType type)
    {
        CancelPendingHide();

        if (isTransitioning)
        {
            Debug.LogWarning("Background transition is already playing.");
            return;
        }

        if (!TryGetEntry(type, out BackgroundEntry nextEntry))
            return;

        if (currentEntry != null && currentEntry.type == type)
            return;

        string nextVisualToken = GetVisualToken(nextEntry);

        if (currentEntry != null && currentVisualToken == nextVisualToken)
        {
            currentBackground = type;
            return;
        }

        UIBackgroundTransitionBase transition = GetSelectedTransition();

        if (currentEntry == null || transition == null)
        {
            SwitchImmediately(nextEntry);
            return;
        }

        isTransitioning = true;
        BackgroundEntry previousEntry = currentEntry;

        transition.Play(
            previousEntry.rect,
            previousEntry.initialAnchoredPosition,
            nextEntry.rect,
            nextEntry.initialAnchoredPosition,
            () =>
            {
                currentEntry = nextEntry;
                currentBackground = nextEntry.type;
                currentVisualToken = nextVisualToken;
                isTransitioning = false;
            });
    }

    public void HideBackground(BackgroundType type)
    {
        if (currentBackground != type)
            return;

        ScheduleHideToDefault();
    }

    public void HideCurrentBackground()
    {
        ScheduleHideToDefault();
    }

    public void SetTransitionMode(BackgroundTransitionMode mode)
    {
        transitionMode = mode;
    }

    private UIBackgroundTransitionBase GetSelectedTransition()
    {
        switch (transitionMode)
        {
            case BackgroundTransitionMode.None:
                return noTransition;
            case BackgroundTransitionMode.Pop:
                return popTransition;
            case BackgroundTransitionMode.Slide:
            default:
                return slideTransition;
        }
    }

    private void BuildMap()
    {
        backgroundMap.Clear();

        if (backgrounds == null || backgrounds.Length == 0)
        {
            Debug.LogWarning("The backgrounds array is empty.");
            return;
        }

        for (int i = 0; i < backgrounds.Length; i++)
        {
            BackgroundEntry entry = backgrounds[i];

            if (entry == null)
            {
                Debug.LogWarning($"backgrounds[{i}] is null.");
                continue;
            }

            if (entry.targetImage == null)
            {
                Debug.LogWarning($"backgrounds[{i}] ({entry.type}) is missing its Image reference.");
                continue;
            }

            if (backgroundMap.ContainsKey(entry.type))
            {
                Debug.LogWarning($"Duplicate background type found: {entry.type}");
                continue;
            }

            RectTransform rect = entry.targetImage.rectTransform;

            if (rect == null)
            {
                Debug.LogWarning($"backgrounds[{i}] ({entry.type}) is missing its RectTransform.");
                continue;
            }

            entry.rect = rect;
            entry.initialAnchoredPosition = rect.anchoredPosition;

            backgroundMap.Add(entry.type, entry);
        }
    }

    private void Initialize(BackgroundType startType)
    {
        if (!TryGetEntry(startType, out BackgroundEntry startEntry))
            return;

        foreach (BackgroundEntry entry in backgroundMap.Values)
        {
            if (entry.rect == null)
                continue;

            entry.rect.anchoredPosition = entry.initialAnchoredPosition;
            entry.rect.localScale = Vector3.one;
            entry.targetImage.gameObject.SetActive(entry.type == startType);
        }

        startEntry.rect.SetAsLastSibling();

        currentEntry = startEntry;
        currentBackground = startType;
        currentVisualToken = GetVisualToken(startEntry);
        isTransitioning = false;
    }

    private void SwitchImmediately(BackgroundEntry nextEntry)
    {
        foreach (BackgroundEntry entry in backgroundMap.Values)
        {
            if (entry.rect == null)
                continue;

            bool shouldShow = entry.type == nextEntry.type;

            entry.rect.anchoredPosition = entry.initialAnchoredPosition;
            entry.rect.localScale = Vector3.one;
            entry.targetImage.gameObject.SetActive(shouldShow);
        }

        nextEntry.rect.SetAsLastSibling();

        currentEntry = nextEntry;
        currentBackground = nextEntry.type;
        currentVisualToken = GetVisualToken(nextEntry);
        isTransitioning = false;
    }

    private void ScheduleHideToDefault()
    {
        CancelPendingHide();
        pendingHideRoutine = StartCoroutine(HideToDefaultNextFrame());
    }

    private void CancelPendingHide()
    {
        if (pendingHideRoutine == null)
            return;

        StopCoroutine(pendingHideRoutine);
        pendingHideRoutine = null;
    }

    private IEnumerator HideToDefaultNextFrame()
    {
        pendingHideRoutine = null;

        if (currentBackground == defaultBackground)
            yield break;

        ShowBackground(defaultBackground);
    }

    private string GetVisualToken(BackgroundEntry entry)
    {
        if (entry == null || entry.targetImage == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(entry.visualKey))
            return entry.visualKey;

        Sprite sprite = entry.targetImage.sprite;
        if (sprite != null)
            return $"sprite:{sprite.GetInstanceID()}";

        return $"image:{entry.targetImage.GetInstanceID()}";
    }

    private bool TryGetEntry(BackgroundType type, out BackgroundEntry entry)
    {
        if (!backgroundMap.TryGetValue(type, out entry))
        {
            Debug.LogWarning($"Background type is not registered: {type}");
            return false;
        }

        if (entry == null || entry.targetImage == null || entry.rect == null)
        {
            Debug.LogWarning($"Background reference is incomplete: {type}");
            return false;
        }

        return true;
    }
}
