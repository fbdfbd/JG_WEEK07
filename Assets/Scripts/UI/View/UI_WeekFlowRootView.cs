using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI_WeekFlowRootView : WeekFlowViewBase
{
    private const string LeftCtrlBindingPath = "<Keyboard>/leftCtrl";
    private const string RightCtrlBindingPath = "<Keyboard>/space";

    private enum EDialogueContinueRoute
    {
        None,
        WeekFeedback,
        Narrative
    }

    [Header("Panels")]
    [SerializeField] private UI_WeekFlowScreenView _weekScreenView;
    [SerializeField] private UI_DialogueScreenView _dialogueScreenView;
    [SerializeField] private UI_WeeklyResultLogPanel _weeklyResultLogPanel;
    [SerializeField] private UI_WeeklyStatResultPanel _weeklyStatResultPanel;
    [SerializeField] private UI_DayFlowProgressView _dayFlowProgressView;
    [SerializeField] private UI_EndingLetterView _endingLetterView;
    [SerializeField] private UI_DialogueLogPanel _dialogueLogPanel;
    [SerializeField] private UI_WeekFlowTransitionPlayer _transitionPlayer;
    [SerializeField] private SO_WeekEntryIntroCatalog _weekEntryIntroCatalog;
    [SerializeField] private UI_WeekEntryIntroOverlay _weekEntryIntroOverlay;
    [SerializeField] private WeekFlowCutsceneBridgeBase _cutsceneBridge;
    [SerializeField] private GameObject _endingFollowUpPanel;
    [SerializeField] private CanvasGroup _mainCanvasGroup;
    [SerializeField] private GameObject _nemo;
    [SerializeField] private GameObject _interactionPanel;

    [Header("Log")]
    [SerializeField] private Button _openLogButton;

    [Header("Advance Input")]
    [SerializeField] private InputAction _advanceAction = CreateAdvanceAction();
    [SerializeField] private float _advanceHoldDelay = 0.35f;
    [SerializeField] private float _advanceRepeatInterval = 0.08f;

    private EDialogueContinueRoute _dialogueContinueRoute;
    private readonly WeekFlowDialogueLogService _dialogueLogService = new();
    private bool _isAdvanceHeld;
    private float _nextAdvanceRepeatTime;

    private void Awake()
    {
        EnsureAdvanceAction();
        InitializePanels();
        SetMainCanvasVisible(true);
        BindWeekScreenEvents();
        BindDialogueScreenEvents();
        BindWeeklyResultLogEvents();
        BindWeeklyStatResultEvents();
        BindEndingLetterEvents();
        BindLogEvents();
        HideTransientViews();
    }

    private void OnEnable()
    {
        BindAdvanceInput();
    }

    private void OnDestroy()
    {
        UnbindAdvanceInput();
        UnbindWeekScreenEvents();
        UnbindDialogueScreenEvents();
        UnbindWeeklyResultLogEvents();
        UnbindWeeklyStatResultEvents();
        UnbindEndingLetterEvents();
        UnbindLogEvents();
    }

    private void OnDisable()
    {
        UnbindAdvanceInput();
    }

    private void Update()
    {
        if (!_isAdvanceHeld || Time.unscaledTime < _nextAdvanceRepeatTime)
        {
            return;
        }

        TryAdvance();
        _nextAdvanceRepeatTime = Time.unscaledTime + Mathf.Max(0.01f, _advanceRepeatInterval);
    }

    public override void RenderWeekHeader(WeekHeaderPresentation presentation)
    {
        if (_weekScreenView == null)
        {
            return;
        }

        _weekScreenView.RenderWeekHeader(presentation);
    }

    public override void RenderSelectionGroups(IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups)
    {
        if (_weekScreenView == null)
        {
            return;
        }

        _weekScreenView.RenderSelectionGroups(groups);
    }

    public override void RenderChildState(ChildStatePresentation presentation)
    {
        if (_weekScreenView == null)
        {
            return;
        }

        _weekScreenView.RenderChildState(presentation);
    }

    public override void RenderDayFlowProgress(DayFlowProgressSnapshot presentation)
    {
        if (_dayFlowProgressView == null)
        {
            return;
        }

        _dayFlowProgressView.Render(presentation);
    }

    public override void ShowWeekFeedback(WeekFeedbackPresentation presentation)
    {
        if (_dialogueScreenView == null)
        {
            return;
        }

        HideEndingLetterView();
        _dialogueContinueRoute = EDialogueContinueRoute.WeekFeedback;
        _dialogueScreenView.ShowWeekFeedback(presentation);
    }

    public override void ShowInteractiveEvent(InteractiveEventPresentation presentation)
    {
        if (_dialogueScreenView == null)
        {
            return;
        }

        HideEndingLetterView();
        _dialogueContinueRoute = EDialogueContinueRoute.Narrative;
        _dialogueScreenView.ShowInteractiveEvent(presentation);
    }

    public override void ShowInteractiveEventResult(InteractiveEventChoiceResultPresentation presentation)
    {
        if (_dialogueScreenView == null)
        {
            return;
        }

        HideEndingLetterView();
        _dialogueContinueRoute = EDialogueContinueRoute.Narrative;
        _dialogueScreenView.ShowInteractiveEventResult(presentation);
    }

    public override void ShowWeeklyResultLog(WeeklyResultLogPresentation presentation)
    {
        CancelAdvanceHold();
        _dialogueContinueRoute = EDialogueContinueRoute.None;
        HideEndingLetterView();

        if (_dialogueScreenView != null)
        {
            _dialogueScreenView.HideView();
        }

        if (_weeklyStatResultPanel != null)
        {
            _weeklyStatResultPanel.Hide();
        }

        if (_weeklyResultLogPanel != null)
        {
            _weeklyResultLogPanel.Show(presentation);
            _weeklyResultLogPanel.SetPreserveOnContinue(true);
            _weeklyResultLogPanel.SetAdvanceButtonEnabled(true);
        }
    }

    public override void ShowWeeklyStatResult(WeeklyStatResultPresentation presentation)
    {
        Debug.Log(
            $"[WeeklyStatDebug] Root.ShowWeeklyStatResult " +
            $"panelNull={_weeklyStatResultPanel == null} " +
            $"resultLogVisible={(_weeklyResultLogPanel != null && _weeklyResultLogPanel.IsVisible)} " +
            $"changeCount={presentation.Changes?.Count ?? -1}");

        CancelAdvanceHold();
        _dialogueContinueRoute = EDialogueContinueRoute.None;
        HideEndingLetterView();

        if (_dialogueScreenView != null)
        {
            _dialogueScreenView.HideView();
        }

        if (_weeklyStatResultPanel != null)
        {
            if (_weeklyResultLogPanel != null)
            {
                _weeklyResultLogPanel.SetAdvanceButtonEnabled(false);
                _weeklyResultLogPanel.SetAdvanceButtonVisible(false);
            }

            _weeklyStatResultPanel.Show(presentation);
            _weeklyStatResultPanel.SelectAdvanceButton();
        }
    }

    public override void ShowEnding(EndingPresentation presentation)
    {
        CancelAdvanceHold();
        _dialogueContinueRoute = EDialogueContinueRoute.Narrative;

        if (_dialogueScreenView != null)
        {
            _dialogueScreenView.HideView();
        }

        if (_endingLetterView != null)
        {
            _endingLetterView.Show(presentation);
            return;
        }

        if (_dialogueScreenView != null)
        {
            _dialogueScreenView.ShowEnding(presentation);
        }
    }

    public override void ShowEndingFollowUp()
    {
        _dialogueContinueRoute = EDialogueContinueRoute.None;

        SetEndingFollowUpPanelVisible(true);
    }

    public override void HideTransientViews()
    {
        _dialogueContinueRoute = EDialogueContinueRoute.None;
        SetEndingFollowUpPanelVisible(false);

        if (_dialogueScreenView != null)
        {
            _dialogueScreenView.HideView();
        }

        if (_weeklyResultLogPanel != null)
        {
            _weeklyResultLogPanel.Hide();
        }

        if (_weeklyStatResultPanel != null)
        {
            _weeklyStatResultPanel.Hide();
        }
    }

    public override void SetMainCanvasVisible(bool visible)
    {
        if (_mainCanvasGroup == null)
        {
            return;
        }

        if (_nemo == null)
        {
            return;
        }

        _nemo.SetActive(visible);
        if (visible)
        {
            NemoEntity.Instance.ResumeRoutine();
        }
        else
        {
            NemoEntity.Instance.StopDailyRoutine();
        }
        if (_interactionPanel != null)
        {
            _interactionPanel.SetActive(visible);
        }
        _mainCanvasGroup.alpha = visible ? 1f : 0f;
        _mainCanvasGroup.interactable = visible;
        _mainCanvasGroup.blocksRaycasts = visible;
    }

    public override void AppendDialogueLogEntries(IReadOnlyList<DialogueLogEntry> entries)
    {
        if (_dialogueLogService == null || entries == null)
        {
            return;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            _dialogueLogService.Append(entries[index]);
        }
    }

    public override WeekFlowCutsceneBridgeBase GetCutsceneBridge()
    {
        return _cutsceneBridge;
    }

    public override void SetFlowScreenContext(WeekFlowScreen screen, RuntimeChildState childState, RuntimeWeekResult lastWeekResult)
    {
        if (_dialogueScreenView == null)
        {
            return;
        }

        _dialogueScreenView.SetScreenContext(screen, childState, lastWeekResult);
    }

    public override IEnumerator PlayCurrentDialogueCutscene()
    {
        if (_weeklyResultLogPanel != null && _weeklyResultLogPanel.IsVisible)
        {
            yield break;
        }

        if (_weeklyStatResultPanel != null && _weeklyStatResultPanel.IsVisible)
        {
            yield break;
        }

        if (_dialogueScreenView == null)
        {
            yield break;
        }

        yield return _dialogueScreenView.PlayCurrentDialogueCutscene();
    }

    public override IEnumerator PlayFlowTransition(WeekFlowTransitionContext context)
    {
        if (_transitionPlayer == null)
        {
            yield break;
        }

        yield return _transitionPlayer.Play(context);
    }

    public override IEnumerator PlayWeekEntryIntro(SO_WeekDefinition weekDefinition)
    {
        if (weekDefinition == null || _weekEntryIntroCatalog == null || _weekEntryIntroOverlay == null)
        {
            yield break;
        }

        if (!_weekEntryIntroCatalog.TryGet(weekDefinition.Id, out WeekEntryIntroEntry entry))
        {
            yield break;
        }

        yield return _weekEntryIntroOverlay.Play(entry);
    }

    private void InitializePanels()
    {
        if (_dialogueScreenView == null)
        {
            return;
        }

        _dialogueScreenView.SetDialogueLogService(_dialogueLogService);
        _dialogueScreenView.SetCutsceneBridge(_cutsceneBridge);

        if (_endingLetterView != null)
        {
            _endingLetterView.SetDialogueLogService(_dialogueLogService);
        }

        if (_dialogueLogPanel != null)
        {
            _dialogueLogPanel.SetDialogueLogService(_dialogueLogService);
            _dialogueLogPanel.Hide();
        }

        if (_weeklyResultLogPanel != null)
        {
            _weeklyResultLogPanel.Hide();
        }

        if (_weeklyStatResultPanel != null)
        {
            _weeklyStatResultPanel.Hide();
        }
    }

    private void BindWeekScreenEvents()
    {
        if (_weekScreenView == null)
        {
            return;
        }

        _weekScreenView.RunWeekRequested += HandleRunWeekRequested;
        _weekScreenView.CardOptionSelected += HandleCardOptionSelected;
        _weekScreenView.AllCardSemanticSelected += HandleAllCardSemanticSelected;
    }

    private void UnbindWeekScreenEvents()
    {
        if (_weekScreenView == null)
        {
            return;
        }

        _weekScreenView.RunWeekRequested -= HandleRunWeekRequested;
        _weekScreenView.CardOptionSelected -= HandleCardOptionSelected;
        _weekScreenView.AllCardSemanticSelected -= HandleAllCardSemanticSelected;
    }

    private void BindDialogueScreenEvents()
    {
        if (_dialogueScreenView == null)
        {
            return;
        }

        _dialogueScreenView.ContinueRequested += HandleDialogueContinueRequested;
        _dialogueScreenView.EventSkipRequested += HandleDialogueEventSkipRequested;
        _dialogueScreenView.ChoiceSelected += HandleDialogueChoiceSelected;
    }

    private void BindEndingLetterEvents()
    {
        if (_endingLetterView == null)
        {
            return;
        }

        _endingLetterView.ContinueRequested += HandleDialogueContinueRequested;
    }

    private void BindWeeklyResultLogEvents()
    {
        if (_weeklyResultLogPanel == null)
        {
            return;
        }

        _weeklyResultLogPanel.ContinueRequested += HandleWeeklyResultLogContinueRequested;
        _weeklyResultLogPanel.AdvanceInputRequested += HandlePanelAdvanceInputRequested;
    }

    private void BindWeeklyStatResultEvents()
    {
        if (_weeklyStatResultPanel == null)
        {
            return;
        }

        _weeklyStatResultPanel.ContinueRequested += HandleWeeklyStatResultContinueRequested;
        _weeklyStatResultPanel.AdvanceInputRequested += HandlePanelAdvanceInputRequested;
    }

    private void BindLogEvents()
    {
        if (_openLogButton == null)
        {
            return;
        }

        _openLogButton.onClick.AddListener(HandleOpenLogButtonClicked);
    }

    private void UnbindDialogueScreenEvents()
    {
        if (_dialogueScreenView == null)
        {
            return;
        }

        _dialogueScreenView.ContinueRequested -= HandleDialogueContinueRequested;
        _dialogueScreenView.EventSkipRequested -= HandleDialogueEventSkipRequested;
        _dialogueScreenView.ChoiceSelected -= HandleDialogueChoiceSelected;
    }

    private void UnbindEndingLetterEvents()
    {
        if (_endingLetterView == null)
        {
            return;
        }

        _endingLetterView.ContinueRequested -= HandleDialogueContinueRequested;
    }

    private void UnbindWeeklyResultLogEvents()
    {
        if (_weeklyResultLogPanel == null)
        {
            return;
        }

        _weeklyResultLogPanel.ContinueRequested -= HandleWeeklyResultLogContinueRequested;
        _weeklyResultLogPanel.AdvanceInputRequested -= HandlePanelAdvanceInputRequested;
    }

    private void UnbindWeeklyStatResultEvents()
    {
        if (_weeklyStatResultPanel == null)
        {
            return;
        }

        _weeklyStatResultPanel.ContinueRequested -= HandleWeeklyStatResultContinueRequested;
        _weeklyStatResultPanel.AdvanceInputRequested -= HandlePanelAdvanceInputRequested;
    }

    private void UnbindLogEvents()
    {
        if (_openLogButton == null)
        {
            return;
        }

        _openLogButton.onClick.RemoveListener(HandleOpenLogButtonClicked);
    }

    private void HandleRunWeekRequested()
    {
        RaiseRunWeekRequested();
    }

    private void HandleOpenLogButtonClicked()
    {
        if (_dialogueLogPanel == null)
        {
            return;
        }

        _dialogueLogPanel.Show();
    }

    private void HandleCardOptionSelected(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        RaiseCardOptionSelected(cardDefinition, optionIndex);
    }

    private void HandleAllCardSemanticSelected(ECardOptionSemantic semantic)
    {
        RaiseAllCardSemanticSelected(semantic);
    }

    public bool TryAdvance()
    {
        if (TrySkipCurrentTransition())
        {
            return true;
        }

        if (IsEndingFollowUpVisible())
        {
            return false;
        }

        if (_endingLetterView != null && _endingLetterView.IsVisible)
        {
            return _endingLetterView.TryAdvance();
        }

        if (_weeklyStatResultPanel != null && _weeklyStatResultPanel.IsVisible)
        {
            return _weeklyStatResultPanel.TryAdvance();
        }

        if (_weeklyResultLogPanel != null && _weeklyResultLogPanel.IsVisible)
        {
            return _weeklyResultLogPanel.TryAdvance();
        }

        if (_dialogueScreenView == null)
        {
            return false;
        }

        return _dialogueScreenView.TryAdvance();
    }

    private void HandleDialogueContinueRequested()
    {
        if (TrySkipCurrentTransition())
        {
            return;
        }

        switch (_dialogueContinueRoute)
        {
            case EDialogueContinueRoute.WeekFeedback:
                RaiseWeekFeedbackClosed();
                break;

            case EDialogueContinueRoute.Narrative:
                RaiseInteractiveEventContinueRequested();
                break;
        }
    }

    private void HandleDialogueChoiceSelected(int choiceIndex)
    {
        if (TrySkipCurrentTransition())
        {
            return;
        }

        RaiseInteractiveEventChoiceSelected(choiceIndex);
    }

    private void HandleWeeklyResultLogContinueRequested()
    {
        Debug.Log(
            $"[WeeklyStatDebug] Root.HandleWeeklyResultLogContinueRequested " +
            $"resultLogVisible={(_weeklyResultLogPanel != null && _weeklyResultLogPanel.IsVisible)} " +
            $"statVisible={(_weeklyStatResultPanel != null && _weeklyStatResultPanel.IsVisible)}");

        CancelAdvanceHold();
        RaiseWeeklyResultLogContinueRequested();
    }

    private void HandleWeeklyStatResultContinueRequested()
    {
        Debug.Log(
            $"[WeeklyStatDebug] Root.HandleWeeklyStatResultContinueRequested " +
            $"resultLogVisible={(_weeklyResultLogPanel != null && _weeklyResultLogPanel.IsVisible)} " +
            $"statVisible={(_weeklyStatResultPanel != null && _weeklyStatResultPanel.IsVisible)}");

        CancelAdvanceHold();
        RaiseWeeklyStatResultContinueRequested();
    }

    private void HandlePanelAdvanceInputRequested()
    {
        TryAdvance();
    }

    private void HandleDialogueEventSkipRequested()
    {
        if (TrySkipCurrentTransition())
        {
            return;
        }

        CancelAdvanceHold();
        _dialogueScreenView?.FlushUnloggedDialogueLinesToLog();
        RaiseInteractiveEventSkipRequested();
    }

    private bool TrySkipCurrentTransition()
    {
        if (_transitionPlayer == null)
        {
            return false;
        }

        return _transitionPlayer.TrySkipCurrent();
    }

    private void BindAdvanceInput()
    {
        EnsureAdvanceAction();
        if (_advanceAction == null)
        {
            return;
        }

        _advanceAction.performed -= HandleAdvancePerformed;
        _advanceAction.canceled -= HandleAdvanceCanceled;
        _advanceAction.performed += HandleAdvancePerformed;
        _advanceAction.canceled += HandleAdvanceCanceled;

        if (!_advanceAction.enabled)
        {
            _advanceAction.Enable();
        }
    }

    private void UnbindAdvanceInput()
    {
        _isAdvanceHeld = false;

        if (_advanceAction == null)
        {
            return;
        }

        _advanceAction.performed -= HandleAdvancePerformed;
        _advanceAction.canceled -= HandleAdvanceCanceled;

        if (_advanceAction.enabled)
        {
            _advanceAction.Disable();
        }
    }

    private void HandleAdvancePerformed(InputAction.CallbackContext context)
    {
        _isAdvanceHeld = true;
        _nextAdvanceRepeatTime = Time.unscaledTime + Mathf.Max(0f, _advanceHoldDelay);
        TryAdvance();
    }

    private void HandleAdvanceCanceled(InputAction.CallbackContext context)
    {
        _isAdvanceHeld = false;
    }

    private void EnsureAdvanceAction()
    {
        if (HasCtrlAdvanceBindings())
        {
            return;
        }

        _advanceAction = CreateAdvanceAction();
    }

    private bool HasCtrlAdvanceBindings()
    {
        if (_advanceAction == null || _advanceAction.bindings.Count != 2)
        {
            return false;
        }

        return _advanceAction.bindings[0].path == LeftCtrlBindingPath
            && _advanceAction.bindings[1].path == RightCtrlBindingPath;
    }

    private static InputAction CreateAdvanceAction()
    {
        InputAction action = new("Advance", InputActionType.Button);
        action.AddBinding(LeftCtrlBindingPath);
        action.AddBinding(RightCtrlBindingPath);
        return action;
    }

    private void CancelAdvanceHold()
    {
        _isAdvanceHeld = false;
        _nextAdvanceRepeatTime = 0f;
    }

    private void SetEndingFollowUpPanelVisible(bool visible)
    {
        if (_endingFollowUpPanel == null)
        {
            return;
        }

        _endingFollowUpPanel.SetActive(visible);
    }

    private bool IsEndingFollowUpVisible()
    {
        return _endingFollowUpPanel != null && _endingFollowUpPanel.activeSelf;
    }

    private void HideEndingLetterView()
    {
        if (_endingLetterView == null)
        {
            return;
        }

        _endingLetterView.Hide();
    }
}

