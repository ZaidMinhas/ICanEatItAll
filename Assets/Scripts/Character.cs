// using System;
// using StarterAssets;
using UnityEngine;
// using UnityEngine.Events;

public class Character : MonoBehaviour
{
    [SerializeField] private HungerMeter hungerMeter;

    public float MIN_SCALE = 0.5f;
    public float DEFAULT_SCALE = 1.8f;
    public Transform player;

    // public UnityEvent<GameStatus> stateChanged;
    // private GameState gameState;

    void Awake()
    {
        if (hungerMeter == null) hungerMeter = GetComponent<HungerMeter>();
        if (hungerMeter == null || player == null)
        {
            Debug.LogError("Character requires a HungerMeter and an assigned player Transform.", this);
            enabled = false;
        }
        
        // gameState = FindAnyObjectByType<GameState>();
        // stateChanged = gameState.onStateChanged;
        // stateChanged.AddListener(OnStateChange);
    }

    void OnEnable()
    {
        GameState gameState = GetComponent<GameState>();
        if (gameState != null) gameState.onStateChanged.AddListener(GameSounds.PlayResult);
    }

    void OnDisable()
    {
        GameState gameState = GetComponent<GameState>();
        if (gameState != null) gameState.onStateChanged.RemoveListener(GameSounds.PlayResult);
    }

    public void Start()
    {
        ChangeSize(hungerMeter.Level);
        GameSounds.Play("Music");
        GameSounds.Play("Ambience");
    }

    void Update()
    {
        ChangeSize(hungerMeter.Level);
    }

    public void ChangeSize(float level)
    {
        float scale = MIN_SCALE + (DEFAULT_SCALE - MIN_SCALE) * level;
        if (player != null) player.localScale = Vector3.one * scale;
    }

    // public void OnStateChange(GameStatus gameStatus)
    // {
    //     FirstPersonController firstPersonController = GetComponentInChildren<FirstPersonController>();
    //     if (gameStatus == GameStatus.Lost)
    //     {
    //         firstPersonController.canMove = false;
    //     }
    // }

    public HungerMeter GetHungerMeter()
    {
        return hungerMeter;
    }
}
