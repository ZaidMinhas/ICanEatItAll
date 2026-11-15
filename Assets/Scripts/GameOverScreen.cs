using System;
using UnityEngine;
using UnityEngine.Events;

public class GameOverScreen : MonoBehaviour
{
    public UnityEvent<GameStatus> stateChanged;
    private GameState gameState;
    void Start()
    {
        GetComponent<CanvasRenderer>().cull = true;
        foreach (CanvasRenderer renderer in GetComponentsInChildren<CanvasRenderer>())
        {
            renderer.cull = true;
        }
    }


    void Awake()
    {
        gameState = FindAnyObjectByType<GameState>();
        stateChanged = gameState.onStateChanged;
        stateChanged.AddListener(OnStateChange);
    }

    public void OnStateChange(GameStatus gameStatus)
    {
        if (gameStatus == GameStatus.Lost)
        {
            GetComponent<CanvasRenderer>().cull = false;
            foreach (CanvasRenderer renderer in GetComponentsInChildren<CanvasRenderer>())
            {
                renderer.cull = false;
            }
        }
    }
}