public sealed class UI_DayFlowProgressView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private GameObject _contentRoot;

    [Header("Progress")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private RectTransform _indicator;
    [SerializeField] private RectTransform[] _dotSlots = System.Array.Empty<RectTransform>();

    [Header("Animation")]
    [SerializeField] private bool _animateFirstRender;
    [SerializeField] private float _moveDuration = 0.35f;
    [SerializeField] private float _fillDuration = 0.35f;
    [SerializeField] private Ease _moveEase = Ease.OutCubic;
    [SerializeField] private Ease _fillEase = Ease.OutCubic;

    private Sequence _sequence;
    private int _lastTotalCount = -1;
    private int _lastCurrentIndex = -1;
    private bool _hasRenderedActiveProgress;

    private void Awake()
    {
        ResolveReferences();
        HideInstant();
    }

    private void OnDestroy()
    {
        KillSequence(false);
    }

    public void Render(DayFlowProgressSnapshot progress)
    {
        ResolveReferences();

        int visibleCount = GetVisibleCount(progress.TotalCount);
        if (!progress.IsActive || visibleCount <= 0)
        {
            HideInstant();
            return;
        }

        bool layoutChanged = visibleCount != _lastTotalCount;
        SetVisible(true);
        RenderDots(visibleCount);

        int targetIndex = Mathf.Clamp(progress.CurrentIndex, 0, visibleCount - 1);
        Vector2 targetPosition = GetIndicatorPosition(targetIndex);
        float targetFillAmount = ResolveFillAmount(progress, visibleCount);
        bool shouldAnimate = _hasRenderedActiveProgress || _animateFirstRender;

        if (layoutChanged || !shouldAnimate)
        {
            KillSequence(false);
            SetIndicatorPosition(targetPosition);
            SetFillAmount(targetFillAmount);
        }
        else if (targetIndex != _lastCurrentIndex)
        {
            PlayProgressTween(targetPosition, targetFillAmount);
        }
        else
        {
            SetFillAmount(targetFillAmount);
        }

        _lastTotalCount = visibleCount;
        _lastCurrentIndex = targetIndex;
        _hasRenderedActiveProgress = true;
    }

    private int GetVisibleCount(int requestedCount)
    {
        if (_dotSlots == null || _dotSlots.Length == 0)
        {
            return 0;
        }

        return Mathf.Clamp(requestedCount, 0, _dotSlots.Length);
    }

    private void RenderDots(int visibleCount)
    {
        if (_dotSlots == null)
        {
            return;
        }

        for (int index = 0; index < _dotSlots.Length; index++)
        {
            RectTransform dotSlot = _dotSlots[index];
            if (dotSlot != null)
            {
                dotSlot.gameObject.SetActive(index < visibleCount);
            }
        }
    }

    private void PlayProgressTween(Vector2 targetPosition, float targetFillAmount)
    {
        KillSequence(false);

        _sequence = DOTween.Sequence();
        if (_indicator != null)
        {
            _sequence.Join(_indicator
                .DOAnchorPos(targetPosition, Mathf.Max(0f, _moveDuration))
                .SetEase(_moveEase));
        }

        if (_fillImage != null)
        {
            _sequence.Join(_fillImage
                .DOFillAmount(targetFillAmount, Mathf.Max(0f, _fillDuration))
                .SetEase(_fillEase));
        }
    }

    private Vector2 GetIndicatorPosition(int dotIndex)
    {
        RectTransform dotSlot = _dotSlots[dotIndex];
        if (_indicator == null || dotSlot == null)
        {
            return Vector2.zero;
        }

        if (_indicator.parent == dotSlot.parent)
        {
            return dotSlot.anchoredPosition;
        }

        RectTransform indicatorParent = _indicator.parent as RectTransform;
        if (indicatorParent == null)
        {
            return dotSlot.anchoredPosition;
        }

        Vector3 localPosition = indicatorParent.InverseTransformPoint(dotSlot.position);
        return new Vector2(localPosition.x, localPosition.y);
    }

    private float ResolveFillAmount(DayFlowProgressSnapshot progress, int visibleCount)
    {
        if (visibleCount <= 0)
        {
            return 0f;
        }

        int currentIndex = Mathf.Clamp(progress.CurrentIndex, 0, visibleCount - 1);
        return Mathf.Clamp01((currentIndex + 1f) / visibleCount);
    }

    private void SetIndicatorPosition(Vector2 position)
    {
        if (_indicator != null)
        {
            _indicator.anchoredPosition = position;
        }
    }

    private void SetFillAmount(float fillAmount)
    {
        if (_fillImage != null)
        {
            _fillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_contentRoot != null)
        {
            _contentRoot.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_indicator != null)
        {
            _indicator.gameObject.SetActive(visible);
        }
    }

    private void HideInstant()
    {
        KillSequence(false);
        RenderDots(0);
        SetFillAmount(0f);
        SetVisible(false);
        _lastTotalCount = -1;
        _lastCurrentIndex = -1;
        _hasRenderedActiveProgress = false;
    }

    private void KillSequence(bool complete)
    {
        if (_sequence == null)
        {
            return;
        }

        _sequence.Kill(complete);
        _sequence = null;
    }

    private void ResolveReferences()
    {
        if (_canvasGroup == null)
        {
            TryGetComponent(out _canvasGroup);
        }
    }
}
