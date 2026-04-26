using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CutsceneCharacterType
{
    None,
    Nemo,
    Friend01,
    Friend02,
    Friend03,
    Cousin01,
    Cousin02,
    Archivist,
    Gardener,
    GuardCaptain,
    Maid,
    Secretary,
    Steward,
    StoreKeeper,
    Servant,
}

public enum CutsceneParticleType
{
    None,
    Happy,
    Heart,
    Line,
    Melancholy,
    Sad,
}

public enum CutsceneCharacterPos
{
    None,
    Left,
    Center,
    Right,
}

public class CutsceneCharacterManager : MonoBehaviour
{
    public static CutsceneCharacterManager I { get; private set; }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    [Serializable]
    private class CharacterPrefabEntry
    {
        public CutsceneCharacterType type;
        public GameObject prefab;
    }

    [Serializable]
    private class ParticlePrefabEntry
    {
        public CutsceneParticleType type;
        public ParticleSystem target;
    }

    [Header("Character Prefabs")]
    [SerializeField] private CharacterPrefabEntry[] _characterPrefabs = Array.Empty<CharacterPrefabEntry>();
    [SerializeField] private ParticlePrefabEntry[] _particlePrefabs = Array.Empty<ParticlePrefabEntry>();

    [Header("Spawn Points")]
    [SerializeField] private Transform _leftSpawnPoint;
    [SerializeField] private Transform _rightSpawnPoint;
    [SerializeField] private Transform _centerSpawnPoint;

    private GameObject _leftCharacterInstance;
    private GameObject _rightCharacterInstance;
    private GameObject _centerCharacterInstance;
    private ParticleSystem _readyParticle;
    private Coroutine _pendingHideRoutine;

    public void ShowLeft(CutsceneCharacterType characterType)
    {
        CancelPendingHide();

        GameObject character = FindPrefab(characterType);
        if (character == null)
        {
            _leftCharacterInstance = null;
            return;
        }

        ShowCharacter(character, _leftSpawnPoint, false);
        _leftCharacterInstance = character;
    }

    public void ShowCenter(CutsceneCharacterType characterType)
    {
        CancelPendingHide();

        GameObject character = FindPrefab(characterType);
        if (character == null)
        {
            _centerCharacterInstance = null;
            return;
        }

        ShowCharacter(character, _centerSpawnPoint, false);
        _centerCharacterInstance = character;
    }

    public void ShowRight(CutsceneCharacterType characterType)
    {
        CancelPendingHide();

        GameObject character = FindPrefab(characterType);
        if (character == null)
        {
            _rightCharacterInstance = null;
            return;
        }

        ShowCharacter(character, _rightSpawnPoint, true);
        _rightCharacterInstance = character;
    }

    public void HideAll()
    {
        HideLeft();
        HideRight();
        HideCenter();
    }

    public void HideAllDeferred()
    {
        CancelPendingHide();
        _pendingHideRoutine = StartCoroutine(HideAllNextFrame());
    }

    public void HideLeft()
    {
        if (_leftCharacterInstance == null) return;

        _leftCharacterInstance.SetActive(false);
        _leftCharacterInstance = null;
    }

    public void HideCenter()
    {
        if (_centerCharacterInstance == null) return;

        _centerCharacterInstance.SetActive(false);
        _centerCharacterInstance = null;
    }

    public void HideRight()
    {
        if (_rightCharacterInstance == null) return;

        _rightCharacterInstance.SetActive(false);
        _rightCharacterInstance = null;
    }

    public void PlayParticleOnLeft(CutsceneParticleType particleType)
    {
        PlayParticle(particleType, _leftCharacterInstance);
    }
    public void PlayParticleOnCenter(CutsceneParticleType particleType)
    {
        PlayParticle(particleType, _centerCharacterInstance);
    }
    public void PlayParticleOnRight(CutsceneParticleType particleType)
    {
        PlayParticle(particleType, _rightCharacterInstance);
    }

