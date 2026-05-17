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
    private Vector2 playerDragOffset;
    private bool playerShouldFollowDrag = false;
    private bool lastFrozenState = false;
    private static HashSet<Monitor_Drag> freezeRequesters = new HashSet<Monitor_Drag>();
    private static Monitor_Drag currentlyDragging = null;
    private Vector3 playerOffsetFromMonitor;

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

    private plugCollision[] plugCollisions;
    private socketCollision[] socketCollisions;

    private bool wasScreenOn = false;

    private MonitorPassengerController passengerController;

    //private bool frozePlayerForDrag = false;

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

        Debug.Log($"{gameObject.name} player found: {sharedPlayer != null}");

        myPowerNode = GetComponent<PowerNode>();
        plugCollisions = GetComponentsInChildren<plugCollision>();
        socketCollisions = GetComponentsInChildren<socketCollision>();
        clickAnimation = GetComponent<Monitor_ClickAnimation>();

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

            if (GetComponent<Collider2D>().OverlapPoint(worldPos))
            {
                isDragging = true;
                dragReady = false;
                dragDelayTimer = 0f;
                currentlyDragging = this;
                offset = transform.position - worldPos;
                lastValidPosition = transform.position;

                Cursor.lockState = CursorLockMode.Confined;

                if (myPowerNode != null)
                    myPowerNode.SetForcePowerDisabled(true);

                //FreezePlayerForDrag();

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

                PlayPickupSE();
                //RecheckAllConnections();  ¥É¥é¥Ã¥°é_Ê¼•r¤Ï½Ó¾A¥Á¥§¥Ã¥¯¤·¤Ê¤¤
            }
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            currentlyDragging = null;

            if (clickAnimation != null)
                clickAnimation.OnDragEnd();

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
            );
            worldPos.z = transform.position.z;

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

                        if (sharedPlayer != null && otherMonitor.playerInside)
                        {
                            Vector3 otherDelta = otherMonitor.transform.position - otherOldPos;
                            sharedPlayer.position += otherDelta;
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
            //UnfreezePlayerForDrag();
        }
        else if (isDragging && currentlyDragging == this)
        {
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
        freezeRequesters.Remove(this);

        if (myPowerNode != null)
            myPowerNode.SetForcePowerDisabled(false);

        Cursor.lockState = CursorLockMode.None;

        if (passengerController != null)
            passengerController.CancelPassengerImmediate();
    }

    /*void FreezePlayerForDrag()
    {
        if (sharedPlayerMovement == null || frozePlayerForDrag) return;

        sharedPlayerMovement.SetFrozen(true);
        frozePlayerForDrag = true;
    }

    void UnfreezePlayerForDrag()
    {
        if (sharedPlayerMovement == null || !frozePlayerForDrag) return;

        sharedPlayerMovement.SetFrozen(false, true);
        frozePlayerForDrag = false;
    }
    */

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
        if (sharedPlayer == null) return;

        Collider2D monitorCol = GetComponent<Collider2D>();
        Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();

        bool isPowered = myPowerNode != null && myPowerNode.IsPowered();
        Bounds monitorBounds = monitorCol.bounds;

        if (isPowered)
            monitorBounds.Expand(new Vector3(1.5f, 1.5f, 100f));

        if (playerCol != null)
            playerInside = monitorBounds.Intersects(playerCol.bounds);
        else
            playerInside = monitorBounds.Contains(sharedPlayer.position);
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
        Collider2D col = GetComponent<Collider2D>();
        Vector2 halfSize = col.bounds.extents;

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