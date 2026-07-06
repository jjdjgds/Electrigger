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

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerCollider = playerObject.GetComponent<Collider2D>();
        }

        powerNode = GetComponent<PowerNode>();

        CacheWalls();
        ForceRefreshWalls();
    }

    private void Update()
    {
        if (!PauseMenuManager.CanGameInput())
            return;

        UpdateWallTriggers();
        DrawDebugWalls();
    }


    // Caches the wall colliders from the Wall_Edge child hierarchy.
    // Wall_Edge 配下にある壁コライダーをキャッシュします。
    private void CacheWalls()
    {
        Transform wallEdge = transform.Find("Wall_Edge");
        if (wallEdge == null)
            return;

        wallRight = wallEdge.Find("Wall_Right")?.GetComponent<Collider2D>();
        wallLeft = wallEdge.Find("Wall_Left")?.GetComponent<Collider2D>();
        wallTop = wallEdge.Find("Wall_Top")?.GetComponent<Collider2D>();
        wallBottom = wallEdge.Find("Wall_Bottom")?.GetComponent<Collider2D>();
    }


    // Forces all wall states to be recalculated immediately.
    // 壁の開閉状態を即座に再計算します。
    public void ForceRefreshWalls()
    {
        UpdateWallTriggers();
    }

 
    // Enables or disables each wall collider based on portal connectivity and power.
    // ポータル接続状態と通電状態に応じて各壁コライダーを有効/無効にします。
    private void UpdateWallTriggers()
    {
        bool localRightOpen = IsConnectedAndPowered(portalRightTarget);
        bool localLeftOpen = IsConnectedAndPowered(portalLeftTarget);
        bool localTopOpen = IsConnectedAndPowered(portalUpTarget);
        bool localBottomOpen = IsConnectedAndPowered(portalDownTarget);

        if (wallRight != null)
            wallRight.enabled = !localRightOpen;

        if (wallLeft != null)
            wallLeft.enabled = !localLeftOpen;

        if (wallTop != null)
            wallTop.enabled = !localTopOpen;

        if (wallBottom != null)
            wallBottom.enabled = !localBottomOpen;
    }


    // Returns true only if this monitor and the target monitor are both powered.
    // 自分と接続先モニターの両方が通電している場合のみ true を返します。
    private bool IsConnectedAndPowered(Transform target)
    {
        if (powerNode == null || !powerNode.IsPowered())
            return false;

        if (target == null)
            return false;

        PowerNode targetPower = target.GetComponent<PowerNode>();
        return targetPower != null && targetPower.IsPowered();
    }


    // Updates the tracked player transform and collider.
    // 参照しているプレイヤーの Transform と Collider を更新します。
    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        playerCollider = newPlayer.GetComponent<Collider2D>();
    }

    // Draws debug lines for all wall colliders.
    // すべての壁コライダーのデバッグ線を描画します。
    private void DrawDebugWalls()
    {
        DrawWallLine(transform.Find("Wall_Edge/Wall_Right"), wallRight);
        DrawWallLine(transform.Find("Wall_Edge/Wall_Left"), wallLeft);
        DrawWallLine(transform.Find("Wall_Edge/Wall_Top"), wallTop);
        DrawWallLine(transform.Find("Wall_Edge/Wall_Bottom"), wallBottom);
    }


    // Draws a rectangle around the wall collider for debugging.
    // Disabled walls are red, enabled walls are green.
    // 壁コライダーの矩形デバッグ表示を行います。
    // 無効な壁は赤、有効な壁は緑で表示されます。
    private void DrawWallLine(Transform wallTransform, Collider2D wallCollider)
    {
        if (wallTransform == null)
            return;

        BoxCollider2D box = wallTransform.GetComponent<BoxCollider2D>();
        if (box == null)
            return;

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