#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

// Tự động load MainMenu trước khi Play trong Editor
[InitializeOnLoad]
public static class StartFromMainMenu
{
    const string MainMenuScene = "Assets/Scenes/MainMenu.unity";

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

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            if (EditorSceneManager.GetActiveScene().path != MainMenuScene)
            {
                EditorSceneManager.LoadScene(MainMenuScene);
            }
        }
    }
}
#endif
