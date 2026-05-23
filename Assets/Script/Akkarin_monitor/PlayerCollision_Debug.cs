using UnityEngine;

public class PlayerCollisionDebug : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[Collision Enter] {collision.collider.name}", collision.collider.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Trigger Enter] {other.name}", other.gameObject);
    }
}