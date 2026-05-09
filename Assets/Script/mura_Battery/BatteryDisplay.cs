using UnityEngine;
using System;


public enum CellType
{
    Green,
    Red,
    Blue

}


[System.Serializable]
public class CellPrefabData 
{
    public CellType type;
    public GameObject prefab;

}



public class BatteryDisplay : MonoBehaviour
{
    [SerializeField] private battery targetBattery;

    [SerializeField] private CellPrefabData[] cellPrefabs;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField] private ColorPaletteSO colorPalette;
    [SerializeField] private float cellWidth = 0.2f;
    [SerializeField] private float cellHeight = 0.3f;
    [SerializeField] private float cellSpacing = 0.05f;
    [SerializeField] private float framePadding = 0.1f;
    [SerializeField] private Color inactiveColor = Color.gray;

    private GameObject[] cells;
    private int lastCharge = -1;
    private Color activeColor;

    //batteryのStartから明示的に呼ばれる
    public void Initialize()
    {
        if (colorPalette != null)
            activeColor = colorPalette.GetColor(targetBattery.GetPlugColor());
        else
            activeColor = Color.green;

        CreateCells();
        ResizeFrame();
        UpdateDisplay();
    }

    void Start()
    {
        // batteryからInitializeが呼ばれなかった場合の保険
        if (cells == null)
            Initialize();
    }

    void Update()
    {
        if (targetBattery.currentCharge != lastCharge)
            UpdateDisplay();
    }

    void CreateCells()
    {
        cells = new GameObject[targetBattery.maxCharge];
        float totalWidth = targetBattery.maxCharge * (cellWidth + cellSpacing) - cellSpacing;
        float startX = -totalWidth / 2f + cellWidth / 2f;

        for (int i = 0; i < targetBattery.maxCharge; i++)
        {
            //GameObject cell = Instantiate(cellPrefab, transform);
            //cell.transform.localPosition = new Vector3(startX + i * (cellWidth + cellSpacing), 0, 0);
            //cell.transform.localScale = new Vector3(cellWidth, cellHeight, 1);
            //cells[i] = cell;
        }
    }

    void ResizeFrame()
    {
        if (frameRenderer == null) return;
        float totalWidth = targetBattery.maxCharge * (cellWidth + cellSpacing) - cellSpacing;
        float frameWidth = totalWidth + framePadding * 2;
        float frameHeight = cellHeight + framePadding * 2;
        frameRenderer.transform.localScale = new Vector3(frameWidth, frameHeight, 1);
    }

    void UpdateDisplay()
    {
        lastCharge = targetBattery.currentCharge;
        for (int i = 0; i < cells.Length; i++)
        {
            SpriteRenderer sr = cells[i].GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = i < targetBattery.currentCharge ? activeColor : inactiveColor;
        }
    }
}