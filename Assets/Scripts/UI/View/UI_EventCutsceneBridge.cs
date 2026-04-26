using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UI_EventCutsceneBridge : WeekFlowCutsceneBridgeBase
{
    [SerializeField] private SO_EventCutsceneRuleCatalog _ruleCatalog;
    [SerializeField] private SO_CutsceneSpeakerCharacterMap _speakerCharacterMap;
    [SerializeField] private UI_DataDrivenCutscenePlayer _dataDrivenPlayer;
    [SerializeField] private WeekFlowCutscenePlayerBase[] _specialPlayers;

    private readonly Dictionary<string, WeekFlowCutscenePlayerBase> _specialPlayerLookup = new();
    private readonly EventCutsceneRuleResolver _resolver = new();

    private WeekFlowCutscenePlayerBase _activeSpecialPlayer;
    private DialogueCharacterAutoPresenter _characterPresenter;
    private bool _autoCharactersActive;
    private string _activeAutoEventId = string.Empty;
    private bool _week000PrologueActive;
    private bool _week000PrologueNemoShown;

    public override bool IsPlaying =>
        (_dataDrivenPlayer != null && _dataDrivenPlayer.IsPlaying) ||
        (_activeSpecialPlayer != null && _activeSpecialPlayer.IsPlaying);

    public override bool IsBlocking =>
        (_dataDrivenPlayer != null && _dataDrivenPlayer.IsPlaying) ||
        (_activeSpecialPlayer != null && _activeSpecialPlayer.IsPlaying && _activeSpecialPlayer.IsBlocking);

    private void Awake()
    {
        CacheSpecialPlayers();
        EnsureCharacterPresenter();
    }

    private void OnValidate()
    {
        CacheSpecialPlayers();
        _characterPresenter = null;
    }

    public override IEnumerator Play(WeekFlowCutsceneRequest request)
    {
        CacheSpecialPlayers();
        EnsureCharacterPresenter();

        if (IsWeek000Prologue(request))
        {
            yield return PlayWeek000Prologue(request);
            yield break;
        }

        if (request.Moment == EWeekFlowCutsceneMoment.LineEnter)
        {
            if (_autoCharactersActive)
            {
                _characterPresenter.ApplyLine(request);
            }

            yield break;
        }

        if (request.Moment != EWeekFlowCutsceneMoment.EventEnter &&
            request.Moment != EWeekFlowCutsceneMoment.EventExit)
        {
            yield break;
        }

        if (request.Moment == EWeekFlowCutsceneMoment.EventExit)
        {
            yield return PlayEventExit(request);
            yield break;
        }

        EnsureAutoCharactersForEvent(request);

        if (!_resolver.TryResolve(request, _ruleCatalog, out EventCutsceneRuleData rule))
        {
            yield break;
        }

        if (rule.UsesSpecialPlayer)
        {
            yield return PlaySpecial(rule.SpecialPlayerId, request);
            yield break;
        }

        if (rule.UsesSequence && _dataDrivenPlayer != null)
        {
            yield return _dataDrivenPlayer.Play(rule.SequenceId, request, ignoreCharacterCommands: true);
            _characterPresenter.ApplyLine(request);
        }
    }

    public override bool TrySkip()
    {
        if (_dataDrivenPlayer != null && _dataDrivenPlayer.TrySkip())
        {
            return true;
        }

        return _activeSpecialPlayer != null && _activeSpecialPlayer.TrySkip();
    }

    public override void StopImmediate()
    {
        _dataDrivenPlayer?.StopImmediate();
        StopSpecialPlayer(ref _activeSpecialPlayer);
    }

    private void CleanupAutoCharacters()
    {
        _autoCharactersActive = false;
        _activeAutoEventId = string.Empty;
        _week000PrologueActive = false;
        _week000PrologueNemoShown = false;
        _characterPresenter?.Reset();

        if (CutsceneCharacterManager.I != null)
        {
            CutsceneCharacterManager.I.HideAll();
        }
    }

    private void EnsureAutoCharactersForEvent(WeekFlowCutsceneRequest request)
    {
        if (_autoCharactersActive && IsActiveAutoEvent(request.EventId))
        {
            _characterPresenter.ApplyLine(request);
            return;
        }

        CleanupAutoCharacters();
        _activeAutoEventId = request.EventId;
        _autoCharactersActive = true;
        _characterPresenter.BeginEvent(request);
    }

    private void EnsureCharacterPresenter()
    {
        if (_characterPresenter == null)
        {
            _characterPresenter = new DialogueCharacterAutoPresenter(_speakerCharacterMap);
        }
    }

    private bool IsActiveAutoEvent(string eventId)
    {
        return !string.IsNullOrWhiteSpace(_activeAutoEventId)
            && string.Equals(_activeAutoEventId, eventId, System.StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerator PlayWeek000Prologue(WeekFlowCutsceneRequest request)
    {
        switch (request.Moment)
        {
            case EWeekFlowCutsceneMoment.EventEnter:
                CleanupAutoCharacters();
                _week000PrologueActive = true;
                if (_resolver.TryResolve(request, _ruleCatalog, out EventCutsceneRuleData rule) &&
                    rule.UsesSequence &&
                    _dataDrivenPlayer != null)
                {
                    yield return _dataDrivenPlayer.Play(rule.SequenceId, request, ignoreCharacterCommands: true);
                }

                ShowWeek000GuardCaptain();
                yield break;

            case EWeekFlowCutsceneMoment.ScreenEnter:
                ApplyWeek000PrologueStepBackground(request.StepName);
                yield break;

            case EWeekFlowCutsceneMoment.LineEnter:
                if (_week000PrologueActive && !_week000PrologueNemoShown && IsRightSideSpeaker(request.DialogueSpeaker))
                {
                    ShowWeek000NemoCenter();
                }

                yield break;

            case EWeekFlowCutsceneMoment.EventExit:
                yield return PlayEventExit(request);
                yield break;
        }
    }

    private static bool IsWeek000Prologue(WeekFlowCutsceneRequest request)
    {
        return string.Equals(request.WeekId, "week_000", System.StringComparison.OrdinalIgnoreCase)
            && string.Equals(request.EventId, "story_prologue", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyWeek000PrologueStepBackground(string stepName)
    {
        if (BackgroundManager.I == null || string.IsNullOrWhiteSpace(stepName))
        {
            return;
        }

        if (IsStepMatch(stepName, "step_prologue_gate"))
        {
            BackgroundManager.I.ShowBackground(BackgroundType.GardenDoor);
            return;
        }

        if (IsStepMatch(stepName, "step_prologue_corridor"))
        {
            BackgroundManager.I.ShowBackground(BackgroundType.Hallway);
            return;
        }

        if (IsStepMatch(stepName, "step_prologue_brother_room") ||
            IsStepMatch(stepName, "step_prologue_abelia_enters") ||
            IsStepMatch(stepName, "step_prologue_after_choice") ||
            IsStepMatch(stepName, "step_prologue_snow_room") ||
            IsStepMatch(stepName, "step_prologue_age_six"))
        {
            BackgroundManager.I.ShowBackground(BackgroundType.BedRoom);
        }
    }

    private static bool IsStepMatch(string stepName, string stepId)
    {
        return stepName.EndsWith(stepId, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRightSideSpeaker(SO_DialogueSpeakerDefinition speaker)
    {
        string id = speaker?.Id;
        return string.Equals(id, "speaker_abelia", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "speaker_abellia", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "speaker_nemo", System.StringComparison.OrdinalIgnoreCase);
    }

    private static void ShowWeek000GuardCaptain()
    {
        if (CutsceneCharacterManager.I == null)
        {
            return;
        }

        CutsceneCharacterManager.I.HideAll();
        CutsceneCharacterManager.I.ShowLeft(CutsceneCharacterType.GuardCaptain);
    }

    private void ShowWeek000NemoCenter()
    {
        if (CutsceneCharacterManager.I == null)
        {
            return;
        }

        CutsceneCharacterManager.I.HideAll();
        CutsceneCharacterManager.I.ShowCenter(CutsceneCharacterType.Nemo);
        _week000PrologueNemoShown = true;
    }

    private void CacheSpecialPlayers()
    {
        _specialPlayerLookup.Clear();

        if (_specialPlayers == null || _specialPlayers.Length == 0)
        {
            _specialPlayers = GetComponentsInChildren<WeekFlowCutscenePlayerBase>(true);
        }

        for (int index = 0; index < _specialPlayers.Length; index++)
        {
            WeekFlowCutscenePlayerBase player = _specialPlayers[index];
            if (player == null || string.IsNullOrWhiteSpace(player.CutsceneId))
            {
                continue;
            }

            _specialPlayerLookup[player.CutsceneId] = player;
        }
    }

    private IEnumerator PlaySpecial(string specialPlayerId, WeekFlowCutsceneRequest request)
    {
        if (!_specialPlayerLookup.TryGetValue(specialPlayerId, out WeekFlowCutscenePlayerBase player) || player == null)
        {
            yield break;
        }

        if (!player.CanPlay(request))
        {
            yield break;
        }

        if (_activeSpecialPlayer != null && _activeSpecialPlayer != player)
        {
            _activeSpecialPlayer.StopImmediate();
            _activeSpecialPlayer = null;
        }

        _activeSpecialPlayer = player;
        yield return player.Play(request);

        if (!player.PersistsForEvent && _activeSpecialPlayer == player)
        {
            _activeSpecialPlayer = null;
        }
    }

    private IEnumerator PlayActiveSpecialExit(WeekFlowCutsceneRequest request)
    {
        if (_activeSpecialPlayer == null)
        {
            yield break;
        }

        if (_activeSpecialPlayer.CanPlay(request))
        {
            yield return _activeSpecialPlayer.Play(request);
        }

        StopSpecialPlayer(ref _activeSpecialPlayer);
    }

    private IEnumerator PlayEventExit(WeekFlowCutsceneRequest request)
    {
        yield return PlayActiveSpecialExit(request);

        if (_resolver.TryResolve(request, _ruleCatalog, out EventCutsceneRuleData rule))
        {
            if (rule.UsesSpecialPlayer)
            {
                yield return PlaySpecial(rule.SpecialPlayerId, request);
            }
            else if (rule.UsesSequence && _dataDrivenPlayer != null)
            {
                yield return _dataDrivenPlayer.Play(rule.SequenceId, request, ignoreCharacterCommands: true);
            }
        }
        else
        {
            AutoCleanupEventCutscene();
        }

        CleanupAutoCharacters();
    }

    private static void AutoCleanupEventCutscene()
    {
        if (CutsceneCharacterManager.I != null)
        {
            CutsceneCharacterManager.I.HideAll();
        }

        if (BackgroundManager.I != null)
        {
            BackgroundManager.I.HideCurrentBackground();
        }
    }

    private static void StopSpecialPlayer(ref WeekFlowCutscenePlayerBase player)
    {
        if (player == null)
        {
            return;
        }

        player.StopImmediate();
        player = null;
    }
}
