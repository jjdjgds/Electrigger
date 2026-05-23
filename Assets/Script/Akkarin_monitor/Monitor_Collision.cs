using UnityEngine;

public class Monitor_Collision : MonoBehaviour
{
    private Transform player;
    private Collider2D playerCollider;
    private PowerNode powerNode;

    [HideInInspector] public Collider2D wallRight;
    [HideInInspector] public Collider2D wallLeft;
    [HideInInspector] public Collider2D wallTop;
    [HideInInspector] public Collider2D wallBottom;

    [HideInInspector] public Transform portalRightTarget;
    [HideInInspector] public Transform portalLeftTarget;
    [HideInInspector] public Transform portalUpTarget;
    [HideInInspector] public Transform portalDownTarget;

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerCollider = pObj.GetComponent<Collider2D>();
        }

        powerNode = GetComponent<PowerNode>();

        CacheWalls();
        ForceRefreshWalls();
    }

    void Update()
    {
        if (!PauseMenuManager.CanGameInput()) return;
        UpdateWallTriggers();
        DrawDebugWalls();
    }

    void CacheWalls()
    {
        Transform wallEdge = transform.Find("Wall_Edge");
        if (wallEdge != null)
        {
            wallRight = wallEdge.Find("Wall_Right")?.GetComponent<Collider2D>();
            wallLeft = wallEdge.Find("Wall_Left")?.GetComponent<Collider2D>();
            wallTop = wallEdge.Find("Wall_Top")?.GetComponent<Collider2D>();
            wallBottom = wallEdge.Find("Wall_Bottom")?.GetComponent<Collider2D>();
        }
    }

    public void ForceRefreshWalls()
    {
        UpdateWallTriggers();
    }

    void UpdateWallTriggers()
    {
        bool localRightOpen = IsConnectedAndPowered(portalRightTarget);
        bool localLeftOpen = IsConnectedAndPowered(portalLeftTarget);
        bool localTopOpen = IsConnectedAndPowered(portalUpTarget);
        bool localBottomOpen = IsConnectedAndPowered(portalDownTarget);

        if (wallRight != null) wallRight.enabled = !localRightOpen;
        if (wallLeft != null) wallLeft.enabled = !localLeftOpen;
        if (wallTop != null) wallTop.enabled = !localTopOpen;
        if (wallBottom != null) wallBottom.enabled = !localBottomOpen;
    }

    private bool IsConnectedAndPowered(Transform target)
    {
        if (powerNode == null || !powerNode.IsPowered()) return false;
        if (target == null) return false;

        PowerNode targetPower = target.GetComponent<PowerNode>();
        return targetPower != null && targetPower.IsPowered();
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        playerCollider = newPlayer.GetComponent<Collider2D>();
    }

    void DrawDebugWalls()
    {
        DrawWallLine(transform.Find("Wall_Edge/Wall_Right"), wallRight);
        DrawWallLine(transform.Find("Wall_Edge/Wall_Left"), wallLeft);
        DrawWallLine(transform.Find("Wall_Edge/Wall_Top"), wallTop);
        DrawWallLine(transform.Find("Wall_Edge/Wall_Bottom"), wallBottom);
    }

    void DrawWallLine(Transform wallTransform, Collider2D wallCollider)
    {
        if (wallTransform == null) return;

        BoxCollider2D box = wallTransform.GetComponent<BoxCollider2D>();
        if (box == null) return;

        Vector3 center = wallTransform.TransformPoint(box.offset);
        Vector3 right = wallTransform.right * (box.size.x * wallTransform.lossyScale.x * 0.5f);
        Vector3 up = wallTransform.up * (box.size.y * wallTransform.lossyScale.y * 0.5f);

        Vector3 p1 = center - right - up;
        Vector3 p2 = center + right - up;
        Vector3 p3 = center + right + up;
        Vector3 p4 = center - right + up;

        Color color = (wallCollider != null && !wallCollider.enabled) ? Color.red : Color.green;

        Debug.DrawLine(p1, p2, color);
        Debug.DrawLine(p2, p3, color);
        Debug.DrawLine(p3, p4, color);
        Debug.DrawLine(p4, p1, color);
    }
}