using UnityEngine;
using UnityEngine.Events;

public enum GameStatus { Playing, Won, Lost }

public class GameState : MonoBehaviour
{
    [SerializeField] private GameStatus currentState = GameStatus.Playing;
    public GameStatus CurrentState => currentState;
    public UnityEvent<GameStatus> onStateChanged = new UnityEvent<GameStatus>();

    // Call these from your own gameplay code, buttons, or events.
    public void Win() { SetState(GameStatus.Won); }
    public void Lose() { SetState(GameStatus.Lost); }
    public void ResetState() { SetState(GameStatus.Playing); }

    public void SetState(GameStatus state)
    {
        if (currentState == state) return;
        currentState = state;
        onStateChanged.Invoke(state);
    }
}
