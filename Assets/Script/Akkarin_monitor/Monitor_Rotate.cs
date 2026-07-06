using UnityEngine;
using UnityEngine.InputSystem;

public class Monitor_Rotate : MonoBehaviour
{
    private Player2DController player;
    private Collider2D playerCollider;
    private Collider2D col;
    private Collider2D[] allMonitorColliders;

    public float smoothSpeed = 5f;
    public float scrollCooldown = 0.15f;

    private float targetRotation;
    private float lastScroll;
    private bool canScroll = true;

    public static bool isRotatingAnyMonitor = false;

    private MonitorPassengerController passengerController;
    private bool passengerStartedByRotate = false;

    private bool isIgnoringPlayerCollision = false;
    private bool isRotationInProgress = false;

    private void Awake()
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

    private void Update()
    {
        if (!canScroll)
            return;

        if (isRotationInProgress)
            UpdateRotatePlayerCollisionState();

        if (!IsRotateHeld())
            return;

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
            if (
                passengerController != null &&
                passengerController.IsPassengerActive() &&
                passengerController.IsPlayerOwner()
            )
            {
                passengerController.EndPassenger();
            }

            passengerStartedByRotate = false;
        }
    }


    // Returns true while the rotate input condition is being held.
    // 左クリックを押したまま、このモニター上で回転入力可能な状態なら true を返します。
    private bool IsRotateHeld()
    {
        if (Mouse.current == null)
            return false;

        if (!Mouse.current.leftButton.isPressed)
            return false;

        Monitor_Drag myDrag = GetComponent<Monitor_Drag>();
        if (Monitor_Drag.IsDraggingAny() && !Monitor_Drag.IsDraggingThis(myDrag))
            return false;

        return IsMouseOver();
    }


    // Returns true if the mouse is currently over this monitor or one of its children.
    // マウスがこのモニター、またはその子オブジェクト上にある場合 true を返します。
    private bool IsMouseOver()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(Camera.main.transform.position.z))
        );
        world.z = transform.position.z;

        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        if (hits == null || hits.Length == 0)
            return false;

        foreach (Collider2D hit in hits)
        {
            if (hit != null && (hit.transform == transform || hit.transform.IsChildOf(transform)))
                return true;
        }

        return false;
    }


    // Rotates the monitor smoothly in 90-degree steps.
    // プレイヤー追従や衝突無視状態を維持しながら、
    // モニターを90度単位で滑らかに回転させます。
    private System.Collections.IEnumerator RotateStep(float amount)
    {
        canScroll = false;
        isRotatingAnyMonitor = true;
        isRotationInProgress = true;

        bool ownsPlayer = passengerController != null && passengerController.IsPlayerOwner();

        if (ownsPlayer)
        {
            passengerController.BeginPassenger(true);
            passengerStartedByRotate = true;
        }

        // Ignore collision before rotation starts.
        // 回転開始前にプレイヤーとの衝突を無視します。
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

        foreach (plugCollision plug in GetComponentsInChildren<plugCollision>())
            plug.RecheckConnections();

        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(scrollCooldown);

        isRotationInProgress = false;
        isRotatingAnyMonitor = false;

        RestorePlayerCollision();
        canScroll = true;
    }


    // Updates collision ignore state during rotation.
    // 回転中のプレイヤーとの衝突無視状態を更新します。
    private void UpdateRotatePlayerCollisionState()
    {
        if (playerCollider == null)
            return;

        bool ownsPlayer = passengerController != null && passengerController.IsPlayerOwner();
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


    // Restores collision between the monitor and the player.
    // モニターとプレイヤーの衝突判定を元に戻します。
    private void RestorePlayerCollision()
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

    private void OnDestroy()
    {
        RestorePlayerCollision();
    }
}