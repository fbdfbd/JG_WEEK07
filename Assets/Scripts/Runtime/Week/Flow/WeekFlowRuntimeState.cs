using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class WeekFlowRuntimeState
{
    private readonly List<SO_InteractiveEventDefinition> _pendingDayEvents = new();
    private readonly List<SO_InteractiveEventDefinition> _pendingNightEvents = new();
    private readonly List<SO_EventResultDefinition> _pendingWeeklyResultLogs = new();
    private readonly List<RuntimeWeeklyResultLogRecord> _weeklyResultLogHistory = new();
    private Dictionary<EChildStatusType, int> _weekStartStats = new();
    private int _nextDayEventIndex;
    private int _nextNightEventIndex;
    private int _currentDayEventIndex = -1;
    private bool _currentEventStartedFromDayFlow;
    private bool _weeklyResultLogConsumed;
    private bool _weeklyStatResultConsumed = true;
    public event Action<RuntimeChildState> ChildStateReplaced;
    public event Action<DayFlowProgressSnapshot> DayFlowProgressChanged;

    public WeekFlowRuntimeState()
    {
        SetChildState(new RuntimeChildState(), false);
        ClearPendingEventState();
        StatusMessage = "Week flow is ready.";
    }

    public RuntimeChildState ChildState { get; private set; }
    public RuntimeWeekResult LastWeekResult { get; set; }
    public IReadOnlyList<RuntimeWeeklyResultLogRecord> WeeklyResultLogHistory => _weeklyResultLogHistory;
    public IReadOnlyDictionary<EChildStatusType, int> WeekStartStats => _weekStartStats;
    public bool HasPendingWeeklyStatResult => !_weeklyStatResultConsumed && _weekStartStats.Count > 0;
    public RuntimeInteractiveEventSession CurrentEventSession { get; private set; }
    public bool ShouldShowEndingAfterEvents { get; set; }
    public bool ShouldAdvanceToNextWeekAfterEvents { get; set; }
    public bool HasReachedEnding { get; set; }
    public bool IsAwaitingEndingFollowUp { get; set; }
    public string StatusMessage { get; private set; }

    public void ResetChildState()
    {
        SetChildState(new RuntimeChildState(), true);
        LastWeekResult = null;
        _weeklyResultLogHistory.Clear();
        HasReachedEnding = false;
        IsAwaitingEndingFollowUp = false;
        ClearPendingEventState();
    }

    public bool IsCurrentEventFromDayFlow => _currentEventStartedFromDayFlow;
    public DayFlowProgressSnapshot DayFlowProgress { get; private set; } = DayFlowProgressSnapshot.Hidden;

    public void SetPendingDayEvents(IReadOnlyList<SO_InteractiveEventDefinition> pendingEvents)
    {
        _pendingDayEvents.Clear();
        AddResolvableEvents(_pendingDayEvents, pendingEvents);
        _nextDayEventIndex = 0;
        _currentDayEventIndex = -1;
        CurrentEventSession = null;
        _currentEventStartedFromDayFlow = false;
        PublishDayFlowProgress(false);
    }

    public void SetPendingNightEvents(IReadOnlyList<SO_InteractiveEventDefinition> pendingEvents)
    {
        _pendingNightEvents.Clear();
        _pendingNightEvents.AddRange(pendingEvents ?? Array.Empty<SO_InteractiveEventDefinition>());
        _nextNightEventIndex = 0;
    }

    public void CaptureWeekStartStats()
    {
        _weekStartStats = WeekFlowQueryUtility.CaptureCurrentStats(ChildState);
        _weeklyStatResultConsumed = false;
    }

    public void MarkWeeklyStatResultConsumed()
    {
        _weeklyStatResultConsumed = true;
    }

    public void AddWeeklyResultLog(SO_EventResultDefinition resultLog)
    {
        if (resultLog != null)
        {
            _pendingWeeklyResultLogs.Add(resultLog);
        }
    }

    public bool TryConsumeWeeklyResultLogs(out SO_EventResultDefinition[] resultLogs)
    {
        if (_weeklyResultLogConsumed || _pendingWeeklyResultLogs.Count == 0)
        {
            resultLogs = Array.Empty<SO_EventResultDefinition>();
            return false;
        }

        _weeklyResultLogConsumed = true;
        resultLogs = _pendingWeeklyResultLogs
            .FindAll(resultLog => resultLog != null)
            .ToArray();
        return resultLogs.Length > 0;
    }

    public void AddWeeklyResultLogHistory(
        SO_WeekDefinition weekDefinition,
        IReadOnlyList<SO_EventResultDefinition> resultLogs)
    {
        if (weekDefinition == null || resultLogs == null || resultLogs.Count == 0)
        {
            return;
        }

        string weekId = weekDefinition.Id;
        _weeklyResultLogHistory.RemoveAll(record => record.IsForWeek(weekId));
        _weeklyResultLogHistory.Add(new RuntimeWeeklyResultLogRecord(weekDefinition, resultLogs));
    }

    public bool TryStartNextDayEvent()
    {
        return TryStartNextEvent(_pendingDayEvents, ref _nextDayEventIndex, true);
    }

    public bool TryStartNextNightEvent()
    {
        return TryStartNextEvent(_pendingNightEvents, ref _nextNightEventIndex, false);
    }

    public void ClearCurrentEventSession()
    {
        CurrentEventSession = null;
    }

    public void ClearPendingEventState()
    {
        _pendingDayEvents.Clear();
        _pendingNightEvents.Clear();
        _pendingWeeklyResultLogs.Clear();
        _nextDayEventIndex = 0;
        _nextNightEventIndex = 0;
        _currentDayEventIndex = -1;
        _weeklyResultLogConsumed = false;
        _weeklyStatResultConsumed = true;
        _weekStartStats.Clear();
        CurrentEventSession = null;
        _currentEventStartedFromDayFlow = false;
        PublishDayFlowProgress(false);
        ShouldShowEndingAfterEvents = false;
        ShouldAdvanceToNextWeekAfterEvents = false;
        IsAwaitingEndingFollowUp = false;
    }

    public void SetStatusMessage(string statusMessage)
    {
        StatusMessage = statusMessage;
    }

    private void SetChildState(RuntimeChildState childState, bool notify)
    {
        ChildState = childState;
        if (notify)
        {
            ChildStateReplaced?.Invoke(ChildState);
        }
    }

    private bool TryStartNextEvent(
        IReadOnlyList<SO_InteractiveEventDefinition> pendingEvents,
        ref int nextEventIndex,
        bool isDayFlowEvent)
    {
        while (nextEventIndex < pendingEvents.Count)
        {
            SO_InteractiveEventDefinition nextEvent = pendingEvents[nextEventIndex++];
            SO_InteractiveEventStepDefinition initialStep = WeekEventRuntimeAugmentationService.ResolveInitialStep(
                nextEvent,
                ChildState);
            if (nextEvent == null || initialStep == null)
            {
                continue;
            }

            CurrentEventSession = new RuntimeInteractiveEventSession(nextEvent, initialStep);
            _currentEventStartedFromDayFlow = isDayFlowEvent;
            if (isDayFlowEvent)
            {
                _currentDayEventIndex = nextEventIndex - 1;
                PublishDayFlowProgress(true);
            }
            return true;
        }

        CurrentEventSession = null;
        _currentEventStartedFromDayFlow = false;
        if (isDayFlowEvent)
        {
            _currentDayEventIndex = -1;
            PublishDayFlowProgress(false);
        }
        return false;
    }

    private void PublishDayFlowProgress(bool isActive)
    {
        int completedCount = isActive
            ? Mathf.Max(0, _currentDayEventIndex)
            : Mathf.Clamp(_nextDayEventIndex, 0, _pendingDayEvents.Count);
        DayFlowProgress = new DayFlowProgressSnapshot(
            _pendingDayEvents.Count,
            _currentDayEventIndex,
            completedCount,
            isActive);
        DayFlowProgressChanged?.Invoke(DayFlowProgress);
    }

    private void AddResolvableEvents(
        List<SO_InteractiveEventDefinition> destination,
        IReadOnlyList<SO_InteractiveEventDefinition> source)
    {
        if (source == null)
        {
            return;
        }

        for (int index = 0; index < source.Count; index++)
        {
            SO_InteractiveEventDefinition eventDefinition = source[index];
            if (eventDefinition == null ||
                WeekEventRuntimeAugmentationService.ResolveInitialStep(eventDefinition, ChildState) == null)
            {
                continue;
            }

            destination.Add(eventDefinition);
        }
    }
}

