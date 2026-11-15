using System;
using UnityEngine;

public enum HungerState { None, Rest, Growing, Decaying, Death }

public class HungerMeter : MonoBehaviour
{
    [SerializeField]protected float level = 0;
    [SerializeField] protected HungerState currState = HungerState.None;

    public float Level
    {
        get
        {
            return level;
        }
        set
        {
            level = value;
            OnValueChange?.Invoke(level);
        }
    }
    protected int IntLevel
    {
        get
        {
            if (level < 2)
            {
                return 1;
            }
            else
            {
                return (int)Mathf.Ceil(level);
            }
        }
    }


    [SerializeField] protected bool isDebug;

    public Action<HungerState> OnStateChange;
    public Action<float> OnValueChange;

    [Header("Editor Variables")]
    [SerializeField] protected float REST_DURATION_SCALE = 10;
    [SerializeField] protected float DECAY_RATE = 0.1f;

    [Header("Runtime Stats")]
    [SerializeField] protected float restDuration;
    [SerializeField] protected float restTimeout;
    [SerializeField] protected float decayRate;
    [SerializeField] protected float growthRate;

    [SerializeField] protected float targetLevel = 0;
    [SerializeField] protected EdibleItem heldItem;


    public void Eat(EdibleItem item)
    {
        heldItem = item;
        setState(HungerState.Growing);
    }

    protected void setState(HungerState state)
    {
        if (state == currState)
        {
            return;
        }
        currState = state;
        EnterState();
        OnStateChange?.Invoke(currState);
    }

    protected void FixedUpdate()
    {
        UpdateState();

        if (isDebug)
        {
            Debug.Log(currState + " " + level + " " + Time.fixedDeltaTime);
        }
    }

    protected void EnterState()
    {
        switch (currState)
        {
            case HungerState.Rest:
                if (IntLevel <= 0)
                {
                    Debug.LogError("The level cannot be below 0, is currently " + IntLevel + ".", this);
                    return;
                }
                restDuration = REST_DURATION_SCALE / IntLevel;
                restTimeout = Time.time + restDuration;
                if (isDebug)
                {
                    Debug.Log("State: " + HungerState.Rest + "Rest duration:" + restDuration + ", Rest Duration: " + restTimeout);
                }
                return;
            case HungerState.Growing:
                targetLevel += heldItem.nourishment;
                growthRate = Mathf.Max(growthRate, heldItem.growthRate * Time.fixedDeltaTime);
                if (isDebug)
                {
                    Debug.Log("State: " + HungerState.Growing + "Target Level:" + targetLevel + ", GrowthRate: " + growthRate);
                }
                return;
            case HungerState.Decaying:
                decayRate = IntLevel * DECAY_RATE * Time.fixedDeltaTime;
                if (isDebug)
                {
                    Debug.Log("State: " + HungerState.Decaying + "Decay Rate:" + decayRate);
                }
                return;
        }
    }

    protected void UpdateState()
    {
        switch (currState)
        {
            case HungerState.Rest:
                if (Time.time >= restTimeout)
                {
                    setState(HungerState.Decaying);
                }
                return;
            case HungerState.Growing:
                level += growthRate;
                if (level >= targetLevel)
                {
                    setState(HungerState.Rest);
                }
                return;
            case HungerState.Decaying:
                level -= decayRate;
                if (level <= 0)
                {
                    setState(HungerState.Death);
                }
                return;
        }
    }
}
