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

    [Header("PowerOff")]
    public GameObject overlay;
    private PowerNode myPowerNode;
    public GridGenerator gridGenerator;
    public Vector3 lastValidPosition;

    [Header("Sound")]
    public AudioClip placeSE;         // Inspectorで効果音を設定
    private AudioSource audioSource;

    // 子オブジェクトのplugとsocketをキャッシュ
    private plugCollision[] plugCollisions;
    private socketCollision[] socketCollisions;

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

        // AudioSourceを自動取得 or 追加
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
                currentlyDragging = this;
                offset = transform.position - worldPos;
                lastValidPosition = transform.position;

                // Fresh live check right here, don't rely on cached playerInside
                if (sharedPlayer != null)
                {
                    Collider2D monitorCol = GetComponent<Collider2D>();
                    Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();
                    Bounds expanded = monitorCol.bounds;
                    expanded.Expand(new Vector3(3f, 3f, 100f));

                    playerShouldFollowDrag = playerCol != null
                        ? expanded.Intersects(playerCol.bounds)
                        : expanded.Contains(sharedPlayer.position);

                    Debug.Log($"{gameObject.name} drag start, playerShouldFollowDrag={playerShouldFollowDrag}");
                }

                RecheckAllConnections();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            currentlyDragging = null;

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

                    // ★ タイルへのスナップ成功時に効果音を再生
                    PlayPlaceSE();
                }
                else
                {
                    transform.position = lastValidPosition;
                }
            }

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
            Vector3 delta = clampedPos - transform.position;
            transform.position = clampedPos;

            if (playerShouldFollowDrag && sharedPlayer != null)
                sharedPlayer.transform.position += delta;
        }
    }

    void OnDestroy()
    {
        freezeRequesters.Remove(this);
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
        overlay.SetActive(isDragging || !isPowered);
    }

    void CheckIfPlayerInside()
    {
        if (sharedPlayer == null) return;
        Collider2D monitorCol = GetComponent<Collider2D>();
        Collider2D playerCol = sharedPlayer.GetComponent<Collider2D>();

        Bounds monitorBounds = monitorCol.bounds;
        // Expand bounds generously in all directions
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