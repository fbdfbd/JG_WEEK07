using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PoCViewBase : MonoBehaviour
{
    public event Action IntroContinueRequested;
    public event Action ReportContinueRequested;
    public event Action<string> MainChoiceSelected;
    public event Action DayEventContinueRequested;
    public event Action NightContinueRequested;
    public event Action<int> NightChoiceSelected;
    public event Action EndContinueRequested;

    protected void RaiseIntroContinueRequested()
    {
        IntroContinueRequested?.Invoke();
    }

    protected void RaiseReportContinueRequested()
    {
        ReportContinueRequested?.Invoke();
    }

    protected void RaiseMainChoiceSelected(string choiceId)
    {
        MainChoiceSelected?.Invoke(choiceId);
    }

    protected void RaiseDayEventContinueRequested()
    {
        DayEventContinueRequested?.Invoke();
    }

    protected void RaiseNightContinueRequested()
    {
        NightContinueRequested?.Invoke();
    }

    protected void RaiseNightChoiceSelected(int choiceIndex)
    {
        NightChoiceSelected?.Invoke(choiceIndex);
    }

    protected void RaiseEndContinueRequested()
    {
        EndContinueRequested?.Invoke();
    }

    public virtual void ShowIntro(PoCTextPresentation presentation) { }
    public virtual void ShowReport(PoCTextPresentation presentation) { }
    public virtual void ShowMainChoice(PoCMainChoicePresentation presentation) { }
    public virtual void ShowDayEvent(PoCDayEventPresentation presentation) { }
    public virtual void ShowNightDialogue(InteractiveEventPresentation presentation) { }
    public virtual void ShowNightChoiceResult(InteractiveEventChoiceResultPresentation presentation) { }
    public virtual void ShowEnd(PoCTextPresentation presentation) { }
    public virtual void RenderPublicStats(IReadOnlyList<PoCStatPresentation> stats) { }
}
