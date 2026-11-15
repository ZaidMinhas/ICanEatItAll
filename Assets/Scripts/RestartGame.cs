using ICanEatItAll.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    public void RestartScene()
    {
        // Close the persistent audio menu before resetting the scene.
        AudioSettingsPanel audioMenu = FindFirstObjectByType<AudioSettingsPanel>();
        if (audioMenu != null) audioMenu.Hide();
        AudioManager.Instance.StopAll();

        // A paused game must run normally after restarting.
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Scene scene = SceneManager.GetActiveScene();

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(scene.buildIndex);
#endif
    }
}
