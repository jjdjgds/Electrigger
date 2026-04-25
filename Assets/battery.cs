using UnityEngine;
public class battery : MonoBehaviour
{
    [SerializeField] public GameObject plugPrefab;
    [SerializeField] private PlugColor plugColor;
    [SerializeField] private float angle;
    [SerializeField] private GameObject spawnPosition;

    void Start()
    {
        if (spawnPosition == null)
        {
            Debug.LogWarning("spawnPosition is not assigned in the inspector.");
            return;
        }
        if (plugPrefab != null)
        {
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            // 第4引数に親のTransformを渡す
            GameObject obj = Instantiate(plugPrefab, spawnPosition.transform.position, rotation, this.transform);

            plugColor plugColorComponent = obj.GetComponent<plugColor>();
            if (plugColorComponent != null)
            {
                plugColorComponent.SetColor(plugColor);
            }
            else
            {
                Debug.LogWarning("plugColorコンポーネントが見つかりません");
            }
        }
        else
        {
            Debug.LogWarning("plugPrefab is not assigned in the inspector.");
        }
    }
}