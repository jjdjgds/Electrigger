using UnityEngine;
using UnityEngine.InputSystem;

public class ExitGoal : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactIcon;

    [Header("Follow Target")]
    [SerializeField] private Vector3 iconOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Input")]
    [SerializeField] public Key resetKey = Key.W;

    private Transform player;
    private Player2DController playerController;

    private bool playerInRange = false;
    private bool isCleared = false;

    private void Start()
    {
        if (interactIcon != null)
        {
            interactIcon.SetActive(false);
        }
    }

    private void Update()
    {
        if (isCleared) return;

        if (playerInRange && player != null)
        {
            UpdateIconPosition();

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[resetKey].wasPressedThisFrame)
            {
                ClearLevel();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCleared) return;

        if (collision.CompareTag("Player"))
        {
            player = collision.transform;
            playerController = collision.GetComponentInParent<Player2DController>();

            playerInRange = true;

            if (interactIcon != null)
            {
                interactIcon.SetActive(true);
            }

            if (playerController != null)
            {
                playerController.SetJumpEnabled(false);
            }
            else
            {
                Debug.LogWarning("Player2DController not found.");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerInRange = false;

        if (playerController != null)
        {
            playerController.SetJumpEnabled(true);
        }

        player = null;
        playerController = null;

        if (interactIcon != null)
        {
            interactIcon.SetActive(false);
        }
    }

    private void UpdateIconPosition()
    {
        if (interactIcon == null || player == null) return;

        interactIcon.transform.position = player.position + iconOffset;
    }

    private void ClearLevel()
    {
        isCleared = true;

        if (interactIcon != null)
        {
            interactIcon.SetActive(false);
        }

        LevelClearUI.Instance?.ShowClearUI();

        Debug.Log("Level Clear!");
    }
}