public sealed class RuntimeWeeklyResultLogRecord
{
    private readonly SO_EventResultDefinition[] _resultLogs;

    public RuntimeWeeklyResultLogRecord(
        SO_WeekDefinition weekDefinition,
        IReadOnlyList<SO_EventResultDefinition> resultLogs)
    {
        WeekId = weekDefinition != null ? weekDefinition.Id : string.Empty;
        WeekIndex = weekDefinition != null ? weekDefinition.WeekIndex : 0;
        WeekTitle = weekDefinition != null ? weekDefinition.Title : string.Empty;
        _resultLogs = CopyLogs(resultLogs);
    }

    public string WeekId { get; }
    public int WeekIndex { get; }
    public string WeekTitle { get; }
    public IReadOnlyList<SO_EventResultDefinition> ResultLogs => _resultLogs;

    public bool IsForWeek(string weekId)
    {
        return string.Equals(WeekId, weekId, StringComparison.OrdinalIgnoreCase);
    }

    private static SO_EventResultDefinition[] CopyLogs(IReadOnlyList<SO_EventResultDefinition> resultLogs)
    {
        if (resultLogs == null || resultLogs.Count == 0)
        {
            return Array.Empty<SO_EventResultDefinition>();
        }

        List<SO_EventResultDefinition> copiedLogs = new();
        for (int index = 0; index < resultLogs.Count; index++)
        {
            if (resultLogs[index] != null)
            {
                copiedLogs.Add(resultLogs[index]);
            }
        }

        return copiedLogs.ToArray();
    }
}

