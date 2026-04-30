using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Monitor_Drag : MonoBehaviour
{
    public bool canDrag = true;
    private bool isDragging = false;
    private Vector3 offset;
    private Camera cam;
    private Transform player;
    private Player2DController playerMovement;
    private bool playerInside = false;
    private bool playerWasInsideAtDragStart = false;
    private bool lastFrozenState = false;

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
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerMovement = player.GetComponent<Player2DController>();

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
        bool shouldFreeze = isDragging || !isPowered || Monitor_Rotate.isRotatingAnyMonitor;

        if (playerMovement != null && shouldFreeze != lastFrozenState)
        {
            playerMovement.SetFrozen(shouldFreeze);
            lastFrozenState = shouldFreeze;
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
                offset = transform.position - worldPos;
                playerWasInsideAtDragStart = playerInside;
                lastValidPosition = transform.position;

                RecheckAllConnections();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

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

        if (isDragging)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
            );
            worldPos.z = transform.position.z;
            Vector3 rawPos = worldPos + offset;
            Vector3 clampedPos = ClampToScreen(rawPos);
            Vector3 delta = clampedPos - transform.position;
            transform.position = clampedPos;

            if (playerWasInsideAtDragStart && player != null)
                player.position += delta;
        }
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
        if (player == null) return;
        playerInside = GetComponent<Collider2D>().OverlapPoint(player.position);
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