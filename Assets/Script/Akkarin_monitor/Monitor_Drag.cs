using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Monitor_Drag : MonoBehaviour
{
    public bool canDrag = true;
    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;
    private static Transform sharedPlayer;
    private static Player2DController sharedPlayerMovement;
    private bool playerInside = false;
    private Vector2 playerDragOffset;
    private bool playerShouldFollowDrag = false;
    private bool lastFrozenState = false;
    private static HashSet<Monitor_Drag> freezeRequesters = new HashSet<Monitor_Drag>();
    private static Monitor_Drag currentlyDragging = null;
    private Vector3 playerOffsetFromMonitor;

    private Monitor_ClickAnimation clickAnimation;

    [Header("PowerOff")]
    public GameObject overlay;
    public PowerNode myPowerNode;
    public GridGenerator gridGenerator;
    public Vector3 lastValidPosition;

    [Header("Sound")]
    public AudioClip pickupSE;        // 持ったときの効果音
    public AudioClip placeSE;         // 置いたときの効果音
    private AudioSource audioSource;
    public bool isPlaced = false;

    private plugCollision[] plugCollisions;
    private socketCollision[] socketCollisions;

    private bool wasScreenOn = false;

    void Start()
    {
        cam = Camera.main;
        var found = GameObject.FindGameObjectWithTag("Player");
        if (found != null)
        {
            sharedPlayer = found.transform;
            sharedPlayerMovement = found.GetComponent<Player2DController>();
        }
        Debug.Log($"{gameObject.name} player found: {sharedPlayer != null}");
        myPowerNode = GetComponent<PowerNode>();

        plugCollisions = GetComponentsInChildren<plugCollision>();
        socketCollisions = GetComponentsInChildren<socketCollision>();

        clickAnimation = GetComponent<Monitor_ClickAnimation>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        CheckIfPlayerInside();
        UpdateOverlay();
        bool isPowered = myPowerNode != null && myPowerNode.IsPowered();
        bool shouldFreeze = (isDragging && playerShouldFollowDrag) || Monitor_Rotate.isRotatingAnyMonitor || (!isPowered && playerInside);

        if (shouldFreeze)
            freezeRequesters.Add(this);
        else
            freezeRequesters.Remove(this);

        bool actualFreeze = freezeRequesters.Count > 0;
        if (sharedPlayerMovement != null && actualFreeze != lastFrozenState)
        {
            sharedPlayerMovement.SetFrozen(actualFreeze);
            lastFrozenState = actualFreeze;
        }

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

                if (clickAnimation != null)
                    clickAnimation.PlayClickAnimation();

                currentlyDragging = this;
                offset = transform.position - worldPos;
                lastValidPosition = transform.position;

                if (sharedPlayer != null)
                {
                    Collider2D monitorCol = GetComponent<Collider2D>();
                    Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();
                    Bounds expanded = monitorCol.bounds;
                    expanded.Expand(new Vector3(0f, 0f, 100f));

                    playerShouldFollowDrag = playerCol != null
                        ? expanded.Intersects(playerCol.bounds)
                        : expanded.Contains(sharedPlayer.position);

                    Debug.Log($"{gameObject.name} drag start, playerShouldFollowDrag={playerShouldFollowDrag}");

                    // Disable player collider to prevent physics conflict with overlapping monitors
                    if (playerShouldFollowDrag && playerCol != null)
                    {
                        playerCol.enabled = false;
                        playerOffsetFromMonitor = sharedPlayer.position - transform.position;
                    
                    }
                }

                //if (playerMovement != null)
                //    playerMovement.allowMovement = false;
                PlayPickupSE();
                RecheckAllConnections();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            currentlyDragging = null;

            // ★ Re-enable player collider and clamp player inside monitor on drop
            if (sharedPlayer != null)
            {
                Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();
                if (playerCol != null) playerCol.enabled = true;

                if (playerShouldFollowDrag)
                {
                    Collider2D monitorCol = GetComponent<Collider2D>();
                    Bounds b = monitorCol.bounds;
                    float push = 0.6f;
                    Vector3 playerPos = sharedPlayer.position;
                    playerPos.x = Mathf.Clamp(playerPos.x, b.min.x + push, b.max.x - push);
                    playerPos.y = Mathf.Clamp(playerPos.y, b.min.y + push, b.max.y - push);
                    sharedPlayer.position = playerPos;
                }
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
            );
            worldPos.z = transform.position.z;

            if (gridGenerator != null)
            {
                var nearest = gridGenerator.GetNearestTile(worldPos);

                if (nearest != null && gridGenerator.IsInsideGrid(worldPos))
                {
                    var otherMonitor = gridGenerator.GetMonitorOnTile(nearest, this);
                    if (otherMonitor != null && otherMonitor.gameObject != gameObject)
                    {
                        Debug.Log($"スワップ: {gameObject.name} ↔ {otherMonitor.gameObject.name}");
                        otherMonitor.transform.position = new Vector3(
                            lastValidPosition.x,
                            lastValidPosition.y,
                            otherMonitor.transform.position.z
                        );
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

            playerShouldFollowDrag = false;
            RecheckAllConnections();
        }

        if (isDragging && currentlyDragging == this)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
            );
            worldPos.z = transform.position.z;
            Vector3 clampedPos = ClampToScreen(worldPos + offset);
            transform.position = clampedPos;

            if (playerShouldFollowDrag && sharedPlayer != null)
            {
                // ★ Reapply fixed offset instead of += delta (no jump at high speed)
                Vector3 targetPlayerPos = transform.position + playerOffsetFromMonitor;

                // Clamp inside monitor
                Collider2D monitorCol = GetComponent<Collider2D>();
                Bounds b = monitorCol.bounds;
                float push = 0.6f;
                targetPlayerPos.x = Mathf.Clamp(targetPlayerPos.x, b.min.x + push, b.max.x - push);
                targetPlayerPos.y = Mathf.Clamp(targetPlayerPos.y, b.min.y + push, b.max.y - push);
                targetPlayerPos.z = sharedPlayer.position.z;

                sharedPlayer.position = targetPlayerPos;

                Rigidbody2D rb = sharedPlayer.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }
    }

    void OnDestroy()
    {
        freezeRequesters.Remove(this);
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

        Bounds monitorBounds = monitorCol.bounds;
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
}