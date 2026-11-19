#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

// Tự động load MainMenu trước khi Play trong Editor
[InitializeOnLoad]
public static class StartFromMainMenu
{
    const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    const string MenuPath = "Tools/Play bắt đầu từ MainMenu";

    // tắt đi để Play thẳng scene đang mở (test cutscene...)
    static bool Enabled
    {
        get => EditorPrefs.GetBool(MenuPath, true);
        set => EditorPrefs.SetBool(MenuPath, value);
    }

    [MenuItem(MenuPath)] static void Toggle() => Enabled = !Enabled;
    [MenuItem(MenuPath, true)] static bool ToggleCheck() { Menu.SetChecked(MenuPath, Enabled); return true; }

    static StartFromMainMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        if (state == PlayModeStateChange.EnteredPlayMode && Enabled)
        {
            if (EditorSceneManager.GetActiveScene().path != MainMenuScene)
            {
                EditorSceneManager.LoadScene(MainMenuScene);
            }
        }
    }
}
#endif