    public void PlayParticle(CutsceneParticleType particleType, GameObject character)
    {
        if (particleType == CutsceneParticleType.None) return;
        if (character == null) return;

        ParticleSystem particle = FindParticlePrefab(particleType);
        if(particle == null) return;

        particle.gameObject.transform.position = character.transform.position;

        particle.Play();
    }

    public GameObject GetLeftInstance()
    {
        return _leftCharacterInstance;
    }

    public GameObject GetCenterInstance()
    {
        return _centerCharacterInstance;
    }

    public GameObject GetRightInstance()
    {
        return _rightCharacterInstance;
    }

    private void ShowCharacter(GameObject character, Transform spawnPoint, bool flipX)
    {
        if (character == null || spawnPoint == null)
        {
            return;
        }

        character.transform.position = spawnPoint.position;
        character.transform.rotation = spawnPoint.rotation;

        SetFlip(character.transform, flipX);

        character.SetActive(true);
    }

    private void SetFlip(Transform target, bool flipX)
    {
        Vector3 scale = target.localScale;
        float absX = Mathf.Abs(scale.x);
        scale.x = flipX ? -absX : absX;
        target.localScale = scale;
    }

    private GameObject FindPrefab(CutsceneCharacterType characterType)
    {
        if (characterType == CutsceneCharacterType.None) return null;

        for (int i = 0; i < _characterPrefabs.Length; i++)
        {
            CharacterPrefabEntry entry = _characterPrefabs[i];
            if (entry == null) continue;
            if (entry.type != characterType) continue;

            return entry.prefab;
        }

        return null;
    }

    private ParticleSystem FindParticlePrefab(CutsceneParticleType particleType)
    {
        if (particleType == CutsceneParticleType.None) return null;

        for (int i = 0; i < _particlePrefabs.Length; i++)
        {
            ParticlePrefabEntry entry = _particlePrefabs[i];
            if (entry == null) continue;
            if (entry.type != particleType) continue;

            return entry.target;
        }

        return null;
    }

    private void CancelPendingHide()
    {
        if (_pendingHideRoutine == null)
        {
            return;
        }

        StopCoroutine(_pendingHideRoutine);
        _pendingHideRoutine = null;
    }

    private IEnumerator HideAllNextFrame()
    {
        yield return null;
        _pendingHideRoutine = null;
        HideAll();
    }
}

public sealed class DialogueCharacterAutoPresenter
{
    private readonly EventDialogueSpeakerScanner _speakerScanner = new();
    private readonly DialogueCharacterLayoutPolicy _layoutPolicy;

    private SO_InteractiveEventDefinition _eventDefinition;
    private IReadOnlyList<SO_DialogueSpeakerDefinition> _eventSpeakers = Array.Empty<SO_DialogueSpeakerDefinition>();
    private CutsceneCharacterType _currentLeft = CutsceneCharacterType.None;
    private CutsceneCharacterType _currentRight = CutsceneCharacterType.None;
    private CutsceneCharacterType _currentCenter = CutsceneCharacterType.None;

    public DialogueCharacterAutoPresenter(SO_CutsceneSpeakerCharacterMap speakerCharacterMap)
    {
        _layoutPolicy = new DialogueCharacterLayoutPolicy(new CutsceneSpeakerCharacterResolver(speakerCharacterMap));
    }

    public void BeginEvent(WeekFlowCutsceneRequest request)
    {
        _eventDefinition = request.EventDefinition;
        _eventSpeakers = _speakerScanner.GetSpeakers(_eventDefinition);
        ApplySpeaker(request.DialogueSpeaker);
    }

    public void ApplyLine(WeekFlowCutsceneRequest request)
    {
        if (_eventDefinition == null || !ReferenceEquals(_eventDefinition, request.EventDefinition))
        {
            BeginEvent(request);
            return;
        }

        ApplySpeaker(request.DialogueSpeaker);
    }

    public void Reset()
    {
        _eventDefinition = null;
        _eventSpeakers = Array.Empty<SO_DialogueSpeakerDefinition>();
        _currentLeft = CutsceneCharacterType.None;
        _currentRight = CutsceneCharacterType.None;
        _currentCenter = CutsceneCharacterType.None;
    }

