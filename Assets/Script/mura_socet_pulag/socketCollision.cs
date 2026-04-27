using UnityEngine;

public class socketCollision : MonoBehaviour
{
    private Quaternion lastRotation;

    void Update()
    {
        if (transform.rotation != lastRotation)
        {
            lastRotation = transform.rotation;

            // 周囲のplugを検索して再接続チェック
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.3f);
            foreach (Collider2D hit in hits)
            {
                plugCollision plug = hit.GetComponent<plugCollision>();
                plug?.RecheckConnections();
            }
        }
    }
}