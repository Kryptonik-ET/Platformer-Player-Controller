using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    #region VARIABLES
    [SerializeField] private PlayerDataSO playerDataSO;

    [Space(15)]
    [Header("CHECKS")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.49f);
    [Space(10)]
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D playerRB;

    private bool isGrounded;
    private bool isJumpCut;
    private bool hasAirJump = true;
    private bool isJumping = false;
    private bool isFalling = false;
    private bool isDashing = false;
    private bool canDash;

    //TIMERS
    // Coyote time is the timer after jumping off a ledge
    //Jump Buffer timer is after space is pressed
    private float jumpBufferTimer;
    private float coyoteTimer;
    private float dashStopTimer;
    private float dashCooldownTimer;

    private Vector2 moveInput;
    #endregion

    private void Awake() {
        playerRB = GetComponent<Rigidbody2D>();
        hasAirJump = true;
        dashCooldownTimer = playerDataSO.dashCooldown;
    }

    private void Start() {
        InputManager.Instance.OnJumpPressed += InputManager_OnJumpPressed;
        InputManager.Instance.OnJumpReleased += Instance_OnJumpReleased;
        InputManager.Instance.OnDashPressed += InputManager_OnDashPressed;

        playerRB.gravityScale = playerDataSO.gravityScale;
    }

    private void InputManager_OnDashPressed(object sender, System.EventArgs e) {
        if (dashCooldownTimer <= 0 && canDash) Dash();
    }

    private void Instance_OnJumpReleased(object sender, System.EventArgs e) {
        isJumpCut = isJumping;
    }

    private void InputManager_OnJumpPressed(object sender, System.EventArgs e) {
        jumpBufferTimer = playerDataSO.jumpBufferTime;
    }

    private void Update() {
        moveInput = InputManager.Instance.GetMoveDirection();

        if (moveInput.x > 0.25f) moveInput.x = 1;
        else if (moveInput.x < -0.25f) moveInput.x = -1;
        else moveInput.x = 0;

        if (moveInput.y > 0.25f) moveInput.y = 1;
        else if (moveInput.y < -0.25f) moveInput.y = -1;
        else moveInput.y = 0;

        isGrounded = IsGrounded();

        //TIMERS
        jumpBufferTimer -= Time.deltaTime;
        coyoteTimer -= Time.deltaTime;
        dashStopTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;

        if (isGrounded) {
            coyoteTimer = playerDataSO.coyoteTime;
            isJumpCut = false;
            isFalling = false;
            hasAirJump = true;
            canDash = true;
            if (jumpBufferTimer > 0 && !isJumping) {
                isJumping = true;
                isJumpCut = false;
            }
        }   
        else if (hasAirJump && jumpBufferTimer > 0 && !isDashing) {
            isJumpCut = false;
            isFalling = false;
            hasAirJump = false;
            if (!isJumping) {
                isJumping = true;
                isJumpCut = false;
            }
            Jump(playerDataSO.airJumpForceMultiplier);
        }

        if (isJumping && playerRB.linearVelocityY < 0) {
            isJumping = false;
            isFalling = true;
        }

        if (coyoteTimer > 0 && jumpBufferTimer > 0 && !isDashing) {
            Jump(1);
        }

        if (playerRB.linearVelocityY < 0) {
            playerRB.linearVelocityY = Mathf.Max(playerRB.linearVelocityY, -playerDataSO.maxFallSpeed);
        }

        if (isDashing && dashStopTimer <= 0) {
            isDashing = false;
            playerRB.gravityScale = playerDataSO.gravityScale;
            playerRB.linearVelocity = Vector2.zero;
            dashCooldownTimer = playerDataSO.dashCooldown;
            playerRB.linearVelocity = Vector2.zero;
        }

        #region GRAVITY
        if (isDashing) {
            playerRB.gravityScale = 0;
        }
        else if (playerRB.linearVelocityY < 0 && moveInput.y < 0) {
            playerRB.gravityScale = playerDataSO.gravityScale * playerDataSO.fastFallMultiplier;
        }
        else if (isJumpCut) {
            playerRB.gravityScale = playerDataSO.gravityScale * playerDataSO.jumpCutGravityMultiplier;
        }
        else if ((isJumping || isFalling) && Mathf.Abs(playerRB.linearVelocityY) < playerDataSO.jumpHangTimeThreshold) {
            playerRB.gravityScale = playerDataSO.gravityScale * playerDataSO.jumpHangGravityMultiplier;
        }
        else playerRB.gravityScale = playerDataSO.gravityScale;
        #endregion
    }

    private void FixedUpdate() {
        if (!isDashing) {
            Move();
        }
    }

    private void OnDestroy() {
        InputManager.Instance.OnJumpPressed -= InputManager_OnJumpPressed;
        InputManager.Instance.OnJumpReleased -= Instance_OnJumpReleased;
    }

    private void Move() {
        float targetSpeed = moveInput.x * playerDataSO.moveSpeed;
        float accelerate;

        accelerate = (Mathf.Abs(targetSpeed) > Mathf.Epsilon) ? playerDataSO.acceleration : playerDataSO.deceleration;

        if ((isJumping || isFalling) && Mathf.Abs(playerRB.linearVelocityY) < playerDataSO.jumpHangTimeThreshold) {
            accelerate *= playerDataSO.jumpHangAccelerationMultiplier;
            targetSpeed *= playerDataSO.jumpHangMoveSpeedMultiplier;
        }

        float speedDif = targetSpeed - playerRB.linearVelocityX;
        float movement = speedDif * accelerate;
        playerRB.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void Jump(float jumpForceMultiplier) {
        jumpBufferTimer = 0;
        coyoteTimer = 0;
        isJumping = true;

        float force = playerDataSO.jumpForce * jumpForceMultiplier;
        force -= playerRB.linearVelocityY;
        force *= playerRB.mass;
        playerRB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    private void Dash() {
        isDashing = true;
        canDash = false;
        dashStopTimer = playerDataSO.dashTime;
        playerRB.linearVelocity = new Vector2(moveInput.x * playerDataSO.dashVelocity, 0);
    }

    private bool IsGrounded()
        => Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0, groundLayer);


    private void OnDrawGizmos() {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
    }
}