    private void ApplySpeaker(SO_DialogueSpeakerDefinition speaker)
    {
        if (CutsceneCharacterManager.I == null)
        {
            return;
        }

        DialogueCharacterLayout layout = _layoutPolicy.Resolve(_eventSpeakers, speaker);

        if (layout.UseCenter)
        {
            HideLeft();
            HideRight();
            ShowCenter(layout.Center);
            return;
        }

        HideCenter();

        if (layout.Right != CutsceneCharacterType.None && _currentRight != layout.Right)
        {
            CutsceneCharacterManager.I.ShowRight(layout.Right);
            _currentRight = layout.Right;
        }

        if (!layout.ChangeLeft)
        {
            return;
        }

        if (layout.Left == CutsceneCharacterType.None)
        {
            HideLeft();
            return;
        }

        if (_currentLeft == layout.Left)
        {
            return;
        }

        HideLeft();
        CutsceneCharacterManager.I.ShowLeft(layout.Left);
        _currentLeft = layout.Left;
    }

    private void HideRight()
    {
        if (_currentRight == CutsceneCharacterType.None)
        {
            return;
        }

        CutsceneCharacterManager.I.HideRight();
        _currentRight = CutsceneCharacterType.None;
    }

    private void ShowCenter(CutsceneCharacterType characterType)
    {
        if (characterType == CutsceneCharacterType.None || _currentCenter == characterType)
        {
            return;
        }

        HideCenter();
        CutsceneCharacterManager.I.ShowCenter(characterType);
        _currentCenter = characterType;
    }

    private void HideCenter()
    {
        if (_currentCenter == CutsceneCharacterType.None)
        {
            return;
        }

        CutsceneCharacterManager.I.HideCenter();
        _currentCenter = CutsceneCharacterType.None;
    }

    private void HideLeft()
    {
        if (_currentLeft == CutsceneCharacterType.None)
        {
            return;
        }

        CutsceneCharacterManager.I.HideLeft();
        _currentLeft = CutsceneCharacterType.None;
    }
}

public sealed class EventDialogueSpeakerScanner
{
    public IReadOnlyList<SO_DialogueSpeakerDefinition> GetSpeakers(SO_InteractiveEventDefinition eventDefinition)
    {
        if (eventDefinition?.FirstStep == null)
        {
            return Array.Empty<SO_DialogueSpeakerDefinition>();
        }

        List<SO_DialogueSpeakerDefinition> speakers = new();
        HashSet<SO_InteractiveEventStepDefinition> visitedSteps = new();
        Queue<SO_InteractiveEventStepDefinition> pendingSteps = new();
        pendingSteps.Enqueue(eventDefinition.FirstStep);

        while (pendingSteps.Count > 0)
        {
            SO_InteractiveEventStepDefinition step = pendingSteps.Dequeue();
            if (step == null || !visitedSteps.Add(step))
            {
                continue;
            }

            AddSpeakers(step.DialogueLines, speakers);

            if (step.Choices != null)
            {
                for (int index = 0; index < step.Choices.Length; index++)
                {
                    InteractiveEventChoiceData choice = step.Choices[index];
                    if (choice == null)
                    {
                        continue;
                    }

                    AddSpeakers(choice.ResponseDialogueLines, speakers);
                    if (choice.NextStep != null)
                    {
                        pendingSteps.Enqueue(choice.NextStep);
                    }
                }
            }

            if (step.ConditionalNext != null)
            {
                if (step.ConditionalNext.NextStep != null)
                {
                    pendingSteps.Enqueue(step.ConditionalNext.NextStep);
                }

                if (step.ConditionalNext.FallbackStep != null)
                {
                    pendingSteps.Enqueue(step.ConditionalNext.FallbackStep);
                }
            }

            if (step.NextStep != null)
            {
                pendingSteps.Enqueue(step.NextStep);
            }
        }

        return speakers;
    }

