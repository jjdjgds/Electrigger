using UnityEngine;

public class battery : MonoBehaviour
{
    [SerializeField] public GameObject plugPrefab;
    [SerializeField] private PlugColor plugColor;
    [SerializeField] private AngleType angleType;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField] private float spawnOffset = 0f;
    [SerializeField] public int maxCharge = 3;
    [SerializeField] private int _currentCharge;
    public int currentCharge => _currentCharge;
    public PlugColor GetPlugColor() => plugColor;

    void Start()
    {
        _currentCharge = maxCharge;
        Debug.Log($"[Battery] {_currentCharge}/{maxCharge}");
        // ✅ BatteryDisplayに枠のリサイズを先に行わせてからプラグ生成
        BatteryDisplay display = GetComponentInChildren<BatteryDisplay>();
        if (display != null)
            display.Initialize();

        Createplug();
    }

    public void SetCharge(int value)
    {
        _currentCharge = Mathf.Clamp(value, 0, maxCharge);
        Debug.Log($"[Battery] {_currentCharge}/{maxCharge}");
    }

    public void Createplug()
    {
        if (plugPrefab == null) return;
        if (frameRenderer == null)
        {
            Debug.LogWarning("frameRendererが設定されていません");
            return;
        }

        Quaternion rotation = Quaternion.identity;
        Vector3 spawnPos = Vector3.zero;

        Bounds bounds = frameRenderer.bounds;

        switch (angleType)
        {
            case AngleType.Up:
                rotation = Quaternion.Euler(0, 0, 0);
                spawnPos = new Vector3(bounds.center.x, bounds.max.y + spawnOffset, 0);
                break;
            case AngleType.Down:
                rotation = Quaternion.Euler(0, 0, 180);
                spawnPos = new Vector3(bounds.center.x, bounds.min.y - spawnOffset, 0);
                break;
            case AngleType.Right:
                rotation = Quaternion.Euler(0, 0, -90);
                spawnPos = new Vector3(bounds.max.x + spawnOffset, bounds.center.y, 0);
                break;
            case AngleType.Left:
                rotation = Quaternion.Euler(0, 0, 90);
                spawnPos = new Vector3(bounds.min.x - spawnOffset, bounds.center.y, 0);
                break;
        }

        GameObject obj = Instantiate(plugPrefab, spawnPos, rotation);
        obj.transform.SetParent(this.transform, true);

        PowerNode plugNode = obj.GetComponent<PowerNode>();
        if (plugNode != null)
        {
            plugNode.isBattery = true;
            plugNode.owner = null;
        }
        else
        {
            Debug.LogWarning("plugPrefabにPowerNodeがありません");
        }

        plugColor c = obj.GetComponent<plugColor>();
        if (c != null) c.SetColor(plugColor);
    }
}