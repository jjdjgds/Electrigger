using UnityEngine;
public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public TilescriptObj tilescriptObj;
    public Vector2 CenterPoint;

    void Start()
    {
        // プレハブのスケールを間隔に使う
        float tileWidth = tilePrefab.transform.localScale.x;
        float tileHeight = tilePrefab.transform.localScale.y;

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
                    Instantiate(tilePrefab, position, Quaternion.identity, transform);
                }
            }
        }
    }
}