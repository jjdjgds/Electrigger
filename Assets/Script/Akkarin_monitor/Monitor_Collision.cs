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
        Vector2 newPosition;


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

        Bounds targetBounds = targetMonitor.GetComponent<Collider2D>().bounds;
        newPosition.x = Mathf.Clamp(newPosition.x, targetBounds.min.x + push, targetBounds.max.x - push);
        newPosition.y = Mathf.Clamp(newPosition.y, targetBounds.min.y + push, targetBounds.max.y - push);

        playerTransform.position = new Vector3(newPosition.x, newPosition.y, playerTransform.position.z);
        if (rb != null) rb.linearVelocity = oldVelocity;
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