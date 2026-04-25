using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UI_EventCutsceneBridge : WeekFlowCutsceneBridgeBase
{
    [SerializeField] private SO_EventCutsceneRuleCatalog _ruleCatalog;
    [SerializeField] private UI_DataDrivenCutscenePlayer _dataDrivenPlayer;
    [SerializeField] private WeekFlowCutscenePlayerBase[] _specialPlayers;

    private readonly Dictionary<string, WeekFlowCutscenePlayerBase> _specialPlayerLookup = new();
    private readonly EventCutsceneRuleResolver _resolver = new();

    private WeekFlowCutscenePlayerBase _activeSpecialPlayer;

    public override bool IsPlaying =>
        (_dataDrivenPlayer != null && _dataDrivenPlayer.IsPlaying) ||
        (_activeSpecialPlayer != null && _activeSpecialPlayer.IsPlaying);

    public override bool IsBlocking =>
        (_dataDrivenPlayer != null && _dataDrivenPlayer.IsPlaying) ||
        (_activeSpecialPlayer != null && _activeSpecialPlayer.IsPlaying && _activeSpecialPlayer.IsBlocking);

    private void Awake()
    {
        CacheSpecialPlayers();
    }

    private void OnValidate()
    {
        CacheSpecialPlayers();
    }

    public override IEnumerator Play(WeekFlowCutsceneRequest request)
    {
        CacheSpecialPlayers();

        if (request.Moment != EWeekFlowCutsceneMoment.EventEnter &&
            request.Moment != EWeekFlowCutsceneMoment.EventExit)
        {
            yield break;
        }

        if (request.Moment == EWeekFlowCutsceneMoment.EventExit)
        {
            yield return PlayActiveSpecialExit(request);
        }

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
            yield return _dataDrivenPlayer.Play(rule.SequenceId, request);
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
