using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShoutText : MonoBehaviour
{
    [SerializeField] private HungerMeter hungerMeter;
    [SerializeField] private TMP_Text label;
    [SerializeField, Min(0.1f)] private float displayDuration = 1.5f;

    private readonly float[] levels = { 2f, 2.5f, 3f, 4f, 6f };
    private readonly string[] messages = {
        "EAT THE PAINTINGS", "EAT THE CHAIRS", "EAT THE TABLES",
        "EAT THE BAR", "EAT THE PEOPLE"
    };
    private readonly bool[] announced = new bool[5];
    private readonly Queue<string> pending = new Queue<string>();
    private float previousLevel;
    private Coroutine playback;
    private bool initialized;

    void Start()
    {
        if (hungerMeter == null) hungerMeter = FindAnyObjectByType<HungerMeter>();
        if (hungerMeter == null)
        {
            Debug.LogError("ShoutText requires a HungerMeter.", this);
            enabled = false;
            return;
        }
        if (label == null)
        {
            var existingText = GetComponentInChildren<TMP_Text>(true);
            var textObject = new GameObject("ShoutText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(transform, false);
            label = textObject.GetComponent<TextMeshProUGUI>();
            if (existingText != null) label.font = existingText.font;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0.3f);
            rect.anchorMax = new Vector2(0.92f, 0.7f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.red;
        label.enableAutoSizing = true;
        label.fontSizeMin = 24;
        label.fontSizeMax = 120;
        label.raycastTarget = false;
        label.text = "";
        previousLevel = hungerMeter.Level;
        for (int i = 0; i < levels.Length; i++)
            announced[i] = previousLevel >= levels[i];
        initialized = true;
        hungerMeter.OnValueChange += OnLevelChanged;
    }

    void OnEnable()
    {
        if (!initialized || hungerMeter == null) return;
        hungerMeter.OnValueChange += OnLevelChanged;
        OnLevelChanged(hungerMeter.Level);
    }

    void OnDisable()
    {
        if (hungerMeter != null) hungerMeter.OnValueChange -= OnLevelChanged;
        if (playback != null) StopCoroutine(playback);
        playback = null;
        pending.Clear();
        if (label != null)
        {
            label.text = "";
            label.rectTransform.localScale = Vector3.one;
        }
    }

    void OnLevelChanged(float level)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (!announced[i] && previousLevel < levels[i] && level >= levels[i])
            {
                announced[i] = true;
                pending.Enqueue(messages[i]);
            }
        }
        previousLevel = level;
        if (playback == null && pending.Count > 0)
            playback = StartCoroutine(PlayMessages());
    }

    IEnumerator PlayMessages()
    {
        while (pending.Count > 0)
        {
            label.text = pending.Dequeue();
            float duration = Mathf.Max(0.1f, displayDuration);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = elapsed / duration;
                label.alpha = 1f - Mathf.InverseLerp(0.75f, 1f, progress);
                label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, Mathf.Clamp01(elapsed / 0.15f));
                yield return null;
            }
            label.text = "";
            yield return new WaitForSecondsRealtime(0.15f);
        }
        label.rectTransform.localScale = Vector3.one;
        playback = null;
    }
}
