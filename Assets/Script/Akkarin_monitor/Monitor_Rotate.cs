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

    private Collider2D[] allMonitorColliders;
    private Collider2D playerCollider;
    private bool isIgnoringPlayerCollision = false;
    private bool isRotationInProgress = false;

    void Awake()
    {
        col = GetComponent<Collider2D>();

        player = FindFirstObjectByType<Player2DController>();
        if (player != null)
            playerCollider = player.GetComponent<Collider2D>();

        targetRotation = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;

        passengerController = GetComponent<MonitorPassengerController>();
        if (passengerController == null)
            passengerController = gameObject.AddComponent<MonitorPassengerController>();

        allMonitorColliders = GetComponentsInChildren<Collider2D>(true);
    }

    void Update()
    {
        if (!canScroll) return;

        if (isRotationInProgress)
        {
            UpdateRotatePlayerCollisionState();
        }

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

        if (passengerStartedByRotate && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (passengerController != null && passengerController.IsPassengerActive())
                passengerController.EndPassenger();

            passengerStartedByRotate = false;
        }
    }

    bool IsRotateHeld()
    {
        if (Mouse.current == null) return false;
        if (!Mouse.current.leftButton.isPressed) return false;

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

        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        if (hits == null || hits.Length == 0) return false;

        foreach (Collider2D hit in hits)
        {
            if (hit != null && (hit.transform == transform || hit.transform.IsChildOf(transform)))
                return true;
        }

        return false;
    }

    System.Collections.IEnumerator RotateStep(float amount)
    {
        canScroll = false;
        isRotatingAnyMonitor = true;
        isRotationInProgress = true;

        bool ownsPlayer = passengerController != null && passengerController.IsPlayerInside();

        if (ownsPlayer)
        {
            passengerController.BeginPassenger(true);
            passengerStartedByRotate = true;
        }

        // IMPORTANT: ignore collision before any rotation happens
        UpdateRotatePlayerCollisionState();

        targetRotation += amount;

        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(0, 0, targetRotation);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * smoothSpeed;

            UpdateRotatePlayerCollisionState();

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

        Monitor_Collision collision = GetComponent<Monitor_Collision>();
        if (collision != null)
            collision.ForceRefreshWalls();

        foreach (var plug in GetComponentsInChildren<plugCollision>())
            plug.RecheckConnections();

        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(scrollCooldown);

        isRotationInProgress = false;
        isRotatingAnyMonitor = false;

        RestorePlayerCollision();
        canScroll = true;
    }

    void UpdateRotatePlayerCollisionState()
    {
        if (playerCollider == null)
            return;

        bool ownsPlayer =
            passengerController != null &&
            passengerController.IsPassengerActive();

        bool shouldIgnore = isRotationInProgress && !ownsPlayer;

        if (shouldIgnore == isIgnoringPlayerCollision)
            return;

        if (allMonitorColliders != null)
        {
            foreach (Collider2D monitorCol in allMonitorColliders)
            {
                if (monitorCol != null)
                    Physics2D.IgnoreCollision(monitorCol, playerCollider, shouldIgnore);
            }
        }

        isIgnoringPlayerCollision = shouldIgnore;
    }

    void RestorePlayerCollision()
    {
        if (playerCollider == null)
            return;

        if (allMonitorColliders != null)
        {
            foreach (Collider2D monitorCol in allMonitorColliders)
            {
                if (monitorCol != null)
                    Physics2D.IgnoreCollision(monitorCol, playerCollider, false);
            }
        }

        isIgnoringPlayerCollision = false;
    }

    void OnDestroy()
    {
        RestorePlayerCollision();
    }
}