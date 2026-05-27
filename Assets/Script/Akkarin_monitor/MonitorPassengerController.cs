using System.ComponentModel;
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

    private Collider2D monitorCollider;

    private bool isPassengerActive;
    private Vector3 playerLocalPosition;
    private Quaternion playerWorldRotation;

    private PowerNode powerNode;

    private bool isPassengerFreezeActive = false;
    private bool isPowerFreezeActive = false;
    private bool lastFrozenState = false;

    public static MonitorPassengerController ActivePassengerMonitor { get; private set; }

    public static MonitorPassengerController PlayerOwnerMonitor { get; private set; }

    private bool ignoreInsideCheckWhileMoving = false;


    private void Awake()
    {
        monitorCollider = GetComponent<Collider2D>();
        powerNode = GetComponent<PowerNode>();
    }

    private void Update()
    {
        if (isPassengerActive)
            return;

        UpdatePowerFreeze();
    }

    private void UpdatePowerFreeze()
    {
        if (ActivePassengerMonitor != null && ActivePassengerMonitor != this)
            return;

        if (powerNode == null || sharedPlayer == null)
            return;

        bool inside = IsPlayerInside();

        if (inside)
            PlayerOwnerMonitor = this;
        else if (PlayerOwnerMonitor == this)
            PlayerOwnerMonitor = null;

        if (PlayerOwnerMonitor != this)
            return;

        bool shouldFreeze = !powerNode.IsPowered();

        if (shouldFreeze == isPowerFreezeActive)
            return;

        //    Debug.Log(
        //$"[PowerFreeze] {gameObject.name} " +
        //$"Powered:{isPowered} Inside:{IsPlayerInside()} " +
        //$"Result:{shouldFreeze}"
        //);

        isPowerFreezeActive = shouldFreeze;
        UpdateFreezeState();
    }

    private void UpdateFreezeState()
    {
        bool shouldFreeze =
            isPassengerFreezeActive
            || isPowerFreezeActive;

        if (shouldFreeze == lastFrozenState)
            return;

        lastFrozenState = shouldFreeze;

        if (sharedPlayerController != null)
        {
            if (shouldFreeze)
            {
                sharedPlayerController.SetFrozen(true);
            }
            else
            {
                sharedPlayerController.SetFrozen(false, true);
            }
        }
    }

    public static void RegisterPlayer(Transform playerTransform)
    {
        sharedPlayer = playerTransform;

        if (sharedPlayer != null)
        {
            sharedPlayerController =
                sharedPlayer.GetComponent<Player2DController>();

            sharedPlayerRb =
                sharedPlayer.GetComponent<Rigidbody2D>();

            sharedPlayerCollider =
                sharedPlayer.GetComponent<Collider2D>();
        }
    }

    public void SetPlayer(Transform newPlayer)
    {
        sharedPlayer = newPlayer;
        sharedPlayerController = sharedPlayer.GetComponent<Player2DController>();
        sharedPlayerRb = sharedPlayer.GetComponent<Rigidbody2D>();
        sharedPlayerCollider = sharedPlayer.GetComponent<Collider2D>();
    }

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

    // モニター追従開始
    public void BeginPassenger(bool ignoreInsideCheck = true)
    {
        if (sharedPlayer == null || isPassengerActive)
            return;

        ActivePassengerMonitor = this;
        ignoreInsideCheckWhileMoving = ignoreInsideCheck;

        isPassengerActive = true;

        playerLocalPosition = transform.InverseTransformPoint(sharedPlayer.position);
        playerWorldRotation = sharedPlayer.rotation;

        isPassengerFreezeActive = true;
        UpdateFreezeState();

        if (sharedPlayerRb != null)
        {
            sharedPlayerRb.linearVelocity = Vector2.zero;
            sharedPlayerRb.angularVelocity = 0f;
            sharedPlayerRb.interpolation = RigidbodyInterpolation2D.None;
        }
    }

    // モニター移動・回転中の追従更新
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
        }

        if (sharedPlayerRb != null)
        {
            sharedPlayerRb.linearVelocity = Vector2.zero;
            sharedPlayerRb.angularVelocity = 0f;
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

        if (sharedPlayerRb != null)
        {
            sharedPlayerRb.linearVelocity = Vector2.zero;
            sharedPlayerRb.angularVelocity = 0f;
            sharedPlayerRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        StartCoroutine(RestorePhysicsNextFrame());

        if (!isPowerFreezeActive && ActivePassengerMonitor == this)
        {
            ActivePassengerMonitor = null;
        }
    }

    public void CancelPassengerImmediate()
    {
        isPassengerActive = false;
        ignoreInsideCheckWhileMoving = false;

        if (ActivePassengerMonitor == this)
            ActivePassengerMonitor = null;

        isPassengerFreezeActive = false;
        UpdateFreezeState();

        if (sharedPlayerRb != null)
        {
            sharedPlayerRb.linearVelocity = Vector2.zero;
            sharedPlayerRb.angularVelocity = 0f;
            sharedPlayerRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private System.Collections.IEnumerator RestorePhysicsNextFrame()
    {
        yield return null;

        yield return new WaitForFixedUpdate();

        if (sharedPlayerController != null)
        {
            isPassengerFreezeActive = false;

            if (powerNode != null)
            {
                bool isPowered = powerNode.IsPowered();

                if (!isPowered)
                {
                    isPowerFreezeActive = true;
                }
                else
                {
                    isPowerFreezeActive = IsPlayerInside() ? false : false;
                }
            }

            UpdateFreezeState();

            UpdateFreezeState();
        }

        if (sharedPlayerRb != null)
        {
            sharedPlayerRb.linearVelocity = Vector2.zero;
            sharedPlayerRb.angularVelocity = 0f;
        }
    }

    private Vector3 ClampInsideMonitor(Vector3 worldPos)
    {
        if (monitorCollider == null)
            return worldPos;

        Bounds b = monitorCollider.bounds;

        float padding = insidePadding;

        if (sharedPlayerCollider != null)
        {
            padding += Mathf.Max(
                sharedPlayerCollider.bounds.extents.x,
                sharedPlayerCollider.bounds.extents.y
            );
        }

        worldPos.x = Mathf.Clamp(worldPos.x, b.min.x + padding, b.max.x - padding);
        worldPos.y = Mathf.Clamp(worldPos.y, b.min.y + padding, b.max.y - padding);

        return worldPos;
    }

    public Vector3 ClampWorldPositionInside(Vector3 worldPos)
    {
        return ClampInsideMonitor(worldPos);
    }


    public bool IsPassengerActive()
    {
        return isPassengerActive;
    }

    public static bool HasPlayerOwner()
    {
        return PlayerOwnerMonitor != null;
    }

    public bool IsPlayerOwner()
    {
        return PlayerOwnerMonitor == this;
    }

}