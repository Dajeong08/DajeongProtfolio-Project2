using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
// 편집기에서 도감 배치와 닫기 버튼 보정
public static class CharacterBookLayoutFix
{
    private const string Key = "CharacterBookLayoutFix.CompactMythics.v4"; // 신화 배치 중복 보정 확인용 키
    // 편집기 보정 함수와 이벤트 연결
    static CharacterBookLayoutFix()
    {
        // +=는 편집기 이벤트에 실행할 함수 등록
        EditorApplication.delayCall += ApplyOnce;
        EditorApplication.delayCall += ApplyAllCloseButtons;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    // 씬을 열면 보정 여부 확인
    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) { ApplyOnce(); ApplyAllCloseButtons(); }
    // 플레이 종료 후 보정 여부 확인
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) { ApplyOnce(); ApplyAllCloseButtons(); }
    }
    // 아직 보정하지 않았으면 실행
    private static void ApplyOnce()
    {
        if (!EditorPrefs.GetBool(Key + Application.dataPath, false)) Apply();
    }
    [MenuItem("Tools/Character Book/Apply Layout Fix")]
    // Archer 디자인을 신화 상세 창에 적용
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (CharacterBookUI book in Object.FindObjectsByType<CharacterBookUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (book.gameObject.scene.path != "Assets/Scenes/main.unity") continue;
            Transform root = book.BookPanel.transform;
            Transform template = root.Find("Detail_Archer/Window");
            if (template == null) continue;
            // 도감 전체 변경을 실행 취소할 수 있게 기록
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Copy custom book design");
            foreach (Transform detail in root)
            {
                if (!detail.name.StartsWith("Detail_") || detail == template.parent) continue;
                if (detail.name != "Detail_Angel" && detail.name != "Detail_Demon" && detail.name != "Detail_Werewolf") continue;
                Transform old = detail.Find("Window");
                string[] names = { "Name", "Grade", "AttackDamage", "AttackSpeed" };
                Dictionary<string, string> values = new Dictionary<string, string>();
                foreach (string name in names) values[name] = Find(old, name).GetComponent<TMP_Text>().text;
                Color grade = Find(old, "Grade").GetComponent<TMP_Text>().color;
                Sprite sprite = Find(old, "CharacterImage").GetComponent<Image>().sprite;
                List<Transform> extras = new List<Transform>();
                foreach (string name in new[] { "SkillTitle", "SkillDescription", "RecipeTitle" })
                {
                    Transform content = Find(old, name);
                    if (content != null) extras.Add(content);
                }
                foreach (Transform child in old)
                    if (child.name.StartsWith("Material_")) extras.Add(child);
                GameObject clone = Object.Instantiate(template.gameObject, detail, false);
                clone.name = "Window";
                clone.SetActive(old.gameObject.activeSelf);
                Undo.RegisterCreatedObjectUndo(clone, "Create detail window");
                Transform window = clone.transform;
                foreach (string name in names) Find(window, name).GetComponent<TMP_Text>().text = values[name];
                Find(window, "Grade").GetComponent<TMP_Text>().color = grade;
                Find(window, "CharacterImage").GetComponent<Image>().sprite = sprite;
                TMP_Text title = Find(window, "Name").GetComponent<TMP_Text>();
                RectTransform titleRect = title.rectTransform;
                titleRect.anchorMin = Vector2.zero; titleRect.anchorMax = Vector2.one;
                titleRect.pivot = new Vector2(.5f, .5f); titleRect.anchoredPosition = Vector2.zero;
                titleRect.sizeDelta = new Vector2(-44, 0);
                title.horizontalAlignment = HorizontalAlignmentOptions.Center;
                title.enableAutoSizing = true; title.fontSizeMin = 24; title.fontSizeMax = 42;
                if (extras.Count > 0) Extend(window, template.GetComponent<RectTransform>().rect.height, extras);
                // 새 창에 정보와 재료를 옮긴 뒤 이전 창 제거
                Undo.DestroyObjectImmediate(old.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
            SceneView.RepaintAll();
            EditorPrefs.SetBool(Key + Application.dataPath, true);
            Debug.Log("Custom book design applied. Save the scene to keep the changes.");
        }
    }
    // 비활성 자식도 포함해 이름으로 찾기
    private static Transform Find(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }
    // 신화 스킬과 작은 재료 이미지를 배치
    private static void Extend(Transform window, float oldHeight, List<Transform> extras)
    {
        foreach (Transform child in window)
        {
            RectTransform item = child.GetComponent<RectTransform>();
            if (item.anchorMin == new Vector2(.5f, .5f) && item.anchorMax == item.anchorMin)
            {
                item.anchorMin = item.anchorMax = new Vector2(.5f, 1);
                item.anchoredPosition -= new Vector2(0, oldHeight / 2);
            }
        }
        RectTransform rect = window.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1100, 750);
        // 신화 창은 일반 창보다 위로 배치
        rect.anchoredPosition = new Vector2(0, 20);
        RectTransform stats = Find(window, "AttackDamage").parent.GetComponent<RectTransform>();
        SetTop(stats, new Vector2(420, -216), new Vector2(610, 360));
        SetTop(Find(window, "AttackDamage").GetComponent<RectTransform>(), new Vector2(24, -22), new Vector2(562, 48));
        SetTop(Find(window, "AttackSpeed").GetComponent<RectTransform>(), new Vector2(24, -78), new Vector2(562, 42));
        Image background = stats.GetComponentInChildren<Image>(true);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero; backgroundRect.anchorMax = Vector2.one;
        backgroundRect.pivot = new Vector2(.5f, .5f); backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = Vector2.zero;
        backgroundRect.SetAsFirstSibling();
        int materialIndex = 0;
        foreach (Transform extra in extras)
        {
            Undo.SetTransformParent(extra, window, "Keep mythic information");
            RectTransform item = extra.GetComponent<RectTransform>();
            if (extra.name.StartsWith("Material_"))
            {
                SetTop(item, new Vector2(48 + materialIndex * 120, -646), new Vector2(96, 80));
                RectTransform image = Find(extra, "CharacterImage").GetComponent<RectTransform>();
                image.anchoredPosition = new Vector2(0, 42); image.sizeDelta = new Vector2(160, 160);
                Transform label = Find(extra, "Name");
                if (label != null) Undo.DestroyObjectImmediate(label.gameObject);
                materialIndex++;
                continue;
            }
            item.anchorMin = item.anchorMax = new Vector2(0, 1); item.pivot = new Vector2(0, 1);
            TMP_Text text = extra.GetComponent<TMP_Text>();
            text.color = Find(window, "Name").GetComponent<TMP_Text>().color;
            if (extra.name == "SkillTitle")
            {
                Undo.SetTransformParent(extra, stats, "Place skill inside stats box");
                SetTop(item, new Vector2(24, -143), new Vector2(562, 32)); text.fontSize = 24;
                text.verticalAlignment = VerticalAlignmentOptions.Top;
            }
            else if (extra.name == "SkillDescription")
            {
                Undo.SetTransformParent(extra, stats, "Place skill inside stats box");
                SetTop(item, new Vector2(24, -183), new Vector2(562, 145)); text.fontSize = 22;
                text.enableAutoSizing = true; text.fontSizeMin = 20; text.fontSizeMax = 22;
                text.verticalAlignment = VerticalAlignmentOptions.Top;
                text.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                SetTop(item, new Vector2(48, -600), new Vector2(990, 32)); text.fontSize = 26;
            }
        }
    }

    // 왼쪽 위 기준으로 UI 위치와 크기 설정
    private static void SetTop(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    [MenuItem("Tools/UI/Apply Close Button Style")]
    // 닫기 버튼 모양 통일, 기존 클릭 기능 유지
    private static void ApplyAllCloseButtons()
    {
        string key = "AllCloseButtons.Style.v1" + Application.dataPath;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(key, false)) return;
        foreach (CharacterBookUI book in Object.FindObjectsByType<CharacterBookUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (book.gameObject.scene.path != "Assets/Scenes/main.unity") continue;
            Transform source = book.BookPanel.transform.Find("CharacterListWindow/Button_CloseBook");
            Image sourceIcon = null;
            foreach (Transform child in source)
                if (child.GetComponent<Image>() != null) { sourceIcon = child.GetComponent<Image>(); break; }
            if (sourceIcon == null) continue;
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.gameObject.scene != book.gameObject.scene || button.transform == source) continue;
                string name = button.name.ToLowerInvariant();
                bool close = name.Contains("close") || name.Contains("back") || name.Contains("exit") || name.Contains("cancel");
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    string method = button.onClick.GetPersistentMethodName(i).ToLowerInvariant();
                    close |= method.Contains("close") || method.Contains("back") || method.Contains("exit") || method.Contains("cancel");
                }
                if (!close) continue;
                RectTransform rect = button.GetComponent<RectTransform>();
                // 화면 전체를 누르는 닫기 배경은 작은 X 버튼으로 바꾸지 않음
                if (rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one && rect.sizeDelta == Vector2.zero) continue;
                // 도감 전체 변경을 실행 취소할 수 있게 기록
                Undo.RegisterFullObjectHierarchyUndo(button.gameObject, "Match close button style");
                rect.sizeDelta = new Vector2(70, 70);
                CopyCloseImage(source.GetComponent<Image>(), button.GetComponent<Image>());
                button.colors = source.GetComponent<Button>().colors;
                button.targetGraphic = button.GetComponent<Image>();
                Transform icon = null;
                foreach (Transform child in button.transform)
                {
                    Image image = child.GetComponent<Image>();
                    if (icon == null && image != null && image.sprite == sourceIcon.sprite) icon = child;
                    else child.gameObject.SetActive(false);
                }
                if (icon == null)
                {
                    GameObject clone = Object.Instantiate(sourceIcon.gameObject, button.transform, false);
                    clone.name = "CloseIcon";
                    Undo.RegisterCreatedObjectUndo(clone, "Create close icon");
                    icon = clone.transform;
                }
                icon.gameObject.SetActive(true);
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one;
                iconRect.pivot = new Vector2(.5f, .5f); iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = Vector2.zero;
                CopyCloseImage(sourceIcon, icon.GetComponent<Image>());
                icon.GetComponent<Image>().raycastTarget = false;
            }
            EditorSceneManager.MarkSceneDirty(book.gameObject.scene);
            EditorPrefs.SetBool(key, true);
            SceneView.RepaintAll();
        }
    }

    // 닫기 버튼의 이미지 설정 복사
    private static void CopyCloseImage(Image source, Image target)
    {
        target.sprite = source.sprite;
        target.color = source.color;
        target.type = source.type;
        target.preserveAspect = source.preserveAspect;
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
    }
}
