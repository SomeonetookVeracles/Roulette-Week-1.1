using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class ParkourMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 12f;
    [SerializeField] private float slideSpeed = 15f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.3f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    
    [Header("Gravity Settings")]
    [SerializeField] private float universalGravity = 30f; // Increased for faster falling
    [SerializeField] private float wallRunGravityMultiplier = 0.1f; // Reduced gravity during wall run
    [SerializeField] private float groundPoundMultiplier = 10f; // Multiplier when holding CTRL in air
    
    [Header("Wall Movement")]
    [SerializeField] private float wallRunSpeed = 10f;
    [SerializeField] private float wallRunDuration = 3f;
    [SerializeField] private float wallJumpForce = 7f;
    [SerializeField] private float wallJumpUpForce = 5f;
    [SerializeField] private float wallCheckDistance = 1f;
    [SerializeField] private LayerMask wallLayer;
    
    [Header("Sliding & Vaulting")]
    [SerializeField] private float slideTime = 1f;
    [SerializeField] private float vaultHeight = 1.5f;
    [SerializeField] private float vaultDistance = 2f;
    [SerializeField] private LayerMask vaultLayer;
    
    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraSensitivity = 2f;
    [SerializeField] private float cameraClampAngle = 85f;
    [SerializeField] private float wallRunTilt = 15f;
    [SerializeField] private float slideFOVChange = 10f;
    [SerializeField] private float gravityFlipDuration = 0.5f;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    
    // Components
    private CharacterController controller;
    private Camera playerCamera;
    
    // Movement variables
    private Vector3 velocity;
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    
    // Wall run variables
    private bool isWallRunning;
    private bool isWallLeft;
    private bool isWallRight;
    private float wallRunTimer;
    private Vector3 wallNormal;
    private float wallRunCameraTilt;
    
    // Sliding variables
    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDirection;
    
    // Ground pound variables
    private bool isGroundPounding;
    
    // Debug Properties (visible in Inspector during play)
    public bool IsGroundPounding => isGroundPounding;
    public float CurrentVelocityY => velocity.y;
    
    // Gravity inversion
    private bool isGravityInverted;
    private bool isFlipping;
    private float gravityRotation = 0f; // Store the camera's gravity-based rotation
    
    // Input variables
    private float mouseX;
    private float mouseY;
    private float xRotation;
    
    // Original height for crouching/sliding
    private float originalHeight;
    private Vector3 originalCenter;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = cameraTransform.GetComponent<Camera>();
        
        originalHeight = controller.height;
        originalCenter = controller.center;
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleGroundCheck();
        HandleMouseLook();
        HandleMovementInput();
        HandleJumping();
        HandleWallRun();
        HandleSliding();
        HandleVaulting();
        HandleGravityInversion();
        HandleGroundPound(); // New separate method for clarity
        
        ApplyMovement();
        ApplyCameraEffects();
    }
    
    void HandleGroundPound()
    {
        // Ground pound input check (separate from other mechanics)
        if (!isGrounded && !isWallRunning && !isSliding)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                Debug.Log("CTRL pressed in air - Starting Ground Pound!");
                // This will be handled in ApplyMovement, just for debug
            }
            
            if (Input.GetKey(KeyCode.LeftControl))
            {
                // Visual feedback that CTRL is being held
                if (showDebugInfo && !isGroundPounding)
                {
                    Debug.Log("Holding CTRL in air...");
                }
            }
        }
    }
    
    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;
        
        if (!isGravityInverted)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            // Check ceiling when gravity is inverted
            isGrounded = Physics.CheckSphere(transform.position + Vector3.up * controller.height, groundDistance, groundMask);
        }
        
        // Coyote time
        if (wasGrounded && !isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else if (isGrounded)
        {
            coyoteTimeCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }
    
    void HandleMouseLook()
    {
        mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;
        
        // INVERT BOTH AXES WHEN GRAVITY IS INVERTED
        // This compensates for the 180-degree camera rotation
        float adjustedMouseX = isGravityInverted ? -mouseX : mouseX;
        float adjustedMouseY = isGravityInverted ? -mouseY : mouseY;
        
        // Rotate player body
        transform.Rotate(Vector3.up * adjustedMouseX);
        
        // Rotate camera
        xRotation -= adjustedMouseY;
        xRotation = Mathf.Clamp(xRotation, -cameraClampAngle, cameraClampAngle);
        
        float targetRotation = xRotation + wallRunCameraTilt;
        
        // Apply rotation with gravity flip rotation preserved
        cameraTransform.localRotation = Quaternion.Euler(targetRotation, 0f, gravityRotation);
    }
    
    void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // ONLY INVERT HORIZONTAL MOVEMENT WHEN GRAVITY IS INVERTED
        // This makes A/D controls feel natural when upside down, but keeps W/S the same
        if (isGravityInverted)
        {
            horizontal = -horizontal;
        }
        
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        move = Vector3.ClampMagnitude(move, 1f);
        
        // Determine target speed
        float targetSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift) && !isSliding)
        {
            targetSpeed = sprintSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftControl) && isGrounded) // Only apply crouch speed on ground
        {
            targetSpeed = crouchSpeed;
        }
        else if (isSliding)
        {
            targetSpeed = slideSpeed;
        }
        else if (isWallRunning)
        {
            targetSpeed = wallRunSpeed;
        }
        
        // Smooth speed transitions
        float speedChangeRate = isGrounded ? 
            (targetSpeed > currentSpeed ? acceleration : deceleration) : 
            airControl;
        
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);
        
        // Apply movement
        if (isWallRunning)
        {
            moveDirection = transform.forward * wallRunSpeed;
            
            // Add slight pull towards wall
            if (isWallLeft)
                moveDirection += -transform.right * 2f;
            else if (isWallRight)
                moveDirection += transform.right * 2f;
        }
        else if (isSliding)
        {
            moveDirection = slideDirection * slideSpeed;
        }
        else
        {
            moveDirection = move * currentSpeed;
        }
    }
    
    void HandleJumping()
    {
        // Jump buffer
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        
        // Don't jump if ground pounding
        if (isGroundPounding)
        {
            jumpBufferCounter = 0;
            return;
        }
        
        // Perform jump
        if (jumpBufferCounter > 0 && (isGrounded || coyoteTimeCounter > 0 || isWallRunning))
        {
            if (isWallRunning)
            {
                // Wall jump
                Vector3 jumpDir = wallNormal * wallJumpForce + transform.up * wallJumpUpForce;
                velocity = jumpDir;
                
                isWallRunning = false;
                wallRunTimer = 0;
            }
            else
            {
                // Regular jump
                float jumpVelocity = Mathf.Sqrt(jumpHeight * 2f * universalGravity);
                velocity.y = isGravityInverted ? -jumpVelocity : jumpVelocity;
            }
            
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }
    }
    
    void HandleWallRun()
    {
        // Check for walls
        RaycastHit leftWallHit;
        RaycastHit rightWallHit;
        
        isWallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, wallLayer);
        isWallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, wallLayer);
        
        // Start wall run
        if ((isWallLeft || isWallRight) && !isGrounded && Input.GetKey(KeyCode.LeftShift))
        {
            if (!isWallRunning)
            {
                wallRunTimer = wallRunDuration;
                isWallRunning = true;
                
                // Reset vertical velocity
                velocity.y = 0;
            }
            
            // Get wall normal
            wallNormal = isWallLeft ? leftWallHit.normal : rightWallHit.normal;
            
            // Apply wall run gravity (reduced)
            if (wallRunTimer > 0)
            {
                float wallGravity = universalGravity * wallRunGravityMultiplier;
                velocity.y = Mathf.Lerp(velocity.y, isGravityInverted ? wallGravity : -wallGravity, Time.deltaTime * 5f);
                wallRunTimer -= Time.deltaTime;
            }
            else
            {
                // End wall run
                isWallRunning = false;
            }
            
            // Camera tilt
            float targetTilt = isWallLeft ? -wallRunTilt : wallRunTilt;
            wallRunCameraTilt = Mathf.Lerp(wallRunCameraTilt, targetTilt, Time.deltaTime * 5f);
        }
        else
        {
            isWallRunning = false;
            wallRunTimer = 0;
            wallRunCameraTilt = Mathf.Lerp(wallRunCameraTilt, 0, Time.deltaTime * 5f);
        }
    }
    
    void HandleSliding()
    {
        // Start slide (only on ground)
        if (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && isGrounded)
        {
            StartSlide();
        }
        
        // Continue slide
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            
            // Adjust controller height
            controller.height = originalHeight * 0.5f;
            controller.center = originalCenter * 0.5f;
            
            if (slideTimer <= 0 || !Input.GetKey(KeyCode.LeftControl) || !isGrounded)
            {
                EndSlide();
            }
        }
    }
    
    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideTime;
        slideDirection = transform.forward;
        
        // Add slide boost
        velocity += transform.forward * 5f;
    }
    
    void EndSlide()
    {
        isSliding = false;
        controller.height = originalHeight;
        controller.center = originalCenter;
    }
    
    void HandleVaulting()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * vaultHeight;
            
            if (Physics.Raycast(rayOrigin, transform.forward, out hit, vaultDistance, vaultLayer))
            {
                // Check if we can vault over
                Vector3 vaultEndPos = hit.point + transform.forward * 2f + Vector3.up * 0.5f;
                
                if (!Physics.CheckSphere(vaultEndPos, 0.5f, vaultLayer))
                {
                    StartCoroutine(VaultCoroutine(vaultEndPos));
                }
            }
        }
    }
    
    IEnumerator VaultCoroutine(Vector3 targetPos)
    {
        float vaultTime = 0.5f;
        float elapsedTime = 0;
        Vector3 startPos = transform.position;
        
        while (elapsedTime < vaultTime)
        {
            float t = elapsedTime / vaultTime;
            t = Mathf.SmoothStep(0, 1, t);
            
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
            
            // Add arc to vault
            newPos.y += Mathf.Sin(t * Mathf.PI) * 1f;
            
            controller.enabled = false;
            transform.position = newPos;
            controller.enabled = true;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        controller.enabled = false;
        transform.position = targetPos;
        controller.enabled = true;
    }
    
    void HandleGravityInversion()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isFlipping)
        {
            StartCoroutine(FlipGravity());
        }
    }
    
    IEnumerator FlipGravity()
    {
        isFlipping = true;
        isGravityInverted = !isGravityInverted;
        
        float elapsedTime = 0;
        float startRotation = gravityRotation;
        float targetRotation = isGravityInverted ? 180f : 0f;
        
        // Smooth camera rotation
        while (elapsedTime < gravityFlipDuration)
        {
            float t = elapsedTime / gravityFlipDuration;
            t = Mathf.SmoothStep(0, 1, t);
            
            gravityRotation = Mathf.LerpAngle(startRotation, targetRotation, t);
            
            // Apply the rotation (this will be properly maintained in HandleMouseLook)
            cameraTransform.localRotation = Quaternion.Euler(xRotation + wallRunCameraTilt, 0, gravityRotation);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        gravityRotation = targetRotation;
        isFlipping = false;
    }
    
    void ApplyMovement()
    {
        // Apply gravity
        if (isGrounded)
        {
            if (velocity.y < 0 && !isGravityInverted)
            {
                velocity.y = -2f;
            }
            else if (velocity.y > 0 && isGravityInverted)
            {
                velocity.y = 2f;
            }
            
            // Reset ground pound when landing
            if (isGroundPounding)
            {
                isGroundPounding = false;
                Debug.Log("Ground Pound Impact!");
            }
        }
        
        // Apply gravity and ground pound
        if (!isWallRunning)
        {
            float gravityMultiplier = 1f;
            
            // Ground pound check - simplified conditions
            bool canGroundPound = !isGrounded && !isWallRunning;
            
            if (canGroundPound && Input.GetKey(KeyCode.LeftControl))
            {
                // Start ground pound
                if (!isGroundPounding && Input.GetKeyDown(KeyCode.LeftControl))
                {
                    isGroundPounding = true;
                    Debug.Log("GROUND POUND ACTIVATED!");
                    
                    // Strong initial downward force
                    if (!isGravityInverted)
                    {
                        velocity.y = -25f;
                    }
                    else
                    {
                        velocity.y = 25f;
                    }
                }
                
                // Continue applying ground pound multiplier
                if (isGroundPounding)
                {
                    gravityMultiplier = groundPoundMultiplier;
                }
            }
            
            // Apply gravity with multiplier
            float gravityForce = isGravityInverted ? universalGravity : -universalGravity;
            velocity.y += gravityForce * gravityMultiplier * Time.deltaTime;
            
            // Terminal velocity
            float maxFallSpeed = isGroundPounding ? 100f : 50f;
            if (!isGravityInverted)
            {
                velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            }
            else
            {
                velocity.y = Mathf.Min(velocity.y, maxFallSpeed);
            }
        }
        
        // Move character
        Vector3 move = moveDirection + velocity;
        controller.Move(move * Time.deltaTime);
    }
    
    void ApplyCameraEffects()
    {
        // FOV changes
        float targetFOV = 60f;
        
        if (isGroundPounding)
        {
            targetFOV = 45f; // Even more dramatic FOV for ground pound
        }
        else if (isSliding)
        {
            targetFOV = 60f + slideFOVChange;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && isGrounded)
        {
            targetFOV = 70f;
        }
        else if (isWallRunning)
        {
            targetFOV = 75f;
        }
        
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 8f);
        }
        
        // Screen shake for ground pound (optional)
        if (isGroundPounding && playerCamera != null)
        {
            float shakeAmount = 0.1f;
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0
            );
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, shakeOffset, Time.deltaTime * 10f);
        }
        else
        {
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, Vector3.zero, Time.deltaTime * 10f);
        }
        
        // Ensure camera rotation stays consistent with gravity
        if (!isFlipping)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation + wallRunCameraTilt, 0, gravityRotation);
        }
    }
    
    // Helper method to reset movement state
    public void ResetMovement()
    {
        velocity = Vector3.zero;
        moveDirection = Vector3.zero;
        isWallRunning = false;
        isSliding = false;
        isGroundPounding = false;
        wallRunTimer = 0;
        slideTimer = 0;
        
        if (isGravityInverted)
        {
            StartCoroutine(FlipGravity());
        }
        
        // Reset gravity rotation
        gravityRotation = 0f;
    }
    
    // Debug UI
    void OnGUI()
    {
        if (showDebugInfo)
        {
            int yPos = 10;
            int lineHeight = 20;
            
            GUI.Label(new Rect(10, yPos, 300, lineHeight), $"Grounded: {isGrounded}");
            yPos += lineHeight;
            
            GUI.Label(new Rect(10, yPos, 300, lineHeight), $"Velocity Y: {velocity.y:F2}");
            yPos += lineHeight;
            
            GUI.Label(new Rect(10, yPos, 300, lineHeight), $"Ground Pounding: {isGroundPounding}");
            yPos += lineHeight;
            
            GUI.Label(new Rect(10, yPos, 300, lineHeight), $"Gravity Inverted: {isGravityInverted}");
            yPos += lineHeight;
            
            GUI.Label(new Rect(10, yPos, 300, lineHeight), $"Wall Running: {isWallRunning}");
            yPos += lineHeight;
            
            GUI.Label(new Rect(10, yPos, 300, lineHeight), $"Sliding: {isSliding}");
            yPos += lineHeight;
            
            if (!isGrounded)
            {
                GUI.Label(new Rect(10, yPos, 300, lineHeight), "Press CTRL to Ground Pound!");
            }
        }
    }
}