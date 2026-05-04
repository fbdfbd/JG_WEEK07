using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeekFlowScreenView : MonoBehaviour
{
    [SerializeField] private UI_CardView _cardPanel;
    [SerializeField] private UI_CharacterStatusView _characterStatusPanel;
    [SerializeField] private UI_TopView _topPanel;
    [SerializeField] private UI_BottomView _bottomPanel;

    [SerializeField] private GameObject _semanticPanel;
    [SerializeField] private UI_CanvasGroupVisibilityEffect _semanticPanelEffect;
    [SerializeField] private List<GameObject> _linkedInfoPanels = new();
    [SerializeField] private Button _cardOutsideClickButton;

    public event Action RunWeekRequested;
    public event Action<SO_CardInfoDefinition, int> CardOptionSelected;
    public event Action<ECardOptionSemantic> AllCardSemanticSelected;

    private bool _isInfoPanelVisible = true;

    private void Awake()
    {
        ResolveSemanticPanelEffect();
        BindCardPanelEvents();
        BindBottomPanelEvents();
        BindOutsideClickButtonEvent();
        SetOutsideClickButtonVisible(_isInfoPanelVisible);
    }

    private void OnDestroy()
    {
        UnbindCardPanelEvents();
        UnbindBottomPanelEvents();
        UnbindOutsideClickButtonEvent();
    }

    public void RenderSelectionGroups(
        IReadOnlyList<WeekSelectionCategoryGroupPresentation> groups,
        WeekSelectionGroupRenderOptions renderOptions = default)
    {
        if (_cardPanel == null)
        {
            return;
        }

        _cardPanel.SetCardGroups(groups, renderOptions.ResetPosition);
    }

    public void RenderChildState(ChildStatePresentation presentation)
    {
        if (_characterStatusPanel == null)
        {
            return;
        }

        _characterStatusPanel.RenderStatus(presentation);
    }

    public void RenderWeekHeader(WeekHeaderPresentation presentation)
    {
        if (_topPanel == null)
        {
            return;
        }

        _topPanel.RenderHeader(presentation);
    }

    private void BindCardPanelEvents()
    {
        if (_cardPanel == null)
        {
            return;
        }

        _cardPanel.OnCardOptionClicked += HandleCardOptionClicked;
        _cardPanel.OnAllCardSemanticRequested += HandleAllCardSemanticRequested;
    }

    private void UnbindCardPanelEvents()
    {
        if (_cardPanel == null)
        {
            return;
        }

        _cardPanel.OnCardOptionClicked -= HandleCardOptionClicked;
        _cardPanel.OnAllCardSemanticRequested -= HandleAllCardSemanticRequested;
    }

    private void BindBottomPanelEvents()
    {
        if (_bottomPanel == null)
        {
            return;
        }

        _bottomPanel.OnInfoButtonClicked += HandleInfoButtonClicked;
        _bottomPanel.OnNextDayButtonClicked += HandleNextDayButtonClicked;
    }

    private void UnbindBottomPanelEvents()
    {
        if (_bottomPanel == null)
        {
            return;
        }

        _bottomPanel.OnInfoButtonClicked -= HandleInfoButtonClicked;
        _bottomPanel.OnNextDayButtonClicked -= HandleNextDayButtonClicked;
    }

    private void HandleCardOptionClicked(SO_CardInfoDefinition cardDefinition, int optionIndex)
    {
        CardOptionSelected?.Invoke(cardDefinition, optionIndex);
    }

    private void HandleAllCardSemanticRequested(ECardOptionSemantic semantic)
    {
        AllCardSemanticSelected?.Invoke(semantic);
    }

    private void HandleInfoButtonClicked()
    {
        SetInfoPanelsVisible(!_isInfoPanelVisible);
    }

    private void HandleOutsideClickButtonClicked()
    {
        SetInfoPanelsVisible(false);
    }

    private void SetInfoPanelsVisible(bool visible)
    {
        _isInfoPanelVisible = visible;

        SetSemanticPanelVisible(visible);
        SetCardPanelVisible(visible);
        SetLinkedInfoPanelsVisible(visible);
        SetOutsideClickButtonVisible(visible);
    }

    private void SetCardPanelVisible(bool visible)
    {
        if (_cardPanel == null)
        {
            return;
        }

        GameObject go = _cardPanel.gameObject;

        if (_cardPanel.TryGetComponent<UI_CardShowEffect>(out var effect))
        {
            if (visible)
            {
                go.SetActive(true);
                effect.PlayOpen();
            }
            else
            {
                effect.Close();
            }
        }
        else
        {
            go.SetActive(visible);
        }
    }

    private void SetSemanticPanelVisible(bool visible)
    {
        if (_semanticPanelEffect != null)
        {
            if (visible)
            {
                _semanticPanelEffect.Open();
            }
            else
            {
                _semanticPanelEffect.Close();
            }

            return;
        }

        if (_semanticPanel != null)
        {
            _semanticPanel.SetActive(visible);
        }
    }

    private void SetLinkedInfoPanelsVisible(bool visible)
    {
        if (_linkedInfoPanels == null)
        {
            return;
        }

        foreach (GameObject panel in _linkedInfoPanels)
        {
            SetLinkedPanelVisible(panel, visible);
        }
    }

    private static void SetLinkedPanelVisible(GameObject panel, bool visible)
    {
        if (panel == null)
        {
            return;
        }

        if (panel.TryGetComponent<UI_CardShowEffect>(out var cardShowEffect))
        {
            if (visible)
            {
                cardShowEffect.Open();
            }
            else
            {
                cardShowEffect.Close();
            }

            return;
        }

        if (panel.TryGetComponent<UI_CanvasGroupVisibilityEffect>(out var canvasGroupEffect))
        {
            if (visible)
            {
                canvasGroupEffect.Open();
            }
            else
            {
                canvasGroupEffect.Close();
            }

            return;
        }

        panel.SetActive(visible);
    }

    private void BindOutsideClickButtonEvent()
    {
        if (_cardOutsideClickButton == null)
        {
            return;
        }

        _cardOutsideClickButton.onClick.AddListener(HandleOutsideClickButtonClicked);
    }

    private void UnbindOutsideClickButtonEvent()
    {
        if (_cardOutsideClickButton == null)
        {
            return;
        }

        _cardOutsideClickButton.onClick.RemoveListener(HandleOutsideClickButtonClicked);
    }

    private void SetOutsideClickButtonVisible(bool visible)
    {
        if (_cardOutsideClickButton == null)
        {
            return;
        }

        _cardOutsideClickButton.gameObject.SetActive(visible);
    }

    private void ResolveSemanticPanelEffect()
    {
        if (_semanticPanelEffect != null || _semanticPanel == null)
        {
            return;
        }

        _semanticPanel.TryGetComponent(out _semanticPanelEffect);
    }

    private void HandleNextDayButtonClicked()
    {
        RunWeekRequested?.Invoke();
    }
}