    private static void AddSpeakers(
        IReadOnlyList<DialogueLineData> lines,
        List<SO_DialogueSpeakerDefinition> speakers)
    {
        if (lines == null)
        {
            return;
        }

        for (int index = 0; index < lines.Count; index++)
        {
            SO_DialogueSpeakerDefinition speaker = lines[index]?.Speaker;
            if (speaker == null || IsNarrator(speaker) || ContainsSpeaker(speakers, speaker))
            {
                continue;
            }

            speakers.Add(speaker);
        }
    }

    private static bool ContainsSpeaker(
        IReadOnlyList<SO_DialogueSpeakerDefinition> speakers,
        SO_DialogueSpeakerDefinition speaker)
    {
        for (int index = 0; index < speakers.Count; index++)
        {
            SO_DialogueSpeakerDefinition existing = speakers[index];
            if (ReferenceEquals(existing, speaker) || SameSpeakerId(existing, speaker))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SameSpeakerId(SO_DialogueSpeakerDefinition first, SO_DialogueSpeakerDefinition second)
    {
        return !string.IsNullOrWhiteSpace(first?.Id)
            && string.Equals(first.Id, second?.Id, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNarrator(SO_DialogueSpeakerDefinition speaker)
    {
        return string.Equals(speaker.Id, "speaker_narrator", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DialogueCharacterLayoutPolicy
{
    private readonly CutsceneSpeakerCharacterResolver _characterResolver;

    public DialogueCharacterLayoutPolicy(CutsceneSpeakerCharacterResolver characterResolver)
    {
        _characterResolver = characterResolver;
    }

    public DialogueCharacterLayout Resolve(
        IReadOnlyList<SO_DialogueSpeakerDefinition> eventSpeakers,
        SO_DialogueSpeakerDefinition currentSpeaker)
    {
        CutsceneCharacterType right = _characterResolver.RightCharacterType;
        if (IsSoloRightSideEvent(eventSpeakers))
        {
            return DialogueCharacterLayout.WithCenter(right);
        }

        if (_characterResolver.IsNarrator(currentSpeaker))
        {
            return DialogueCharacterLayout.KeepLeft(right);
        }

        if (eventSpeakers == null || eventSpeakers.Count <= 2)
        {
            return ResolveFixedLayout(eventSpeakers, right);
        }

        if (currentSpeaker == null || _characterResolver.IsRightSideSpeaker(currentSpeaker))
        {
            return DialogueCharacterLayout.KeepLeft(right);
        }

        return _characterResolver.TryResolve(currentSpeaker, out CutsceneCharacterType left)
            ? DialogueCharacterLayout.WithLeft(left, right)
            : DialogueCharacterLayout.KeepLeft(right);
    }

    private DialogueCharacterLayout ResolveFixedLayout(
        IReadOnlyList<SO_DialogueSpeakerDefinition> eventSpeakers,
        CutsceneCharacterType right)
    {
        if (eventSpeakers == null)
        {
            return DialogueCharacterLayout.KeepLeft(right);
        }

        for (int index = 0; index < eventSpeakers.Count; index++)
        {
            SO_DialogueSpeakerDefinition speaker = eventSpeakers[index];
            if (speaker == null || _characterResolver.IsRightSideSpeaker(speaker))
            {
                continue;
            }

            return _characterResolver.TryResolve(speaker, out CutsceneCharacterType left)
                ? DialogueCharacterLayout.WithLeft(left, right)
                : DialogueCharacterLayout.KeepLeft(right);
        }

        return DialogueCharacterLayout.KeepLeft(right);
    }

    private bool IsSoloRightSideEvent(IReadOnlyList<SO_DialogueSpeakerDefinition> eventSpeakers)
    {
        return eventSpeakers != null
            && eventSpeakers.Count == 1
            && _characterResolver.IsRightSideSpeaker(eventSpeakers[0]);
    }
}

public readonly struct DialogueCharacterLayout
{
    private DialogueCharacterLayout(
        bool changeLeft,
        CutsceneCharacterType left,
        CutsceneCharacterType right,
        bool useCenter,
        CutsceneCharacterType center)
    {
        ChangeLeft = changeLeft;
        Left = left;
        Right = right;
        UseCenter = useCenter;
        Center = center;
    }

    public bool ChangeLeft { get; }
    public CutsceneCharacterType Left { get; }
    public CutsceneCharacterType Right { get; }
    public bool UseCenter { get; }
    public CutsceneCharacterType Center { get; }

    public static DialogueCharacterLayout KeepLeft(CutsceneCharacterType right)
    {
        return new DialogueCharacterLayout(false, CutsceneCharacterType.None, right, false, CutsceneCharacterType.None);
    }

    public static DialogueCharacterLayout WithLeft(CutsceneCharacterType left, CutsceneCharacterType right)
    {
        return new DialogueCharacterLayout(true, left, right, false, CutsceneCharacterType.None);
    }

    public static DialogueCharacterLayout WithCenter(CutsceneCharacterType center)
    {
        return new DialogueCharacterLayout(false, CutsceneCharacterType.None, CutsceneCharacterType.None, true, center);
    }
}

public sealed class CutsceneSpeakerCharacterResolver
{
    private readonly SO_CutsceneSpeakerCharacterMap _speakerCharacterMap;

    public CutsceneSpeakerCharacterResolver(SO_CutsceneSpeakerCharacterMap speakerCharacterMap)
    {
        _speakerCharacterMap = speakerCharacterMap;
    }

    public CutsceneCharacterType RightCharacterType => CutsceneCharacterType.Nemo;

    public bool TryResolve(SO_DialogueSpeakerDefinition speaker, out CutsceneCharacterType characterType)
    {
        characterType = CutsceneCharacterType.None;
        if (speaker == null || IsNarrator(speaker))
        {
            return false;
        }

        if (_speakerCharacterMap != null && _speakerCharacterMap.TryGetCharacterType(speaker.Id, out characterType))
        {
            return characterType != CutsceneCharacterType.None;
        }

        characterType = ResolveFallback(speaker.Id);
        return characterType != CutsceneCharacterType.None;
    }

    public bool IsNarrator(SO_DialogueSpeakerDefinition speaker)
    {
        return string.Equals(speaker?.Id, "speaker_narrator", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsRightSideSpeaker(SO_DialogueSpeakerDefinition speaker)
    {
        string id = speaker?.Id;
        return string.Equals(id, "speaker_abelia", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "speaker_abellia", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "speaker_nemo", StringComparison.OrdinalIgnoreCase);
    }

    private static CutsceneCharacterType ResolveFallback(string speakerId)
    {
        switch (Normalize(speakerId))
        {
            case "speaker_abelia":
            case "speaker_abellia":
            case "speaker_nemo":
                return CutsceneCharacterType.Nemo;
            case "speaker_rian":
                return CutsceneCharacterType.Cousin02;
            case "speaker_max":
                return CutsceneCharacterType.Friend01;
            case "speaker_millia":
                return CutsceneCharacterType.Friend02;
            case "speaker_yuffie":
                return CutsceneCharacterType.Friend03;
            case "speaker_gardener":
            case "speaker_gardener_1":
            case "speaker_gardener_2":
                return CutsceneCharacterType.Gardener;
            case "speaker_guard_captain":
                return CutsceneCharacterType.GuardCaptain;
            case "speaker_maid":
            case "speaker_maid_1":
            case "speaker_maid_2":
                return CutsceneCharacterType.Maid;
            case "speaker_secretary":
            case "speaker_servant":
            case "speaker_count":
            case "speaker_hunter":
            case "speaker_npc":
            case "speaker_worker_1":
            case "speaker_worker_2":
                return CutsceneCharacterType.Secretary;
            case "speaker_butler":
                return CutsceneCharacterType.Steward;
            case "speaker_librarian":
                return CutsceneCharacterType.Archivist;
            case "speaker_warehouse_keeper":
                return CutsceneCharacterType.StoreKeeper;
            default:
                return CutsceneCharacterType.None;
        }
    }

    private static string Normalize(string value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
