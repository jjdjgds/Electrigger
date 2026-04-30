using UnityEngine;
using UnityEngine.InputSystem;

public class Player2DController : MonoBehaviour
{
    [Header("Move")]
    public float maxMoveSpeed = 6f;           // 最大移動速度
    public float accelerationTime = 0.2f;     // 加速にかかる時間
    public float decelerationTime = 0.2f;     // 減速にかかる時間

    [Header("Jump")]
    public float jumpStartSpeed = 5f;         // ジャンプ開始時の初速
    public float jumpHoldAcceleration = 35f;  // 長押し時の上昇加速度
    public float maxJumpHoldSpeed = 10f;      // 上昇速度の上限
    public float maxJumpHoldTime = 0.22f;     // 長押し可能時間
    public float jumpCutMultiplier = 0.45f;   // 短押し時に上昇速度を弱める倍率

    [Header("Gravity")]
    public float normalGravityScale = 3f;      // 通常時の重力
    public float jumpHoldGravityScale = 0f;    // 長押しジャンプ中の重力
    public float apexGravityScale = 1.2f;      // 頂点付近の重力
    public float fallGravityScale = 7f;        // 落下時の重力
    public float apexVelocityThreshold = 1.2f; // 頂点付近とみなすY速度

    [Header("Wall Check")]
    public Vector2 wallCheckOffset = new Vector2(0f, -0.2f);   // 壁判定位置の補正
    public Vector2 wallCheckBoxSize = new Vector2(0.06f, 0.4f); // 壁判定ボックスのサイズ

    [Header("Ground Check")]
    public Vector2 groundCheckSize = new Vector2(0.6f, 0.1f); // 地面判定ボックスのサイズ
    public float groundCheckDistance = 0.05f;                 // 下方向への判定距離
    public LayerMask groundLayer;                             // 通常地面レイヤー
    public LayerMask oneWayPlatformLayer;                     // 一方通行足場レイヤー

    [Header("Ledge Snap")]
    public float ledgeCheckDistance = 0.15f; // 横方向に足場を探す距離
    public float ledgeSnapOffset = 0.03f;    // 補正後に少し上へ浮かせる量
    public float ledgeBodyRatio = 0.25f;     // 下から1/4地点が足場より上なら補正対象
    public float ledgeTolerance = 0.1f;      // 判定の許容範囲

    [Header("Jump Assist")]
    public float coyoteTime = 0.12f;     // 離地後もジャンプ可能な時間
    public float jumpBufferTime = 0.12f; // ジャンプ先行入力時間

    private Rigidbody2D rb;
    private Collider2D playerCollider;

    private float moveInput;
    private float lastMoveDirection = 1f;
    private float moveRate;

    private bool jumpHeld;
    private bool jumpReleased;

    private bool isGrounded;
    private bool isJumping;
    private bool isJumpHolding;

    private bool canJump = true;

    private bool touchingWallLeft;
    private bool touchingWallRight;

    private float jumpHoldTimer;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private bool isFrozen = false;
    private Vector2 storedVelocity;
    private float storedGravity;
    private bool colliderStateBeforeFreeze;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (isFrozen) return;

