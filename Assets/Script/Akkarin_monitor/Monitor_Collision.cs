using UnityEngine;

public class Monitor_Collision : MonoBehaviour
{
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        CheckPlayerInside();
    }

    void CheckPlayerInside()
    {
        if (player == null) return;

        bool inside = GetComponent<Collider2D>().OverlapPoint(player.position);

        // You can expand this later (room switching, etc.)
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerMovement_Test>()
                 .SetBounds(GetComponent<Collider2D>().bounds);
        }
    }
}
