using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using ICanEatItAll.Audio;

public class MainMenuCanvas : MonoBehaviour
{

    [SerializeField] private string scenePath;
    private int menuMusicHandle;

    void Start()
    {
        SoundDefinition music = Resources.Load<SoundDefinition>("Sounds/MenuMusic");
        if (music != null) menuMusicHandle = AudioManager.Instance.Play(music);
    }

    void OnDestroy()
    {
        // Stop only this playback, so gameplay music is not stopped during a scene change.
        if (menuMusicHandle != 0) AudioManager.Instance?.Stop(menuMusicHandle);
    }

    public void OnPlay()
    {
        SceneManager.LoadScene(scenePath);
        // LoadSceneAsync(scenePath);
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    //     IEnumerator LoadSceneAsync(string sceneName)
    // {
    //     AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
    //     while (!asyncLoad.isDone)
    //     {
    //         yield return null;
    //     }
    // }
}
