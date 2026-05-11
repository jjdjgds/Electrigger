// plugCollision.cs
using UnityEngine;
using System.Collections;

public class plugCollision : MonoBehaviour
{
    private plugColor myColorType;
    private PowerNode myNode;
    [SerializeField] private float connectRadius = 0.5f;
    private Quaternion lastRotation;
    private PowerNode connectedSocket = null;
    private bool isRechecking = false;

    void Awake()
    {
        myColorType = GetComponent<plugColor>();
        myNode = GetComponent<PowerNode>();
        lastRotation = transform.rotation;
    }

    void Update()
    {
        if (Quaternion.Angle(transform.rotation, lastRotation) > 0.1f)
        {
            lastRotation = transform.rotation;
            StartCoroutine(RecheckAfterFrame());
        }
    }

    IEnumerator RecheckAfterFrame()
    {
        yield return null;
        RecheckConnections();
    }

    public void RecheckConnections()
    {
        //多重呼び出し防止
        if (isRechecking) return;
        isRechecking = true;

        ConnectionManager.Instance?.Disconnect(myNode);

        if (myNode != null && myNode.owner != null && connectedSocket != null)
        {
            // FIX: Wipe the 4-way portals before we disconnect!
            ClearPortals(myNode.owner, connectedSocket);

            if (myNode.owner.connectedNode == connectedSocket)
            {
                myNode.owner.connectedNode = null;
            }
        }

        connectedSocket = null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, connectRadius);
        foreach (Collider2D hit in hits)
            TryConnect(hit);

        isRechecking = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("electricaloutlet")) return;
        if (gameObject.activeInHierarchy)
            StartCoroutine(RecheckAfterFrame());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("electricaloutlet")) return;
        if (gameObject.activeInHierarchy)
            StartCoroutine(RecheckAfterFrame());
    }

    void OnDisable()
    {
        if (ConnectionManager.Instance != null && myNode != null)
        {
            ConnectionManager.Instance?.Disconnect(myNode);

            PowerNode myOwner = myNode.owner != null ? myNode.owner : myNode;
            if (connectedSocket != null)
            {
                ClearPortals(myOwner, connectedSocket);
            }

            connectedSocket = null;
        }
        connectedSocket = null;
    }

    void TryConnect(Collider2D other)
    {
        // 1. Initial Validation
        if (!other.CompareTag("electricaloutlet")) return;
        if (other.GetComponent<socketCollision>() == null) return;

        // 2. Color check
        plugColor otherColor = other.GetComponent<plugColor>();
        if (otherColor == null) return;
        if (myColorType.GetColorType() != otherColor.GetColorType()) return;

        // 3. Get PowerNodes
        PowerNode socketNode = other.GetComponent<PowerNode>();
        if (socketNode == null || myNode == null) return;

        // Safely get the owners (the main Monitor PowerNodes)
        PowerNode myOwner = myNode.owner != null ? myNode.owner : myNode;
        PowerNode socketOwner = socketNode.owner != null ? socketNode.owner : socketNode;

        // Prevent redundant connections
        if (connectedSocket == socketOwner) return;

        // --- TELEPORT SETUP LOGIC ---

        // 4. Get Monitor_Collision components
        Monitor_Collision myMonitor = myOwner.GetComponent<Monitor_Collision>();
        Monitor_Collision socketMonitor = socketOwner.GetComponent<Monitor_Collision>();

        if (myMonitor != null && socketMonitor != null)
        {
            // 5. Use world position relative to monitor center to determine side
            SetPortalSide(myMonitor, transform, socketMonitor);
            SetPortalSide(socketMonitor, other.transform, myMonitor);

            //Debug.Log($"Plug({gameObject.name}) → {myMonitor.name} portal set");
            //Debug.Log($"Socket({other.name}) → {socketMonitor.name} portal set");
        }

        // 6. Link them together
        myOwner.connectedNode = socketOwner;
        socketOwner.connectedNode = myOwner;

        // 7. Finalize Connection
        connectedSocket = socketOwner;
        ConnectionManager.Instance?.Connect(myNode, socketOwner);

    }

    private void SetPortalSide(Monitor_Collision monitor, Transform plugTransform, Monitor_Collision targetMonitor)
    {
        // Convert plug world position to monitor LOCAL space
        // This matches the wall names regardless of monitor rotation
        Vector2 localDir = monitor.transform.InverseTransformPoint(plugTransform.position).normalized;
        float angle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;

        if (angle >= -45f && angle < 45f)
            monitor.portalRightTarget = targetMonitor.transform;
        else if (angle >= 45f && angle < 135f)
            monitor.portalUpTarget = targetMonitor.transform;
        else if (angle >= 135f || angle < -135f)
            monitor.portalLeftTarget = targetMonitor.transform;
        else
            monitor.portalDownTarget = targetMonitor.transform;
    }

    private void ClearPortals(PowerNode ownerA, PowerNode ownerB)
    {
        Monitor_Collision monitorA = ownerA.GetComponent<Monitor_Collision>();
        Monitor_Collision monitorB = ownerB.GetComponent<Monitor_Collision>();

        if (monitorA != null && monitorB != null)
        {
            // Wipe Monitor A's targets
            if (monitorA.portalRightTarget == monitorB.transform) monitorA.portalRightTarget = null;
            if (monitorA.portalLeftTarget == monitorB.transform) monitorA.portalLeftTarget = null;
            if (monitorA.portalUpTarget == monitorB.transform) monitorA.portalUpTarget = null;
            if (monitorA.portalDownTarget == monitorB.transform) monitorA.portalDownTarget = null;

            // Wipe Monitor B's targets
            if (monitorB.portalRightTarget == monitorA.transform) monitorB.portalRightTarget = null;
            if (monitorB.portalLeftTarget == monitorA.transform) monitorB.portalLeftTarget = null;
            if (monitorB.portalUpTarget == monitorA.transform) monitorB.portalUpTarget = null;
            if (monitorB.portalDownTarget == monitorA.transform) monitorB.portalDownTarget = null;
        }
    }
}