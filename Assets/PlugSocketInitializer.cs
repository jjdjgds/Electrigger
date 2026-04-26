using JetBrains.Annotations;
using UnityEngine;

[System.Serializable]
public enum AttachType
{
    Socket,
    Plug
}
[System.Serializable]
public enum AngleType
{
    Up,
    Right,
    Down,
    Left

}




[System.Serializable]
public class PlugSocketConfig
{
    public AttachType attachType;         // ソケットかプラグか
    public PlugColor plugColor;       // 色
    public GameObject spawnPosition;  // 場所
    public AngleType angleType;       // 角度
    public bool isEnabled;
}



public class PlugSocketInitializer : MonoBehaviour
{
    [SerializeField] public GameObject plugPrefab;
    [SerializeField] public GameObject socketPrefab;
    [SerializeField] private PlugSocketConfig[] socketConfigs = new PlugSocketConfig[4];

    void Start()
    {
        if (plugPrefab == null)
        {
            Debug.LogWarning("plugPrefab is not assigned in the inspector.");
            return;
        }
        if (socketPrefab == null)
        {
            Debug.LogWarning("socketPrefab is not assigned in the inspector.");
            return;
        }

        foreach (PlugSocketConfig config in socketConfigs)
        {
            if (!config.isEnabled) continue;


            if (config.spawnPosition == null)
            {
                Debug.LogWarning("spawnPositionが未設定のConfigがあります");
                continue;
            }
            Quaternion rotation = Quaternion.identity;
            switch (config.angleType)
            {
                case AngleType.Up:
                    rotation = Quaternion.Euler(0, 0, 0);
                    break;
                case AngleType.Right:
                    rotation = Quaternion.Euler(0, 0, -90);
                    break;
                case AngleType.Down:
                    rotation = Quaternion.Euler(0, 0, 180);
                    break;
                case AngleType.Left:
                    rotation = Quaternion.Euler(0, 0, 90);
                    break;
            }


            if (config.attachType == AttachType.Socket)
            {
                GameObject obj = Instantiate(socketPrefab, config.spawnPosition.transform.position, rotation, this.transform);

                PowerNode socketNode = obj.GetComponent<PowerNode>();
                PowerNode ownerNode = GetComponent<PowerNode>();
                if (socketNode != null && ownerNode != null)
                    socketNode.owner = ownerNode;

                plugColor plugColorComponent = obj.GetComponent<plugColor>();
                if (plugColorComponent != null)
                    plugColorComponent.SetColor(config.plugColor);
            }
            else if (config.attachType==AttachType.Plug)
            {
                GameObject obj = Instantiate(plugPrefab, config.spawnPosition.transform.position, rotation, this.transform);
                PowerNode plugNode = obj.GetComponent<PowerNode>();
                PowerNode ownerNode = GetComponent<PowerNode>(); // 親のPowerNode
                if (plugNode != null && ownerNode != null)
                    plugNode.owner = ownerNode;
                plugColor plugColorComponent = obj.GetComponent<plugColor>();
                if (plugColorComponent != null)
                {
                    plugColorComponent.SetColor(config.plugColor);
                }
            }
           
            else
            {
                Debug.LogWarning("plugColorコンポーネントが見つかりません");
            }
        }
    }
}