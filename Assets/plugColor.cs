using System.Collections.Generic;
using UnityEngine;

public enum PlugColor
{
    Red,
    Blue,
    Green,
    Yellow
}

[System.Serializable]
public class ColorEntry
{
    public PlugColor colorType;
    public Color color;
}

public class plugColor : MonoBehaviour
{
    [SerializeField] private PlugColor plugcolor;
    [SerializeField] private ColorPaletteSO colorPalette;  // SOÇéQè∆

    void Start()
    {
        ApplyColor();
    }
    public PlugColor GetPlugColor()
    {
        return plugcolor;
    }
    void ApplyColor()
    {
        if (colorPalette == null)
        {
            Debug.LogWarning("ColorPaletteÇ™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒ");
            return;
        }
        Color targetColor = colorPalette.GetColor(plugcolor);
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color = targetColor;
        }
    }

    public void SetColor(PlugColor newColor)
    {
        plugcolor = newColor;
        ApplyColor();
    }
}