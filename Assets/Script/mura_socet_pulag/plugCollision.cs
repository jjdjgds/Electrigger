// plugCollision.cs
using UnityEngine;
using System.Collections;

public class plugCollision : MonoBehaviour
{
    private plugColor myPlugColor;
    private PowerNode myNode;
    [SerializeField] private float connectRadius = 0.5f;
    private Quaternion lastRotation;
    private PowerNode connectedSocket = null;
    private bool isRechecking = false;

    void Awake()
    {
        myPlugColor = GetComponent<plugColor>();
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
        // ✅ 多重呼び出し防止
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

    void OnTriggerEnter2D(Collider2D other) => TryConnect(other);

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("electricaloutlet")) return;
        if (other.GetComponent<socketCollision>() == null) return;

        PowerNode socketNode = other.GetComponent<PowerNode>();
        PowerNode socketOwner = socketNode?.owner != null ? socketNode.owner : socketNode;

        if (connectedSocket == socketOwner)
        {
            ConnectionManager.Instance?.Disconnect(myNode);

            PowerNode myOwner = myNode.owner != null ? myNode.owner : myNode;
            ClearPortals(myOwner, socketOwner);

            connectedSocket = null;
        }
    }

    void TryConnect(Collider2D other)
    {
        // 1. Initial Validation
        if (!other.CompareTag("electricaloutlet")) return;
        if (other.GetComponent<socketCollision>() == null) return;

        // 2. Color check
        plugColor otherColor = other.GetComponent<plugColor>();
        if (otherColor == null) return;
        if (myPlugColor.GetPlugColor() != otherColor.GetPlugColor()) return;

        // 3. Get PowerNodes
        PowerNode socketNode = other.GetComponent<PowerNode>();
        if (socketNode == null || myNode == null) return;

        // Safely get the owners (the main Monitor PowerNodes)
        PowerNode myOwner = myNode.owner != null ? myNode.owner : myNode;
        PowerNode socketOwner = socketNode.owner != null ? socketNode.owner : socketNode;

        // Prevent redundant connections
        if (connectedSocket == socketOwner) return;

        // --- TELEPORT SETUP LOGIC ---

        // 4. Set the side for the PLUG's monitor based on the plug's rotation
        Monitor_Collision myMonitor = myOwner.GetComponent<Monitor_Collision>();
        Monitor_Collision socketMonitor = socketOwner.GetComponent<Monitor_Collision>();

        if (myMonitor != null && socketMonitor != null)
        {
            // 5. Save the connection for Monitor A (The Plug)
            float myZRot = transform.localEulerAngles.z;
            if (myZRot >= 315 || myZRot < 45) myMonitor.portalUpTarget = socketMonitor.transform;
            else if (myZRot >= 45 && myZRot < 135) myMonitor.portalLeftTarget = socketMonitor.transform;
            else if (myZRot >= 135 && myZRot < 225) myMonitor.portalDownTarget = socketMonitor.transform;
            else myMonitor.portalRightTarget = socketMonitor.transform;

            // 6. Save the connection for Monitor B (The Socket)
            float socketZRot = other.transform.localEulerAngles.z;
            if (socketZRot >= 315 || socketZRot < 45) socketMonitor.portalUpTarget = myMonitor.transform;
            else if (socketZRot >= 45 && socketZRot < 135) socketMonitor.portalLeftTarget = myMonitor.transform;
            else if (socketZRot >= 135 && socketZRot < 225) socketMonitor.portalDownTarget = myMonitor.transform;
            else socketMonitor.portalRightTarget = myMonitor.transform;
        }

        // 7. Link them together
        myOwner.connectedNode = socketOwner;
        socketOwner.connectedNode = myOwner;

        // 7. Finalize Connection
        connectedSocket = socketOwner;
        ConnectionManager.Instance?.Connect(myNode, socketOwner);
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