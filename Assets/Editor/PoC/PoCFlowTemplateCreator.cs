using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PoCFlowTemplateCreator
{
    private const string RootFolder = "Assets/Data/PoC/N03_AccumulatedChoice";
    private const string DialogueFolder = RootFolder + "/NightDialogues";

    private static readonly DayChoiceTemplate[] PlaceChoices =
    {
        new(
            "allow",
            "허가",
            "place_accepted",
            "장소 접근을 그대로 허용한다.",
            "장소 접근이 유지되었다.\n대상은 평소처럼 그곳에 머물렀고, 머무는 시간이 조금 증가했다.\n강제로 행동을 바꾸려는 반응은 보이지 않았다.\n\n피드백 의도\n대상은 자기 리듬을 유지한다.",
            "대상은 평소보다 안정되어 있다.\n플레이어가 가까이 있어도 피하지 않는다.\n대상은 자신이 머물던 장소 근처에서 조용히 시간을 보낸다.",
            new[]
            {
                new StatChangeTemplate(CharacterStatType.Stability, 1),
                new StatChangeTemplate(CharacterStatType.Autonomy, 1),
                new StatChangeTemplate(CharacterStatType.Trust, 1),
            }),
        new(
            "block",
            "차단",
            "place_denied",
            "장소 접근을 제한한다.",
            "장소 접근이 제한되었다.\n대상은 한동안 입구 근처에 머물렀지만, 안으로 들어가지는 못했다.\n이후 활동량이 줄고 대기 시간이 늘었다.\n\n피드백 의도\n행동은 막혔지만, 욕구는 사라지지 않았다.",
            "대상은 조용하다.\n막힌 장소 방향을 한 번 바라본 뒤 움직이지 않는다.\n플레이어를 피하지는 않지만, 먼저 가까이 오지도 않는다.",
            new[]
            {
                new StatChangeTemplate(CharacterStatType.Stability, 1),
                new StatChangeTemplate(CharacterStatType.Autonomy, -1),
                new StatChangeTemplate(CharacterStatType.Trust, -1),
            }),
        new(
            "modify",
            "수정",
            "place_altered",
            "장소 구조나 자극을 일부 변경한다.",
            "장소의 구조가 일부 변경되었다.\n대상은 처음에는 멈칫했지만, 곧 변경된 장소 안에 머물렀다.\n안정 반응은 유지되었으나, 이전에 보이던 자발적 움직임은 줄었다.\n\n피드백 의도\n안정은 유지됐지만, 원래 방식은 흐려졌다.",
            "대상은 주변을 살핀다.\n무언가를 하려다 한 번 멈춘 뒤, 변경된 장소 안에서만 움직인다.\n불안정하진 않지만 행동이 조심스러워 보인다.",
            new[]
            {
                new StatChangeTemplate(CharacterStatType.Stability, 1),
                new StatChangeTemplate(CharacterStatType.Autonomy, -1),
                new StatChangeTemplate(CharacterStatType.ControlAwareness, 1),
            }),
    };

    private static readonly DayChoiceTemplate[] StimulusChoices =
    {
        new(
            "allow",
            "허가",
            "stimulus_allowed",
            "낯선 물체에 자유롭게 접근하도록 둔다.",
            "대상은 낯선 물체에 바로 닿지 않았다.\n익숙한 장소와 물체 사이를 오가며, 접근과 후퇴를 반복했다.\n시간이 지나자 스스로 거리를 좁히는 행동이 관찰되었다.\n\n피드백 의도\n대상은 스스로 시도하고 조절한다.",
            "대상은 낮에 물체가 있던 방향을 한 번 바라본다.\n이후 천천히 자리를 잡는다.\n긴장되어 보이지만, 피하려는 반응은 크지 않다.",
            new[]
            {
                new StatChangeTemplate(CharacterStatType.Autonomy, 1),
                new StatChangeTemplate(CharacterStatType.Trust, 1),
            }),
        new(
            "block",
            "차단",
            "stimulus_blocked",
            "낯선 물체 접근을 제한한다.",
            "대상은 물체 쪽으로 이동하려다 멈췄다.\n이후 물체를 바라보는 시간은 늘었지만, 접근 행동은 발생하지 않았다.\n대기 시간이 증가했다.\n\n피드백 의도\n위험은 줄었지만, 시도도 함께 멈췄다.",
            "대상은 움직이기 전에 잠시 멈춘다.\n물체가 있던 방향을 바라보지만, 그쪽으로 이동하지 않는다.\n플레이어 쪽도 한 번 바라본다.",
            new[]
            {
                new StatChangeTemplate(CharacterStatType.Stability, 1),
                new StatChangeTemplate(CharacterStatType.Autonomy, -1),
            }),
        new(
            "modify",
            "수정",
            "stimulus_guided",
            "물체 위치나 접근 경로를 조정한다.",
            "물체의 위치와 접근 경로가 조정되었다.\n대상은 변경된 경로를 따라 물체 근처까지 이동했다.\n행동은 안정적이었지만, 경로 밖으로 벗어나려는 시도는 적었다.\n\n피드백 의도\n행동은 유도됐지만, 자발적인 탐색은 줄었다.",
            "대상은 주변을 확인한 뒤 움직인다.\n낮에 조정된 경로와 비슷한 방식으로만 이동한다.\n안정적이지만 움직임이 조심스럽다.",
            new[]
            {
                new StatChangeTemplate(CharacterStatType.Stability, 1),
                new StatChangeTemplate(CharacterStatType.ControlAwareness, 1),
            }),
    };

    private static readonly Day3OutcomeTemplate[] Day3Outcomes =
    {
        new("place_accepted", "stimulus_allowed", "자율 탐색형", "DAY 3 결과 - 자율 탐색형", "대상은 익숙한 장소를 기준점으로 삼아 낯선 물체 근처까지 이동했다.\n접근과 후퇴를 스스로 반복하며 거리를 조절했다.\n새로운 자극에 대한 자발적 시도가 증가했다.", "대상은 낮에 이동했던 방향을 바라본 뒤, 플레이어 근처에 머문다.\n허락을 기다리는 느낌은 적다.", "대상은 스스로 시도하는 방향으로 자랐다."),
        new("place_accepted", "stimulus_blocked", "열린 뒤 멈춤형", "DAY 3 결과 - 열린 뒤 멈춤형", "대상은 익숙한 장소에 머물렀지만, 새로운 자극이 생기자 접근하지 않았다.\n움직이려다 멈추는 행동이 증가했다.\n안정된 기준점은 남아 있지만, 새로운 시도 앞에서는 망설임이 생겼다.", "대상은 움직이려다 멈춘다.\n플레이어 쪽을 잠시 바라본 뒤 다시 자리에 머문다.", "대상은 안정감을 얻었지만, 시도 앞에서 멈추는 방식이 생겼다."),
        new("place_accepted", "stimulus_guided", "안정 기반 유도형", "DAY 3 결과 - 안정 기반 유도형", "대상은 익숙한 장소에서 출발해 조정된 경로를 따라 움직였다.\n새로운 자극에 접근하긴 했지만, 경로 밖으로 벗어나는 시도는 적었다.\n자발성과 유도된 행동이 함께 나타났다.", "대상은 움직이다가 경로 밖을 잠시 바라본다.\n하지만 곧 조정된 범위 안으로 돌아온다.", "대상은 안정감을 기반으로 움직이지만, 정해진 방식 안에서 행동하게 되었다."),
        new("place_denied", "stimulus_allowed", "조심스러운 재시도형", "DAY 3 결과 - 조심스러운 재시도형", "대상은 처음에는 낯선 자극에 접근하지 않았다.\n시간이 지난 뒤 짧게 접근하고 곧바로 물러났다.\n이전 제한의 영향으로 조심스럽지만, 다시 시도하려는 반응이 나타났다.", "대상은 가까이 오지 않지만, 도망치지도 않는다.\n한 번 멈춘 뒤 천천히 다시 움직인다.", "대상은 위축되었지만, 다시 시도할 여지가 생겼다."),
        new("place_denied", "stimulus_blocked", "수동 대기형", "DAY 3 결과 - 수동 대기형", "대상은 새로운 자극을 발견했지만 접근하지 않았다.\n움직이기 전 멈추는 시간이 길어졌다.\n활동량은 낮고, 대기 행동이 반복되었다.", "대상은 플레이어 쪽을 먼저 본다.\n이후 움직이지 않고 기다린다.", "대상은 안전해졌지만, 스스로 행동하기보다 기다리는 방식으로 굳어졌다."),
        new("place_denied", "stimulus_guided", "관리된 순응형", "DAY 3 결과 - 관리된 순응형", "대상은 조정된 경로 안에서만 움직였다.\n제한된 장소를 대신할 행동은 생겼지만, 자발적인 확장은 적었다.\n행동은 안정적이지만 수동적이다.", "대상은 주변을 확인한 뒤 정해진 위치로 이동한다.\n먼저 시도하는 행동은 보이지 않는다.", "대상은 관리 가능한 방식으로 안정되었지만, 스스로 선택하는 느낌은 약하다."),
        new("place_altered", "stimulus_allowed", "유도에서 벗어나는 실험형", "DAY 3 결과 - 유도에서 벗어나는 실험형", "대상은 처음에는 조정된 경로를 따랐다.\n이후 스스로 경로 밖으로 짧게 벗어나 낯선 자극을 확인했다.\n유도된 행동에서 벗어나려는 작은 시도가 관찰되었다.", "대상은 조정된 장소 밖에 잠시 머문다.\n이후 플레이어 쪽을 바라보지만, 바로 물러나지는 않는다.", "대상은 유도된 환경에 익숙하지만, 다시 자기 방식으로 시도하기 시작했다."),
        new("place_altered", "stimulus_blocked", "교정 후 위축형", "DAY 3 결과 - 교정 후 위축형", "대상은 변경된 환경에 맞춰 움직이려 했지만, 새 자극 앞에서 멈췄다.\n이후 주변을 확인하는 행동이 늘었다.\n행동 범위가 이전보다 좁아졌다.", "대상은 움직이기 전에 주변을 살핀다.\n이후 아무것도 하지 않고 자리에 머문다.", "대상은 안정적이지만, 변화 앞에서 위축되는 방식이 생겼다."),
        new("place_altered", "stimulus_guided", "안정적 루틴형", "DAY 3 결과 - 안정적 루틴형", "대상은 조정된 환경과 경로에 안정적으로 적응했다.\n행동은 예측 가능하고 안정적이다.\n하지만 새로운 시도는 정해진 범위 안에서만 나타났다.", "대상은 같은 경로를 반복해서 움직인다.\n불안정하지는 않지만, 새로운 행동은 보이지 않는다.", "대상은 안정적인 루틴을 형성했지만, 자발적인 확장은 약하다."),
    };

    [MenuItem("Tools/PoC/Create N03 Accumulated Choice Flow")]
    public static void CreateTemplate()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(DialogueFolder);

        SO_PoCFlowDefinition flow = ScriptableObject.CreateInstance<SO_PoCFlowDefinition>();
        string flowPath = AssetDatabase.GenerateUniqueAssetPath(RootFolder + "/PoCFlow_N03_AccumulatedChoice.asset");
        AssetDatabase.CreateAsset(flow, flowPath);

        SerializedObject serializedFlow = new(flow);
        SetupIntro(serializedFlow);
        SetupDays(serializedFlow);
        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(flow);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = flow;
    }

    private static void SetupIntro(SerializedObject serializedFlow)
    {
        SerializedProperty introPages = serializedFlow.FindProperty("_introPages");
        introPages.arraySize = 1;

        SerializedProperty intro = introPages.GetArrayElementAtIndex(0);
        SetString(intro, "_title", "선택 누적형 육성 PoC");
        SetString(
            intro,
            "_body",
            "이 PoC는 간접통제 자체의 재미가 아니라, 선택이 누적되어 육성 대상의 행동 방식이 형성되는 재미를 검증한다.\n\n내가 직접 시킨 것은 아니지만, 내 선택들이 누적되어 대상이 이런 방식으로 자라났다고 느끼는 재미를 확인한다.");
    }

    private static void SetupDays(SerializedObject serializedFlow)
    {
        SerializedProperty days = serializedFlow.FindProperty("_days");
        days.arraySize = 3;

        SetupChoiceDay(
            days.GetArrayElementAtIndex(0),
            "day1_place_policy",
            "DAY 1 - 첫 번째 선택: 장소에 대한 방침",
            "DAY 1 낮 보고서",
            "대상은 오늘도 같은 장소에 오래 머물렀다.\n해당 장소에서는 안정적인 행동을 보였지만, 다른 자극에는 반응이 줄었다.\n현재 행동을 유지할지, 제한할지, 조건을 조정할지 결정이 필요하다.",
            "대상이 특정 장소에 반복적으로 머무는 행동",
            "장소 접근에 대한 환경 방침을 선택한다.",
            PlaceChoices,
            1);

        SetupChoiceDay(
            days.GetArrayElementAtIndex(1),
            "day2_stimulus_policy",
            "DAY 2 - 두 번째 선택: 새 자극에 대한 방침",
            "DAY 2 공통 상황",
            "대상이 머무는 장소 근처에 낯선 물체가 놓였다.\n대상은 물체를 발견한 뒤, 잠시 멈춰서 바라본다.\n접근을 허용할지, 차단할지, 조건을 수정할지 결정이 필요하다.",
            "낯선 물체에 대한 방침",
            "DAY 1 선택의 결과가 반영된 상태에서 새 자극에 대한 환경 방침을 선택한다.",
            StimulusChoices,
            2,
            setupDay2ReportRules: true);

        SetupDay3(days.GetArrayElementAtIndex(2));
    }

    private static void SetupChoiceDay(
        SerializedProperty day,
        string id,
        string label,
        string reportTitle,
        string reportBody,
        string choiceTitle,
        string choiceBody,
        DayChoiceTemplate[] choices,
        int dayNumber,
        bool setupDay2ReportRules = false)
    {
        SetString(day, "_id", id);
        SetString(day, "_label", label);

        SerializedProperty report = day.FindPropertyRelative("_report");
        SetString(report.FindPropertyRelative("_fallbackPage"), "_title", reportTitle);
        SetString(report.FindPropertyRelative("_fallbackPage"), "_body", reportBody);

        if (setupDay2ReportRules)
        {
            SetupDay2ReportRules(report.FindPropertyRelative("_rules"));
        }
        else
        {
            report.FindPropertyRelative("_rules").arraySize = 0;
        }

        SetString(day, "_choiceTitle", choiceTitle);
        SetString(day, "_choiceBody", choiceBody);
        SetupChoices(day.FindPropertyRelative("_mainChoices"), choices, dayNumber);
        SetupDayEventRules(day.FindPropertyRelative("_dayEvent"), $"DAY {dayNumber} 낮 피드백", choices);
        SetupNightDialogueRules(day.FindPropertyRelative("_nightDialogueRules"), dayNumber, choices);
    }

    private static void SetupDay2ReportRules(SerializedProperty rules)
    {
        rules.arraySize = 3;
        SetupReportRule(
            rules.GetArrayElementAtIndex(0),
            3,
            new[] { "place_accepted" },
            "DAY 2 시작 보고서 - PlaceMemory Accepted",
            "대상은 같은 장소를 다시 찾았다.\n어제보다 긴장 반응이 줄었고, 장소 주변을 천천히 살피는 행동이 추가되었다.\n해당 장소를 안정된 기준점처럼 사용하는 것으로 보인다.");
        SetupReportRule(
            rules.GetArrayElementAtIndex(1),
            2,
            new[] { "place_denied" },
            "DAY 2 시작 보고서 - PlaceMemory Denied",
            "대상은 제한된 장소에 접근하지 않았다.\n대신 비슷한 구조의 다른 구석에 오래 머물렀다.\n기존 행동은 사라지지 않고 다른 형태로 옮겨간 것으로 보인다.");
        SetupReportRule(
            rules.GetArrayElementAtIndex(2),
            1,
            new[] { "place_altered" },
            "DAY 2 시작 보고서 - PlaceMemory Altered",
            "대상은 변경된 장소를 사용했다.\n안정 반응은 유지되었지만, 움직임은 이전보다 정해진 경로를 따랐다.\n자발적인 탐색 행동은 감소했다.");
    }

    private static void SetupDay3(SerializedProperty day)
    {
        SetString(day, "_id", "day3_accumulated_result");
        SetString(day, "_label", "DAY 3 - 누적 결과 확인");
        SetString(day, "_choiceTitle", string.Empty);
        SetString(day, "_choiceBody", string.Empty);
        day.FindPropertyRelative("_mainChoices").arraySize = 0;

        SerializedProperty report = day.FindPropertyRelative("_report");
        SetString(report.FindPropertyRelative("_fallbackPage"), "_title", "DAY 3 결과 확인");
        SetString(
            report.FindPropertyRelative("_fallbackPage"),
            "_body",
            "DAY 1 선택과 DAY 2 선택이 합쳐져 대상의 행동 방식이 어떻게 형성되었는지 확인한다.");

        SerializedProperty reportRules = report.FindPropertyRelative("_rules");
        reportRules.arraySize = Day3Outcomes.Length;
        for (int index = 0; index < Day3Outcomes.Length; index++)
        {
            Day3OutcomeTemplate outcome = Day3Outcomes[index];
            SetupReportRule(
                reportRules.GetArrayElementAtIndex(index),
                Day3Outcomes.Length - index,
                new[] { outcome.PlaceFlag, outcome.StimulusFlag },
                outcome.Title,
                $"{outcome.DayFeedback}\n\n형성된 행동 방식\n{outcome.BehaviorType}");
        }

        SetupDay3EventRules(day.FindPropertyRelative("_dayEvent"));
        SetupDay3NightRules(day.FindPropertyRelative("_nightDialogueRules"));
    }

    private static void SetupChoices(SerializedProperty choicesProperty, DayChoiceTemplate[] choices, int dayNumber)
    {
        choicesProperty.arraySize = choices.Length;
        for (int index = 0; index < choices.Length; index++)
        {
            DayChoiceTemplate choiceTemplate = choices[index];
            SerializedProperty choice = choicesProperty.GetArrayElementAtIndex(index);
            SetString(choice, "_id", $"day{dayNumber}_{choiceTemplate.Action}");
            SetString(choice, "_label", choiceTemplate.Label);
            SetString(choice, "_description", choiceTemplate.Description);
            SetStringArray(choice.FindPropertyRelative("_setFlags"), choiceTemplate.Flag);
            SetStatChanges(choice.FindPropertyRelative("_statChanges"), choiceTemplate.StatChanges);
        }
    }

    private static void SetupDayEventRules(
        SerializedProperty dayEvent,
        string title,
        DayChoiceTemplate[] choices)
    {
        SetString(dayEvent, "_title", title);
        SetString(dayEvent, "_body", "선택에 따른 낮 피드백이 표시된다.");
        dayEvent.FindPropertyRelative("_statChanges").arraySize = 0;

        SerializedProperty rules = dayEvent.FindPropertyRelative("_rules");
        rules.arraySize = choices.Length;
        for (int index = 0; index < choices.Length; index++)
        {
            DayChoiceTemplate choice = choices[index];
            SerializedProperty rule = rules.GetArrayElementAtIndex(index);
            SetInt(rule, "_priority", choices.Length - index);
            SetStringArray(rule.FindPropertyRelative("_requiredFlags"), choice.Flag);

            SerializedProperty content = rule.FindPropertyRelative("_event");
            SetString(content, "_title", $"{title} - {choice.Label}");
            SetString(content, "_body", choice.DayFeedback);
            content.FindPropertyRelative("_statChanges").arraySize = 0;
        }
    }

    private static void SetupDay3EventRules(SerializedProperty dayEvent)
    {
        SetString(dayEvent, "_title", "DAY 3 낮 피드백");
        SetString(dayEvent, "_body", "누적 선택에 따른 낮 피드백이 표시된다.");
        dayEvent.FindPropertyRelative("_statChanges").arraySize = 0;

        SerializedProperty rules = dayEvent.FindPropertyRelative("_rules");
        rules.arraySize = Day3Outcomes.Length;
        for (int index = 0; index < Day3Outcomes.Length; index++)
        {
            Day3OutcomeTemplate outcome = Day3Outcomes[index];
            SerializedProperty rule = rules.GetArrayElementAtIndex(index);
            SetInt(rule, "_priority", Day3Outcomes.Length - index);
            SetStringArray(rule.FindPropertyRelative("_requiredFlags"), outcome.PlaceFlag, outcome.StimulusFlag);

            SerializedProperty content = rule.FindPropertyRelative("_event");
            SetString(content, "_title", outcome.Title);
            SetString(content, "_body", $"{outcome.DayFeedback}\n\n최종 감각\n{outcome.FinalFeeling}");
            content.FindPropertyRelative("_statChanges").arraySize = 0;
        }
    }

    private static void SetupNightDialogueRules(
        SerializedProperty rules,
        int dayNumber,
        DayChoiceTemplate[] choices)
    {
        rules.arraySize = choices.Length;
        for (int index = 0; index < choices.Length; index++)
        {
            DayChoiceTemplate choice = choices[index];
            SerializedProperty rule = rules.GetArrayElementAtIndex(index);
            SetInt(rule, "_priority", choices.Length - index);
            SetStringArray(rule.FindPropertyRelative("_requiredFlags"), choice.Flag);
            rule.FindPropertyRelative("_dialogue").objectReferenceValue =
                CreateNightDialogue(dayNumber, choice.Action, choice.Label, choice.NightContact);
        }
    }

    private static void SetupDay3NightRules(SerializedProperty rules)
    {
        rules.arraySize = Day3Outcomes.Length;
        for (int index = 0; index < Day3Outcomes.Length; index++)
        {
            Day3OutcomeTemplate outcome = Day3Outcomes[index];
            SerializedProperty rule = rules.GetArrayElementAtIndex(index);
            SetInt(rule, "_priority", Day3Outcomes.Length - index);
            SetStringArray(rule.FindPropertyRelative("_requiredFlags"), outcome.PlaceFlag, outcome.StimulusFlag);
            rule.FindPropertyRelative("_dialogue").objectReferenceValue =
                CreateNightDialogue(3, outcome.BehaviorType, outcome.BehaviorType, outcome.NightContact);
        }
    }

    private static void SetupReportRule(
        SerializedProperty rule,
        int priority,
        string[] requiredFlags,
        string title,
        string body)
    {
        SetInt(rule, "_priority", priority);
        SetStringArray(rule.FindPropertyRelative("_requiredFlags"), requiredFlags);

        SerializedProperty page = rule.FindPropertyRelative("_page");
        SetString(page, "_title", title);
        SetString(page, "_body", body);
    }

    private static SO_PrivateDialogueDefinition CreateNightDialogue(
        int dayNumber,
        string key,
        string label,
        string body)
    {
        string assetPrefix = $"Day{dayNumber}_{SanitizeFileName(key)}";
        SO_InteractiveEventStepDefinition step = ScriptableObject.CreateInstance<SO_InteractiveEventStepDefinition>();
        string stepPath = AssetDatabase.GenerateUniqueAssetPath($"{DialogueFolder}/Step_{assetPrefix}.asset");
        AssetDatabase.CreateAsset(step, stepPath);

        SerializedObject serializedStep = new(step);
        SetString(serializedStep, "_titleOverride", $"DAY {dayNumber} 밤 접촉 - {label}");
        SetString(serializedStep, "_bodyText", "낮의 환경 조정 결과가 대상의 정서와 태도로 어떻게 나타났는지 확인한다.");
        SetString(serializedStep, "_nemoLine", body);
        serializedStep.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(step);

        SO_PrivateDialogueDefinition dialogue = ScriptableObject.CreateInstance<SO_PrivateDialogueDefinition>();
        string dialoguePath = AssetDatabase.GenerateUniqueAssetPath($"{DialogueFolder}/PrivateDialogue_{assetPrefix}.asset");
        AssetDatabase.CreateAsset(dialogue, dialoguePath);

        SerializedObject serializedDialogue = new(dialogue);
        SetString(serializedDialogue, "_id", $"poc_day{dayNumber}_night_{SanitizeFileName(key)}");
        SetString(serializedDialogue, "_title", $"DAY {dayNumber} 밤 접촉 - {label}");
        SetInt(serializedDialogue, "_priority", 0);
        serializedDialogue.FindProperty("_firstStep").objectReferenceValue = step;
        serializedDialogue.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogue);

        return dialogue;
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetFolder)?.Replace("\\", "/");
        string name = Path.GetFileName(assetFolder);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "empty";
        }

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(' ', '_');
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetString(SerializedProperty parent, string propertyName, string value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetInt(SerializedProperty parent, string propertyName, int value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetStringArray(SerializedProperty arrayProperty, params string[] values)
    {
        if (arrayProperty == null)
        {
            return;
        }

        arrayProperty.arraySize = values?.Length ?? 0;
        for (int index = 0; index < arrayProperty.arraySize; index++)
        {
            arrayProperty.GetArrayElementAtIndex(index).stringValue = values[index] ?? string.Empty;
        }
    }

    private static void SetStatChanges(SerializedProperty arrayProperty, StatChangeTemplate[] changes)
    {
        if (arrayProperty == null)
        {
            return;
        }

        arrayProperty.arraySize = changes?.Length ?? 0;
        for (int index = 0; index < arrayProperty.arraySize; index++)
        {
            SerializedProperty statChange = arrayProperty.GetArrayElementAtIndex(index);
            statChange.FindPropertyRelative("_statType").enumValueIndex = (int)changes[index].StatType;
            statChange.FindPropertyRelative("_delta").intValue = changes[index].Delta;
        }
    }

    private readonly struct DayChoiceTemplate
    {
        public DayChoiceTemplate(
            string action,
            string label,
            string flag,
            string description,
            string dayFeedback,
            string nightContact,
            StatChangeTemplate[] statChanges)
        {
            Action = action;
            Label = label;
            Flag = flag;
            Description = description;
            DayFeedback = dayFeedback;
            NightContact = nightContact;
            StatChanges = statChanges;
        }

        public string Action { get; }
        public string Label { get; }
        public string Flag { get; }
        public string Description { get; }
        public string DayFeedback { get; }
        public string NightContact { get; }
        public StatChangeTemplate[] StatChanges { get; }
    }

    private readonly struct Day3OutcomeTemplate
    {
        public Day3OutcomeTemplate(
            string placeFlag,
            string stimulusFlag,
            string behaviorType,
            string title,
            string dayFeedback,
            string nightContact,
            string finalFeeling)
        {
            PlaceFlag = placeFlag;
            StimulusFlag = stimulusFlag;
            BehaviorType = behaviorType;
            Title = title;
            DayFeedback = dayFeedback;
            NightContact = nightContact;
            FinalFeeling = finalFeeling;
        }

        public string PlaceFlag { get; }
        public string StimulusFlag { get; }
        public string BehaviorType { get; }
        public string Title { get; }
        public string DayFeedback { get; }
        public string NightContact { get; }
        public string FinalFeeling { get; }
    }

    private readonly struct StatChangeTemplate
    {
        public StatChangeTemplate(CharacterStatType statType, int delta)
        {
            StatType = statType;
            Delta = delta;
        }

        public CharacterStatType StatType { get; }
        public int Delta { get; }
    }
}
