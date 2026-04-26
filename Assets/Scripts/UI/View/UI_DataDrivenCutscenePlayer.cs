using System.Collections;
using UnityEngine;

public sealed class UI_DataDrivenCutscenePlayer : MonoBehaviour
{
    [SerializeField] private SO_CutsceneSequenceLibrary _sequenceLibrary;
    [SerializeField] private WeekFlowCutsceneTargetRegistry _targetRegistry;

    private WeekFlowCutsceneCommandExecutor _executor;
    private Coroutine _activeCoroutine;
    private bool _isPlaying;
    private bool _ignoreCharacterCommands;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        EnsureExecutor();
    }

    public IEnumerator Play(string sequenceId, WeekFlowCutsceneRequest request, bool ignoreCharacterCommands = false)
    {
        EnsureExecutor();
        StopImmediate();

        if (_sequenceLibrary == null || !_sequenceLibrary.TryGetSequence(sequenceId, out CutsceneSequenceData sequence))
        {
            yield break;
        }

        _isPlaying = true;
        _ignoreCharacterCommands = ignoreCharacterCommands;
        _activeCoroutine = StartCoroutine(PlaySequence(sequence, request));
        yield return _activeCoroutine;
        _activeCoroutine = null;
        _ignoreCharacterCommands = false;
        _isPlaying = false;
    }

    public bool TrySkip()
    {
        if (!_isPlaying)
        {
            return false;
        }

        StopImmediate();
        return true;
    }

    public void StopImmediate()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }

        _executor?.StopImmediate();
        _ignoreCharacterCommands = false;
        _isPlaying = false;
    }

    private IEnumerator PlaySequence(CutsceneSequenceData sequence, WeekFlowCutsceneRequest request)
    {
        CutsceneCommandData[] commands = sequence.Commands;
        if (commands == null)
        {
            yield break;
        }

        for (int index = 0; index < commands.Length; index++)
        {
            CutsceneCommandData command = commands[index];
            if (command == null)
            {
                continue;
            }

            IEnumerator execution = _executor.Execute(command, request, _ignoreCharacterCommands);
            if (command.Blocking)
            {
                yield return execution;
            }
            else
            {
                StartCoroutine(execution);
            }
        }
    }

    private void EnsureExecutor()
    {
        if (_executor != null)
        {
            return;
        }

        if (_targetRegistry == null)
        {
            _targetRegistry = GetComponent<WeekFlowCutsceneTargetRegistry>();
        }

        _executor = new WeekFlowCutsceneCommandExecutor(_targetRegistry);
    }
}
