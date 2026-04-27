using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement_Test : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 move;

    // Current room bounds
    private float minX, maxX, minY, maxY;

    public bool allowMovement = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        move = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) move.y += 1;
            if (Keyboard.current.sKey.isPressed) move.y -= 1;
            if (Keyboard.current.aKey.isPressed) move.x -= 1;
            if (Keyboard.current.dKey.isPressed) move.x += 1;
        }

        move = move.normalized;
    }

    void FixedUpdate()
    {
        if (!allowMovement) return;

        Vector2 newPos = rb.position + move * speed * Time.fixedDeltaTime;

        // Clamp BEFORE applying movement
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        rb.MovePosition(newPos);
    }

    // Called by room when player enters
    public void SetBounds(Bounds bounds)
    {
        // Get player size
        Collider2D col = GetComponent<Collider2D>();
        Vector2 halfSize = col.bounds.extents; // half width & height

        minX = bounds.min.x + halfSize.x;
        maxX = bounds.max.x - halfSize.x;
        minY = bounds.min.y + halfSize.y;
        maxY = bounds.max.y - halfSize.y;
    }
}
