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

    void Start()
    {
        cam = Camera.main;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement_Test>();
    }

    void Update()
    {
        CheckIfPlayerInside();

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

                if (playerMovement != null)
                    playerMovement.allowMovement = false;

                if (overlay != null)
                    overlay.SetActive(true);
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;

            if (playerMovement != null)
                playerMovement.allowMovement = true;

            if (overlay != null)
                overlay.SetActive(false);
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
            {
                player.position += delta;
            }
        }
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