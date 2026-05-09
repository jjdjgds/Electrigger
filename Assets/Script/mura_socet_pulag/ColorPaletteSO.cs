using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Game/ColorPalette")]
public class ColorPaletteSO : ScriptableObject
{
    [SerializeField] private List<ColorEntry> colorPalette = new List<ColorEntry>();

    public Color GetColor(ColorType type)
    {
        foreach (ColorEntry entry in colorPalette)
        {
            if (entry.colorType == type)
                return entry.color;
        }
        Debug.LogWarning($"{type} がカラーパレットに登録されていません");
        return Color.white;
    }
}