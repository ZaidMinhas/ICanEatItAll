using System;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private HungerMeter hungerMeter;

    public float MIN_SCALE = 0.5f;
    public float DEFAULT_SCALE = 1.8f;
    public Transform player;

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
        hungerMeter = GetComponent<HungerMeter>();
        player.localScale = Vector3.one * DEFAULT_SCALE;
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
        player.localScale = Vector3.one * scale;
    }

    public HungerMeter GetHungerMeter()
    {
        return hungerMeter;
    }
}
