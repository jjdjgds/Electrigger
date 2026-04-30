using UnityEngine;

public class Monitor_Collision : MonoBehaviour
{
    private Transform player;
    private Collider2D playerCollider;
    private PowerNode powerNode;

    private Collider2D wallRight;
    private Collider2D wallLeft;
    private Collider2D wallTop;
    private Collider2D wallBottom;

    public float teleportCooldown = 0f;

    [HideInInspector] public Transform portalRightTarget;
    [HideInInspector] public Transform portalLeftTarget;
    [HideInInspector] public Transform portalUpTarget;
    [HideInInspector] public Transform portalDownTarget;

    // Define the 4 sides so the teleport knows EXACTLY which door was used
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
    }

    void Update()
    {
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

        if (portalRightTarget != null && portalRightTarget.GetComponent<PowerNode>().IsPowered()) { if (wallRight != null) wallRight.isTrigger = true; }
        if (portalLeftTarget != null && portalLeftTarget.GetComponent<PowerNode>().IsPowered()) { if (wallLeft != null) wallLeft.isTrigger = true; }
        if (portalUpTarget != null && portalUpTarget.GetComponent<PowerNode>().IsPowered()) { if (wallTop != null) wallTop.isTrigger = true; }
        if (portalDownTarget != null && portalDownTarget.GetComponent<PowerNode>().IsPowered()) { if (wallBottom != null) wallBottom.isTrigger = true; }
    }

    void CheckTeleport()
    {
        if (player == null || playerCollider == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) { player = pObj.transform; playerCollider = pObj.GetComponent<Collider2D>(); }
            else return;
        }

        if (powerNode == null || teleportCooldown > 0) return;

        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null || !myCollider.OverlapPoint(playerCollider.bounds.center)) return;

        Bounds playerReach = playerCollider.bounds;
        playerReach.Expand(0.2f);

        // Pass the EXACT side into the teleport method!
        if (wallRight != null && wallRight.isTrigger && IsOverlapping2D(playerReach, wallRight.bounds))
        {
            TeleportPlayer(player, portalRightTarget, PortalSide.Right);
            return;
        }
        if (wallLeft != null && wallLeft.isTrigger && IsOverlapping2D(playerReach, wallLeft.bounds))
        {
            TeleportPlayer(player, portalLeftTarget, PortalSide.Left);
            return;
        }
        if (wallTop != null && wallTop.isTrigger && IsOverlapping2D(playerReach, wallTop.bounds))
        {
            TeleportPlayer(player, portalUpTarget, PortalSide.Top);
            return;
        }
        if (wallBottom != null && wallBottom.isTrigger && IsOverlapping2D(playerReach, wallBottom.bounds))
        {
            TeleportPlayer(player, portalDownTarget, PortalSide.Bottom);
            return;
        }
    }

    private void TeleportPlayer(Transform playerTransform, Transform targetMonitor, PortalSide sideTouched)
    {
        if (targetMonitor == null) return;

        Monitor_Collision targetMC = targetMonitor.GetComponent<Monitor_Collision>();
        if (targetMC == null) return;

        teleportCooldown = 0.5f;
        targetMC.teleportCooldown = 0.5f;

        // 1. Find which side of the TARGET monitor connects back to THIS monitor
        PortalSide exitSide = PortalSide.Left; // default fallback
        if (targetMC.portalRightTarget == transform) exitSide = PortalSide.Right;
        else if (targetMC.portalLeftTarget == transform) exitSide = PortalSide.Left;
        else if (targetMC.portalUpTarget == transform) exitSide = PortalSide.Top;
        else if (targetMC.portalDownTarget == transform) exitSide = PortalSide.Bottom;

        // 2. Find the exact offset relative to the CENTER of the current monitor
        float parallelOffset = 0f;
        if (sideTouched == PortalSide.Right || sideTouched == PortalSide.Left)
            parallelOffset = playerTransform.position.y - transform.position.y;
        else
            parallelOffset = playerTransform.position.x - transform.position.x;

        // 3. Calculate target monitor edges using localScale (No bounds needed)
        float targetHalfWidth = targetMonitor.localScale.x / 2f;
        float targetHalfHeight = targetMonitor.localScale.y / 2f;

        float push = 0.6f; // Distance to push player inside the room so they don't hit the trigger
        Vector2 newPosition = targetMonitor.position;

        // 4. Apply the exact same offset to the target monitor
        switch (exitSide)
        {
            case PortalSide.Right:
                newPosition.x = targetMonitor.position.x + targetHalfWidth - push;
                newPosition.y = targetMonitor.position.y + parallelOffset;
                break;
            case PortalSide.Left:
                newPosition.x = targetMonitor.position.x - targetHalfWidth + push;
                newPosition.y = targetMonitor.position.y + parallelOffset;
                break;
            case PortalSide.Top:
                newPosition.x = targetMonitor.position.x + parallelOffset;
                newPosition.y = targetMonitor.position.y + targetHalfHeight - push;
                break;
            case PortalSide.Bottom:
                newPosition.x = targetMonitor.position.x + parallelOffset;
                newPosition.y = targetMonitor.position.y - targetHalfHeight + push;
                break;
        }

        // 5. Move the player (maintains exact velocity because we don't reset rb.velocity)
        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = newPosition;
        }
        else
        {
            playerTransform.position = newPosition;
        }
    }

    private bool IsOverlapping2D(Bounds p, Bounds w)
    {
        return p.min.x <= w.max.x && p.max.x >= w.min.x &&
               p.min.y <= w.max.y && p.max.y >= w.min.y;
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw Player Reach (Green)
        if (playerCollider != null)
        {
            Gizmos.color = Color.green;
            Bounds pBounds = playerCollider.bounds;
            pBounds.Expand(0.5f);
            Gizmos.DrawWireCube(pBounds.center, pBounds.size);
        }

        // Draw Open Portals (Red)
        Gizmos.color = Color.red;
        if (wallRight != null && wallRight.isTrigger) Gizmos.DrawWireCube(wallRight.bounds.center, wallRight.bounds.size);
        if (wallLeft != null && wallLeft.isTrigger) Gizmos.DrawWireCube(wallLeft.bounds.center, wallLeft.bounds.size);
        if (wallTop != null && wallTop.isTrigger) Gizmos.DrawWireCube(wallTop.bounds.center, wallTop.bounds.size);
        if (wallBottom != null && wallBottom.isTrigger) Gizmos.DrawWireCube(wallBottom.bounds.center, wallBottom.bounds.size);
    }
}


//private void OnDrawGizmos()
//{
//    if (!Application.isPlaying) return;
//
//    // Draw Player Reach (Green)
//    if (playerCollider != null)
//    {
//        Gizmos.color = Color.green;
//        Bounds pBounds = playerCollider.bounds;
//        pBounds.Expand(0.5f);
//        Gizmos.DrawWireCube(pBounds.center, pBounds.size);
//    }
//
//    // Draw Open Portals (Red)
//    Gizmos.color = Color.red;
//    if (wallRight != null && wallRight.isTrigger) Gizmos.DrawWireCube(wallRight.bounds.center, wallRight.bounds.size);
//    if (wallLeft != null && wallLeft.isTrigger) Gizmos.DrawWireCube(wallLeft.bounds.center, wallLeft.bounds.size);
//    if (wallTop != null && wallTop.isTrigger) Gizmos.DrawWireCube(wallTop.bounds.center, wallTop.bounds.size);
//    if (wallBottom != null && wallBottom.isTrigger) Gizmos.DrawWireCube(wallBottom.bounds.center, wallBottom.bounds.size);
//}