using System;
using UnityEngine;

[RequireComponent(typeof(Outline))]
[DefaultExecutionOrder(100)] // Initialize after Outline has created its materials in Awake.
public class EdibleItem : MonoBehaviour
{
    public bool destroyParents = false;
    public bool usesOutline = true;
    public float requirement = 0;
    public float nourishment = 1;

    public float growthRate = 0.075f;

    public Outline myOutline;
    public bool IsConsumed { get; private set; }

    private Material mat;

    void Awake()
    {
        ToggleOutline(false);
    }

    void OnDisable() => ToggleOutline(false);

    public bool CanBeEatenBy(HungerMeter meter)
    {
        return isActiveAndEnabled && !IsConsumed && meter != null && meter.isActiveAndEnabled
            && meter.CurrentState != HungerState.Death && meter.Level > 0 && meter.Level >= requirement
            && !float.IsNaN(nourishment) && !float.IsInfinity(nourishment) && nourishment > 0
            && !float.IsNaN(growthRate) && !float.IsInfinity(growthRate) && growthRate > 0;
    }

    internal void MarkConsumed()
    {
        IsConsumed = true;
        ToggleOutline(false);
    }

    internal void ToggleOutline(bool outlineOn)
    {
        if (!usesOutline)
        {
            ToggleGlow(outlineOn);
            return;
        }
        if (myOutline == null)
        {
            myOutline = GetComponent<Outline>();
        }
        if (myOutline == null)
        {
            return;
        }

        myOutline.OutlineMode = Outline.Mode.OutlineVisible;
        myOutline.OutlineWidth = 10;
        myOutline.enabled = outlineOn;
    }

    void ToggleGlow(bool glowOn)
    {

        if (!mat)
        {
            mat = GetComponent<Renderer>().material;
        }

        Color color = new Color(0, 0, 0, 0);
        if (glowOn)
        {
            color = new Color(1, 1, 1, 1);
        }
        mat.SetColor("_CubemapColor", color);
    }

    internal void ChangeOutlineColor(Color newColor)
    {
        if (myOutline == null)
        {
            myOutline = GetComponent<Outline>();
        }
        if (myOutline != null)
        {
            myOutline.OutlineColor = newColor;
        }
    }

    public void OnDestroy()
    {
        if (destroyParents && transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
    }
}
