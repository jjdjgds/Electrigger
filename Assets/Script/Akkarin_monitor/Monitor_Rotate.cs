using UnityEngine;
using UnityEngine.InputSystem;

public class Monitor_Rotate : MonoBehaviour
{
    private Player2DController player;
    public float smoothSpeed = 5f;
    public float scrollCooldown = 0.15f;

    private float targetRotation;
    private float lastScroll;
    private bool canScroll = true;
    public static bool isRotatingAnyMonitor = false;

    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();

        player = FindFirstObjectByType<Player2DController>();

        if (player == null)
            Debug.LogError("Playerが見つからない");

        targetRotation = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;
    }

    void Update()
    {
        if (!canScroll) return;
        if (!IsRotateHeld()) return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0 && lastScroll <= 0)
        {
            StartCoroutine(RotateStep(-90));
        }
        else if (scroll < 0 && lastScroll >= 0)
        {
            StartCoroutine(RotateStep(90));
        }

        lastScroll = scroll;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0, 0, targetRotation),
            Time.deltaTime * smoothSpeed
        );
    }

    bool IsRotateHeld()
    {
        if (Mouse.current == null) return false;
        if (!Mouse.current.leftButton.isPressed) return false;

        return IsMouseOver();
    }

    bool IsMouseOver()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(Camera.main.transform.position.z))
        );
        world.z = transform.position.z;

        Collider2D hit = Physics2D.OverlapPoint(world);

        if (hit == null) return false;

        return hit.transform == transform || hit.transform.IsChildOf(transform);
    }

    System.Collections.IEnumerator RotateStep(float amount)
    {
        canScroll = false;
        isRotatingAnyMonitor = true;

        targetRotation += amount;

        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(0, 0, targetRotation);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * smoothSpeed;
            transform.rotation = Quaternion.Lerp(start, end, t);
            yield return null;
        }

        transform.rotation = end;

        Monitor_Drag drag = GetComponent<Monitor_Drag>();
        if (drag != null)
        {
            drag.RecheckAllConnections();
        }

        yield return new WaitForSeconds(scrollCooldown);

        isRotatingAnyMonitor = false;
        foreach (var plug in GetComponentsInChildren<plugCollision>())
            plug.RecheckConnections();

        canScroll = true;
    }
}