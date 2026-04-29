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
    private PlayerMovement_Test playerMovement;
    private bool playerInside = false;
    private bool playerWasInsideAtDragStart = false;

    [Header("PowerOff")]
    public GameObject overlay;
    private PowerNode myPowerNode;
    public GridGenerator gridGenerator;
    private Vector3 lastValidPosition;

    // 子オブジェクトのplugとsocketをキャッシュ
    private plugCollision[] plugCollisions;
    private socketCollision[] socketCollisions;

    void Start()
    {
        cam = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement_Test>();

        myPowerNode = GetComponent<PowerNode>();

        // 子オブジェクトのplug/socketをキャッシュ
        plugCollisions = GetComponentsInChildren<plugCollision>();
        socketCollisions = GetComponentsInChildren<socketCollision>();
    }

    void Update()
    {
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
                offset = transform.position - worldPos;
                playerWasInsideAtDragStart = playerInside;
                lastValidPosition = transform.position;

                if (playerMovement != null)
                    playerMovement.allowMovement = false;

                // ✅ ドラッグ開始時に全接続を切断
                RecheckAllConnections();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            if (playerMovement != null)
                playerMovement.allowMovement = true;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
            );
            worldPos.z = transform.position.z;

            if (gridGenerator != null)
            {
                var nearest = gridGenerator.GetNearestTile(worldPos);
                //Debug.Log($"マウス離した位置: {worldPos}");
                //Debug.Log($"最近傍タイル: {nearest?.transform.position}");
                //Debug.Log($"盤面内判定: {gridGenerator.IsInsideGrid(worldPos)}");

                if (nearest != null && gridGenerator.IsInsideGrid(worldPos))
                {
                    transform.position = new Vector3(
                        nearest.transform.position.x,
                        nearest.transform.position.y,
                        transform.position.z
                    );
                    lastValidPosition = transform.position;
                    //Debug.Log($"スナップ先: {transform.position}");
                }
                else
                {
                    //Debug.Log($"戻す位置: {lastValidPosition}");
                    transform.position = lastValidPosition;
                }
            }

            //ドロップ後に接続を再チェック
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

    // Monitor_Drag.cs の RecheckAllConnections
    public void RecheckAllConnections()
    {
        StartCoroutine(RecheckAfterFrame());
    }

    IEnumerator RecheckAfterFrame()
    {
        yield return null;

        // ✅ plugCollisionだけ再チェック（socketは呼ばない）
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