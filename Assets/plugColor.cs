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

    [SerializeField] private List<ColorEntry> colorPalette = new List<ColorEntry>();



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void ApplyColor()
    {
        Color targetColor = GetColor(plugcolor);
        Debug.Log($"適用する色: {targetColor}, plugcolor: {plugcolor}");  // 追加

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"見つかったSpriteRenderer数: {renderers.Length}");  // 追加

        foreach (SpriteRenderer sr in renderers)
        {
            sr.color = targetColor;
            Debug.Log($"{sr.gameObject.name} に色を適用");  // 追加
        }
    }
    Color GetColor(PlugColor type)
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
