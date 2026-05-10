using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class ColorEntry
{
    public ColorType colorType;
    public Color color;
}

public class plugColor : MonoBehaviour
{
    [SerializeField] private ColorType plugcolor;
    [SerializeField] private ColorPaletteSO colorPalette;  // SOÇéQè∆

    void Start()
    {
        ApplyColor();
    }
    public ColorType GetColorType()
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

    public void SetColor(ColorType newColor)
    {
        plugcolor = newColor;
        ApplyColor();
    }
}