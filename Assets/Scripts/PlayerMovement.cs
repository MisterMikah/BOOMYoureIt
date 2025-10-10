using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Player")]
    public int playerNumber = 1; // 1 = Player1 input map, 2 = Player2 input map

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public int maxJumps = 1;

    [Header("Ground Check")]
    public Transform groundCheck; // Empty GameObject at player’s feet
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Jump Feel")]
    public float coyoteTime = 0.1f;      // grace after leaving ground
    public float jumpBufferTime = 0.1f;  // buffer before landing

    [Header("Air / Fast-Fall")]
    public float gravityScale = 2.8f;
    public float fastFallAccel = 60f;    // extra downward accel when holding down
    public float maxFallSpeed = -20f;    // clamp downward velocity

    [Header("Wall Movement")]
    public float wallClingTime = 0.3f;   // short cling before sliding
    public float wallSlideSpeed = -4f;   // slide speed after cling
    public Vector2 wallJumpImpulse = new Vector2(7f, 10f);
    public LayerMask wallLayer;
    public float wallFrictionLerp = 12f; // how quickly X damps toward 0 while holding wall
    public bool unlimitedWallJumps = true; // unlimited jumps while holding toward wall

    [Header("Visuals (Flip Container)")]
    public Transform graphics; // child holding all sprites/arrow

    // components and state
    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 moveInput;
    private bool jumpPressed;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private int jumpsRemaining;
    private int facingDir = 1;
    private int lastFacingDir = 1;
    private float wallContactTimer;
    private Vector3 graphicsBaseScale;

    // input setup
    void Awake()
    {
        controls = new Controls();
    }

    // enable the correct input map
    void OnEnable()
    {
        if (playerNumber == 1)
        {
            controls.Player1.Enable();
            controls.Player1.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Player1.Move.canceled += ctx => moveInput = Vector2.zero;
            controls.Player1.Jump.performed += ctx => jumpPressed = true;
        }
        else
        {
            controls.Player2.Enable();
            controls.Player2.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Player2.Move.canceled += ctx => moveInput = Vector2.zero;
            controls.Player2.Jump.performed += ctx => jumpPressed = true;
        }
    }

    // disable maps when not in use
    void OnDisable()
    {
        if (playerNumber == 1) controls.Player1.Disable();
        if (playerNumber == 2) controls.Player2.Disable();
    }

    // physics defaults and graphics cache
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (graphics != null) graphicsBaseScale = graphics.localScale;

        jumpsRemaining = maxJumps;
    }

    // input, facing, flipping, and jump timers
    void Update()
    {
        // facing direction from horizontal input
        if (moveInput.x > 0.1f) facingDir = 1;
        else if (moveInput.x < -0.1f) facingDir = -1;

        // flip only the graphics child so colliders stay stable
        if (graphics != null && facingDir != lastFacingDir)
        {
            float sign = facingDir == 1 ? 1f : -1f;
            graphics.localScale = new Vector3(
                Mathf.Abs(graphicsBaseScale.x) * sign,
                graphicsBaseScale.y,
                graphicsBaseScale.z
            );
            lastFacingDir = facingDir;
        }

        // grounded + coyote time refill
        if (IsGrounded())
        {
            lastGroundedTime = coyoteTime;
            jumpsRemaining = maxJumps;
            wallContactTimer = 0f;
        }
        else
        {
            lastGroundedTime -= Time.deltaTime;
        }

        // jump buffer (consume press)
        if (jumpPressed)
        {
            lastJumpPressedTime = jumpBufferTime;
            jumpPressed = false;
        }
        else
        {
            lastJumpPressedTime -= Time.deltaTime;
        }
    }

    // movement, wall behavior, jumps, and fall physics
    void FixedUpdate()
    {
        // horizontal movement
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // detect states
        bool grounded = IsGrounded();
        var wallInfo = CheckWallFacingSizeAware();
        bool onWall = wallInfo.onWall;
        int wallDir = wallInfo.dir;

        // must be holding into the wall for any wall behavior
        bool pressingTowardWall = Mathf.Abs(moveInput.x) > 0.1f && Mathf.Sign(moveInput.x) == facingDir;
        bool canCling = !grounded && onWall && pressingTowardWall;

        // wall cling/slide only while holding the wall
        if (canCling)
        {
            wallContactTimer += Time.fixedDeltaTime;

            // horizontal wall "friction": damp X toward 0 while holding the wall
            float dampX = Mathf.Lerp(rb.linearVelocity.x, 0f, wallFrictionLerp * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(dampX, rb.linearVelocity.y);

            // brief cling, then slide; use Max (numbers are negative) to cap downward speed
            if (wallContactTimer < wallClingTime)
            {
                if (rb.linearVelocity.y < -0.2f)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -0.2f);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, wallSlideSpeed));
                // If you prefer a constant slide speed instead of a cap:
                // rb.velocity = new Vector2(rb.velocity.x, wallSlideSpeed);
            }
        }
        else
        {
            wallContactTimer = 0f;
        }

        // jumps: unlimited wall jumps (when enabled) take priority over ground/coyote
        if (lastJumpPressedTime > 0f)
        {
            bool canWallJump = onWall && !grounded && pressingTowardWall;
            bool canCoyoteJump = (lastGroundedTime > 0f) && (jumpsRemaining > 0);

            if (canWallJump && unlimitedWallJumps)
            {
                rb.linearVelocity = Vector2.zero;
                Vector2 impulse = new Vector2(-wallDir * wallJumpImpulse.x, wallJumpImpulse.y);
                rb.AddForce(impulse, ForceMode2D.Impulse);

                // prevent a free mid-air jump after the wall jump
                jumpsRemaining = 0;

                lastJumpPressedTime = 0f;
                wallContactTimer = 0f;
            }
            else if (canWallJump && !unlimitedWallJumps && jumpsRemaining > 0)
            {
                rb.linearVelocity = Vector2.zero;
                Vector2 impulse = new Vector2(-wallDir * wallJumpImpulse.x, wallJumpImpulse.y);
                rb.AddForce(impulse, ForceMode2D.Impulse);

                jumpsRemaining--;
                lastJumpPressedTime = 0f;
                wallContactTimer = 0f;
            }
            else if (canCoyoteJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

                jumpsRemaining--;
                lastJumpPressedTime = 0f;
            }
        }

        // fast-fall only when NOT clinging/sliding on a wall
        bool pressingDown = moveInput.y < -0.5f;
        if (!grounded && !canCling && pressingDown)
        {
            float newVy = rb.linearVelocity.y - fastFallAccel * Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(newVy, maxFallSpeed));
        }
        else if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }

    // ground check
    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    // facing-side wall detection that scales with collider size
    (bool onWall, int dir) CheckWallFacingSizeAware()
    {
        var col = GetComponent<Collider2D>();
        Bounds b = col.bounds;

        Vector2 boxSize = new Vector2(Mathf.Max(0.04f, b.extents.x * 0.25f), b.size.y * 0.6f);
        float xOff = boxSize.x * 0.6f * facingDir;
        Vector2 boxCenter = new Vector2((facingDir == 1 ? b.max.x : b.min.x) + xOff, b.center.y);

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.right * facingDir, 0f, wallLayer);
        if (hit.collider != null && !IsGrounded()) return (true, facingDir);
        return (false, 0);
    }

    // gizmos to visualize ground check & wall box
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            int dir = Application.isPlaying ? facingDir : 1;
            Vector2 boxSize = new Vector2(Mathf.Max(0.04f, b.extents.x * 0.25f), b.size.y * 0.6f);
            float xOff = boxSize.x * 0.6f * dir;
            Vector3 boxCenter = new Vector3((dir == 1 ? b.max.x : b.min.x) + xOff, b.center.y, 0f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boxCenter, boxSize);
        }
    }
}
