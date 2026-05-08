using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PoCSceneUiSetup
{
    private const string ScenePath = "Assets/Scenes/POC/POC.unity";
    private const string FlowPath = "Assets/Data/PoC/N03_AccumulatedChoice/PoCFlow_N03_AccumulatedChoice.asset";
    private const string PrefabPath = "Assets/Prefab/POC/PoCFlowView.prefab";

    static PoCSceneUiSetup()
    {
        EditorApplication.delayCall += AutoWireOpenPoCScene;
    }

    [MenuItem("Tools/PoC/Setup PoC Scene UI")]
    public static void SetupSceneUi()
    {
        GameObject prefab = CreateOrReplacePrefab();

        var scene = EditorSceneManager.OpenScene(ScenePath);
        UI_PoCFlowView[] existingViews = Object.FindObjectsByType<UI_PoCFlowView>(FindObjectsSortMode.None);
        foreach (UI_PoCFlowView view in existingViews)
        {
            Object.DestroyImmediate(view.gameObject);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "PoCFlowView";

        PoCController controller = Object.FindFirstObjectByType<PoCController>();
        if (controller != null)
        {
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("_flow").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SO_PoCFlowDefinition>(FlowPath);
            serializedController.FindProperty("_view").objectReferenceValue =
                instance.GetComponent<UI_PoCFlowView>();
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void AutoWireOpenPoCScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        PoCController controller = Object.FindFirstObjectByType<PoCController>();
        UI_PoCFlowView view = Object.FindFirstObjectByType<UI_PoCFlowView>();
        if (controller == null || view == null)
        {
            return;
        }

        view.BuildOrRepairPlaceholderUiInEditor();

        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("_flow").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<SO_PoCFlowDefinition>(FlowPath);
        serializedController.FindProperty("_view").objectReferenceValue = view;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);

        if (controller.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
    }


    private static GameObject CreateOrReplacePrefab()
    {
        GameObject root = CreateRoot();
        UI_PoCFlowView view = root.AddComponent<UI_PoCFlowView>();

        GameObject textPanel = CreatePanel("TextPanel", root.transform, new Color(0.08f, 0.08f, 0.1f, 0.86f));
        SetStretch(textPanel.GetComponent<RectTransform>(), 160, 120, 160, 120);
        TextMeshProUGUI phaseText = CreateText("PhaseLabelText", textPanel.transform, "PHASE", 24, TextAlignmentOptions.Left);
        SetRect(phaseText.rectTransform, 30, -35, 520, 50, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        TextMeshProUGUI titleText = CreateText("TitleText", textPanel.transform, "Title", 44, TextAlignmentOptions.Left);
        SetRect(titleText.rectTransform, 30, -90, 1180, 70, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        TextMeshProUGUI bodyText = CreateText("BodyText", textPanel.transform, "Body", 30, TextAlignmentOptions.TopLeft);
        SetRect(bodyText.rectTransform, 30, -180, 1220, 520, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        TextMeshProUGUI effectText = CreateText("EffectSummaryText", textPanel.transform, "Effect", 24, TextAlignmentOptions.Left);
        SetRect(effectText.rectTransform, 30, 90, 900, 60, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        Button continueButton = CreateButton("ContinueButton", textPanel.transform, "Continue");
        SetRect(continueButton.GetComponent<RectTransform>(), -260, 36, 220, 70, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
        TextMeshProUGUI continueText = continueButton.GetComponentInChildren<TextMeshProUGUI>();

        GameObject choicePanel = CreatePanel("ChoicePanel", root.transform, new Color(0.08f, 0.08f, 0.1f, 0.86f));
        SetStretch(choicePanel.GetComponent<RectTransform>(), 120, 90, 120, 90);
        TextMeshProUGUI choiceTitle = CreateText("ChoiceTitleText", choicePanel.transform, "Choice Title", 42, TextAlignmentOptions.Left);
        SetRect(choiceTitle.rectTransform, 40, -45, 1280, 70, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        TextMeshProUGUI choiceBody = CreateText("ChoiceBodyText", choicePanel.transform, "Choice Body", 28, TextAlignmentOptions.TopLeft);
        SetRect(choiceBody.rectTransform, 40, -120, 1280, 120, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

        Button[] choiceButtons = new Button[3];
        TextMeshProUGUI[] choiceLabels = new TextMeshProUGUI[3];
        TextMeshProUGUI[] choiceDescriptions = new TextMeshProUGUI[3];
        TextMeshProUGUI[] choiceEffects = new TextMeshProUGUI[3];
        for (int index = 0; index < 3; index++)
        {
            GameObject choiceCard = CreatePanel($"Choice{index + 1}", choicePanel.transform, new Color(0.18f, 0.18f, 0.22f, 0.92f));
            SetRect(choiceCard.GetComponent<RectTransform>(), 40 + index * 470, -300, 420, 430, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            choiceButtons[index] = choiceCard.AddComponent<Button>();
            choiceButtons[index].targetGraphic = choiceCard.GetComponent<Image>();
            choiceLabels[index] = CreateText("LabelText", choiceCard.transform, "Choice", 34, TextAlignmentOptions.Center);
            SetRect(choiceLabels[index].rectTransform, 24, -28, 372, 62, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            choiceDescriptions[index] = CreateText("DescriptionText", choiceCard.transform, "Description", 24, TextAlignmentOptions.TopLeft);
            SetRect(choiceDescriptions[index].rectTransform, 28, -118, 364, 220, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            choiceEffects[index] = CreateText("EffectText", choiceCard.transform, "Effect", 20, TextAlignmentOptions.BottomLeft);
            SetRect(choiceEffects[index].rectTransform, 28, 26, 364, 70, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            choiceEffects[index].gameObject.SetActive(false);
        }

        GameObject nightPanel = CreatePanel("NightDialoguePanel", root.transform, new Color(0.04f, 0.04f, 0.05f, 0.2f));
        SetStretch(nightPanel.GetComponent<RectTransform>(), 0, 0, 0, 0);
        UI_DialogueScreenView dialogueView = nightPanel.AddComponent<UI_DialogueScreenView>();
        GameObject titlePanel = CreatePanel("NightTitlePanel", nightPanel.transform, new Color(0.08f, 0.08f, 0.1f, 0.84f));
        SetRect(titlePanel.GetComponent<RectTransform>(), 160, -120, 850, 120, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        TextMeshProUGUI nightTitle = CreateText("NightTitleText", titlePanel.transform, "Night", 34, TextAlignmentOptions.Left);
        SetStretch(nightTitle.rectTransform, 24, 16, 24, 16);
        TextMeshProUGUI nightBody = CreateText("NightBodyText", nightPanel.transform, "Night Body", 24, TextAlignmentOptions.Left);
        SetRect(nightBody.rectTransform, 160, -245, 1100, 80, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        TextMeshProUGUI nightEffect = CreateText("NightEffectText", nightPanel.transform, "Effect", 22, TextAlignmentOptions.Left);
        SetRect(nightEffect.rectTransform, 160, -330, 1100, 60, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

        GameObject dialogBox = CreatePanel("DialogBox", nightPanel.transform, new Color(0.08f, 0.08f, 0.1f, 0.92f));
        SetRect(dialogBox.GetComponent<RectTransform>(), 260, 90, 1400, 250, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        UI_DialogView dialogView = dialogBox.AddComponent<UI_DialogView>();
        GameObject nameTag = CreatePanel("NameTag", dialogBox.transform, new Color(0.16f, 0.16f, 0.22f, 1f));
        SetRect(nameTag.GetComponent<RectTransform>(), 30, 190, 260, 58, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
        TextMeshProUGUI nameText = CreateText("NameText", nameTag.transform, "N-03", 24, TextAlignmentOptions.Center);
        SetStretch(nameText.rectTransform, 8, 4, 8, 4);
        TextMeshProUGUI dialogueText = CreateText("DialogueText", dialogBox.transform, "Dialogue", 30, TextAlignmentOptions.TopLeft);
        SetStretch(dialogueText.rectTransform, 44, 70, 44, 32);
        Button nightContinueButton = CreateButton("NightContinueButton", nightPanel.transform, "Continue");
        SetRect(nightContinueButton.GetComponent<RectTransform>(), -380, 115, 240, 70, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));

        GameObject statsPanel = CreatePanel("StatsPanel", root.transform, new Color(0.08f, 0.08f, 0.1f, 0.72f));
        SetRect(statsPanel.GetComponent<RectTransform>(), -390, -40, 340, 150, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
        TextMeshProUGUI[] statLabels = new TextMeshProUGUI[3];
        TextMeshProUGUI[] statValues = new TextMeshProUGUI[3];
        for (int index = 0; index < 3; index++)
        {
            statLabels[index] = CreateText($"StatLabel{index + 1}", statsPanel.transform, "Stat", 18, TextAlignmentOptions.Left);
            SetRect(statLabels[index].rectTransform, 18, -18 - index * 42, 210, 34, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            statValues[index] = CreateText($"StatValue{index + 1}", statsPanel.transform, "0", 18, TextAlignmentOptions.Right);
            SetRect(statValues[index].rectTransform, 230, -18 - index * 42, 80, 34, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        }

        AssignDialogueView(dialogueView, titlePanel, nightTitle, nightBody, nightEffect, dialogView, nightContinueButton);
        AssignDialogView(dialogView, nameTag, nameText, dialogueText);
        AssignPoCView(
            view,
            textPanel,
            choicePanel,
            nightPanel,
            phaseText,
            titleText,
            bodyText,
            effectText,
            continueButton,
            continueText,
            choiceTitle,
            choiceBody,
            choiceButtons,
            choiceLabels,
            choiceDescriptions,
            choiceEffects,
            dialogueView,
            statsPanel,
            statLabels,
            statValues);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return savedPrefab;
    }

    private static GameObject CreateRoot()
    {
        GameObject root = new("PoCFlowView");
        root.layer = 5;
        RectTransform rectTransform = root.AddComponent<RectTransform>();
        SetStretch(rectTransform, 0, 0, 0, 0);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();
        return root;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new(name);
        panel.layer = 5;
        panel.transform.SetParent(parent, false);
        panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name);
        textObject.layer = 5;
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.color = Color.white;
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/PyeojinGothic-Bold SDF.asset");
        if (font != null)
        {
            tmp.font = font;
        }

        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.25f, 0.25f, 0.3f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        TextMeshProUGUI text = CreateText("Text", buttonObject.transform, label, 24, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 12, 6, 12, 6);
        return button;
    }

    private static void AssignPoCView(
        UI_PoCFlowView view,
        GameObject textPanel,
        GameObject choicePanel,
        GameObject nightPanel,
        TextMeshProUGUI phaseText,
        TextMeshProUGUI titleText,
        TextMeshProUGUI bodyText,
        TextMeshProUGUI effectText,
        Button continueButton,
        TextMeshProUGUI continueText,
        TextMeshProUGUI choiceTitle,
        TextMeshProUGUI choiceBody,
        Button[] choiceButtons,
        TextMeshProUGUI[] choiceLabels,
        TextMeshProUGUI[] choiceDescriptions,
        TextMeshProUGUI[] choiceEffects,
        UI_DialogueScreenView nightDialogueView,
        GameObject statsPanel,
        TextMeshProUGUI[] statLabels,
        TextMeshProUGUI[] statValues)
    {
        SerializedObject serializedView = new(view);
        serializedView.FindProperty("_textPanel").objectReferenceValue = textPanel;
        serializedView.FindProperty("_choicePanel").objectReferenceValue = choicePanel;
        serializedView.FindProperty("_nightDialoguePanel").objectReferenceValue = nightPanel;
        serializedView.FindProperty("_phaseLabelText").objectReferenceValue = phaseText;
        serializedView.FindProperty("_titleText").objectReferenceValue = titleText;
        serializedView.FindProperty("_bodyText").objectReferenceValue = bodyText;
        serializedView.FindProperty("_effectSummaryText").objectReferenceValue = effectText;
        serializedView.FindProperty("_continueButton").objectReferenceValue = continueButton;
        serializedView.FindProperty("_continueButtonText").objectReferenceValue = continueText;
        serializedView.FindProperty("_choiceTitleText").objectReferenceValue = choiceTitle;
        serializedView.FindProperty("_choiceBodyText").objectReferenceValue = choiceBody;
        SetObjectArray(serializedView.FindProperty("_mainChoiceButtons"), choiceButtons);
        SetObjectArray(serializedView.FindProperty("_mainChoiceLabelTexts"), choiceLabels);
        SetObjectArray(serializedView.FindProperty("_mainChoiceDescriptionTexts"), choiceDescriptions);
        SetObjectArray(serializedView.FindProperty("_mainChoiceEffectTexts"), choiceEffects);
        serializedView.FindProperty("_nightDialogueView").objectReferenceValue = nightDialogueView;
        serializedView.FindProperty("_statsPanel").objectReferenceValue = statsPanel;
        SetObjectArray(serializedView.FindProperty("_statLabelTexts"), statLabels);
        SetObjectArray(serializedView.FindProperty("_statValueTexts"), statValues);
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(view);
    }

    private static void AssignDialogueView(
        UI_DialogueScreenView dialogueView,
        GameObject titlePanel,
        TextMeshProUGUI titleText,
        TextMeshProUGUI bodyText,
        TextMeshProUGUI effectText,
        UI_DialogView dialogPanel,
        Button continueButton)
    {
        SerializedObject serializedView = new(dialogueView);
        serializedView.FindProperty("_titlePanel").objectReferenceValue = titlePanel;
        serializedView.FindProperty("_titleText").objectReferenceValue = titleText;
        serializedView.FindProperty("_bodyText").objectReferenceValue = bodyText;
        serializedView.FindProperty("_effectSummaryText").objectReferenceValue = effectText;
        serializedView.FindProperty("_dialogPanel").objectReferenceValue = dialogPanel;
        serializedView.FindProperty("_continueButton").objectReferenceValue = continueButton;
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogueView);
    }

    private static void AssignDialogView(
        UI_DialogView dialogView,
        GameObject nameTag,
        TextMeshProUGUI nameText,
        TextMeshProUGUI contentText)
    {
        SerializedObject serializedView = new(dialogView);
        serializedView.FindProperty("_nameTagPanel").objectReferenceValue = nameTag;
        serializedView.FindProperty("_nameText").objectReferenceValue = nameText;
        serializedView.FindProperty("_mainContentText").objectReferenceValue = contentText;
        serializedView.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialogView);
    }

    private static void SetObjectArray<T>(SerializedProperty property, T[] values)
        where T : Object
    {
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    private static void SetStretch(RectTransform rectTransform, float left, float top, float right, float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private static void SetRect(
        RectTransform rectTransform,
        float x,
        float y,
        float width,
        float height,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = new Vector2(width, height);
    }
}
