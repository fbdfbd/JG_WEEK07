using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class NemoEntity : MonoBehaviour, IPointerClickHandler
{
    public enum NemoState
    {
        Routine,
        Interacting,
        Event
    }

    public static NemoEntity Instance { get; private set; }

    [SerializeField] private NemoWeeklyDialogController _dialogController;

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;

    [Header("Interaction")]
    [SerializeField] private GameObject interactPanel;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Vector3 offset;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;

    [Header("Room Bounds")]
    [SerializeField] private Transform leftEdge;
    [SerializeField] private Transform rightEdge;

    [Header("Random Action Weights")]
    [SerializeField] private int idleWeight = 50;
    // [SerializeField] private int balanceWeight = 30; // 나중에 사용할 수도 있어 보존
    [SerializeField] private int turnWeight = 20;

    private NemoAnimation _anim;
    private NemoInteract _interact;
    private NemoRoutine _routine;
    private Coroutine _dailyRoutineCoroutine;

    public NemoState CurrentState { get; private set; } = NemoState.Routine;

    public bool IsInitialized => _anim != null && _interact != null && _routine != null;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("Instance가 null이 아님");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _anim = new NemoAnimation(playerAnimator);
        _interact = new NemoInteract(interactPanel, canvasRect, offset);
        _routine = new NemoRoutine(
            transform,
            _anim,
            walkSpeed,
            leftEdge,
            rightEdge,
            idleWeight,
            0, // balanceWeight 자리 - 나중에 되살릴 때 balanceWeight로 교체
            turnWeight,
            CanRunRoutine
        );
    }

    private void Start()
    {
        if (_routine == null) Debug.Log("NemoRoutine이 null임");
        if (_interact == null) Debug.Log("NemoInteract이 null임");
        if (_anim == null) Debug.Log("NemoAnimation이 null임");

        StartDailyRoutine();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool CanRunRoutine()
    {
        return CurrentState == NemoState.Routine;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"OnPointerClick called. CurrentState = {CurrentState}");

        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (CurrentState == NemoState.Event) return;

        if (CurrentState == NemoState.Routine && _dialogController != null && _dialogController.SpeakNow())
        {
            return;
        }

        if (CurrentState == NemoState.Routine)
        {
            PauseRoutine();
            return;
        }

        //if (CurrentState == NemoState.Interacting)
        //{
        //    ResumeRoutine();
        //}
    }

    public void PauseRoutine()
    {
        SetState(NemoState.Interacting);
        PlayIdle();
        OpenInteractionPanel();
    }

    public void ResumeRoutine()
    {
        // CloseInteractionPanel();
        SetState(NemoState.Routine);
        StartDailyRoutine();
    }

    public void EnterEventState()
    {
        CloseInteractionPanel();
        SetState(NemoState.Event);
    }

    public void FinishEvent()
    {
        SetState(NemoState.Routine);
        StartDailyRoutine();
    }

    private void SetState(NemoState newState)
    {
        CurrentState = newState;
        Debug.Log($"Nemo state changed to {CurrentState}");
    }

    public void StartDailyRoutine()
    {
        if (!IsInitialized)
        {
            Debug.LogError("클래스 중 하나가 없음");
            return;
        }

        if (_dailyRoutineCoroutine != null)
        {
            StopCoroutine(_dailyRoutineCoroutine);
        }
        _dailyRoutineCoroutine = StartCoroutine(_routine.DailyRoutine());
    }

    public void StopDailyRoutine()
    {
        if (_dailyRoutineCoroutine != null)
        {
            StopCoroutine(_dailyRoutineCoroutine);
            _dailyRoutineCoroutine = null;
        }
    }

    public void OpenInteractionPanel()
    {
        _interact.OpenPanel(gameObject.transform);
    }

    public void CloseInteractionPanel()
    {
        _interact.ClosePanel();
    }

    public void PlayIdle()
    {
        _anim.PlayIdle();
    }

    public void PlayWalk()
    {
        _anim.PlayWalk();
    }

    // public void PlayBalance()
    // {
    //     _anim.PlayBalance();
    // }

    public void PlayRandomAction()
    {
        _routine.PlayRandomBehavior();
    }

    public void Turn()
    {
        _routine.ChangeMoveDirection();
    }
}