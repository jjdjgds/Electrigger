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

    private MonitorPassengerController passengerController;
    private bool passengerStartedByRotate = false;

    void Awake()
    {
        col = GetComponent<Collider2D>();

        player = FindFirstObjectByType<Player2DController>();

        if (player == null)
            Debug.LogError("Playerが見つからない");

        targetRotation = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;

        passengerController = GetComponent<MonitorPassengerController>();

        if (passengerController == null)
            passengerController = gameObject.AddComponent<MonitorPassengerController>();
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

        if (passengerStartedByRotate && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (passengerController != null && passengerController.IsPassengerActive())
                passengerController.EndPassenger();

            passengerStartedByRotate = false;
            isRotatingAnyMonitor = false;
            return;
        }
    }

    bool IsRotateHeld()
    {
        if (Mouse.current == null) return false;
        if (!Mouse.current.leftButton.isPressed) return false;

        // 他のモニターをドラッグ中なら回転しない
        Monitor_Drag myDrag = GetComponent<Monitor_Drag>();
        if (Monitor_Drag.IsDraggingAny() && !Monitor_Drag.IsDraggingThis(myDrag))
            return false;

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

    //System.Collections.IEnumerator RotateStep(float amount)
    //{
    //    canScroll = false;
    //    isRotatingAnyMonitor = true;

    //    targetRotation += amount;

    //    Quaternion start = transform.rotation;
    //    Quaternion end = Quaternion.Euler(0, 0, targetRotation);

    //    float t = 0f;

    //    while (t < 1f)
    //    {
    //        t += Time.deltaTime * smoothSpeed;
    //        transform.rotation = Quaternion.Lerp(start, end, t);
    //        yield return null;
    //    }

    //    transform.rotation = end;

    //    Monitor_Drag drag = GetComponent<Monitor_Drag>();
    //    if (drag != null)
    //    {
    //        drag.RecheckAllConnections();
    //    }

    //    yield return new WaitForSeconds(scrollCooldown);

    //    isRotatingAnyMonitor = false;
    //    foreach (var plug in GetComponentsInChildren<plugCollision>())
    //        plug.RecheckConnections();

    //    canScroll = true;
    //}

    System.Collections.IEnumerator RotateStep(float amount)
    {
        canScroll = false;
        isRotatingAnyMonitor = true;

        if (passengerController != null && passengerController.IsPlayerInside())
        {
            passengerController.BeginPassenger(true);
            passengerStartedByRotate = true;
        }

        targetRotation += amount;

        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(0, 0, targetRotation);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * smoothSpeed;

            transform.rotation = Quaternion.Lerp(start, end, t);

            if (passengerController != null && passengerController.IsPassengerActive())
                passengerController.UpdatePassenger();

            yield return null;
        }

        transform.rotation = end;

        if (passengerController != null && passengerController.IsPassengerActive())
            passengerController.UpdatePassenger();

        Monitor_Drag drag = GetComponent<Monitor_Drag>();
        if (drag != null)
            drag.RecheckAllConnections();

        yield return new WaitForSeconds(scrollCooldown);

        isRotatingAnyMonitor = false;

        foreach (var plug in GetComponentsInChildren<plugCollision>())
            plug.RecheckConnections();

        canScroll = true;
    }
}