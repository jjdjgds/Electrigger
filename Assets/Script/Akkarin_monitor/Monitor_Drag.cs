using System.Collections;
using System.Collections.Generic;
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
    public bool playerInside = false;
    private bool playerShouldFollowDrag = false;
    private static Monitor_Drag currentlyDragging = null;

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

    void Start()
    {
        cam = Camera.main;

        var found = GameObject.FindGameObjectWithTag("Player");
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

    void Update()
    {
        if (!PauseMenuManager.CanGameInput()) return;

        CheckIfPlayerInside();
        UpdateOverlay();

        if (!canDrag) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
            );
            worldPos.z = transform.position.z;

            if (monitorCollider != null && monitorCollider.OverlapPoint(worldPos))
            {
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
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
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
            {
                passengerController.UpdatePassenger();
            }

            StartCoroutine(EndDragAfterPowerCheck());
        }
        else if (isDragging && currentlyDragging == this)
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
            {
                passengerController.UpdatePassenger();
            }
        }
    }

    void OnDestroy()
    {
        RestorePlayerCollision();

        if (myPowerNode != null)
            myPowerNode.SetForcePowerDisabled(false);

        Cursor.lockState = CursorLockMode.None;

        if (passengerController != null)
            passengerController.CancelPassengerImmediate();
    }

    void UpdateDragPlayerCollisionState()
    {
        if (sharedPlayer == null)
            return;

        Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();
        if (playerCol == null)
            return;

        bool ownsPlayer =
            passengerController != null &&
            passengerController.IsPlayerOwner();

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

    void RestorePlayerCollision()
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

    void PlayPickupSE()
    {
        if (pickupSE != null && audioSource != null)
            audioSource.PlayOneShot(pickupSE);
    }

    void PlayPlaceSE()
    {
        if (placeSE != null && audioSource != null)
            audioSource.PlayOneShot(placeSE);
    }

    public void RecheckAllConnections()
    {
        StartCoroutine(RecheckAfterFrame());
    }

    IEnumerator RecheckAfterFrame()
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

    IEnumerator EndDragAfterPowerCheck()
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

        if(MonitorPassengerController.PlayerOwnerMonitor != null)
        {
            MonitorPassengerController.PlayerOwnerMonitor.RefreshFreezeState();
        }

        Cursor.lockState = CursorLockMode.None;
    }

    void UpdateOverlay()
    {
        if (overlay == null) return;

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

    void CheckIfPlayerInside()
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

    public void SetPlayer(Transform newPlayer)
    {
        sharedPlayer = newPlayer;
        sharedPlayerMovement = newPlayer.GetComponent<Player2DController>();
    }

    Vector3 ClampToScreen(Vector3 targetPos)
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

    public static bool IsDraggingAny()
    {
        return currentlyDragging != null;
    }

    public static bool IsDraggingThis(Monitor_Drag target)
    {
        return currentlyDragging == target;
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}