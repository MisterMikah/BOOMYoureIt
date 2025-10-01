using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public int maxJumps = 1;
    public int playerNumber = 1; // Assign in Inspector (1 or 2)

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Jump Feel")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    private Rigidbody2D rb;
    private Controls controls;
    private Vector2 moveInput;
    private bool jumpPressed;

    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private int jumpsRemaining;

    void Awake()
    {
        controls = new Controls();
    }

    void OnEnable()
    {
        if (playerNumber == 1)
        {
            controls.Player1.Enable();
            controls.Player1.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Player1.Move.canceled += ctx => moveInput = Vector2.zero;
            controls.Player1.Jump.performed += ctx => jumpPressed = true;
        }
        else if (playerNumber == 2)
        {
            controls.Player2.Enable();
            controls.Player2.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Player2.Move.canceled += ctx => moveInput = Vector2.zero;
            controls.Player2.Jump.performed += ctx => jumpPressed = true;
        }
    }

    void OnDisable()
    {
        if (playerNumber == 1) controls.Player1.Disable();
        if (playerNumber == 2) controls.Player2.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 2f;        // stronger gravity
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // smoother motion
        rb.freezeRotation = true;    // don’t tip over
        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        // Track timers
        if (IsGrounded())
        {
            lastGroundedTime = coyoteTime;
            jumpsRemaining = maxJumps;
        }
        else
        {
            lastGroundedTime -= Time.deltaTime;
        }

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

    void FixedUpdate()
    {
        // Horizontal move
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // Handle jump in FixedUpdate (sync with physics)
        if (lastJumpPressedTime > 0 && lastGroundedTime > 0 && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            jumpsRemaining--;
            lastJumpPressedTime = 0;
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
