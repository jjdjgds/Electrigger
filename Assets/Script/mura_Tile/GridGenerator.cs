using System.Collections.Generic;
using UnityEngine;
public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public TilescriptObj tilescriptObj;
    public Vector2 CenterPoint;

    private List<GameObject> spawnedTiles = new List<GameObject>();
    public float scaleMultiplier = 1.2f;
    void Start()
    {
        float tileWidth = tilePrefab.transform.localScale.x * scaleMultiplier;
        float tileHeight = tilePrefab.transform.localScale.y * scaleMultiplier;

        float offsetX = (TilescriptObj.width - 1) / 2f * tileWidth;
        float offsetY = (TilescriptObj.height - 1) / 2f * tileHeight;

        for (int y = 0; y < TilescriptObj.height; y++)
        {
            for (int x = 0; x < TilescriptObj.width; x++)
            {
                int index = y * TilescriptObj.width + x;
                if (tilescriptObj.tiles[index])
                {
                    Vector3 position = new Vector3(
                        CenterPoint.x + x * tileWidth - offsetX,
                        CenterPoint.y - y * tileHeight + offsetY,
                        0);

                    var tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);

                    tile.transform.localScale = tilePrefab.transform.localScale * scaleMultiplier;

                    spawnedTiles.Add(tile);
                }
            }
        }
    }

    // 一番近いタイルを返す
    public GameObject GetNearestTile(Vector3 worldPos)
    {
        GameObject nearest = null;
        float minDist = float.MaxValue;
        foreach (var tile in spawnedTiles)
        {
            float dist = Vector3.Distance(worldPos, tile.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = tile;
            }
        }
        return nearest;
    }

    // 盤面の範囲内かどうか
    public bool IsInsideGrid(Vector3 worldPos)
    {
        float tileWidth = tilePrefab.transform.localScale.x;
        float tileHeight = tilePrefab.transform.localScale.y;
        foreach (var tile in spawnedTiles)
        {
            float distX = Mathf.Abs(worldPos.x - tile.transform.position.x);
            float distY = Mathf.Abs(worldPos.y - tile.transform.position.y);
            // タイル1枚分の範囲内なら盤面内
            if (distX < tileWidth && distY < tileHeight)
            {
                Debug.Log($"盤面内タイル: {tile.name} pos:{tile.transform.position}");
                return true;
            }
        }
        Debug.Log($"盤面外 worldPos:{worldPos}");
        return false;
    }

}