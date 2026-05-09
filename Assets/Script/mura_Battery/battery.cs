using System.Collections;
using UnityEngine;

public class battery : MonoBehaviour
{
    [SerializeField] public GameObject plugPrefab;
    [SerializeField] private ColorType plugColor;
    [SerializeField] private AngleType angleType;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField] private float spawnOffset = 0f;
    [SerializeField] public int maxCharge = 3;
    [SerializeField] private int _currentCharge;
    public int currentCharge => _currentCharge;
    public ColorType GetColorType() => plugColor;


    private plugCollision[] plugCollisions;
    private socketCollision[] socketCollisions;
    void Start()
    {
        _currentCharge = maxCharge;
        //Debug.Log($"[Battery] {_currentCharge}/{maxCharge}");
        //BatteryDisplayに枠のリサイズを先に行わせてからプラグ生成
        BatteryDisplay display = GetComponentInChildren<BatteryDisplay>();
        if (display != null)
            display.Initialize();

        Createplug();
        //生成後にキャッシュ
        plugCollisions = GetComponentsInChildren<plugCollision>();
        socketCollisions = GetComponentsInChildren<socketCollision>();
    }

    public void SetCharge(int value)
    {
        _currentCharge = Mathf.Clamp(value, 0, maxCharge);
        //Debug.Log($"[Battery] {_currentCharge}/{maxCharge}");
    }
    public void RecheckAllConnections()
    {
        StartCoroutine(RecheckAfterFrame());
    }
    // battery.cs の RecheckAfterFrame
    IEnumerator RecheckAfterFrame()
    {
        yield return null;

        //plugCollisionだけ再チェック
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D col in nearbyColliders)
        {
            plugCollision nearbyPlug = col.GetComponent<plugCollision>();
            if (nearbyPlug != null)
                nearbyPlug.RecheckConnections();
        }
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