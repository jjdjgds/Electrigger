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
    public AttachType attachType;
    public PlugColor plugColor;
    public Vector2 spawnOffset;  // ← GameObjectの代わりにオフセット値
    public AngleType angleType;
    public bool isEnabled;
}

public class PlugSocketInitializer : MonoBehaviour
{
    [SerializeField] public GameObject plugPrefab;
    [SerializeField] public GameObject socketPrefab;
    [SerializeField] private PlugSocketConfig[] socketConfigs = new PlugSocketConfig[4];
    Vector3 GetSpawnWorldPos(Vector2 offset, AngleType angleType)
    {
        // 向きに応じてスケールの半分を使う
        switch (angleType)
        {
            case AngleType.Up:
                return transform.position + new Vector3(offset.x, (transform.localScale.y / 2f) + offset.y, 0);
            case AngleType.Down:
                return transform.position + new Vector3(offset.x, -(transform.localScale.y / 2f) - offset.y, 0);
            case AngleType.Right:
                return transform.position + new Vector3((transform.localScale.x / 2f) + offset.x, offset.y, 0);
            case AngleType.Left:
                return transform.position + new Vector3(-(transform.localScale.x / 2f) - offset.x, offset.y, 0);
            default:
                return transform.position;
        }
    }


    void Start()
    {
        if (plugPrefab == null || socketPrefab == null) return;

        foreach (PlugSocketConfig config in socketConfigs)
        {
            if (!config.isEnabled) continue;

            Quaternion rotation = Quaternion.identity;
            switch (config.angleType)
            {
                case AngleType.Up: rotation = Quaternion.Euler(0, 0, 0); break;
                case AngleType.Right: rotation = Quaternion.Euler(0, 0, -90); break;
                case AngleType.Down: rotation = Quaternion.Euler(0, 0, 180); break;
                case AngleType.Left: rotation = Quaternion.Euler(0, 0, 90); break;
            }

            //向きに応じたスケール+オフセットで位置計算
            Vector3 spawnWorldPos = GetSpawnWorldPos(config.spawnOffset, config.angleType);

            if (config.attachType == AttachType.Socket)
            {
                GameObject obj = Instantiate(socketPrefab, spawnWorldPos, rotation);
                obj.transform.SetParent(this.transform, true); // ← worldPositionStays=true

                PowerNode socketNode = obj.GetComponent<PowerNode>();
                PowerNode ownerNode = GetComponent<PowerNode>();
                if (socketNode != null && ownerNode != null)
                    socketNode.owner = ownerNode;

                plugColor plugColorComponent = obj.GetComponent<plugColor>();
                if (plugColorComponent != null)
                    plugColorComponent.SetColor(config.plugColor);
            }
            else if (config.attachType == AttachType.Plug)
            {
                GameObject obj = Instantiate(plugPrefab, spawnWorldPos, rotation);
                obj.transform.SetParent(this.transform, true); // ← worldPositionStays=true

                PowerNode plugNode = obj.GetComponent<PowerNode>();
                PowerNode ownerNode = GetComponent<PowerNode>();
                if (plugNode != null && ownerNode != null)
                    plugNode.owner = ownerNode;

                plugColor plugColorComponent = obj.GetComponent<plugColor>();
                if (plugColorComponent != null)
                    plugColorComponent.SetColor(config.plugColor);
            }
        }
    }
}