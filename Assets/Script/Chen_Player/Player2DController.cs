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
    public float jumpCutMultiplier = 0.45f;   // 短押し時の速度カット倍率

    [Header("Gravity")]
    public float normalGravityScale = 3f;     // 通常時の重力
    public float jumpHoldGravityScale = 0f;   // ジャンプ長押し中の重力（ほぼ無重力）
    public float apexGravityScale = 1.2f;     // 頂点付近の重力
    public float fallGravityScale = 7f;       // 落下時の重力
    public float apexVelocityThreshold = 1.2f; // 頂点判定速度

    [Header("Ground Check")]
    public Vector2 groundCheckSize = new Vector2(0.6f, 0.1f); // 地面判定サイズ
    public LayerMask groundLayer; // 地面レイヤー

    [Header("Jump Assist")]
    public float coyoteTime = 0.12f;      // コヨーテタイム（離地後ジャンプ可能時間）
    public float jumpBufferTime = 0.12f;  // ジャンプバッファ（先行入力）

    private Rigidbody2D rb;
    private Collider2D playerCollider;

    private float moveInput;
    private float lastMoveDirection = 1f;
    private float moveRate;

    private bool jumpHeld;
    private bool jumpReleased;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isJumping;
    private bool isJumpHolding;

    private float jumpHoldTimer;
    private float coyoteCounter;
    private float jumpBufferCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();     // Rigidbody取得
        playerCollider = GetComponent<Collider2D>(); // Collider取得
    }

    void Update()
    {
        ReadInput();          // 入力取得
        UpdateJumpBuffer();   // ジャンプバッファ更新
    }

    void FixedUpdate()
    {
        CheckGround();        // 地面判定
        UpdateCoyoteTime();   // コヨーテタイム更新

        Move();               // 移動処理

        TryStartJump();       // ジャンプ開始判定
        HandleJumpHold();     // 長押しジャンプ
        HandleJumpCut();      // 短押しジャンプ
        ApplyJumpGravity();   // 重力適用

        wasGrounded = isGrounded;
        jumpReleased = false;
    }

    // 入力処理
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

        jumpHeld = keyboard.wKey.isPressed;

        if (keyboard.wKey.wasReleasedThisFrame)
            jumpReleased = true;

        if (keyboard.wKey.wasPressedThisFrame)
            jumpBufferCounter = jumpBufferTime;
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

        float xSpeed = lastMoveDirection * maxMoveSpeed * moveRate;
        rb.linearVelocity = new Vector2(xSpeed, rb.linearVelocity.y);
    }

    // ジャンプ開始判定
    void TryStartJump()
    {
        bool canJump = !isJumping && (isGrounded || coyoteCounter > 0f);

        if (jumpBufferCounter > 0f && canJump)
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

    // 地面判定（Colliderベース）
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

        RaycastHit2D hit = Physics2D.BoxCast(
            boxCenter,
            boxSize,
            0f,
            Vector2.down,
            0.05f,
            groundLayer
        );

        isGrounded = hit.collider != null;
    }

    // コヨーテタイム更新
    void UpdateCoyoteTime()
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;
    }

    // デバッグ用：地面判定表示
    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds bounds = col.bounds;

        Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y);
        Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, 0.08f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            boxCenter + Vector2.down * 0.05f,
            boxSize
        );
    }
}