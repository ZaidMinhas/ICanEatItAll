using System;
using UnityEngine;

public enum HungerState { None, Rest, Growing, Decaying, Death }

public class HungerMeter : MonoBehaviour
{
    [SerializeField, Min(0)] protected float level = 1;
    [SerializeField] protected HungerState currState = HungerState.None;

    public float Level
    {
        get => level;
        set
        {
            if (!IsFinite(value) || currState == HungerState.Death) return;
            float nextLevel = Mathf.Max(0, value);
            if (level == nextLevel) return;
            level = nextLevel;
            OnValueChange?.Invoke(level);
        }
    }

    public float TargetLevel
    {
        get
        {
            return targetLevel;
        }
    }

    public HungerState CurrentState => currState;
    protected int IntLevel => Mathf.Max(1, Mathf.CeilToInt(level));

    [SerializeField] protected bool isDebug;
    public event Action<HungerState> OnStateChange;
    public event Action<float> OnValueChange;

    [Header("Editor Variables")]
    [SerializeField, Min(0)] protected float REST_DURATION_SCALE = 10;
    [SerializeField, Min(0.0001f)] protected float DECAY_RATE = 0.1f;

    [Header("Runtime Stats")]
    [SerializeField] protected float restDuration;
    [SerializeField] protected float restTimeout;
    // Rates are measured in level units per second.
    [SerializeField] protected float decayRate;
    [SerializeField] protected float growthRate;
    [SerializeField] protected float targetLevel;

    protected void Awake()
    {
        level = IsFinite(level) ? Mathf.Max(0, level) : 1;
        targetLevel = level;
        growthRate = 0;
        currState = HungerState.None;
        setState(level > 0 ? HungerState.Rest : HungerState.Death);
    }

    public void Eat(EdibleItem item)
    {
        TryEat(item);
    }

    public bool TryEat(EdibleItem item)
    {
        if (item == null || !item.CanBeEatenBy(this)) return false;
        if (!IsFinite(item.nourishment) || item.nourishment <= 0 ||
            !IsFinite(item.growthRate) || item.growthRate <= 0) return false;

        bool alreadyGrowing = currState == HungerState.Growing;
        float nextTarget = (alreadyGrowing ? Mathf.Max(targetLevel, level) : level)
            + item.nourishment;
        if (!IsFinite(nextTarget)) return false;

        item.MarkConsumed();
        targetLevel = nextTarget;
        growthRate = alreadyGrowing ? Mathf.Max(growthRate, item.growthRate) : item.growthRate;
        setState(HungerState.Growing);
        GameSounds.Play("Grow");
        Debug.Log("Eating " + item.name);
        return true;
    }

    protected void setState(HungerState state)
    {
        if (state == currState || currState == HungerState.Death) return;
        currState = state;
        EnterState();
        OnStateChange?.Invoke(currState);
    }

    protected void FixedUpdate()
    {
        UpdateState();
        if (isDebug)
            Debug.Log(currState + " " + level + " " + Time.fixedDeltaTime);
    }

    protected void EnterState()
    {
        switch (currState)
        {
            case HungerState.Death:
                growthRate = 0;
                decayRate = 0;
                targetLevel = 0;
                GameState gameState = GetComponent<GameState>();
                if (gameState != null) gameState.Lose();
                break;
            case HungerState.Rest:
                growthRate = 0;
                targetLevel = level;
                restDuration = IsFinite(REST_DURATION_SCALE)
                    ? Mathf.Max(0, REST_DURATION_SCALE) / IntLevel : 0;
                restTimeout = Time.time + restDuration;
                break;
        }
    }

    protected void UpdateState()
    {
        if (currState != HungerState.Death && level <= 0)
        {
            setState(HungerState.Death);
            return;
        }

        switch (currState)
        {
            case HungerState.Rest:
                if (Time.time >= restTimeout) setState(HungerState.Decaying);
                break;
            case HungerState.Growing:
                Level = Mathf.MoveTowards(level, targetLevel, growthRate * Time.fixedDeltaTime);
                if (currState == HungerState.Growing && level >= targetLevel)
                    setState(HungerState.Rest);
                break;
            case HungerState.Decaying:
                decayRate = IntLevel * (IsFinite(DECAY_RATE)
                    ? Mathf.Max(0.0001f, DECAY_RATE) : 0.1f);
                Level = Mathf.MoveTowards(level, 0, decayRate * Time.fixedDeltaTime);
                if (level <= 0) setState(HungerState.Death);
                break;
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public int GetIntLevel()
    {
        return IntLevel;
    }
}
