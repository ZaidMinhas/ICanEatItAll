using ICanEatItAll.Audio;
using UnityEngine;

public static class GameSounds
{
    // Sound assets live in Assets/Resources/Sounds so scenes need no audio wiring.
    public static void Play(string name, Vector3 position = default)
    {
        SoundDefinition sound = Resources.Load<SoundDefinition>("Sounds/" + name);
        if (sound != null) AudioManager.Instance?.Play(sound, position);
    }

    public static void PlayResult(GameStatus state)
    {
        if (state == GameStatus.Won) Play("Win");
        if (state == GameStatus.Lost) Play("Lose");
    }
}