public sealed class RuntimeInteractiveEventSession
{
    public RuntimeInteractiveEventSession(SO_InteractiveEventDefinition eventDefinition)
        : this(eventDefinition, eventDefinition != null ? eventDefinition.FirstStep : null)
    {
    }

    public RuntimeInteractiveEventSession(
        SO_InteractiveEventDefinition eventDefinition,
        SO_InteractiveEventStepDefinition initialStep)
    {
        EventDefinition = eventDefinition;
        CurrentStep = initialStep;
    }

    public SO_InteractiveEventDefinition EventDefinition { get; }
    public SO_InteractiveEventStepDefinition CurrentStep { get; private set; }
    public InteractiveEventChoiceData SelectedChoice { get; private set; }
    public bool HasAppliedLinkedCardRewards { get; private set; }
    public bool HasAppliedCurrentStepEffects { get; private set; }
    public bool HasAppliedCurrentStep => HasAppliedCurrentStepEffects;
    public bool HasPendingChoiceResult => SelectedChoice != null;

    public void MarkLinkedCardRewardsApplied()
    {
        HasAppliedLinkedCardRewards = true;
    }

    public void MarkCurrentStepEffectsApplied()
    {
        HasAppliedCurrentStepEffects = true;
    }

    public void MarkCurrentStepApplied()
    {
        MarkCurrentStepEffectsApplied();
    }

    public void SelectChoice(InteractiveEventChoiceData choice)
    {
        SelectedChoice = choice;
    }

    public void ClearChoiceResult()
    {
        SelectedChoice = null;
    }

    public bool TryMoveToNextStep(RuntimeChildState childState)
    {
        SO_InteractiveEventStepDefinition nextStep = ResolveNextStep(childState);
        if (nextStep == null)
        {
            return false;
        }

        CurrentStep = nextStep;
        SelectedChoice = null;
        HasAppliedCurrentStepEffects = false;
        return true;
    }

    private SO_InteractiveEventStepDefinition ResolveNextStep(RuntimeChildState childState)
    {
        if (SelectedChoice?.NextStep != null)
        {
            return SelectedChoice.NextStep;
        }

        if (CurrentStep?.ConditionalNext != null &&
            CurrentStep.ConditionalNext.TryResolve(childState, out SO_InteractiveEventStepDefinition conditionalNextStep))
        {
            return conditionalNextStep;
        }

        return CurrentStep != null ? CurrentStep.NextStep : null;
    }
}

public static class WeekEventRuntimeAugmentationService
{
    private static readonly List<ConditionalIntroRule> ConditionalIntroRules = new();
    private static readonly List<CompletionRule> CompletionRules = new();

    public static void Clear()
    {
        ConditionalIntroRules.Clear();
        CompletionRules.Clear();
    }

