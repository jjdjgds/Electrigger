using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゴールオブジェクトのスクリプト
/// </summary>
public class ExitGoal : MonoBehaviour
{
    // UI参照
    [Header("UI")]
    [SerializeField] private GameObject interactIcon;

    // アイコンオフセット
    [Header("Follow Target")]
    [SerializeField] private Vector3 iconOffset = new Vector3(0f, 1.5f, 0f);

    // クリアメニュー管理
    [Header("Clear Menu")]
    [SerializeField] private ClearMenuManager clearMenuManager;

    // 入力設定
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
                ClearStage();
            }
        }
    }

    // プレイヤーがゴール範囲に入ったとき
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
                // アイコン表示
                interactIcon.SetActive(true);
            }

            if (playerController != null)
            {
                // ジャンプ無効化
                playerController.SetJumpEnabled(false);
            }
        }
    }

    // プレイヤーがゴール範囲から出たとき
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

    private void ClearStage()
    {
        isCleared = true;

        if (interactIcon != null)
        {
            interactIcon.SetActive(false);
        }

        clearMenuManager.ShowClearMenu();

        Debug.Log("Level Clear!");
    }
}