using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// モニター内のプレイヤー追従を管理する
/// </summary>
public class MonitorPassengerController : MonoBehaviour
{
    [Header("Clamp")]
    [SerializeField] private float insidePadding = 0.3f;

    private static Transform sharedPlayer;
    private static Player2DController sharedPlayerController;
    private static Rigidbody2D sharedPlayerRb;
    private static Collider2D sharedPlayerCollider;

    private static readonly List<MonitorPassengerController> instances = new();

    private static bool isAnyMonitorDragFreezeActive = false;
    private static bool lastSharedFrozenState = false;

    private Collider2D monitorCollider;
    private PowerNode powerNode;

    private bool isPassengerActive = false;
    private bool isPassengerFreezeActive = false;
    private bool isPowerFreezeActive = false;
    private bool ignoreInsideCheckWhileMoving = false;

    private Vector3 playerLocalPosition;
    private Quaternion playerWorldRotation;

    public static MonitorPassengerController ActivePassengerMonitor { get; private set; }
    public static MonitorPassengerController PlayerOwnerMonitor { get; private set; }


    private void Awake()
    {
        monitorCollider = GetComponent<Collider2D>();
        powerNode = GetComponent<PowerNode>();
    }

    private void OnEnable()
    {
        if (!instances.Contains(this))
            instances.Add(this);
    }

    private void OnDisable()
    {
        instances.Remove(this);

        if (ActivePassengerMonitor == this)
            ActivePassengerMonitor = null;

        if (PlayerOwnerMonitor == this)
            PlayerOwnerMonitor = null;
    }

    private void Update()
    {
        if (!isPassengerActive)
        {
            UpdateOwnerByInsideCheck();
            UpdatePowerFreeze();
        }

        UpdateFreezeState();
    }



    // 全モニターで共有するプレイヤー参照を登録する。
    public static void RegisterPlayer(Transform playerTransform)
    {
        sharedPlayer = playerTransform;

        sharedPlayerController = null;
        sharedPlayerRb = null;
        sharedPlayerCollider = null;

        if (sharedPlayer == null)
            return;

        sharedPlayerController = sharedPlayer.GetComponent<Player2DController>();
        sharedPlayerRb = sharedPlayer.GetComponent<Rigidbody2D>();
        sharedPlayerCollider = sharedPlayer.GetComponent<Collider2D>();
    }

    // プレイヤー参照を更新する。
    public void SetPlayer(Transform newPlayer)
    {
        RegisterPlayer(newPlayer);
    }



    public static bool HasPlayerOwner()
    {
        return PlayerOwnerMonitor != null;
    }

    public bool IsPlayerOwner()
    {
        return PlayerOwnerMonitor == this;
    }

    public bool IsPassengerActive()
    {
        return isPassengerActive;
    }

    // プレイヤーがこのモニター内にいるかを判定する。
    public bool IsPlayerInside()
    {
        if (sharedPlayer == null || monitorCollider == null)
            return false;

        if (sharedPlayerCollider != null)
        {
            ColliderDistance2D dist = monitorCollider.Distance(sharedPlayerCollider);
            return dist.isOverlapped;
        }

        return monitorCollider.OverlapPoint(sharedPlayer.position);
    }

    // 任意のモニターがドラッグ中かどうかを全体凍結状態に反映する
    public static void SetAnyMonitorDragFreeze(bool shouldFreeze)
    {
        if (isAnyMonitorDragFreezeActive == shouldFreeze)
            return;

        isAnyMonitorDragFreezeActive = shouldFreeze;
        RefreshAllFreezeStates();
    }

    // 所有判定・電源判定・凍結状態を即時更新する。
    public void RefreshFreezeState()
    {
        UpdateOwnerByInsideCheck();
        UpdatePowerFreeze();
        UpdateFreezeState();
    }



    // モニター移動・回転中のプレイヤー追従を開始する。
    public void BeginPassenger(bool ignoreInsideCheck = true)
    {
        if (sharedPlayer == null || isPassengerActive)
            return;

        ActivePassengerMonitor = this;
        PlayerOwnerMonitor = this;

        ignoreInsideCheckWhileMoving = ignoreInsideCheck;

        isPassengerActive = true;

        playerLocalPosition = transform.InverseTransformPoint(sharedPlayer.position);
        playerWorldRotation = sharedPlayer.rotation;

        isPassengerFreezeActive = true;
        UpdateFreezeState();

        StopPlayerPhysicsForPassenger();
    }

    // モニター移動・回転中にプレイヤー位置を追従更新する。
    public void UpdatePassenger()
    {
        if (!isPassengerActive || sharedPlayer == null)
            return;

        // 移動中にプレイヤーがモニター内にいない場合は追従をキャンセル
        if (!ignoreInsideCheckWhileMoving && !IsPlayerInside())
        {
            CancelPassengerImmediate();
            return;
        }

        Vector3 targetWorldPos = transform.TransformPoint(playerLocalPosition);
        targetWorldPos.z = sharedPlayer.position.z;

        targetWorldPos = ClampInsideMonitor(targetWorldPos);

        // プレイヤーをモニターの動きに合わせて移動・回転させる
        sharedPlayer.position = targetWorldPos;
        sharedPlayer.rotation = playerWorldRotation;

        if (sharedPlayerRb != null)
        {
            sharedPlayerRb.rotation = playerWorldRotation.eulerAngles.z;
            ClearPlayerVelocity();
        }
    }

