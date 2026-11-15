using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerRestartAction : MonoBehaviour
{
    public HungerMeter hungerMeter;

    void Awake()
    {
        hungerMeter = GetComponentInParent<HungerMeter>();
    }

    void OnRestart(InputValue inputValue)
    {
        if (hungerMeter.CurrentState == HungerState.Death)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Time.timeScale = 1;
        }
    }
}
