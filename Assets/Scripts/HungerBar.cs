using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HungerBar : MonoBehaviour
{

    [SerializeField] protected HungerMeter meter;
    [SerializeField] protected Slider slider;
    [SerializeField] protected Image fillArea;
    protected float _maxLevel;

    public void Start()
    {
        meter.OnStateChange += OnHungerStateChange;
    }

    public void Update()
    {
        var maxLevel = Mathf.Max(meter.Level, meter.TargetLevel);
        if (maxLevel > _maxLevel)
        {
            _maxLevel = maxLevel;
        }
        slider.value = meter.Level / _maxLevel;
    }

    protected void OnHungerStateChange(HungerState state)
    {
        switch (state)
        {
            case HungerState.Decaying:
                fillArea.color = Color.red;
                return;
            case HungerState.Rest:
                fillArea.color = Color.green;
                return;
            case HungerState.Growing:
                fillArea.color = Color.yellow;
                return;
        }
    }
}
