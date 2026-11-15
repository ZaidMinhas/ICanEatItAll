using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuCanvas : MonoBehaviour
{

    [SerializeField] private string scenePath;

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