    public static void RegisterConditionalIntro(
        string eventId,
        Func<RuntimeChildState, bool> predicate,
        Func<SO_InteractiveEventDefinition, SO_InteractiveEventStepDefinition> introFactory)
    {
        if (string.IsNullOrWhiteSpace(eventId) || predicate == null || introFactory == null)
        {
            return;
        }

        ConditionalIntroRules.Add(new ConditionalIntroRule(eventId, predicate, introFactory));
    }

    public static void RegisterCompletionAction(
        string eventId,
        Action<RuntimeChildState> completionAction)
    {
        if (string.IsNullOrWhiteSpace(eventId) || completionAction == null)
        {
            return;
        }

        CompletionRules.Add(new CompletionRule(eventId, completionAction));
    }

    public static SO_InteractiveEventStepDefinition ResolveInitialStep(
        SO_InteractiveEventDefinition eventDefinition,
        RuntimeChildState childState)
    {
        if (eventDefinition?.FirstStep == null || childState == null)
        {
            return eventDefinition?.FirstStep;
        }

        string eventId = eventDefinition.Id;
        foreach (ConditionalIntroRule rule in ConditionalIntroRules)
        {
            if (!rule.Matches(eventId, childState))
            {
                continue;
            }

            SO_InteractiveEventStepDefinition introStep = rule.CreateIntroStep(eventDefinition);
            if (introStep != null)
            {
                return introStep;
            }
        }

        return eventDefinition.FirstStep;
    }

    public static void ApplyOnCompleted(
        SO_InteractiveEventDefinition eventDefinition,
        RuntimeChildState childState)
    {
        if (eventDefinition == null || childState == null)
        {
            return;
        }

        string eventId = eventDefinition.Id;
        foreach (CompletionRule rule in CompletionRules)
        {
            if (!rule.Matches(eventId))
            {
                continue;
            }

            rule.Apply(childState);
        }
    }

    private sealed class ConditionalIntroRule
    {
        private readonly string _eventId;
        private readonly Func<RuntimeChildState, bool> _predicate;
        private readonly Func<SO_InteractiveEventDefinition, SO_InteractiveEventStepDefinition> _introFactory;

        public ConditionalIntroRule(
            string eventId,
            Func<RuntimeChildState, bool> predicate,
            Func<SO_InteractiveEventDefinition, SO_InteractiveEventStepDefinition> introFactory)
        {
            _eventId = eventId;
            _predicate = predicate;
            _introFactory = introFactory;
        }

        public bool Matches(string eventId, RuntimeChildState childState)
        {
            return string.Equals(_eventId, eventId, StringComparison.OrdinalIgnoreCase) &&
                   _predicate(childState);
        }

        public SO_InteractiveEventStepDefinition CreateIntroStep(SO_InteractiveEventDefinition eventDefinition)
        {
            return _introFactory(eventDefinition);
        }
    }

    private sealed class CompletionRule
    {
        private readonly string _eventId;
        private readonly Action<RuntimeChildState> _completionAction;

        public CompletionRule(string eventId, Action<RuntimeChildState> completionAction)
        {
            _eventId = eventId;
            _completionAction = completionAction;
        }

        public bool Matches(string eventId)
        {
            return string.Equals(_eventId, eventId, StringComparison.OrdinalIgnoreCase);
        }

        public void Apply(RuntimeChildState childState)
        {
            _completionAction(childState);
        }
    }
}

public static class Week002PresenceRuntimeBootstrap
{
    private const string Week001EtiquetteDirectId = "event_week_001_etiquette_direct";
    private const string Week002EtiquetteDirectId = "event_week_002_etiquette_direct";
    private const string Week002NatureDirectId = "event_week_002_nature_direct";
    private const string Week001EtiquetteDirectFlagId = "test_w001_etiquette_direct_seen";

