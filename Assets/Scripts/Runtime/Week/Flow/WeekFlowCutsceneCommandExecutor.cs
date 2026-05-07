using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class WeekFlowCutsceneCommandExecutor
{
    private readonly WeekFlowCutsceneTargetRegistry _targetRegistry;
    private readonly List<Tween> _activeTweens = new();

    public WeekFlowCutsceneCommandExecutor(WeekFlowCutsceneTargetRegistry targetRegistry)
    {
        _targetRegistry = targetRegistry;
    }

    public IEnumerator Execute(CutsceneCommandData command, WeekFlowCutsceneRequest request, bool ignoreCharacterCommands = false)
    {
        if (command == null || command.CommandType == EDataCutsceneCommandType.None)
        {
            yield break;
        }

        if (ignoreCharacterCommands &&
            (command.CommandType == EDataCutsceneCommandType.ShowCharacter ||
             command.CommandType == EDataCutsceneCommandType.HideCharacters))
        {
            yield break;
        }

        switch (command.CommandType)
        {
            case EDataCutsceneCommandType.ShowBackground:
                ShowBackground(command.Value1);
                break;

            case EDataCutsceneCommandType.HideBackground:
                HideBackground(command.Value1);
                break;

            case EDataCutsceneCommandType.ShowCharacter:
                ShowCharacter(command.TargetKey, command.Value1);
                break;

            case EDataCutsceneCommandType.HideCharacters:
                HideCharacters(command.TargetKey);
                break;

            case EDataCutsceneCommandType.FadeCanvas:
                yield return PlayFadeCanvas(command);
                break;

            case EDataCutsceneCommandType.MoveRect:
                yield return PlayMoveRect(command);
                break;

            case EDataCutsceneCommandType.SetActive:
                SetActive(command.TargetKey, command.Value1);
                break;

            case EDataCutsceneCommandType.Wait:
                yield return new WaitForSeconds(Mathf.Max(0f, command.Duration));
                break;
        }
    }

    public void StopImmediate()
    {
        for (int index = _activeTweens.Count - 1; index >= 0; index--)
        {
            Tween tween = _activeTweens[index];
            if (tween != null && tween.IsActive())
            {
                tween.Kill(false);
            }
        }

        _activeTweens.Clear();
        _targetRegistry?.RestoreDefaults();
    }

    private void ShowBackground(string value)
    {
        if (BackgroundManager.I == null || !TryParseEnum(value, out BackgroundType backgroundType))
        {
            return;
        }

        BackgroundManager.I.ShowBackground(backgroundType);
    }

    private void HideBackground(string value)
    {
        if (BackgroundManager.I == null || !TryParseEnum(value, out BackgroundType backgroundType))
        {
            return;
        }

        BackgroundManager.I.HideBackground(backgroundType);
    }

    private void ShowCharacter(string targetKey, string value)
    {
        if (CutsceneCharacterManager.I == null || !TryParseEnum(value, out CutsceneCharacterType characterType))
        {
            return;
        }

        switch (Normalize(targetKey))
        {
            case "left":
                CutsceneCharacterManager.I.ShowLeft(characterType);
                break;
            case "right":
                CutsceneCharacterManager.I.ShowRight(characterType);
                break;
            case "center":
                CutsceneCharacterManager.I.ShowCenter(characterType);
                break;
        }
    }

    private void HideCharacters(string targetKey)
    {
        if (CutsceneCharacterManager.I == null)
        {
            return;
        }

        switch (Normalize(targetKey))
        {
            case "":
            case "all":
                CutsceneCharacterManager.I.HideAllDeferred();
                break;
            case "left":
                CutsceneCharacterManager.I.HideLeft();
                break;
            case "right":
                CutsceneCharacterManager.I.HideRight();
                break;
            case "center":
                CutsceneCharacterManager.I.HideCenter();
                break;
        }
    }

    private IEnumerator PlayFadeCanvas(CutsceneCommandData command)
    {
        if (_targetRegistry == null ||
            !_targetRegistry.TryGetCanvasGroup(command.TargetKey, out CanvasGroup canvasGroup, out float defaultAlpha))
        {
            yield break;
        }

        float duration = Mathf.Max(0f, command.Duration);
        string mode = Normalize(command.Value1);

        if (duration <= 0f)
        {
            canvasGroup.alpha = mode == "out" ? 0f : defaultAlpha;
            yield break;
        }

        if (mode == "inout")
        {
            canvasGroup.alpha = 0f;
            Sequence sequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(defaultAlpha, duration).SetEase(command.Ease))
                .Append(canvasGroup.DOFade(0f, duration).SetEase(command.Ease));
            yield return TrackTween(sequence);
            yield break;
        }

        float startAlpha = mode == "out" ? defaultAlpha : 0f;
        float endAlpha = mode == "out" ? 0f : defaultAlpha;
        canvasGroup.alpha = startAlpha;
        yield return TrackTween(canvasGroup.DOFade(endAlpha, duration).SetEase(command.Ease));
    }

    private IEnumerator PlayMoveRect(CutsceneCommandData command)
    {
        if (_targetRegistry == null ||
            !_targetRegistry.TryGetRectTransform(command.TargetKey, out RectTransform rectTransform, out Vector2 defaultPosition))
        {
            yield break;
        }

        Vector2 offset = new(ParseFloat(command.Value1), ParseFloat(command.Value2));
        float duration = Mathf.Max(0f, command.Duration);
        rectTransform.anchoredPosition = defaultPosition + offset;

        if (duration <= 0f)
        {
            rectTransform.anchoredPosition = defaultPosition;
            yield break;
        }

        yield return TrackTween(rectTransform.DOAnchorPos(defaultPosition, duration).SetEase(command.Ease));
    }

    private void SetActive(string targetKey, string value)
    {
        if (_targetRegistry == null || !_targetRegistry.TryGetGameObject(targetKey, out GameObject target))
        {
            return;
        }

        target.SetActive(ParseBool(value));
    }

    private IEnumerator TrackTween(Tween tween)
    {
        if (tween == null)
        {
            yield break;
        }

        _activeTweens.Add(tween);
        yield return tween.WaitForCompletion();
        _activeTweens.Remove(tween);
    }

    private static bool TryParseEnum<TEnum>(string value, out TEnum parsed)
        where TEnum : struct
    {
        return Enum.TryParse(value, true, out parsed);
    }

    private static string Normalize(string value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static float ParseFloat(string value)
    {
        return float.TryParse(value, out float parsed) ? parsed : 0f;
    }

    private static bool ParseBool(string value)
    {
        return string.IsNullOrWhiteSpace(value) || bool.TryParse(value, out bool parsed) && parsed;
    }
}
