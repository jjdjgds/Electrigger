using UnityEngine;



public class GridGenerator : MonoBehaviour 
{
    public GameObject tilePrefab;
    public TilescriptObj tilescriptObj;
    public Vector2 CenterPoint ;


    void Start()
    {
        float offsetX = (TilescriptObj.width - 1) / 2f;
        float offsetY = (TilescriptObj.height - 1) / 2f;

        for (int y = 0; y < TilescriptObj.height; y++)
        {
            for (int x = 0; x < TilescriptObj.width; x++)
            {
                int index = y * TilescriptObj.width + x;

                if (tilescriptObj.tiles[index])
                {
                    Vector3 position = new Vector3(
                        CenterPoint.x + x - offsetX,
                        CenterPoint.y - y + offsetY,
                        0);

                    Instantiate(tilePrefab, position, Quaternion.identity, transform);
                }
            }
        }
    }


}
