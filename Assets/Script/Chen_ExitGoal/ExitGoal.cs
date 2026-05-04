using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ･ｴｩ`･・ﾐｶｨ･ｯ･鬣ｹ
/// </summary>
public class ExitGoal : MonoBehaviour
{
    // UIｲﾎﾕﾕ
    [Header("UI")]
    [SerializeField] private GameObject interactIcon;

    // ･｢･､･ｳ･ｪ･ﾕ･ｻ･ﾃ･ﾈ
    [Header("Follow Target")]
    [SerializeField] private Vector3 iconOffset = new Vector3(0f, 1.5f, 0f);

    // ﾈ・ｦﾔOｶｨ
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

    // ･ﾗ･・､･茫`､ｬ･ｴｩ`･・ｨ･・｢､ﾋﾈ・ﾃ､ｿ､ﾈ､ｭ､ﾎИﾀ・
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
                // ･｢･､･ｳ･桄ｾ
                interactIcon.SetActive(true);
            }

            if (playerController != null)
            {
                // ･ｸ･罕ﾗ殪・ｻｯ
                playerController.SetJumpEnabled(false);
            }
        }
    }

    // ･ﾗ･・､･茫`､ｬ･ｴｩ`･・ｨ･・｢､ｫ､魑ｿ､ﾈ､ｭ､ﾎИﾀ・
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