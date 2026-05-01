using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player")]
    public GameObject playerPrefab; // 生成するプレイヤーのPrefab

    [Header("Input")]
    public Key resetKey = Key.R; // リセットに使用するキー

    private GameObject currentPlayer; // 現在シーン上に存在するプレイヤー

    void Start()
    {
        // ゲーム開始時にプレイヤーを生成
        SpawnPlayer();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 指定キーが押された瞬間にリセット処理
        if (keyboard[resetKey].wasPressedThisFrame)
        {
            ResetPlayer();
        }
    }

    // プレイヤーを削除して再生成
    void ResetPlayer()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer); // 既存プレイヤーを削除
        }

        SpawnPlayer(); // 新しく生成
    }

    // プレイヤー生成処理
    void SpawnPlayer()
    {
        if (playerPrefab == null) return;

        currentPlayer = Instantiate(playerPrefab, transform.position, transform.rotation);

        // Notify all monitors of new player
        foreach (var monitor in FindObjectsByType<Monitor_Drag>(FindObjectsSortMode.None))
        {
            monitor.SetPlayer(currentPlayer.transform);
        }
    }
}