using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f; 

    [Header("Dash Settings")]
    public float dashForce = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 89f;

    [Header("Camera Effects (Momentum & Flow)")]
    public float baseFOV = 90f;
    public float dashFOV = 110f;
    public float fovTransitionSpeed = 10f;
    public float strafeTiltAngle = 3f;
    public float dashTiltAngle = 10f;
    public float tiltTransitionSpeed = 6f;

    private Camera camComponent;
    private float currentTilt = 0f;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;

    private bool isDashing;
    private float dashTime;
    private float lastDashTime = -999f;
    private Vector3 dashDirection;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>()?.transform;
        }
        
        if (playerCamera != null)
        {
            camComponent = playerCamera.GetComponent<Camera>();
            if (camComponent != null) camComponent.fieldOfView = baseFOV;
        }
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraEffects();

        if (isDashing)
        {
            HandleDash();
        }
        else
        {
            HandleMovement();
            HandleJump();
            HandleDashInput();
        }
    }

    void HandleMouseLook()
    {
        if (playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // --- ระบบเอียงกล้องเวลาเดินข้าง (Strafe Tilt) ---
        float xMove = Input.GetAxisRaw("Horizontal");
        float targetTilt = -xMove * strafeTiltAngle;

        // ถ้ากำลังพุ่ง (Dash) ไปด้านข้าง ให้กล้องเอียงเยอะขึ้นเพื่อเน้นโมเมนตัม
        if (isDashing && Mathf.Abs(xMove) > 0.1f)
        {
            targetTilt = -xMove * dashTiltAngle;
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltTransitionSpeed * Time.deltaTime);

        // ใส่ค่า Tilt ไปที่แกน Z ของกล้อง
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCameraEffects()
    {
        if (camComponent == null) return;

        // --- ระบบยืด/หด FOV ตามความเร็ว ---
        float targetFOV = baseFOV;
        
        if (isDashing)
        {
            targetFOV = dashFOV; // ยืดหน้าจอตอนพุ่ง
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            targetFOV = baseFOV + 10f; // เดินเร็วหน้าจอจะยืดออกนิดนึง
        }

        camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }

    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move.Normalize(); 

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        
        characterController.Move(move * currentSpeed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1)) // Using Right Click or Q for Dash
        {
            if (Time.time >= lastDashTime + dashCooldown)
            {
                StartDash();
            }
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTime = Time.time;
        lastDashTime = Time.time;

        velocity.y = 0f;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // ให้ dash อิงตามมุมมองของกล้องเพื่อให้พุ่งขึ้นฟ้า/ลงดินได้ตามอิสระ
        Transform camTransform = playerCamera != null ? playerCamera : transform;
        Vector3 move = camTransform.right * x + camTransform.forward * z;

        if (move.magnitude > 0.1f)
        {
            dashDirection = move.normalized;
        }
        else
        {
            // ถ้าไม่ได้กดปุ่มเดิน จะพุ่งไปตามที่กล้องมองตรงๆ
            dashDirection = camTransform.forward;
        }
    }

    void HandleDash()
    {
        // ยกเลิกข้อจำกัดแรงโน้มถ่วงชั่วคราวตอนพุ่ง
        characterController.Move(dashDirection * dashForce * Time.deltaTime);

        if (Time.time >= dashTime + dashDuration)
        {
            isDashing = false;
        }
    }
}
