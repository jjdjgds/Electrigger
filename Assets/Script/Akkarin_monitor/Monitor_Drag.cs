using System.Collections;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class Monitor_Drag : MonoBehaviour
{
    public bool canDrag = true;

    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;

    private static Transform sharedPlayer;
    private static Player2DController sharedPlayerMovement;
    private static Monitor_Drag currentlyDragging = null;

    public bool playerInside = false;
    private bool playerShouldFollowDrag = false;

    private float dragDelay = 0.15f;
    private float dragDelayTimer = 0f;
    private bool dragReady = false;

    private Monitor_ClickAnimation clickAnimation;

    [Header("PowerOff")]
    public GameObject overlay;
    public PowerNode myPowerNode;
    public GridGenerator gridGenerator;
    public Vector3 lastValidPosition;

    [Header("Sound")]
    public AudioClip pickupSE;
    public AudioClip placeSE;
    private AudioSource audioSource;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    public bool isPlaced = false;

    private bool wasScreenOn = false;

    private MonitorPassengerController passengerController;
    private Collider2D monitorCollider;
    private Collider2D[] allMonitorColliders;
    private bool isIgnoringPlayerCollision = false;

    private void Start()
    {
        cam = Camera.main;

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
        {
            sharedPlayer = found.transform;
            sharedPlayerMovement = found.GetComponent<Player2DController>();
            MonitorPassengerController.RegisterPlayer(sharedPlayer);
        }

        myPowerNode = GetComponent<PowerNode>();
        clickAnimation = GetComponent<Monitor_ClickAnimation>();
        monitorCollider = GetComponent<Collider2D>();
        allMonitorColliders = GetComponentsInChildren<Collider2D>(true);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (sfxMixerGroup != null)
            audioSource.outputAudioMixerGroup = sfxMixerGroup;

        passengerController = GetComponent<MonitorPassengerController>();
        if (passengerController == null)
            passengerController = gameObject.AddComponent<MonitorPassengerController>();
    }

    private void Update()
    {
        if (!PauseMenuManager.CanGameInput())
            return;

        CheckIfPlayerInside();
        UpdateOverlay();

        if (!canDrag)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryBeginDrag();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            EndDrag();
        }
        else if (isDragging && currentlyDragging == this)
        {
            UpdateDragging();
        }
    }

    private void OnDestroy()
    {
        RestorePlayerCollision();

        if (myPowerNode != null)
            myPowerNode.SetForcePowerDisabled(false);

        Cursor.lockState = CursorLockMode.None;

        if (passengerController != null)
            passengerController.CancelPassengerImmediate();
    }


    // Attempts to begin dragging if the mouse is currently over this monitor.
    // マウスがこのモニター上にある場合、ドラッグ開始を試みます。
    private void TryBeginDrag()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
        );
        worldPos.z = transform.position.z;

        if (monitorCollider == null || !monitorCollider.OverlapPoint(worldPos))
            return;

        isDragging = true;
        MonitorPassengerController.SetAnyMonitorDragFreeze(true);

        dragReady = false;
        dragDelayTimer = 0f;
        currentlyDragging = this;

        offset = transform.position - worldPos;
        lastValidPosition = transform.position;

        Cursor.lockState = CursorLockMode.Confined;

        if (myPowerNode != null)
            myPowerNode.SetForcePowerDisabled(true);

        if (clickAnimation != null)
        {
            clickAnimation.PlayClickAnimation();
            clickAnimation.OnDragStart();
        }

        playerShouldFollowDrag = false;

        if (passengerController != null && passengerController.IsPlayerInside())
        {
            playerShouldFollowDrag = true;
            passengerController.BeginPassenger(true);
        }

        UpdateDragPlayerCollisionState();
        PlayPickupSE();
    }


    // Handles dragging completion, placement, and post-drag recovery.
    // ドラッグ終了時の配置処理と後始末を行います。
    private void EndDrag()
    {
        isDragging = false;
        MonitorPassengerController.SetAnyMonitorDragFreeze(false);
        currentlyDragging = null;

        RestorePlayerCollision();

        if (clickAnimation != null)
            clickAnimation.OnDragEnd();

        if (gridGenerator != null)
        {
            var nearest = gridGenerator.GetNearestTile(transform.position);

            if (nearest != null && gridGenerator.IsInsideGrid(transform.position))
            {
                var otherMonitor = gridGenerator.GetMonitorOnTile(nearest, this);
                if (otherMonitor != null && otherMonitor.gameObject != gameObject)
                {
                    Vector3 otherOldPos = otherMonitor.transform.position;

                    otherMonitor.transform.position = new Vector3(
                        lastValidPosition.x,
                        lastValidPosition.y,
                        otherMonitor.transform.position.z
                    );

                    MonitorPassengerController otherPassengerController =
                        otherMonitor.GetComponent<MonitorPassengerController>();

                    if (
                        sharedPlayer != null &&
                        otherPassengerController != null &&
                        MonitorPassengerController.PlayerOwnerMonitor == otherPassengerController
                    )
                    {
                        Vector3 otherDelta = otherMonitor.transform.position - otherOldPos;
                        sharedPlayer.position += otherDelta;

                        otherPassengerController.RefreshFreezeState();
                    }

                    otherMonitor.lastValidPosition = otherMonitor.transform.position;
                }

                transform.position = new Vector3(
                    nearest.transform.position.x,
                    nearest.transform.position.y,
                    transform.position.z
                );

                lastValidPosition = transform.position;
                isPlaced = true;
                PlayPlaceSE();
            }
            else
            {
                isPlaced = false;
                transform.position = lastValidPosition;
            }
        }

        if (playerShouldFollowDrag && passengerController != null)
            passengerController.UpdatePassenger();

        StartCoroutine(EndDragAfterPowerCheck());
    }


    // Updates the object position while dragging and handles drag delay behavior.
    // ドラッグ中の位置更新とドラッグ開始ディレイ処理を行います。
    private void UpdateDragging()
    {
        UpdateDragPlayerCollisionState();

        if (!dragReady)
        {
            dragDelayTimer += Time.deltaTime;
            if (dragDelayTimer >= dragDelay)
            {
                dragReady = true;

                if (clickAnimation != null)
                    clickAnimation.StopShake();
            }

            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
        );
        worldPos.z = transform.position.z;

        transform.position = worldPos + offset;

        if (playerShouldFollowDrag && passengerController != null)
            passengerController.UpdatePassenger();
    }


    // Updates collision ignore state between the dragged monitor and the player.
    // ドラッグ中のモニターとプレイヤーの衝突無視状態を更新します。
    private void UpdateDragPlayerCollisionState()
    {
        if (sharedPlayer == null)
            return;

        Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();
        if (playerCol == null)
            return;

        bool ownsPlayer = passengerController != null && passengerController.IsPlayerOwner();
        bool shouldIgnore = isDragging && !ownsPlayer;

        if (shouldIgnore == isIgnoringPlayerCollision)
            return;

        if (allMonitorColliders != null)
        {
            foreach (Collider2D col in allMonitorColliders)
            {
                if (col != null)
                    Physics2D.IgnoreCollision(col, playerCol, shouldIgnore);
            }
        }

        isIgnoringPlayerCollision = shouldIgnore;
    }


    // Restores collision between the monitor and player if it was ignored.
    // 無視していたモニターとプレイヤーの衝突判定を元に戻します。
    private void RestorePlayerCollision()
    {
        if (!isIgnoringPlayerCollision || sharedPlayer == null)
            return;

        Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();
        if (playerCol == null)
            return;

        if (allMonitorColliders != null)
        {
            foreach (Collider2D col in allMonitorColliders)
            {
                if (col != null)
                    Physics2D.IgnoreCollision(col, playerCol, false);
            }
        }

        isIgnoringPlayerCollision = false;
    }


    // Plays the pickup sound effect.
    // 持ち上げ時の効果音を再生します。
    private void PlayPickupSE()
    {
        if (pickupSE != null && audioSource != null)
            audioSource.PlayOneShot(pickupSE);
    }


    // Plays the placement sound effect.
    // 配置時の効果音を再生します。
    private void PlayPlaceSE()
    {
        if (placeSE != null && audioSource != null)
            audioSource.PlayOneShot(placeSE);
    }


    // Requests a delayed recheck of nearby plug connections.
    // 近くのプラグ接続状態を次フレームで再確認します。
    public void RecheckAllConnections()
    {
        StartCoroutine(RecheckAfterFrame());
    }

    private IEnumerator RecheckAfterFrame()
    {
        yield return null;

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D col in nearbyColliders)
        {
            plugCollision nearbyPlug = col.GetComponent<plugCollision>();
            if (nearbyPlug != null)
                nearbyPlug.RecheckConnections();
        }
    }


    // Restores power and passenger state after dragging has completed.
    // ドラッグ完了後に通電状態と乗客追従状態を復帰します。
    private IEnumerator EndDragAfterPowerCheck()
    {
        if (myPowerNode != null)
            myPowerNode.SetForcePowerDisabled(false);

        RecheckAllConnections();

        yield return null;
        yield return new WaitForFixedUpdate();

        if (playerShouldFollowDrag && passengerController != null)
        {
            passengerController.UpdatePassenger();
            passengerController.EndPassenger();
        }

        playerShouldFollowDrag = false;

        if (MonitorPassengerController.PlayerOwnerMonitor != null)
            MonitorPassengerController.PlayerOwnerMonitor.RefreshFreezeState();

        Cursor.lockState = CursorLockMode.None;
    }


    // Updates the power-off overlay and plays power-on sound on transition.
    // 電源オフ用オーバーレイを更新し、通電開始時にサウンドを再生します。
    private void UpdateOverlay()
    {
        if (overlay == null)
            return;

        bool isPowered = myPowerNode != null && myPowerNode.IsPowered();
        bool isScreenOn = !isDragging && isPowered;

        overlay.SetActive(!isScreenOn);

        if (isScreenOn && !wasScreenOn)
        {
            if (myPowerNode != null)
                myPowerNode.PlayPowerOnSE();
        }

        wasScreenOn = isScreenOn;
    }


    // Checks whether the player is currently inside this monitor.
    // プレイヤーが現在このモニター内にいるかを確認します。
    private void CheckIfPlayerInside()
    {
        if (sharedPlayer == null || monitorCollider == null)
        {
            playerInside = false;
            return;
        }

        Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();

        if (playerCol != null)
        {
            ColliderDistance2D dist = monitorCollider.Distance(playerCol);
            playerInside = dist.isOverlapped;
        }
        else
        {
            playerInside = monitorCollider.OverlapPoint(sharedPlayer.position);
        }
    }


    // Updates the shared player reference.
    // 共有プレイヤー参照を更新します。
    public void SetPlayer(Transform newPlayer)
    {
        sharedPlayer = newPlayer;
        sharedPlayerMovement = newPlayer.GetComponent<Player2DController>();
    }


    // Clamps a target position so the monitor stays inside the camera bounds.
    // モニターがカメラ範囲内に収まるよう座標を制限します。
    private Vector3 ClampToScreen(Vector3 targetPos)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        if (monitorCollider == null)
            return targetPos;

        Vector2 halfSize = monitorCollider.bounds.extents;

        float minX = camPos.x - camWidth + halfSize.x;
        float maxX = camPos.x + camWidth - halfSize.x;
        float minY = camPos.y - camHeight + halfSize.y;
        float maxY = camPos.y + camHeight - halfSize.y;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        return targetPos;
    }


    // Returns true if any monitor is currently being dragged.
    // いずれかのモニターが現在ドラッグ中なら true を返します。
    public static bool IsDraggingAny()
    {
        return currentlyDragging != null;
    }


    // Returns true if the specified monitor is the one currently being dragged.
    // 指定したモニターが現在ドラッグ中の対象であれば true を返します。
    public static bool IsDraggingThis(Monitor_Drag target)
    {
        return currentlyDragging == target;
    }


    // Returns whether this monitor is currently being dragged.
    // このモニターが現在ドラッグ中かどうかを返します。
    public bool IsDragging()
    {
        return isDragging;
    }
}