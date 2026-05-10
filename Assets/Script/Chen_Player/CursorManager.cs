using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private GameObject normalCursor;
    [SerializeField] private GameObject dragCursor;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        Cursor.visible = false;
    }

    void Update()
    {
        UpdatePosition();
        UpdateView();
    }

    void UpdatePosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector3 worldPos = cam.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z))
        );

        worldPos.z = 0;

        transform.position = worldPos;
    }

    void UpdateView()
    {
        bool isDragging = Monitor_Drag.IsDraggingAny();

        normalCursor.SetActive(!isDragging);
        dragCursor.SetActive(isDragging);
    }
}