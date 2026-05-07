using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class WeekFlowPlaytesterController : MonoBehaviour
{
    private enum PlaytestPhase
    {
        PreTurnSelection,
        WeekFeedback,
        InteractiveEvent,
        InteractiveEventResult,
        ReadyForNextWeek,
        Ending,
    }

    [Header("Optional Real Weeks")]
    [SerializeField] private SO_WeekDefinition[] _weekDefinitions = Array.Empty<SO_WeekDefinition>();

    private readonly WeekRunner _weekRunner = new();
    private readonly WeekUiTextProvider _weekUiText = new(null);
    private readonly Dictionary<SO_CardInfoDefinition, int> _selectedOptionByCard = new();
    private readonly List<SO_InteractiveEventDefinition> _pendingEvents = new();
    private readonly List<string> _eventHistory = new();

    private SO_WeekDefinition[] _weeks = Array.Empty<SO_WeekDefinition>();
    private RuntimeChildState _childState;
    private RuntimeWeekResult _lastWeekResult;
    private RuntimeInteractiveEventSession _currentEventSession;
    private WeekFeedbackPresentation _weekFeedback;
    private InteractiveEventPresentation _eventPresentation;
    private InteractiveEventChoiceResultPresentation _eventChoiceResult;
    private NemoFeedbackPresentation _nemoFeedback;
    private EndingPresentation _endingPresentation;
    private Vector2 _scrollPosition;
    private PlaytestPhase _phase;
    private string _statusMessage = "Playtester is ready.";
    private int _currentWeekArrayIndex;
    private int _nextPendingEventIndex;
    private bool _showWeekFeedback;
    private bool _showChoiceResult;
    private bool _showEnding;

    private SO_WeekDefinition CurrentWeek =>
        _weeks.Length == 0 || _currentWeekArrayIndex < 0 || _currentWeekArrayIndex >= _weeks.Length
            ? null
            : _weeks[_currentWeekArrayIndex];

    private bool IsFinalWeek => _currentWeekArrayIndex >= _weeks.Length - 1;

    private void Awake()
    {
        InitializeWeeks();
        RestartScenario();
    }

    private void OnGUI()
    {
        float columnWidth = Mathf.Max(340f, (Screen.width - 56f) / 3f);
        GUI.Box(new Rect(8f, 8f, Screen.width - 16f, Screen.height - 16f), "Week Flow Playtester");
        GUILayout.BeginArea(new Rect(20f, 36f, Screen.width - 40f, Screen.height - 48f));
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        DrawHeader();
        DrawCommands();
        GUILayout.BeginHorizontal();
        DrawSelectionColumn(columnWidth);
        DrawCenterColumn(columnWidth);
        DrawDebugColumn(columnWidth);
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void InitializeWeeks()
    {
        _weeks = _weekDefinitions?
            .Where(week => week != null)
            .OrderBy(week => week.WeekIndex)
            .ToArray()
            ?? Array.Empty<SO_WeekDefinition>();

        if (_weeks.Length == 0)
        {
            _weeks = CreateGeneratedWeeks();
        }
    }

    private void RestartScenario()
    {
        _currentWeekArrayIndex = 0;
        _childState = new RuntimeChildState();
        _eventHistory.Clear();
        ResetWeekState();
        ResetSelectionsForCurrentWeek();
        UpdateNemoFromCurrentState();
        _phase = PlaytestPhase.PreTurnSelection;
        _statusMessage = "Scenario restarted.";
    }

    private void ResetSelectionsForCurrentWeek()
    {
        _selectedOptionByCard.Clear();
        foreach (WeekCardEntryData cardEntry in GetCurrentWeekCards())
        {
            if (cardEntry?.Card != null)
            {
                _selectedOptionByCard[cardEntry.Card] = 0;
            }
        }
    }

    private void ResetWeekState()
    {
        _lastWeekResult = null;
        _pendingEvents.Clear();
        _nextPendingEventIndex = 0;
        _currentEventSession = null;
        _showWeekFeedback = false;
        _showChoiceResult = false;
        _showEnding = false;
    }

    private void RunCurrentWeek()
    {
        if (_phase != PlaytestPhase.PreTurnSelection || CurrentWeek == null)
        {
            return;
        }

        Dictionary<EChildStatusType, int> previousStats = CaptureCurrentStats();
        _childState.ClearReactionLogs();
        WeekCardEntryData[] entries = GetCurrentWeekCards();
        _lastWeekResult = _weekRunner.RunWeek(CurrentWeek, _childState, BuildSelections(entries), entries);
        _weekFeedback = WeekFeedbackResolver.Resolve(CurrentWeek, _lastWeekResult, _childState, previousStats);
        _pendingEvents.Clear();
        _pendingEvents.AddRange(WeekNarrativeResolver.ResolvePendingEvents(
            CurrentWeek,
            _childState,
            _lastWeekResult));
        _nextPendingEventIndex = 0;
        _showWeekFeedback = true;
        _phase = PlaytestPhase.WeekFeedback;
        _statusMessage = $"Week {CurrentWeek.WeekIndex} completed.";
        UpdateNemoFromCurrentState();
        _eventHistory.Add($"W{CurrentWeek.WeekIndex} run completed.");
    }

    private void ContinueAfterWeekFeedback()
    {
        _showWeekFeedback = false;
        StartNextEventOrFinishWeek();
    }

    private void StartNextEventOrFinishWeek()
    {
        while (_nextPendingEventIndex < _pendingEvents.Count)
        {
            SO_InteractiveEventDefinition nextEvent = _pendingEvents[_nextPendingEventIndex++];
            if (nextEvent?.FirstStep == null)
            {
                continue;
            }

            _currentEventSession = new RuntimeInteractiveEventSession(nextEvent);
            ShowCurrentEventStep();
            return;
        }

        _currentEventSession = null;
        if (IsFinalWeek)
        {
            ShowEnding();
            return;
        }

        _phase = PlaytestPhase.ReadyForNextWeek;
        _statusMessage = $"Week {CurrentWeek.WeekIndex} is finished. Move to the next week.";
        UpdateNemoFromCurrentState();
    }

    private void ShowCurrentEventStep()
    {
        if (_currentEventSession?.CurrentStep == null)
        {
            StartNextEventOrFinishWeek();
            return;
        }

        if (!_currentEventSession.HasAppliedCurrentStep)
        {
            GameplayInteractionExecutor.ApplyAll(_currentEventSession.CurrentStep.OnEnterInteractions, _childState);
            _currentEventSession.MarkCurrentStepApplied();
        }

        _eventPresentation = WeekNarrativeResolver.CreatePresentation(_currentEventSession, _childState, _weekUiText);
        _showChoiceResult = false;
        _phase = PlaytestPhase.InteractiveEvent;
        _statusMessage = $"Event step: {_eventPresentation.Title}";
        DialogueLinePresentation primaryDialogueLine = WeekNarrativeResolver.GetPrimaryDialogueLine(_eventPresentation.DialogueLines);
        _nemoFeedback = new NemoFeedbackPresentation(
            primaryDialogueLine.SpeakerName,
            _eventPresentation.VisualState,
            primaryDialogueLine.Text);
        _eventHistory.Add($"Event started: {_currentEventSession.EventDefinition.Title}");
    }

    private void SelectEventChoice(int choiceIndex)
    {
        if (_currentEventSession?.CurrentStep?.Choices == null || _showChoiceResult)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= _currentEventSession.CurrentStep.Choices.Length)
        {
            return;
        }

        InteractiveEventChoiceData selectedChoice = _currentEventSession.CurrentStep.Choices[choiceIndex];
        _currentEventSession.SelectChoice(selectedChoice);
        GameplayInteractionExecutor.ApplyAll(selectedChoice.Interactions, _childState);
        _eventChoiceResult = WeekNarrativeResolver.CreateChoiceResultPresentation(selectedChoice, _weekUiText);
        _showChoiceResult = true;
        _phase = PlaytestPhase.InteractiveEventResult;
        _statusMessage = $"Choice applied: {selectedChoice.Label}";
        DialogueLinePresentation primaryDialogueLine = WeekNarrativeResolver.GetPrimaryDialogueLine(_eventChoiceResult.DialogueLines);
        _nemoFeedback = new NemoFeedbackPresentation(
            primaryDialogueLine.SpeakerName,
            WeekNarrativeResolver.GetVisualStateForCurrentState(_childState),
            primaryDialogueLine.Text);
        _eventHistory.Add($"Choice selected: {selectedChoice.Label}");
    }

    private void ContinueInteractiveEvent()
    {
        if (_currentEventSession == null)
        {
            return;
        }

        if (_showChoiceResult && _currentEventSession.HasPendingChoiceResult)
        {
            _showChoiceResult = false;
        }

        if (_currentEventSession.TryMoveToNextStep(_childState))
        {
            ShowCurrentEventStep();
            return;
        }

        CompleteCurrentEvent();
    }

    private void CompleteCurrentEvent()
    {
        GameplayInteractionExecutor.ApplyAll(_currentEventSession.EventDefinition.OnCompletedInteractions, _childState);
        _eventHistory.Add($"Event completed: {_currentEventSession.EventDefinition.Title}");
        _currentEventSession = null;
        StartNextEventOrFinishWeek();
    }

    private void MoveToNextWeek()
    {
        if (IsFinalWeek)
        {
            return;
        }

        _currentWeekArrayIndex++;
        ResetWeekState();
        ResetSelectionsForCurrentWeek();
        UpdateNemoFromCurrentState();
        _phase = PlaytestPhase.PreTurnSelection;
        _statusMessage = $"Ready for week {CurrentWeek.WeekIndex}.";
    }

    private void ResetCurrentSelections()
    {
        if (_phase == PlaytestPhase.PreTurnSelection)
        {
            ResetSelectionsForCurrentWeek();
            _statusMessage = "Selections reset.";
        }
    }

    private void ResetChildState()
    {
        _childState = new RuntimeChildState();
        ResetWeekState();
        ResetSelectionsForCurrentWeek();
        UpdateNemoFromCurrentState();
        _phase = PlaytestPhase.PreTurnSelection;
        _statusMessage = "Child state reset.";
    }

    private void ShowEnding()
    {
        _endingPresentation = EndingResolver.Resolve(_childState);
        _showEnding = true;
        _phase = PlaytestPhase.Ending;
        _statusMessage = $"Ending reached: {_endingPresentation.EndingId}";
        _nemoFeedback = new NemoFeedbackPresentation(_endingPresentation.VisualState, _endingPresentation.ClosingLine);
        _eventHistory.Add($"Ending reached: {_endingPresentation.EndingId}");
    }

    private void UpdateNemoFromCurrentState()
    {
        RuntimeResolvedCardRecord lastResolvedCard = _lastWeekResult?.ResolvedCards?.LastOrDefault();
        _nemoFeedback = NemoFeedbackResolver.Resolve(_childState, lastResolvedCard);
    }

    private RuntimeWeekSelection[] BuildSelections(IReadOnlyList<WeekCardEntryData> weekCardEntries)
    {
        return (weekCardEntries ?? Array.Empty<WeekCardEntryData>())
            .Where(cardEntry => cardEntry?.Card != null)
            .Select(cardEntry => new RuntimeWeekSelection(
                cardEntry.Card,
                _selectedOptionByCard.TryGetValue(cardEntry.Card, out int optionIndex) ? optionIndex : 0))
            .ToArray();
    }

    private Dictionary<EChildStatusType, int> CaptureCurrentStats()
    {
        Dictionary<EChildStatusType, int> snapshot = new();
        foreach (EChildStatusType statType in Enum.GetValues(typeof(EChildStatusType)))
        {
            snapshot[statType] = _childState.GetStat(statType);
        }

        return snapshot;
    }

    private WeekCardEntryData[] GetCurrentWeekCards()
    {
        return WeekFlowQueryUtility.GetCurrentWeekEntries(CurrentWeek, _childState);
    }

    private void DrawHeader()
    {
        string weekLabel = CurrentWeek == null ? "-" : $"WEEK {CurrentWeek.WeekIndex}";
        string title = CurrentWeek == null ? "No Week" : CurrentWeek.Title;
        GUILayout.Label($"{weekLabel}  {title}");
        GUILayout.Label($"Phase: {_phase}");
        GUILayout.Label($"Status: {_statusMessage}");
    }

    private void DrawCommands()
    {
        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();
        GUI.enabled = _phase == PlaytestPhase.PreTurnSelection;
        if (GUILayout.Button("Run Week", GUILayout.Height(28f)))
        {
            RunCurrentWeek();
        }

        if (GUILayout.Button("Reset Selections", GUILayout.Height(28f)))
        {
            ResetCurrentSelections();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Reset Child State", GUILayout.Height(28f)))
        {
            ResetChildState();
        }

        GUI.enabled = _phase == PlaytestPhase.ReadyForNextWeek;
        if (GUILayout.Button("Next Week", GUILayout.Height(28f)))
        {
            MoveToNextWeek();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Restart Scenario", GUILayout.Height(28f)))
        {
            RestartScenario();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSelectionColumn(float width)
    {
        GUILayout.BeginVertical("box", GUILayout.Width(width));
        GUILayout.Label("PreTurn Information Control");

        foreach (WeekCardEntryData cardEntry in GetCurrentWeekCards())
        {
            if (cardEntry?.Card == null)
            {
                continue;
            }

            SO_CardInfoDefinition card = cardEntry.Card;
            int selectedIndex = _selectedOptionByCard.TryGetValue(card, out int value) ? value : 0;
            GUILayout.BeginVertical("box");
            GUILayout.Label($"[{card.CardType?.DisplayName ?? "Unknown"}] {card.Title}");
            GUILayout.Label(card.OriginalText ?? string.Empty);
            GUI.enabled = _phase == PlaytestPhase.PreTurnSelection;
            for (int optionIndex = 0; optionIndex < card.Options.Length; optionIndex++)
            {
                CardOptionData option = card.Options[optionIndex];
                string marker = optionIndex == selectedIndex ? "> " : string.Empty;
                if (GUILayout.Button($"{marker}{option.Label} ({option.Semantic})"))
                {
                    _selectedOptionByCard[card] = optionIndex;
                }
            }
            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        GUILayout.EndVertical();
    }

    private void DrawCenterColumn(float width)
    {
        GUILayout.BeginVertical("box", GUILayout.Width(width));
        GUILayout.Label("Turn Progress");

        if (_showWeekFeedback)
        {
            DrawWeekFeedbackPanel();
        }
        else if (_showChoiceResult)
        {
            DrawEventChoiceResultPanel();
        }
        else if (_currentEventSession != null)
        {
            DrawInteractiveEventPanel();
        }
        else if (_showEnding)
        {
            DrawEndingPanel();
        }
        else if (_phase == PlaytestPhase.ReadyForNextWeek)
        {
            GUILayout.Label("This week is finished. Move to the next week to continue.");
        }
        else
        {
            GUILayout.Label("Select information cards and press Run Week.");
        }

        GUILayout.Space(8f);
        GUILayout.Label($"{_nemoFeedback.SpeakerName}: {_nemoFeedback.VisualState}");
        GUILayout.Label(_nemoFeedback.DialogueLine ?? string.Empty);
        GUILayout.EndVertical();
    }

    private void DrawWeekFeedbackPanel()
    {
        GUILayout.Label(_weekFeedback.Title ?? "Week Feedback");
        foreach (string line in _weekFeedback.EventLines ?? Array.Empty<string>())
        {
            GUILayout.Label(line);
        }

        GUILayout.Label(_weekFeedback.SummaryLine ?? string.Empty);
        GUILayout.Label(_weekFeedback.StatDeltaLine ?? string.Empty);
        if (GUILayout.Button("Continue After Feedback", GUILayout.Height(28f)))
        {
            ContinueAfterWeekFeedback();
        }
    }

    private void DrawInteractiveEventPanel()
    {
        GUILayout.Label(_eventPresentation.Title ?? "Event");
        GUILayout.Label(_eventPresentation.BodyText ?? string.Empty);
        GUILayout.Label($"Effect: {_eventPresentation.EffectSummaryLine}");
        DrawDialogueLines(_eventPresentation.DialogueLines);

        if (_eventPresentation.Choices != null && _eventPresentation.Choices.Count > 0)
        {
            for (int choiceIndex = 0; choiceIndex < _eventPresentation.Choices.Count; choiceIndex++)
            {
                InteractiveEventChoicePresentation choice = _eventPresentation.Choices[choiceIndex];
                if (GUILayout.Button(choice.Label, GUILayout.Height(28f)))
                {
                    SelectEventChoice(choiceIndex);
                }
            }
        }
        else if (_eventPresentation.CanContinue && GUILayout.Button("Continue Event", GUILayout.Height(28f)))
        {
            ContinueInteractiveEvent();
        }
    }

    private void DrawEventChoiceResultPanel()
    {
        GUILayout.Label("Choice Result");
        DrawDialogueLines(_eventChoiceResult.DialogueLines);
        GUILayout.Label($"Effect: {_eventChoiceResult.EffectSummaryLine}");
        if (GUILayout.Button("Continue", GUILayout.Height(28f)))
        {
            ContinueInteractiveEvent();
        }
    }

    private void DrawDialogueLines(IReadOnlyList<DialogueLinePresentation> dialogueLines)
    {
        if (dialogueLines == null)
        {
            return;
        }

        foreach (DialogueLinePresentation dialogueLine in dialogueLines)
        {
            if (!dialogueLine.HasContent)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(dialogueLine.SpeakerName))
            {
                GUILayout.Label(dialogueLine.Text);
                continue;
            }

            GUILayout.Label($"{dialogueLine.SpeakerName}: {dialogueLine.Text}");
        }
    }

    private void DrawEndingPanel()
    {
        GUILayout.Label(_endingPresentation.Title ?? "Ending");
        foreach (string line in _endingPresentation.DetailLines ?? Array.Empty<string>())
        {
            GUILayout.Label(line);
        }

        GUILayout.Label(_endingPresentation.Summary ?? string.Empty);
        GUILayout.Label(_endingPresentation.ClosingLine ?? string.Empty);
        GUILayout.Label(_endingPresentation.ReputationLine ?? string.Empty);
    }

    private void DrawDebugColumn(float width)
    {
        GUILayout.BeginVertical("box", GUILayout.Width(width));
        GUILayout.Label("Debug State");

        foreach (EChildStatusType statType in Enum.GetValues(typeof(EChildStatusType)))
        {
            GUILayout.Label($"{statType}: {_childState.GetStat(statType)}");
        }

        string flags = _childState.Flags.Count == 0
            ? "(none)"
            : string.Join(", ", _childState.Flags.Select(flag => flag.DisplayName));
        GUILayout.Label($"Flags: {flags}");

        GUILayout.Space(6f);
        GUILayout.Label("Information Result");
        foreach (RuntimeInformationSelectionRecord selection in _lastWeekResult?.InformationControlResult?.Selections ?? Array.Empty<RuntimeInformationSelectionRecord>())
        {
            GUILayout.Label($"- {selection.CardDefinition.Title} / {selection.Semantic}");
        }

        GUILayout.Space(6f);
        GUILayout.Label("Pending Event Queue");
        if (_currentEventSession != null)
        {
            GUILayout.Label($"* NOW: {_currentEventSession.EventDefinition.Title}");
        }

        foreach (SO_InteractiveEventDefinition pendingEvent in _pendingEvents.Skip(_nextPendingEventIndex))
        {
            GUILayout.Label($"- {pendingEvent.Title}");
        }

        GUILayout.Space(6f);
        GUILayout.Label("Resolved Cards");
        foreach (RuntimeResolvedCardRecord resolvedCard in _lastWeekResult?.ResolvedCards ?? Array.Empty<RuntimeResolvedCardRecord>())
        {
            GUILayout.Label($"- {resolvedCard.CardDefinition.Title} -> {resolvedCard.SelectedOption.Label}");
        }

        GUILayout.Space(6f);
        GUILayout.Label("History");
        foreach (string historyLine in _eventHistory)
        {
            GUILayout.Label($"- {historyLine}");
        }

        GUILayout.EndVertical();
    }

    private static SO_WeekDefinition[] CreateGeneratedWeeks()
    {
        SO_FlagDefinition letterFlag = CreateFlag("letter_suspected", "편지를 의심함");
        SO_FlagDefinition outsideFlag = CreateFlag("external_interest", "바깥에 관심이 생김");
        SO_FlagDefinition hiddenFlag = CreateFlag("hidden_info_detected", "숨겨진 정보를 감지함");

        SO_CardInfoTypeDefinition letterType = CreateCardType("card_type_letter", "편지");
        SO_CardInfoTypeDefinition externalType = CreateCardType("card_type_externalinfo", "외부 정보");
        SO_CardInfoTypeDefinition visitorType = CreateCardType("card_type_visitor", "방문자");
        SO_CardInfoTypeDefinition houseTalkType = CreateCardType("card_type_houseTalk", "저택 소문");
        SO_CardInfoTypeDefinition textbookType = CreateCardType("card_type_textbook", "교재");

        SO_CardInteractionDefinition trustUp = CreateStatDelta(EChildStatusType.Trust, 1, "신뢰 +1");
        SO_CardInteractionDefinition curiosityUp = CreateStatDelta(EChildStatusType.Curiosity, 1, "호기심 +1");
        SO_CardInteractionDefinition anxietyUp = CreateStatDelta(EChildStatusType.Anxiety, 1, "불안 +1");
        SO_CardInteractionDefinition obedienceUp = CreateStatDelta(EChildStatusType.Obedience, 1, "순응 +1");
        SO_CardInteractionDefinition letterFlagSet = CreateSetFlag(letterFlag, "편지 의심 플래그");
        SO_CardInteractionDefinition outsideFlagSet = CreateSetFlag(outsideFlag, "바깥 관심 플래그");
        SO_CardInteractionDefinition hiddenFlagSet = CreateSetFlag(hiddenFlag, "숨겨진 정보 플래그");

        SO_CardInfoDefinition letterCard = CreateCard(
            "card_letter_debug",
            letterType,
            "수상한 편지",
            "안채 책장 뒤에서 접힌 편지 하나가 나왔다. 네모에게 어떻게 전할까?",
            CreateOption(ECardOptionSemantic.Direct, "그대로 전한다", "편지의 내용을 거의 그대로 전했다.", trustUp),
            CreateOption(ECardOptionSemantic.Modified, "부드럽게 바꿔 전한다", "불안할 만한 부분을 덜어내고 조심스럽게 전했다.", curiosityUp),
            CreateOption(ECardOptionSemantic.Blocked, "막아둔다", "편지 자체를 숨기고 전하지 않았다.", anxietyUp, letterFlagSet));

        SO_CardInfoDefinition outsideCard = CreateCard(
            "card_external_debug",
            externalType,
            "바깥 세계 이야기",
            "저택 밖 풍경과 소식에 대한 이야기를 전할 기회가 생겼다.",
            CreateOption(ECardOptionSemantic.Direct, "있는 그대로 말한다", "저택 밖 이야기를 있는 그대로 들려주었다.", curiosityUp, outsideFlagSet),
            CreateOption(ECardOptionSemantic.Modified, "조심스럽게 순화한다", "조금 더 안전한 부분만 골라 들려주었다.", trustUp),
            CreateOption(ECardOptionSemantic.Blocked, "아예 막는다", "바깥 이야기는 꺼내지 않았다.", anxietyUp));

        SO_CardInfoDefinition visitorCard = CreateCard(
            "card_visitor_debug",
            visitorType,
            "낯선 방문자",
            "새로 온 방문자에 대한 이야기를 네모에게 꺼낼 수 있다.",
            CreateOption(ECardOptionSemantic.Direct, "직접 들려준다", "방문자 이야기를 사실대로 전했다.", trustUp),
            CreateOption(ECardOptionSemantic.Modified, "안전하게 정리한다", "위험하지 않은 쪽으로 정리해서 말했다.", curiosityUp),
            CreateOption(ECardOptionSemantic.Blocked, "말하지 않는다", "방문자 이야기는 하지 않았다.", anxietyUp));

        SO_CardInfoDefinition houseTalkCard = CreateCard(
            "card_house_talk_debug",
            houseTalkType,
            "저택 내부 소문",
            "하인들 사이에서 돈 소문을 전할지 정해야 한다.",
            CreateOption(ECardOptionSemantic.Direct, "소문을 그대로 말한다", "저택의 균열이 그대로 전해졌다.", curiosityUp),
            CreateOption(ECardOptionSemantic.Modified, "강도를 낮춰 말한다", "핵심만 남기고 부드럽게 바꿔 전했다.", obedienceUp),
            CreateOption(ECardOptionSemantic.Blocked, "입을 다문다", "소문 이야기는 전하지 않았다.", trustUp));

        SO_CardInfoDefinition textbookCard = CreateCard(
            "card_textbook_debug",
            textbookType,
            "교재 내용",
            "오늘은 책 속 이야기를 어떤 방향으로 들려줄지 고른다.",
            CreateOption(ECardOptionSemantic.Direct, "깊게 읽어준다", "질문이 생길 만한 대목까지 그대로 읽어주었다.", curiosityUp),
            CreateOption(ECardOptionSemantic.Modified, "안전한 부분만 읽는다", "정리된 부분 위주로 읽어주었다.", obedienceUp),
            CreateOption(ECardOptionSemantic.Blocked, "오늘은 덮어둔다", "책 이야기는 다음으로 미뤘다.", anxietyUp));

        SO_InteractiveEventStepDefinition letterRoutineStep = CreateStep(
            "편지에 대한 반응",
            "전하지 않은 편지가 있다는 걸 네모가 어렴풋이 눈치챈 듯하다.",
            "오늘은... 뭔가 빠진 말이 있는 것 같아.",
            false,
            ENemoVisualState.Neutral,
            Array.Empty<SO_CardInteractionDefinition>(),
            Array.Empty<InteractiveEventChoiceData>(),
            null);

        SO_InteractiveEventStepDefinition outsideRoutineStep = CreateStep(
            "바깥 이야기의 여운",
            "네모는 바깥 풍경 이야기의 일부를 오래 붙잡고 있다.",
            "밖에는... 정말 저런 색이 있어?",
            false,
            ENemoVisualState.Curious,
            Array.Empty<SO_CardInteractionDefinition>(),
            Array.Empty<InteractiveEventChoiceData>(),
            null);

        SO_InteractiveEventStepDefinition letterStoryStep = CreateStep(
            "숨은 진실의 조각",
            "네모가 편지의 빈칸을 스스로 메우기 시작했다. 이제 모르는 척 넘기기 어렵다.",
            "그 편지, 처음부터 다 말해준 건 아니었지?",
            false,
            ENemoVisualState.Conflicted,
            new[] { hiddenFlagSet },
            Array.Empty<InteractiveEventChoiceData>(),
            null);

        SO_InteractiveEventStepDefinition dialogueFollowStep = CreateStep(
            "더 이어진 밤의 대화",
            "네모가 조금 더 가까이 와서 바깥 이야기를 다시 물었다.",
            "그럼... 다음엔 내가 직접 확인해도 돼?",
            false,
            ENemoVisualState.Curious,
            Array.Empty<SO_CardInteractionDefinition>(),
            Array.Empty<InteractiveEventChoiceData>(),
            null);

        InteractiveEventChoiceData[] week1Choices =
        {
            CreateChoice("다음엔 같이 확인하자", "네모는 조심스럽게 기대를 드러냈다.", trustUp, dialogueFollowStep),
            CreateChoice("아직은 조용히 있자", "네모는 말없이 고개를 끄덕였다.", anxietyUp, null),
        };

        SO_InteractiveEventStepDefinition week1DialogueStep = CreateStep(
            "밤 대화",
            "네모가 오늘 들은 바깥 이야기를 곱씹으며 먼저 입을 열었다.",
            "오늘 들은 이야기... 계속 생각났어.",
            false,
            ENemoVisualState.Neutral,
            Array.Empty<SO_CardInteractionDefinition>(),
            week1Choices,
            null);

        SO_DayRoutineEventDefinition letterRoutineEvent = CreateRoutineEvent(
            "routine_letter_blocked",
            "편지를 막아둔 여파",
            20,
            letterRoutineStep,
            letterCard,
            new[] { letterType },
            new[] { ECardOptionSemantic.Blocked });

        SO_DayRoutineEventDefinition outsideRoutineEvent = CreateRoutineEvent(
            "routine_outside_direct",
            "바깥 정보를 곱씹는 아이",
            15,
            outsideRoutineStep,
            outsideCard,
            new[] { externalType },
            new[] { ECardOptionSemantic.Direct });

        SO_StoryEventDefinition week1StoryEvent = CreateStoryEvent(
            "story_letter_suspected",
            "편지를 의심하는 아이",
            30,
            letterStoryStep,
            new[] { letterFlag },
            Array.Empty<SO_FlagDefinition>());

        SO_PrivateDialogueDefinition week1Dialogue = CreatePrivateDialogue(
            "night_outside_interest",
            "바깥에 대한 밤 대화",
            50,
            week1DialogueStep,
            new[] { outsideFlag },
            Array.Empty<SO_FlagDefinition>());

        SO_InteractiveEventStepDefinition textbookRoutineStep = CreateStep(
            "책 속 빈칸",
            "네모가 교재의 문장을 읽으며 스스로 빈칸을 채우기 시작했다.",
            "책에는 없는 말이 더 있는 것 같아.",
            false,
            ENemoVisualState.Curious,
            Array.Empty<SO_CardInteractionDefinition>(),
            Array.Empty<InteractiveEventChoiceData>(),
            null);

        SO_InteractiveEventStepDefinition week2StoryStep = CreateStep(
            "감춰진 방의 단서",
            "숨겨진 정보의 조각들이 서로 이어지기 시작했다. 네모는 이제 저택 안의 모순을 분명히 느낀다.",
            "여기엔 말해주지 않은 방이 더 있는 것 같아.",
            false,
            ENemoVisualState.Conflicted,
            Array.Empty<SO_CardInteractionDefinition>(),
            Array.Empty<InteractiveEventChoiceData>(),
            null);

        SO_InteractiveEventStepDefinition week2DialogueFollowStep = CreateStep(
            "밤 대화의 끝",
            "네모는 한참 망설이다가, 바깥을 향한 마음을 숨기지 않았다.",
            "언젠가는 내가 직접 봐야 할 것 같아.",
            false,
            ENemoVisualState.Curious,
            Array.Empty<SO_CardInteractionDefinition>(),
            Array.Empty<InteractiveEventChoiceData>(),
            null);

        InteractiveEventChoiceData[] week2Choices =
        {
            CreateChoice("의심을 더 깊게 따라가자", "네모는 작은 목소리로 더 알고 싶다고 말했다.", curiosityUp, week2DialogueFollowStep),
            CreateChoice("지금은 시키는 대로 있자", "네모는 순순히 고개를 끄덕였다.", obedienceUp, null),
        };

        SO_InteractiveEventStepDefinition week2DialogueStep = CreateStep(
            "둘째 주 밤 대화",
            "네모는 오늘 들은 소문과 교재 내용을 자꾸 연결해서 생각한다.",
            "말들이 자꾸 이어져... 그냥 우연은 아닌 것 같아.",
            false,
            ENemoVisualState.Conflicted,
            Array.Empty<SO_CardInteractionDefinition>(),
            week2Choices,
            null);

        SO_DayRoutineEventDefinition textbookRoutineEvent = CreateRoutineEvent(
            "routine_textbook_direct",
            "교재를 더 깊게 파고드는 아이",
            20,
            textbookRoutineStep,
            textbookCard,
            new[] { textbookType },
            new[] { ECardOptionSemantic.Direct });

        SO_StoryEventDefinition week2StoryEvent = CreateStoryEvent(
            "story_hidden_info",
            "숨겨진 정보를 이어붙이는 아이",
            35,
            week2StoryStep,
            new[] { hiddenFlag },
            Array.Empty<SO_FlagDefinition>());

        SO_PrivateDialogueDefinition week2Dialogue = CreatePrivateDialogue(
            "night_hidden_truth",
            "숨은 진실에 대한 밤 대화",
            60,
            week2DialogueStep,
            new[] { hiddenFlag },
            Array.Empty<SO_FlagDefinition>());

        return new[]
        {
            CreateWeekDefinition(
                "debug_week_1",
                1,
                "플레이테스트 1주차",
                "정보 조절과 낮/밤 이벤트 흐름을 확인하는 첫 주차입니다.",
                CreatePreTurn("1주차 정보 조절", "카드를 고른 뒤 주간 루프를 실행하세요.", letterCard, outsideCard, visitorCard),
                CreateDayFlow(new[] { letterRoutineEvent, outsideRoutineEvent }, new[] { week1StoryEvent }),
                CreateNightFlow(week1Dialogue)),
            CreateWeekDefinition(
                "debug_week_2",
                2,
                "플레이테스트 2주차",
                "첫 주의 플래그를 이어받아 후속 이벤트와 엔딩을 확인합니다.",
                CreatePreTurn("2주차 정보 조절", "이어지는 흐름과 엔딩을 보기 위한 주차입니다.", houseTalkCard, textbookCard),
                CreateDayFlow(new[] { textbookRoutineEvent }, new[] { week2StoryEvent }),
                CreateNightFlow(week2Dialogue)),
        };
    }

    private static SO_WeekDefinition CreateWeekDefinition(
        string id,
        int weekIndex,
        string title,
        string summary,
        SO_WeekPreTurnDefinition preTurn,
        SO_WeekDayFlowDefinition dayFlow,
        SO_WeekNightFlowDefinition nightFlow)
    {
        SO_WeekDefinition weekDefinition = ScriptableObject.CreateInstance<SO_WeekDefinition>();
        PrepareRuntimeAsset(weekDefinition, title);
        SetSerializedField(weekDefinition, "_id", id);
        SetSerializedField(weekDefinition, "_weekIndex", weekIndex);
        SetSerializedField(weekDefinition, "_title", title);
        SetSerializedField(weekDefinition, "_summary", summary);
        SetSerializedField(weekDefinition, "_preTurn", preTurn);
        SetSerializedField(weekDefinition, "_dayFlow", dayFlow);
        SetSerializedField(weekDefinition, "_nightFlow", nightFlow);
        SetSerializedField(weekDefinition, "_weekRules", Array.Empty<SO_WeekRuleDefinition>());
        SetSerializedField(weekDefinition, "_onWeekStartInteractions", Array.Empty<SO_CardInteractionDefinition>());
        SetSerializedField(weekDefinition, "_onWeekEndInteractions", Array.Empty<SO_CardInteractionDefinition>());
        return weekDefinition;
    }

    private static SO_WeekPreTurnDefinition CreatePreTurn(string title, string summary, params SO_CardInfoDefinition[] cards)
    {
        SO_WeekPreTurnDefinition preTurn = ScriptableObject.CreateInstance<SO_WeekPreTurnDefinition>();
        PrepareRuntimeAsset(preTurn, title);
        SetSerializedField(preTurn, "_title", title);
        SetSerializedField(preTurn, "_summary", summary);
        SetSerializedField(preTurn, "_informationCards", cards.Select((card, index) => CreateWeekCardEntry(card, index)).ToArray());
        return preTurn;
    }

    private static SO_WeekDayFlowDefinition CreateDayFlow(
        SO_DayRoutineEventDefinition[] routineEvents,
        SO_StoryEventDefinition[] storyEvents)
    {
        SO_WeekDayFlowDefinition dayFlow = ScriptableObject.CreateInstance<SO_WeekDayFlowDefinition>();
        PrepareRuntimeAsset(dayFlow, "Day Flow");
        SetSerializedField(dayFlow, "_routineEvents", routineEvents ?? Array.Empty<SO_DayRoutineEventDefinition>());
        SetSerializedField(dayFlow, "_storyEvents", storyEvents ?? Array.Empty<SO_StoryEventDefinition>());
        return dayFlow;
    }

    private static SO_WeekNightFlowDefinition CreateNightFlow(params SO_PrivateDialogueDefinition[] dialogues)
    {
        SO_WeekNightFlowDefinition nightFlow = ScriptableObject.CreateInstance<SO_WeekNightFlowDefinition>();
        PrepareRuntimeAsset(nightFlow, "Night Flow");
        SetSerializedField(nightFlow, "_dialogues", dialogues ?? Array.Empty<SO_PrivateDialogueDefinition>());
        return nightFlow;
    }

    private static WeekCardEntryData CreateWeekCardEntry(SO_CardInfoDefinition card, int displayOrder)
    {
        WeekCardEntryData entry = new();
        SetSerializedField(entry, "_card", card);
        SetSerializedField(entry, "_isRequired", true);
        SetSerializedField(entry, "_displayOrder", displayOrder);
        return entry;
    }

    private static SO_FlagDefinition CreateFlag(string id, string displayName)
    {
        SO_FlagDefinition flag = ScriptableObject.CreateInstance<SO_FlagDefinition>();
        PrepareRuntimeAsset(flag, displayName);
        SetSerializedField(flag, "_id", id);
        SetSerializedField(flag, "_displayName", displayName);
        SetSerializedField(flag, "_description", displayName);
        return flag;
    }

    private static SO_CardInfoTypeDefinition CreateCardType(string id, string displayName)
    {
        SO_CardInfoTypeDefinition cardType = ScriptableObject.CreateInstance<SO_CardInfoTypeDefinition>();
        PrepareRuntimeAsset(cardType, displayName);
        SetSerializedField(cardType, "_id", id);
        SetSerializedField(cardType, "_displayName", displayName);
        return cardType;
    }

    private static SO_CardInfoDefinition CreateCard(
        string id,
        SO_CardInfoTypeDefinition cardType,
        string title,
        string originalText,
        params CardOptionData[] options)
    {
        SO_CardInfoDefinition card = ScriptableObject.CreateInstance<SO_CardInfoDefinition>();
        PrepareRuntimeAsset(card, title);
        SetSerializedField(card, "_id", id);
        SetSerializedField(card, "_cardType", cardType);
        SetSerializedField(card, "_title", title);
        SetSerializedField(card, "_originalText", originalText);
        SetSerializedField(card, "_options", options);
        return card;
    }

    private static CardOptionData CreateOption(
        ECardOptionSemantic semantic,
        string label,
        string presentedText,
        params SO_CardInteractionDefinition[] interactions)
    {
        CardOptionData option = new();
        SetSerializedField(option, "_semantic", semantic);
        SetSerializedField(option, "_label", label);
        SetSerializedField(option, "_presentedText", presentedText);
        SetSerializedField(option, "_interactions", interactions);
        return option;
    }

    private static InteractiveEventChoiceData CreateChoice(
        string label,
        string responseLine,
        SO_CardInteractionDefinition interaction,
        SO_InteractiveEventStepDefinition nextStep)
    {
        InteractiveEventChoiceData choice = new();
        SetSerializedField(choice, "_label", label);
        SetSerializedField(choice, "_responseDialogueLines", CreateDialogueLines(responseLine));
        SetSerializedField(choice, "_responseLine", responseLine);
        SetSerializedField(choice, "_interactions", interaction == null ? Array.Empty<SO_CardInteractionDefinition>() : new[] { interaction });
        SetSerializedField(choice, "_nextStep", nextStep);
        return choice;
    }

    private static SO_InteractiveEventStepDefinition CreateStep(
        string title,
        string bodyText,
        string nemoLine,
        bool useCustomVisualState,
        ENemoVisualState visualState,
        SO_CardInteractionDefinition[] onEnterInteractions,
        InteractiveEventChoiceData[] choices,
        SO_InteractiveEventStepDefinition nextStep)
    {
        SO_InteractiveEventStepDefinition step = ScriptableObject.CreateInstance<SO_InteractiveEventStepDefinition>();
        PrepareRuntimeAsset(step, title);
        SetSerializedField(step, "_titleOverride", title);
        SetSerializedField(step, "_bodyText", bodyText);
        SetSerializedField(step, "_dialogueLines", CreateDialogueLines(nemoLine));
        SetSerializedField(step, "_nemoLine", nemoLine);
        SetSerializedField(step, "_useCustomVisualState", useCustomVisualState);
        SetSerializedField(step, "_visualState", visualState);
        SetSerializedField(step, "_onEnterInteractions", onEnterInteractions ?? Array.Empty<SO_CardInteractionDefinition>());
        SetSerializedField(step, "_choices", choices ?? Array.Empty<InteractiveEventChoiceData>());
        SetSerializedField(step, "_conditionalNext", null);
        SetSerializedField(step, "_nextStep", nextStep);
        return step;
    }

    private static DialogueLineData[] CreateDialogueLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<DialogueLineData>();
        }

        DialogueLineData line = new();
        SetSerializedField(line, "_speaker", null);
        SetSerializedField(line, "_text", text);
        return new[] { line };
    }

    private static SO_DayRoutineEventDefinition CreateRoutineEvent(
        string id,
        string title,
        int priority,
        SO_InteractiveEventStepDefinition firstStep,
        SO_CardInfoDefinition linkedCard,
        SO_CardInfoTypeDefinition[] relatedTypes,
        ECardOptionSemantic[] preferredSemantics)
    {
        SO_DayRoutineEventDefinition routineEvent = ScriptableObject.CreateInstance<SO_DayRoutineEventDefinition>();
        PrepareRuntimeAsset(routineEvent, title);
        SetEventFields(routineEvent, id, title, priority, firstStep);
        SetSerializedField(routineEvent, "_linkedCard", linkedCard);
        SetSerializedField(routineEvent, "_relatedInformationTypes", relatedTypes ?? Array.Empty<SO_CardInfoTypeDefinition>());
        SetSerializedField(routineEvent, "_preferredSemantics", preferredSemantics ?? Array.Empty<ECardOptionSemantic>());
        return routineEvent;
    }

    private static SO_StoryEventDefinition CreateStoryEvent(
        string id,
        string title,
        int priority,
        SO_InteractiveEventStepDefinition firstStep,
        SO_FlagDefinition[] requiredFlags,
        SO_FlagDefinition[] blockedFlags)
    {
        SO_StoryEventDefinition storyEvent = ScriptableObject.CreateInstance<SO_StoryEventDefinition>();
        PrepareRuntimeAsset(storyEvent, title);
        SetEventFields(storyEvent, id, title, priority, firstStep);
        SetEventConditions(storyEvent, requiredFlags, blockedFlags);
        return storyEvent;
    }

    private static SO_PrivateDialogueDefinition CreatePrivateDialogue(
        string id,
        string title,
        int priority,
        SO_InteractiveEventStepDefinition firstStep,
        SO_FlagDefinition[] requiredFlags,
        SO_FlagDefinition[] blockedFlags)
    {
        SO_PrivateDialogueDefinition dialogue = ScriptableObject.CreateInstance<SO_PrivateDialogueDefinition>();
        PrepareRuntimeAsset(dialogue, title);
        SetEventFields(dialogue, id, title, priority, firstStep);
        SetEventConditions(dialogue, requiredFlags, blockedFlags);
        return dialogue;
    }

    private static void SetEventFields(
        SO_InteractiveEventDefinition eventDefinition,
        string id,
        string title,
        int priority,
        SO_InteractiveEventStepDefinition firstStep)
    {
        SetSerializedField(eventDefinition, "_id", id);
        SetSerializedField(eventDefinition, "_title", title);
        SetSerializedField(eventDefinition, "_priority", priority);
        SetSerializedField(eventDefinition, "_firstStep", firstStep);
        SetSerializedField(eventDefinition, "_onCompletedInteractions", Array.Empty<SO_CardInteractionDefinition>());
    }

    private static void SetEventConditions(
        SO_InteractiveEventDefinition eventDefinition,
        SO_FlagDefinition[] requiredFlags,
        SO_FlagDefinition[] blockedFlags)
    {
        WeekEventConditionData conditions = new();
        SetSerializedField(conditions, "_requiredFlags", requiredFlags ?? Array.Empty<SO_FlagDefinition>());
        SetSerializedField(conditions, "_blockedFlags", blockedFlags ?? Array.Empty<SO_FlagDefinition>());
        SetSerializedField(conditions, "_statRequirements", Array.Empty<WeekStatRequirementData>());
        SetSerializedField(conditions, "_informationRequirements", Array.Empty<InformationTypeConditionData>());
        SetSerializedField(eventDefinition, "_conditions", conditions);
    }

    private static SO_CardInteractionDefinition CreateStatDelta(
        EChildStatusType statType,
        int amount,
        string assetName)
    {
        SO_CardInteraction_StatDelta interaction = ScriptableObject.CreateInstance<SO_CardInteraction_StatDelta>();
        PrepareRuntimeAsset(interaction, assetName);
        SetSerializedField(interaction, "_statType", statType);
        SetSerializedField(interaction, "_amount", amount);
        return interaction;
    }

    private static SO_CardInteractionDefinition CreateSetFlag(
        SO_FlagDefinition flagDefinition,
        string assetName)
    {
        SO_CardInteraction_SetFlag interaction = ScriptableObject.CreateInstance<SO_CardInteraction_SetFlag>();
        PrepareRuntimeAsset(interaction, assetName);
        SetSerializedField(interaction, "_flagDefinition", flagDefinition);
        return interaction;
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
