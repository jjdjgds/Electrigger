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

    public float teleportCooldown = 0f;

    [HideInInspector] public Transform portalRightTarget;
    [HideInInspector] public Transform portalLeftTarget;
    [HideInInspector] public Transform portalUpTarget;
    [HideInInspector] public Transform portalDownTarget;

    [Header("Wall Detection")]
    public LayerMask wallLayer; // Inspectorで設定可能
    public LayerMask blockTeleportLayer;
    private enum PortalSide { Right, Left, Top, Bottom }

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerCollider = pObj.GetComponent<Collider2D>();
        }

        powerNode = GetComponent<PowerNode>();

        Transform wallEdge = transform.Find("Wall_Edge");
        if (wallEdge != null)
        {
            wallRight = wallEdge.Find("Wall_Right")?.GetComponent<Collider2D>();
            wallLeft = wallEdge.Find("Wall_Left")?.GetComponent<Collider2D>();
            wallTop = wallEdge.Find("Wall_Top")?.GetComponent<Collider2D>();
            wallBottom = wallEdge.Find("Wall_Bottom")?.GetComponent<Collider2D>();
        }

        // ★デバッグ: レイヤー情報を確認
        Debug.Log($"wallLayer value: {wallLayer.value}");
        Debug.Log($"wallLayer binary: {System.Convert.ToString(wallLayer.value, 2)}");

        // 壁のレイヤーを確認
        if (wallRight != null)
            Debug.Log($"Wall_Right layer: {LayerMask.LayerToName(wallRight.gameObject.layer)} (index: {wallRight.gameObject.layer})");
    }

    void Update()
    {
        if (!PauseMenuManager.CanGameInput()) return;

        if (teleportCooldown > 0) teleportCooldown -= Time.deltaTime;

        UpdateWallTriggers();
        CheckTeleport();
    }

    void UpdateWallTriggers()
    {
        if (wallRight != null) wallRight.isTrigger = false;
        if (wallLeft != null) wallLeft.isTrigger = false;
        if (wallTop != null) wallTop.isTrigger = false;
        if (wallBottom != null) wallBottom.isTrigger = false;

        if (powerNode == null || !powerNode.IsPowered()) return;

        if (portalRightTarget != null && portalRightTarget.GetComponent<PowerNode>().IsPowered())
        {
            if (wallRight != null) wallRight.isTrigger = true;
        }

        if (portalLeftTarget != null && portalLeftTarget.GetComponent<PowerNode>().IsPowered())
        {
            if (wallLeft != null) wallLeft.isTrigger = true;
        }

        if (portalUpTarget != null && portalUpTarget.GetComponent<PowerNode>().IsPowered())
        {
            if (wallTop != null) wallTop.isTrigger = true;
        }

        if (portalDownTarget != null && portalDownTarget.GetComponent<PowerNode>().IsPowered())
        {
            if (wallBottom != null) wallBottom.isTrigger = true;
        }
    }

    void CheckTeleport()
    {
        if (player == null || playerCollider == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                player = pObj.transform;
                playerCollider = pObj.GetComponent<Collider2D>();
            }
            else
            {
                return;
            }
        }

        if (powerNode == null || teleportCooldown > 0) return;

        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null || !myCollider.OverlapPoint(playerCollider.bounds.center)) return;

        Bounds playerReach = playerCollider.bounds;
        playerReach.Expand(0.2f);

        if (wallRight != null && wallRight.isTrigger && IsOverlapping2D(playerReach, wallRight.bounds))
        {
            if (CanTeleportTo(player.position, portalRightTarget, PortalSide.Right))
            {
                TeleportPlayer(player, portalRightTarget, PortalSide.Right);
            }
            else
            {
                //ブロックされた場合はパッセンジャー状態を解除
                MonitorPassengerController passenger = GetComponent<MonitorPassengerController>();
                if (passenger != null)
                    passenger.CancelPassengerImmediate();
            }
            return;
        }

        if (wallLeft != null && wallLeft.isTrigger && IsOverlapping2D(playerReach, wallLeft.bounds))
        {
            if (CanTeleportTo(player.position, portalLeftTarget, PortalSide.Left))
            {
                TeleportPlayer(player, portalLeftTarget, PortalSide.Left);
            }
            else
            {
                //ブロックされた場合はパッセンジャー状態を解除
                MonitorPassengerController passenger = GetComponent<MonitorPassengerController>();
                if (passenger != null)
                    passenger.CancelPassengerImmediate();
            }
            return;
        }

        if (wallTop != null && wallTop.isTrigger && IsOverlapping2D(playerReach, wallTop.bounds))
        {
            if (CanTeleportTo(player.position, portalUpTarget, PortalSide.Top))
            {
                TeleportPlayer(player, portalUpTarget, PortalSide.Top);
            }
            else
            {
                //ブロックされた場合はパッセンジャー状態を解除
                MonitorPassengerController passenger = GetComponent<MonitorPassengerController>();
                if (passenger != null)
                    passenger.CancelPassengerImmediate();
            }
            return;
        }

        if (wallBottom != null && wallBottom.isTrigger && IsOverlapping2D(playerReach, wallBottom.bounds))
        {
            if (CanTeleportTo(player.position, portalDownTarget, PortalSide.Bottom))
            {
                TeleportPlayer(player, portalDownTarget, PortalSide.Bottom);
            }
            else
            {
                //ブロックされた場合はパッセンジャー状態を解除
                MonitorPassengerController passenger = GetComponent<MonitorPassengerController>();
                if (passenger != null)
                    passenger.CancelPassengerImmediate();
            }
            return;
        }
    }

    private void TeleportPlayer(Transform playerTransform, Transform targetMonitor, PortalSide sideTouched)
    {
        if (targetMonitor == null) return;

        Monitor_Collision targetMC = targetMonitor.GetComponent<Monitor_Collision>();
        if (targetMC == null) return;

        MonitorPassengerController sourcePassenger = GetComponent<MonitorPassengerController>();
        if (sourcePassenger != null)
            sourcePassenger.CancelPassengerImmediate();

        MonitorPassengerController targetPassenger = targetMonitor.GetComponent<MonitorPassengerController>();
        if (targetPassenger != null)
            targetPassenger.CancelPassengerImmediate();

        teleportCooldown = 0.5f;
        targetMC.teleportCooldown = 0.5f;

        PortalSide exitSide = PortalSide.Left;

        if (targetMC.portalRightTarget == transform) exitSide = PortalSide.Right;
        else if (targetMC.portalLeftTarget == transform) exitSide = PortalSide.Left;
        else if (targetMC.portalUpTarget == transform) exitSide = PortalSide.Top;
        else if (targetMC.portalDownTarget == transform) exitSide = PortalSide.Bottom;

        Collider2D exitWall = null;
        switch (exitSide)
        {
            case PortalSide.Right: exitWall = targetMC.wallRight; break;
            case PortalSide.Left: exitWall = targetMC.wallLeft; break;
            case PortalSide.Top: exitWall = targetMC.wallTop; break;
            case PortalSide.Bottom: exitWall = targetMC.wallBottom; break;
        }

        if (exitWall == null) return;

        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        Vector2 oldVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

        float push = 0.6f;
        Vector2 exitWallCenter = exitWall.bounds.center;
        Vector2 inwardDir = ((Vector2)targetMonitor.position - exitWallCenter).normalized;

        Vector2 newPosition = playerTransform.position;

        if (Mathf.Abs(inwardDir.x) > Mathf.Abs(inwardDir.y))
        {
            newPosition.x = exitWallCenter.x + inwardDir.x * push;
            newPosition.y = playerTransform.position.y;
        }
        else
        {
            newPosition.x = playerTransform.position.x;
            newPosition.y = exitWallCenter.y + inwardDir.y * push;
        }

        Collider2D targetCollider = targetMonitor.GetComponent<Collider2D>();
        if (targetCollider == null) return;

        Bounds targetBounds = targetCollider.bounds;
        newPosition.x = Mathf.Clamp(newPosition.x, targetBounds.min.x + push, targetBounds.max.x - push);
        newPosition.y = Mathf.Clamp(newPosition.y, targetBounds.min.y + push, targetBounds.max.y - push);

        Vector3 finalPos = new Vector3(newPosition.x, newPosition.y, playerTransform.position.z);

        if (targetPassenger != null)
            finalPos = targetPassenger.ClampWorldPositionInside(finalPos);

        playerTransform.position = finalPos;

        if (rb != null)
        {
            rb.linearVelocity = oldVelocity;
            rb.angularVelocity = 0f;
        }
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        playerCollider = newPlayer.GetComponent<Collider2D>();
    }

    private bool IsOverlapping2D(Bounds p, Bounds w)
    {
        return p.min.x <= w.max.x && p.max.x >= w.min.x &&
               p.min.y <= w.max.y && p.max.y >= w.min.y;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (playerCollider != null)
        {
            Gizmos.color = Color.green;
            Bounds pBounds = playerCollider.bounds;
            pBounds.Expand(0.5f);
            Gizmos.DrawWireCube(pBounds.center, pBounds.size);
        }

        Gizmos.color = Color.red;
        if (wallRight != null && wallRight.isTrigger) Gizmos.DrawWireCube(wallRight.bounds.center, wallRight.bounds.size);
        if (wallLeft != null && wallLeft.isTrigger) Gizmos.DrawWireCube(wallLeft.bounds.center, wallLeft.bounds.size);
        if (wallTop != null && wallTop.isTrigger) Gizmos.DrawWireCube(wallTop.bounds.center, wallTop.bounds.size);
        if (wallBottom != null && wallBottom.isTrigger) Gizmos.DrawWireCube(wallBottom.bounds.center, wallBottom.bounds.size);

        // Linecast/BoxCastの経路を可視化
        if (player != null && Application.isPlaying && playerCollider != null)
        {
            Vector2 boxSize = new Vector2(
                playerCollider.bounds.size.x * 0.8f,
                playerCollider.bounds.size.y * 0.8f
            );

            Monitor_Collision targetMC;
            Vector2 dest;

            // Right
            if (portalRightTarget != null)
            {
                targetMC = portalRightTarget.GetComponent<Monitor_Collision>();
                if (targetMC != null)
                {
                    dest = CalculateDestinationPosition(portalRightTarget, targetMC, PortalSide.Right);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(player.position, dest);
                    Gizmos.DrawWireCube(dest, boxSize); // 目的地のボックスサイズ
                    Gizmos.DrawSphere(dest, 0.1f);
                }
            }

            // Left
            if (portalLeftTarget != null)
            {
                targetMC = portalLeftTarget.GetComponent<Monitor_Collision>();
                if (targetMC != null)
                {
                    dest = CalculateDestinationPosition(portalLeftTarget, targetMC, PortalSide.Left);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(player.position, dest);
                    Gizmos.DrawWireCube(dest, boxSize);
                    Gizmos.DrawSphere(dest, 0.1f);
                }
            }

            // Top & Bottom も同様...
        }
    }
    private bool IsPathBlocked(Vector2 start, Vector2 end)
    {
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        Vector2 boxSize = Vector2.one * 0.3f;
        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            boxSize = new Vector2(bounds.size.x * 0.8f, bounds.size.y * 0.8f);
        }

        Vector2 adjustedStart = start + direction * 0.1f; // ★0.05f → 0.1f に増やす

        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        LayerMask groundMask = 1 << groundLayerIndex;

        RaycastHit2D hit = Physics2D.BoxCast(
            adjustedStart,
            boxSize,
            0f,
            direction,
            distance - 0.1f, // ★0.05f → 0.1f に増やす
            groundMask
        );

        Debug.DrawLine(adjustedStart, end, hit.collider != null ? Color.red : Color.green, 2f);

        if (hit.collider != null)
        {
            // ★より厳密に無視するコライダーをチェック
            // プレイヤー自身
            if (hit.collider == playerCollider)
                return false;

            // ソースモニター
            Collider2D sourceMonitorCollider = GetComponent<Collider2D>();
            if (hit.collider == sourceMonitorCollider)
                return false;

            // ソースモニターの壁（Wall_Edge配下）
            if (hit.collider == wallRight || hit.collider == wallLeft ||
                hit.collider == wallTop || hit.collider == wallBottom)
                return false;

            // ターゲットモニターのコライダー（計算必要）
            // これは後で対処

            Debug.Log($"★壁検出 (BoxCast): {hit.collider.name}");
            return true;
        }

        return false;
    }


    private bool CanTeleportTo(Vector2 currentPos, Transform targetMonitor, PortalSide entrySide)
    {
        if (targetMonitor == null) return false;

        Monitor_Collision targetMC = targetMonitor.GetComponent<Monitor_Collision>();
        if (targetMC == null) return false;

        // テレポート先の位置を事前計算
        Vector2 destinationPos = CalculateDestinationPosition(targetMonitor, targetMC, entrySide);

        // 経路に壁があるかチェック（ターゲットモニターの情報を渡す）
        if (IsPathBlocked(currentPos, destinationPos, targetMonitor))
        {
            return false;
        }

        return true;
    }

    private bool IsPathBlocked(Vector2 start, Vector2 end, Transform targetMonitor = null)
    {
        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        if (distance < 0.01f) return false; // ★ゼロ距離ガード

        // ★Inspectorで設定したレイヤーを使う
        if (blockTeleportLayer.value == 0) return false;

        Vector2 boxSize = Vector2.one * 0.3f;
        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            boxSize = new Vector2(bounds.size.x * 0.7f, bounds.size.y * 0.7f); // ★少し小さく
        }

        // ★全ヒットを取得して除外リストと照合
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            start + direction * 0.15f,
            boxSize,
            0f,
            direction,
            distance - 0.15f,
            blockTeleportLayer
        );

        // 無視するコライダーを収集
        System.Collections.Generic.HashSet<Collider2D> ignored =
            new System.Collections.Generic.HashSet<Collider2D>();

        ignored.Add(playerCollider);
        ignored.Add(GetComponent<Collider2D>());
        if (wallRight) ignored.Add(wallRight);
        if (wallLeft) ignored.Add(wallLeft);
        if (wallTop) ignored.Add(wallTop);
        if (wallBottom) ignored.Add(wallBottom);

        if (targetMonitor != null)
        {
            ignored.Add(targetMonitor.GetComponent<Collider2D>());
            var tmc = targetMonitor.GetComponent<Monitor_Collision>();
            if (tmc != null)
            {
                if (tmc.wallRight) ignored.Add(tmc.wallRight);
                if (tmc.wallLeft) ignored.Add(tmc.wallLeft);
                if (tmc.wallTop) ignored.Add(tmc.wallTop);
                if (tmc.wallBottom) ignored.Add(tmc.wallBottom);
            }
        }

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (ignored.Contains(hit.collider)) continue;

            //Debug.Log($"テレポートブロック: {hit.collider.name} (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            Debug.DrawLine(start, end, Color.red, 2f);
            return true;
        }

        Debug.DrawLine(start, end, Color.green, 2f);
        return false;
    }
    private Vector2 CalculateDestinationPosition(Transform targetMonitor, Monitor_Collision targetMC, PortalSide entrySide)
    {
        // 出口側を判定
        PortalSide exitSide = PortalSide.Left;

        if (targetMC.portalRightTarget == transform) exitSide = PortalSide.Right;
        else if (targetMC.portalLeftTarget == transform) exitSide = PortalSide.Left;
        else if (targetMC.portalUpTarget == transform) exitSide = PortalSide.Top;
        else if (targetMC.portalDownTarget == transform) exitSide = PortalSide.Bottom;

        Collider2D exitWall = null;
        switch (exitSide)
        {
            case PortalSide.Right: exitWall = targetMC.wallRight; break;
            case PortalSide.Left: exitWall = targetMC.wallLeft; break;
            case PortalSide.Top: exitWall = targetMC.wallTop; break;
            case PortalSide.Bottom: exitWall = targetMC.wallBottom; break;
        }

        if (exitWall == null) return targetMonitor.position;

        float push = 0.6f;
        Vector2 exitWallCenter = exitWall.bounds.center;
        Vector2 inwardDir = ((Vector2)targetMonitor.position - exitWallCenter).normalized;

        // テレポート先の位置を計算（TeleportPlayerと同じロジック）
        Vector2 newPosition;

        if (Mathf.Abs(inwardDir.x) > Mathf.Abs(inwardDir.y))
        {
            // 横方向（左右）からの出現
            newPosition.x = exitWallCenter.x + inwardDir.x * push;

            // Y座標: プレイヤーの現在Y座標をターゲットモニター内に収める
            if (player != null)
            {
                newPosition.y = player.position.y;
            }
            else
            {
                newPosition.y = targetMonitor.position.y;
            }
        }
        else
        {
            // 縦方向（上下）からの出現
            // X座標: プレイヤーの現在X座標をターゲットモニター内に収める
            if (player != null)
            {
                newPosition.x = player.position.x;
            }
            else
            {
                newPosition.x = targetMonitor.position.x;
            }

            newPosition.y = exitWallCenter.y + inwardDir.y * push;
        }

        // ターゲットモニターの範囲内に制限
        Collider2D targetCollider = targetMonitor.GetComponent<Collider2D>();
        if (targetCollider != null)
        {
            Bounds targetBounds = targetCollider.bounds;
            newPosition.x = Mathf.Clamp(newPosition.x, targetBounds.min.x + push, targetBounds.max.x - push);
            newPosition.y = Mathf.Clamp(newPosition.y, targetBounds.min.y + push, targetBounds.max.y - push);
        }

        return newPosition;
    }
}