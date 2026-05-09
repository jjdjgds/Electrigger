using UnityEngine;
using System;





[System.Serializable]
public class CellPrefabData 
{
    public ColorType type;
    public GameObject prefab;

}



public class BatteryDisplay : MonoBehaviour
{
    [SerializeField] private battery targetBattery;

    [SerializeField] private CellPrefabData[] cellPrefabs;
    [SerializeField] private GameObject emptyCellPrefab;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField] private ColorPaletteSO colorPalette;
    [SerializeField] private float cellWidth = 0.2f;
    [SerializeField] private float cellHeight = 0.3f;
    [SerializeField] private float cellSpacing = 0.05f;
    [SerializeField] private float framePadding = 0.1f;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Transform cellsRoot;
    private GameObject[] cells;
    private int lastCharge = -1;
    private Color activeColor;
    private bool initialized;
    SpriteRenderer[] renderers;

    //batteryのStartから明示的に呼ばれる
    public void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        renderers = new SpriteRenderer[targetBattery.maxCharge];
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

        float totalWidth =
            targetBattery.maxCharge * (cellWidth + cellSpacing) - cellSpacing;

        float startX =
            -totalWidth / 2f + cellWidth / 2f;

        GameObject prefab =
            GetCellPrefab(targetBattery.GetColorType());

        if (prefab == null)
        {
            Debug.LogError("CellPrefabが取得できません");
            return;
        }

        for (int i = 0; i < targetBattery.maxCharge; i++)
        {
            GameObject cell = Instantiate(prefab, cellsRoot);

            cell.transform.localPosition =
                new Vector3(startX + i * (cellWidth + cellSpacing), 0, 0);

            SpriteRenderer sr =
                cell.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                Vector2 spriteSize = sr.sprite.bounds.size;

                cell.transform.localScale = new Vector3(
                    cellWidth / spriteSize.x,
                    cellHeight / spriteSize.y,
                    1
                );
            }

            cells[i] = cell;
            renderers[i] = sr;
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

    //void UpdateDisplay()
    //{
    //    lastCharge = targetBattery.currentCharge;
    //    for (int i = 0; i < cells.Length; i++)
    //    {
    //        SpriteRenderer sr = cells[i].GetComponent<SpriteRenderer>();
    //        if (sr != null)
    //            sr.color = i < targetBattery.currentCharge ? activeColor : inactiveColor;
    //    }
    //}

    void UpdateDisplay()
    {
        lastCharge = targetBattery.currentCharge;

        GameObject activePrefab =
            GetCellPrefab(targetBattery.GetColorType());

        Sprite activeSprite =
            activePrefab.GetComponent<SpriteRenderer>().sprite;

        Sprite emptySprite =
            emptyCellPrefab.GetComponent<SpriteRenderer>().sprite;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            renderers[i].sprite =
                i < targetBattery.currentCharge
                ? activeSprite
                : emptySprite;
        }
    }

    private GameObject GetCellPrefab(ColorType type)
    {
        foreach (var data in cellPrefabs)
        {
            if (data.type == type)
            {
                return data.prefab;
            }
        }

        Debug.LogWarning($"{type} のPrefabが登録されていません");

        return null;
    }
}