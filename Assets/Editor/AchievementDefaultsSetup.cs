using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
// 편집기에서 도전과제 기본값 적용
public static class AchievementDefaultsSetup
{
    private const string Key = "AchievementDefaults.TenDoubleRewards.v1"; // 기본 설정 중복 적용 확인용 키

    // 편집기 시작, 씬 열기, 플레이 종료 이벤트 등록
    static AchievementDefaultsSetup()
    {
        // +=는 이벤트에 함수 등록, 해당 시점에 자동 호출
        EditorApplication.delayCall += ApplyOnce;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    // 씬을 열면 기본 설정 확인
    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        ApplyOnce();
    }

    // 편집 모드로 돌아오면 기본 설정 확인
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            ApplyOnce();
    }

    // 도전과제 기본 목록을 프로젝트에 한 번 적용
    private static void ApplyOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || EditorPrefs.GetBool(Key + Application.dataPath, false))
            return;

        foreach (AchievementManager manager in Object.FindObjectsByType<AchievementManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (manager.gameObject.scene.path != "Assets/Scenes/main.unity")
                continue;
            // 편집기 변경을 실행 취소할 수 있게 기록
            Undo.RecordObject(manager, "Set ten achievement rewards");
            manager.Achievements = AchievementManager.CreateDefaultAchievements();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorPrefs.SetBool(Key + Application.dataPath, true);
        }
    }
}
