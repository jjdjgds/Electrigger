using UnityEngine;

public class plugCollision : MonoBehaviour
{
    private plugColor myPlugColor;
    private PowerNode myNode;

    [SerializeField] private float connectRadius = 0.3f; // 接続判定の半径
    private Quaternion lastRotation;
    void Awake()
    {
        myPlugColor = GetComponent<plugColor>();
        myNode = GetComponent<PowerNode>();
    }
    void Update()
    {
        // 回転が変化したら接続を再チェック
        if (transform.rotation != lastRotation)
        {
            lastRotation = transform.rotation;
            RecheckConnections();
        }
    }
    public void RecheckConnections()
    {
        // 一旦切断して再接続
        ConnectionManager.Instance?.Disconnect(myNode);

        // 周囲のsocketを検索して再接続
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, connectRadius);
        foreach (Collider2D hit in hits)
        {
            TryConnect(hit);
        }
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        TryConnect(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 回転後に再接続できるようにStayでも判定
        TryConnect(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("electricaloutlet")) return;
        if (other.GetComponent<socketCollision>() == null) return;
        ConnectionManager.Instance?.Disconnect(myNode);
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

        // 既に同じ接続が記録されていればスキップ
        ConnectionManager.Instance?.Connect(myNode, socketOwner);
    }
}