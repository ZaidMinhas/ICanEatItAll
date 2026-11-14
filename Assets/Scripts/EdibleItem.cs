using System;
using UnityEngine;

public class EdibleItem : MonoBehaviour
{
    public int requirement = 0;
    public int nourishment = 1;

    public Outline myOutline;

    void Start()
    {
        myOutline = GetComponent<Outline>();
    }

    internal void ToggleOutline(bool outlineOn)
    {
        if (outlineOn)
        {
            myOutline.OutlineMode = Outline.Mode.OutlineVisible;
        }
        else
        {
            myOutline.OutlineMode = Outline.Mode.OutlineHidden;
        }
    }

    internal void ChangeOutlineColor(Color newColor)
    {
        myOutline.OutlineColor = newColor;
    }
}
