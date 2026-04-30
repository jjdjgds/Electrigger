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
        //多重呼び出し防止
        if (isRechecking) return;
        isRechecking = true;

        ConnectionManager.Instance?.Disconnect(myNode);
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
            ConnectionManager.Instance.Disconnect(myNode);
        }
        connectedSocket = null;
    }

    void TryConnect(Collider2D other)
    {
        if (!other.CompareTag("electricaloutlet")) return;
        if (other.GetComponent<socketCollision>() == null) return;

        plugColor otherColor = other.GetComponent<plugColor>();
        if (otherColor == null) return;
        if (myPlugColor.GetPlugColor() != otherColor.GetPlugColor()) return;

        PowerNode socketNode = other.GetComponent<PowerNode>();
        if (socketNode == null || myNode == null) return;

        PowerNode socketOwner = socketNode.owner != null ? socketNode.owner : socketNode;
        if (connectedSocket == socketOwner) return;

        connectedSocket = socketOwner;
        ConnectionManager.Instance?.Connect(myNode, socketOwner);
    }
}