    // モニター追従終了
    public void EndPassenger()
    {
        if (!isPassengerActive)
            return;

        UpdatePassenger();

        isPassengerActive = false;
        ignoreInsideCheckWhileMoving = false;

        RestorePlayerInterpolation();
        StartCoroutine(RestorePhysicsNextFrame());
    }

    // 追従状態を即時解除する。
    public void CancelPassengerImmediate()
    {
        isPassengerActive = false;
        ignoreInsideCheckWhileMoving = false;

        if (ActivePassengerMonitor == this)
            ActivePassengerMonitor = null;

        if (PlayerOwnerMonitor == this)
            PlayerOwnerMonitor = null;

        isPassengerFreezeActive = false;
        isPowerFreezeActive = false;

        UpdateFreezeState();
        RestorePlayerInterpolation();
    }



    // 指定座標をモニター内に収める。
    public Vector3 ClampWorldPositionInside(Vector3 worldPos)
    {
        return ClampInsideMonitor(worldPos);
    }

    private Vector3 ClampInsideMonitor(Vector3 worldPos)
    {
        if (monitorCollider == null)
            return worldPos;

        Bounds b = monitorCollider.bounds;
        float padding = GetPlayerClampPadding();

        worldPos.x = Mathf.Clamp(worldPos.x, b.min.x + padding, b.max.x - padding);
        worldPos.y = Mathf.Clamp(worldPos.y, b.min.y + padding, b.max.y - padding);

        return worldPos;
    }

    private float GetPlayerClampPadding()
    {
        float padding = insidePadding;

        if (sharedPlayerCollider != null)
        {
            padding += Mathf.Max(
                sharedPlayerCollider.bounds.extents.x,
                sharedPlayerCollider.bounds.extents.y
            );
        }

        return padding;
    }



    // プレイヤーの現在位置から所有モニターを更新する。
    private void UpdateOwnerByInsideCheck()
    {
        if (sharedPlayer == null || monitorCollider == null)
            return;

        if (ActivePassengerMonitor != null && ActivePassengerMonitor != this)
            return;

        bool inside = IsPlayerInside();

        if (inside)
        {
            PlayerOwnerMonitor = this;
        }
        else if (PlayerOwnerMonitor == this && !lastSharedFrozenState)
        {
            PlayerOwnerMonitor = null;
        }

        if (ActivePassengerMonitor == this)
        {
            ActivePassengerMonitor = null;
        }
    }

    // 所有モニターの電源状態から凍結フラグを更新する。
    private void UpdatePowerFreeze()
    {
        if (PlayerOwnerMonitor != this || powerNode == null)
        {
            isPowerFreezeActive = false;
            return;
        }

        isPowerFreezeActive = !powerNode.IsPowered();

        //    Debug.Log(
        //$"[PowerFreeze] {gameObject.name} " +
        //$"Powered:{isPowered} Inside:{IsPlayerInside()} " +
        //$"Result:{shouldFreeze}"
        //);
    }

    // 全体状態からプレイヤーの最終凍結状態を適用する。
    private void UpdateFreezeState()
    {
        bool shouldFreeze = ShouldFreezeSharedPlayer();

        if (shouldFreeze == lastSharedFrozenState)
            return;

        lastSharedFrozenState = shouldFreeze;

        if (sharedPlayerController == null)
            return;

        if (shouldFreeze)
        {
            sharedPlayerController.SetFrozen(true);
        }
        else
        {
            sharedPlayerController.SetFrozen(false, true);
        }
    }

    // プレイヤーを凍結すべきかを全体状態から判定する。
    private static bool ShouldFreezeSharedPlayer()
    {
        if (isAnyMonitorDragFreezeActive)
            return true;

        if (
            ActivePassengerMonitor != null &&
            ActivePassengerMonitor.isPassengerFreezeActive
        )
        {
            return true;
        }

        if (PlayerOwnerMonitor != null)
        {
            PowerNode ownerPowerNode = PlayerOwnerMonitor.powerNode;

            if (ownerPowerNode != null && !ownerPowerNode.IsPowered())
                return true;
        }

        return false;
    }

    private static void RefreshAllFreezeStates()
    {
        foreach (var instance in instances)
        {
            if (instance != null)
                instance.RefreshFreezeState();
        }
    }



    // 追従開始時にプレイヤーの物理速度と補間を停止する。
    private void StopPlayerPhysicsForPassenger()
    {
        if (sharedPlayerRb == null)
            return;

        ClearPlayerVelocity();
        sharedPlayerRb.interpolation = RigidbodyInterpolation2D.None;
    }

    // 追従終了時にプレイヤーの物理補間を戻す。
    private void RestorePlayerInterpolation()
    {
        if (sharedPlayerRb == null)
            return;

        ClearPlayerVelocity();
        sharedPlayerRb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // プレイヤーの速度をゼロにする。
    private static void ClearPlayerVelocity()
    {
        if (sharedPlayerRb == null)
            return;

        sharedPlayerRb.linearVelocity = Vector2.zero;
        sharedPlayerRb.angularVelocity = 0f;
    }

    private System.Collections.IEnumerator RestorePhysicsNextFrame()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        isPassengerFreezeActive = false;

        UpdateOwnerByInsideCheck();
        UpdatePowerFreeze();
        UpdateFreezeState();

        ClearPlayerVelocity();

        if (!isPowerFreezeActive && ActivePassengerMonitor == this)
        {
            ActivePassengerMonitor = null;
        }
    }
}