using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TMPFontReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;
    private bool includeInactive = true;
    private bool replaceMaterialPreset = false; // 머티리얼 프리셋도 기본값으로 초기화할지 여부
    private Vector2 scrollPos;
    private List<TMP_Text> foundTexts = new List<TMP_Text>();
    private int prefabInstanceCount = 0; // 검색된 것 중 프리팹 인스턴스의 수

    [MenuItem("Tools/TMP/Scene Font Replacer")]
    public static void ShowWindow()
    {
        GetWindow<TMPFontReplacer>("TMP Font Replacer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("씬의 TMP_Text 폰트 일괄 교체", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "씬에 배치된 프리팹 인스턴스의 TMP도 함께 변경됩니다.\n" +
            "이 경우 씬에 오버라이드가 생기며, 프리팹 원본은 변경되지 않습니다.\n" +
            "프리팹 원본까지 바꾸려면 'Prefab Font Replacer'를 사용하세요.",
            MessageType.Info);

        EditorGUILayout.Space();

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "교체할 폰트", targetFont, typeof(TMP_FontAsset), false);

        includeInactive = EditorGUILayout.Toggle("비활성 오브젝트 포함", includeInactive);
        replaceMaterialPreset = EditorGUILayout.Toggle(
            "머티리얼 프리셋도 초기화", replaceMaterialPreset);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(targetFont == null))
        {
            if (GUILayout.Button("씬에서 TMP_Text 찾기", GUILayout.Height(28)))
            {
                FindAllTMPTextsInScene();
            }

            if (GUILayout.Button($"폰트 일괄 교체 ({foundTexts.Count}개)", GUILayout.Height(32)))
            {
                ReplaceFonts();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"검색된 TMP_Text: {foundTexts.Count}개  (프리팹 인스턴스: {prefabInstanceCount}개)",
            EditorStyles.miniBoldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
        foreach (var t in foundTexts)
        {
            if (t == null) continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(t);
                string prefix = isPrefabInstance ? "[Prefab] " : "";
                EditorGUILayout.ObjectField(prefix + t.gameObject.name, t, typeof(TMP_Text), true);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void FindAllTMPTextsInScene()
    {
        foundTexts.Clear();
        prefabInstanceCount = 0;

        // TMP_Text 는 TextMeshPro(3D) 와 TextMeshProUGUI(UGUI) 모두의 부모 클래스
        // FindObjectsOfTypeAll: 비활성 오브젝트, 프리팹 인스턴스 모두 포함
        var all = Resources.FindObjectsOfTypeAll<TMP_Text>()
            .Where(t => t != null
                        && t.gameObject.scene.IsValid()                  // 씬에 속한 오브젝트만 (프리팹 에셋 자체 제외)
                        && (t.hideFlags & HideFlags.NotEditable) == 0    // 에디터 임시 오브젝트 제외
                        && (t.hideFlags & HideFlags.HideAndDontSave) == 0
                        && (includeInactive || t.gameObject.activeInHierarchy))
            .ToList();

        foundTexts = all;
        prefabInstanceCount = foundTexts.Count(t => PrefabUtility.IsPartOfPrefabInstance(t));

        Debug.Log($"[TMPFontReplacer] {foundTexts.Count}개의 TMP_Text를 찾았습니다. " +
                  $"(프리팹 인스턴스: {prefabInstanceCount}개)");
    }

    private void ReplaceFonts()
    {
        if (targetFont == null)
        {
            EditorUtility.DisplayDialog("알림", "교체할 폰트를 지정해주세요.", "확인");
            return;
        }

        if (foundTexts.Count == 0)
        {
            FindAllTMPTextsInScene();
            if (foundTexts.Count == 0) return;
        }

        int changed = 0;
        int prefabOverride = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace TMP Fonts");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var text in foundTexts)
        {
            if (text == null) continue;

            Undo.RecordObject(text, "Replace TMP Font");

            text.font = targetFont;

            if (replaceMaterialPreset)
            {
                // 폰트 에셋의 기본 머티리얼로 초기화
                text.fontSharedMaterial = targetFont.material;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(text))
            {
                prefabOverride++;
                // 프리팹 인스턴스의 경우 명시적으로 modification을 기록해야 씬 저장 시 오버라이드로 보존됨
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }

            EditorUtility.SetDirty(text);
            changed++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        // 씬을 더티 처리하여 저장 가능 상태로
        if (changed > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        Debug.Log($"[TMPFontReplacer] {changed}개의 TMP_Text 폰트를 '{targetFont.name}'(으)로 교체했습니다. " +
                  $"(프리팹 오버라이드 발생: {prefabOverride}개)");
        EditorUtility.DisplayDialog("완료",
            $"{changed}개 교체 완료!\n프리팹 오버라이드: {prefabOverride}개",
            "확인");
    }
}