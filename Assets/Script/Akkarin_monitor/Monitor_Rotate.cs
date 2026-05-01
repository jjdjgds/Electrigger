using UnityEngine;
using UnityEngine.InputSystem;

public class Monitor_Rotate : MonoBehaviour
{
    public float smoothSpeed = 5f;
    public float scrollCooldown = 0.15f;

    private float targetRotation;
    private float lastScroll;
    private bool canScroll = true;

    private Collider2D col;

    void Start()
    {
        col = GetComponent<Collider2D>();
        targetRotation = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;
    }

    void Update()
    {
        if (!canScroll) return;
        if (!IsMouseOver()) return;

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

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0, 0, targetRotation),
            Time.deltaTime * smoothSpeed
        );
    }

    bool IsMouseOver()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(mousePos);

        return col.OverlapPoint(world);
    }

    System.Collections.IEnumerator RotateStep(float amount)
    {
        canScroll = false;

        targetRotation += amount;

        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(0, 0, targetRotation);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * smoothSpeed;
            transform.rotation = Quaternion.Lerp(start, end, t);
            yield return null;
        }

        transform.rotation = end;

        Monitor_Drag drag = GetComponent<Monitor_Drag>();
        if (drag != null)
        {
            drag.RecheckAllConnections();
        }

        yield return new WaitForSeconds(scrollCooldown);

        canScroll = true;
    }
}