    private static bool _isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRules()
    {
        _isRegistered = false;
        WeekEventRuntimeAugmentationService.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterRules()
    {
        if (_isRegistered)
        {
            return;
        }

        _isRegistered = true;

        SO_FlagDefinition etiquetteDirectFlag = CreateRuntimeFlag(
            Week001EtiquetteDirectFlagId,
            "1주차 예절 Direct 확인");

        WeekEventRuntimeAugmentationService.RegisterCompletionAction(
            Week001EtiquetteDirectId,
            childState => childState.SetFlag(etiquetteDirectFlag));

        WeekEventRuntimeAugmentationService.RegisterConditionalIntro(
            Week002EtiquetteDirectId,
            childState => childState.HasFlag(etiquetteDirectFlag),
            eventDefinition => CreateIntroStep(
                "지난주 배운 손놀림",
                "네모는 식탁에 앉자마자 지난주 배운 대로 먼저 손목 각도부터 맞춰 본다.",
                new[]
                {
                    "(식탁에 앉자, 네모가 스푼을 들기 전에 손끝과 손목을 먼저 고쳐 잡는다.)",
                    "이렇게 들면 저번보다 덜 튀겠지.",
                    "지난번 순서를 기억하고 계시는군요. 이번엔 더 자연스럽습니다.",
                },
                ENemoVisualState.Obedient,
                eventDefinition?.FirstStep));

        WeekEventRuntimeAugmentationService.RegisterConditionalIntro(
            Week002NatureDirectId,
            childState => childState.GetStat(EChildStatusType.Curiosity) >= 5,
            eventDefinition => CreateIntroStep(
                "먼저 살피는 버릇",
                "네모는 꽃과 벌레를 보기 전에 스스로 몸을 가까이 기울이며 먼저 무늬와 움직임을 찾는다.",
                new[]
                {
                    "(네모가 손을 뻗기 전에 먼저 고개를 들이밀어 잎맥과 벌레 다리를 살핀다.)",
                    "가만히 있어 봐. 저거, 아까랑 다르게 움직여.",
                    "이번엔 먼저 관찰하려 드시는군요. 좋아요, 천천히 같이 봅시다.",
                },
                ENemoVisualState.Curious,
                eventDefinition?.FirstStep));
    }

    private static SO_FlagDefinition CreateRuntimeFlag(string id, string displayName)
    {
        SO_FlagDefinition flag = ScriptableObject.CreateInstance<SO_FlagDefinition>();
        PrepareRuntimeAsset(flag, displayName);
        SetSerializedField(flag, "_id", id);
        SetSerializedField(flag, "_displayName", displayName);
        SetSerializedField(flag, "_description", displayName);
        return flag;
    }

    private static SO_InteractiveEventStepDefinition CreateIntroStep(
        string title,
        string bodyText,
        string[] dialogueTexts,
        ENemoVisualState visualState,
        SO_InteractiveEventStepDefinition nextStep)
    {
        SO_InteractiveEventStepDefinition step = ScriptableObject.CreateInstance<SO_InteractiveEventStepDefinition>();
        PrepareRuntimeAsset(step, title);
        SetSerializedField(step, "_titleOverride", title);
        SetSerializedField(step, "_bodyText", bodyText);
        SetSerializedField(step, "_dialogueLines", CreateDialogueLines(dialogueTexts));
        SetSerializedField(step, "_nemoLine", dialogueTexts != null && dialogueTexts.Length > 0 ? dialogueTexts[0] : string.Empty);
        SetSerializedField(step, "_useCustomVisualState", true);
        SetSerializedField(step, "_visualState", visualState);
        SetSerializedField(step, "_onEnterInteractions", Array.Empty<SO_CardInteractionDefinition>());
        SetSerializedField(step, "_choices", Array.Empty<InteractiveEventChoiceData>());
        SetSerializedField(step, "_nextStep", nextStep);
        SetSerializedField(step, "_cinematicCues", new WeekFlowScreenCues());
        return step;
    }

    private static DialogueLineData[] CreateDialogueLines(string[] dialogueTexts)
    {
        if (dialogueTexts == null || dialogueTexts.Length == 0)
        {
            return Array.Empty<DialogueLineData>();
        }

        DialogueLineData[] lines = new DialogueLineData[dialogueTexts.Length];
        for (int index = 0; index < dialogueTexts.Length; index++)
        {
            DialogueLineData line = new();
            SetSerializedField(line, "_speaker", null);
            SetSerializedField(line, "_text", dialogueTexts[index]);
            lines[index] = line;
        }

        return lines;
    }

    private static void PrepareRuntimeAsset(UnityEngine.Object asset, string assetName)
    {
        if (asset == null)
        {
            return;
        }

        asset.name = assetName;
        asset.hideFlags = HideFlags.HideAndDontSave;
    }

    private static void SetSerializedField(object target, string fieldName, object value)
    {
        Type currentType = target.GetType();
        while (currentType != null)
        {
            FieldInfo field = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            currentType = currentType.BaseType;
        }
    }
}