        ReadInput();
        UpdateJumpBuffer();
    }

    void FixedUpdate()
    {
        if (isFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            rb.MovePosition(rb.position);
            return;
        }

        CheckWall();        // 壁判定
        CheckGround();        // 地面判定
        UpdateCoyoteTime();   // コヨーテタイム更新

        Move();               // 移動処理
        HandleLedgeSnap();   // レッジスナップ

        TryStartJump();       // ジャンプ開始判定
        HandleJumpHold();     // 長押しジャンプ
        HandleJumpCut();      // 短押しジャンプ
        ApplyJumpGravity();   // 重力適用

        jumpReleased = false;
    }

    // 入力取得
    void ReadInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        moveInput = 0f;

        if (keyboard.aKey.isPressed)
            moveInput = -1f;
        else if (keyboard.dKey.isPressed)
            moveInput = 1f;

        if (moveInput != 0f)
            lastMoveDirection = moveInput;

        if (canJump)
        {
            jumpHeld = keyboard.wKey.isPressed;

            if (keyboard.wKey.wasReleasedThisFrame)
                jumpReleased = true;

            if (keyboard.wKey.wasPressedThisFrame)
                jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpHeld = false;
            jumpReleased = false;
            jumpBufferCounter = 0f;
        }
    }

    // ジャンプバッファ更新
    void UpdateJumpBuffer()
    {
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    // 移動処理（加速・減速）
    void Move()
    {
        if (moveInput != 0f)
            moveRate += Time.fixedDeltaTime / accelerationTime;
        else
            moveRate -= Time.fixedDeltaTime / decelerationTime;

        moveRate = Mathf.Clamp01(moveRate);

        float targetSpeed = lastMoveDirection * maxMoveSpeed * moveRate;

        // 壁に接触している方向への移動を制限
        if ((targetSpeed < 0 && touchingWallLeft) ||
            (targetSpeed > 0 && touchingWallRight))
        {
            targetSpeed = 0f;
        }

        // 空中での移動は減速させる
        if (!isGrounded)
        {
            targetSpeed = Mathf.Lerp(
                rb.linearVelocity.x,
                targetSpeed,
                0.3f
            );
        }

        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    // ジャンプ開始判定
    void TryStartJump()
    {
        if (!canJump) return;

        bool canStartJump = !isJumping && (isGrounded || coyoteCounter > 0f);

        if (jumpBufferCounter > 0f && canStartJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpStartSpeed);

            isJumping = true;
            isJumpHolding = true;
            jumpHoldTimer = 0f;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    // 長押しジャンプ処理
    void HandleJumpHold()
    {
        if (!isJumpHolding) return;

        jumpHoldTimer += Time.fixedDeltaTime;

        bool holdTimeOver = jumpHoldTimer >= maxJumpHoldTime;
        bool speedOver = rb.linearVelocity.y >= maxJumpHoldSpeed;

        if (!jumpHeld || holdTimeOver || speedOver)
        {
            isJumpHolding = false;
            return;
        }

        float newY = rb.linearVelocity.y + jumpHoldAcceleration * Time.fixedDeltaTime;
        newY = Mathf.Min(newY, maxJumpHoldSpeed);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
    }

    // 短押しジャンプ（ジャンプカット）
    void HandleJumpCut()
    {
        if (!jumpReleased) return;

        if (rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }

        isJumpHolding = false;
    }

    // 状態に応じた重力制御
    void ApplyJumpGravity()
    {
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            rb.gravityScale = normalGravityScale;
            isJumping = false;
            isJumpHolding = false;
            return;
        }

        if (isJumpHolding)
        {
            rb.gravityScale = jumpHoldGravityScale;
        }
        else if (Mathf.Abs(rb.linearVelocity.y) <= apexVelocityThreshold)
        {
            rb.gravityScale = apexGravityScale;
        }
        else if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = fallGravityScale;
        }
        else
        {
            rb.gravityScale = normalGravityScale;
        }
    }

    // 地面判定
    void CheckGround()
    {
        if (playerCollider == null)
        {
            isGrounded = false;
            return;
        }

        Bounds bounds = playerCollider.bounds;

        Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, 0.08f);

        LayerMask checkLayer = groundLayer | oneWayPlatformLayer;

        RaycastHit2D hit = Physics2D.BoxCast(
            boxCenter,
            boxSize,
            0f,
            Vector2.down,
            0.05f,
            checkLayer
        );

        isGrounded = hit.collider != null;
    }

    // 壁判定
    void CheckWall()
    {
        touchingWallLeft = false;
        touchingWallRight = false;

        if (playerCollider == null) return;

        Bounds bounds = playerCollider.bounds;

        float dirValue = moveInput != 0f ? moveInput : lastMoveDirection;
        Vector2 direction = dirValue > 0f ? Vector2.right : Vector2.left;

        float sideX = direction.x > 0f ? bounds.max.x : bounds.min.x;

        Vector2 origin = new Vector2(sideX, bounds.center.y) + wallCheckOffset;
        Vector2 boxCenter = origin + direction * (wallCheckBoxSize.x * 0.5f);

        Collider2D hit = Physics2D.OverlapBox(
            boxCenter,
            wallCheckBoxSize,
            0f,
            groundLayer | oneWayPlatformLayer
        );

        if (hit == null) return;

        if (direction.x > 0f) touchingWallRight = true;
        else touchingWallLeft = true;
    }

    // コヨーテタイム更新
    void UpdateCoyoteTime()
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;
    }

    // レッジスナップ処理
    void HandleLedgeSnap()
    {
        if (isGrounded) return;
        if (rb.linearVelocity.y > 0.2f) return;
        if (moveInput == 0f) return;
        if (playerCollider == null) return;

        Bounds bounds = playerCollider.bounds;

        Vector2 direction = moveInput > 0f ? Vector2.right : Vector2.left;
        float sideX = direction.x > 0f ? bounds.max.x : bounds.min.x;

        Vector2 boxCenter = new Vector2(
            sideX + direction.x * (ledgeCheckDistance * 0.5f),
            bounds.center.y
        );

        boxCenter.y = bounds.min.y + bounds.size.y * 0.25f;

        Vector2 boxSize = new Vector2(
            ledgeCheckDistance,
            bounds.size.y * 0.6f
        );

        Collider2D platform = Physics2D.OverlapBox(
            boxCenter,
            boxSize,
            0f,
            oneWayPlatformLayer | groundLayer
        );

        if (platform == null) return;

        float platformTopY = platform.bounds.max.y;

        float quarterPointY = bounds.min.y + bounds.size.y * ledgeBodyRatio;
        if (quarterPointY < platformTopY - ledgeTolerance) return;

        float targetY = platformTopY + bounds.extents.y + ledgeSnapOffset;

        Collider2D blocked = Physics2D.OverlapBox(
            new Vector2(rb.position.x, targetY),
            bounds.size * 0.95f,
            0f,
            groundLayer | oneWayPlatformLayer
        );

        if (blocked != null) return;

        rb.position = new Vector2(rb.position.x, targetY);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        isGrounded = true;
        isJumping = false;
        isJumpHolding = false;
        coyoteCounter = coyoteTime;

        Physics2D.IgnoreCollision(playerCollider, platform, false);
    }

    public void SetJumpEnabled(bool enabled)
    {
        canJump = enabled;

        if (!canJump)
        {
            jumpHeld = false;
            jumpReleased = false;
            jumpBufferCounter = 0f;
            isJumpHolding = false;
        }
    }

    // デバッグ用

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        if (frozen)
        {
            storedVelocity = rb.linearVelocity;
            storedGravity = rb.gravityScale;

            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;

            if (playerCollider != null)
            {
                colliderStateBeforeFreeze = playerCollider.enabled;
                playerCollider.enabled = false;
            }

        }
        else
        {
            rb.linearVelocity = storedVelocity;
            rb.gravityScale = storedGravity;

            if (playerCollider != null)
            {
                playerCollider.enabled = colliderStateBeforeFreeze;
            }
        }
    }

}