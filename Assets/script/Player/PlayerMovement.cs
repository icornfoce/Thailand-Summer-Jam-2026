using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f; 
    public float airResistance = 1.5f; 

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
    [Tooltip("ระยะที่กล้องจะลดระดับลงมาเพื่อให้วิสัยทัศน์สมจริงเวลาสไลด์")]
    public float cameraCrouchOffset = -0.8f;
    [Tooltip("ความนุ่มนวลในการก้ม/เงยหน้ากลับ ของหล้อง")]
    public float cameraCrouchSpeed = 12f;

    private Camera camComponent;
    private float currentTilt = 0f;
    private float originalCameraLocalY;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;

    private bool isDashing;
    private float dashTime;
    private float lastDashTime = -999f;
    private Vector3 dashDirection;

    [Header("Sliding Settings (Ctrl + W)")]
    public float slideForce = 15f;
    public float slideDuration = 0.6f;
    public float slideHeight = 1f; // Height while sliding
    public float slideFOV = 105f;
    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding;
    private float slideTimer;
    private float currentSlideSpeed;

    [Header("Wall Running Settings")]
    public float wallRunForce = 10f;
    public float wallRunGravity = -1f; // Slow gravity while wall running
    public float wallRunJumpForce = 12f;
    public float wallDistance = 0.6f;
    public float wallTiltAngle = 15f;
    public float minHeightToWallRun = 1.5f; // ระยะห่างจากพื้นที่อนุญาตให้ Wall Run
    public LayerMask wallMask;
    private bool isWallRunning;
    private bool wallLeft, wallRight;
    private RaycastHit leftHit, rightHit;
    private float wallRunCooldownTimer;
    public float wallRunCooldown = 0.5f;

    [Header("Screen EFX Context")]
    [Tooltip("ใส่ Effect เช่น Speed Lines หรือ Overlay ที่นี่")]
    public GameObject speedBurstEffect; 
    public GameObject wallRunEffect;

    [Header("Audio Settings")]
    public AudioSource movementAudioSource;
    public AudioClip dashSound;
    public AudioClip slideSound;
    public AudioClip wallRunLoopSound;
    public AudioClip jumpSound;

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
            originalCameraLocalY = playerCamera.localPosition.y;
        }

        originalHeight = characterController.height;
        originalCenter = characterController.center;
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;

        HandleMouseLook();
        HandleCameraEffects();

        CheckForWall();
        
        if (isDashing)
        {
            HandleDash();
        }
        else if (isWallRunning)
        {
            HandleWallRun();
            HandleWallJump();
            HandleSlidingInput(); // เปิดให้กดสไลด์แทรกลงมาจากกำแพงได้ทันที
        }
        else if (isSliding)
        {
            HandleSliding();
        }
        else
        {
            HandleMovement();
            HandleJump();
            HandleDashInput();
            HandleSlidingInput();
            HandleWallRunStart();
        }

        HandleScreenEffectsVisibility();

        if (wallRunCooldownTimer > 0) wallRunCooldownTimer -= Time.deltaTime;
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

        // ถ้ากำลังพุ่ง (Dash/Slide) หรือไต่กำแพง ให้กล้องเอียงเพื่อเน้นโมเมนตัม
        if (isDashing || isSliding)
        {
            targetTilt = -xMove * dashTiltAngle;
        }
        
        if (isWallRunning)
        {
            if (wallLeft) targetTilt -= wallTiltAngle;
            if (wallRight) targetTilt += wallTiltAngle;
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltTransitionSpeed * Time.deltaTime);

        // ใส่ค่า Tilt ไปที่แกน Z ของกล้อง
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleCameraEffects()
    {
        if (camComponent == null || playerCamera == null) return;

        // --- ระบบยืด/หด FOV ---
        float targetFOV = baseFOV;
        
        if (isDashing || isSliding)
        {
            targetFOV = slideFOV; // ยืดหน้าจอตอนพุ่ง/สไลด์
        }
        else if (isWallRunning)
        {
            targetFOV = baseFOV + 10f;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            targetFOV = baseFOV + 10f; // เดินเร็วหน้าจอจะยืดออกนิดนึง
        }

        camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);

        // --- ระบบย่อกล้องลงตอนสไลด์ (Smooth Crouch) ---
        // ถ้ากำลังสไลด์ เป้าหมายคือกล้องต่ำลง ถ้ากลับปกติหรือถูกขัดจังหวะ เป้าหมายคือจุดเดิม
        float targetCameraY = isSliding ? originalCameraLocalY + cameraCrouchOffset : originalCameraLocalY;
        Vector3 camLocalPos = playerCamera.localPosition;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetCameraY, cameraCrouchSpeed * Time.deltaTime);
        playerCamera.localPosition = camLocalPos;
    }

    void HandleMovement()
    {
        if (isGrounded)
        {
            // ล้างแรงส่งในแนวราบเมื่อแตะพื้น เพื่อไม่ให้ไหลไม่หยุด
            velocity.x = 0;
            velocity.z = 0;

            if (velocity.y < 0)
            {
                velocity.y = -2f; 
            }
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
            PlaySound(jumpSound);
        }

        // --- เพิ่มระบบ Air Resistance (แรงต้านอากาศ) ---
        // จะค่อยๆ ลดแรงส่งแนวนอนของ velocity ให้เหลือ 0 เพื่อไม่ให้กระโดดแล้วลอยไปไกลไม่หยุด
        if (!isGrounded)
        {
            velocity.x = Mathf.Lerp(velocity.x, 0, airResistance * Time.deltaTime);
            velocity.z = Mathf.Lerp(velocity.z, 0, airResistance * Time.deltaTime);
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
        PlaySound(dashSound);

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // ให้ dash อิงตามลำตัวของผู้เล่น (แกนระนาบพื้น) แทนกล้อง
        // เพื่อป้องกันบั๊กการพุ่งขึ้นฟ้าเวลาเราก้มมองพื้นแล้วกดเดินถอยหลัง
        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 0.1f)
        {
            dashDirection = move.normalized;
        }
        else
        {
            // ถ้าไม่ได้กดปุ่มทิศทางใดๆ ให้พุ่งไปข้างหน้าของลำตัว
            dashDirection = transform.forward;
        }
    }

    void HandleDash()
    {
        // คำนวณเปอร์เซ็นต์เวลาพุ่ง (0.0 ถึง 1.0)
        float dashProgress = (Time.time - dashTime) / dashDuration;
        
        // ความเร็วค่อยๆ ลดลงจากแรงพุ่งไปสู่ความเร็วเดินปกติ
        float currentDashSpeed = Mathf.Lerp(dashForce, walkSpeed, dashProgress);

        // ยกเลิกข้อจำกัดแรงโน้มถ่วงชั่วคราวตอนพุ่ง
        characterController.Move(dashDirection * currentDashSpeed * Time.deltaTime);

        if (dashProgress >= 1f)
        {
            isDashing = false;
        }
    }

    #region Sliding System
    void HandleSlidingInput()
    {
        // เช็คปุ่ม Ctrl หรือ C และต้องเดินหน้าอยู่ (W)
        bool slideKeyPressed = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C);
        
        // อนุญาตให้เริ่มสไลด์ได้ ถ้าอยู่บนพื้นปกติ หรือกำลังวิ่งไต่กำแพงอยู่ (Aerial Slide)
        bool canSlide = (isGrounded || isWallRunning) && !isSliding;

        if (slideKeyPressed && Input.GetAxisRaw("Vertical") > 0 && canSlide)
        {
            if (isWallRunning) StopWallRun(); // ยกเลิกการเกาะกำแพง แล้วทิ้งตัวสไลด์ลงพื้นอย่างเร็ว
            StartSlide();
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        currentSlideSpeed = slideForce;
        
        // ย่อตัวลง และปรับจุดศูนย์กลางให้เท้ายังติดพื้น (กันปัญหา CC ลอยแล้วหลุดสไลด์)
        characterController.height = slideHeight;
        characterController.center = originalCenter + new Vector3(0, (slideHeight - originalHeight) / 2f, 0);
        
        // Push initial slide energy
        velocity.y = -5f; 
        PlaySound(slideSound);
    }

    void HandleSliding()
    {
        // คำนวณเปอร์เซ็นต์เวลาสไลด์ (0.0 ถึง 1.0)
        float slideProgress = 1f - (slideTimer / slideDuration);
        
        // ความเร็วค่อยๆ ลดลงจากแรงสไลด์ไปสู่ความเร็วเดินปกติ (ใช้วิธีเดียวกับ Dash)
        float currentSpeed = Mathf.Lerp(slideForce, walkSpeed, slideProgress);

        slideTimer -= Time.deltaTime;

        // คำนวณแนวพุ่งสไลด์
        Vector3 slideMomentum = transform.forward * currentSpeed;

        // จัดการตกพื้น (ตั้งค่า Ground Glue เพื่อรักษาให้ไถลลงทางลาดได้เนียนๆ)
        if (isGrounded)
        {
            velocity.y = -10f; // แรงสำหรับกดให้แนบซอก/เนินลาด
        }
        else
        {
            // ถ้าสไลด์ร่วงจากกำแพงหรืออยู่ในอากาศ ก็ให้ตกลงมาตามแรงดึงดูดธรรมชาติ
            velocity.y += gravity * Time.deltaTime;
        }

        slideMomentum.y = velocity.y;

        // ต้องสั่ง Move() แค่ครั้งเดียวในการขยับ! 
        // ถ้ารวม 2 แกนเข้าด้วยกันก่อน Move ระบบ Step Offset (ปีนเนินเตี้ยๆอัตโนมัติ) ถึงจะทำงานอย่างสมบูรณ์
        characterController.Move(slideMomentum * Time.deltaTime);

        // เงื่อนไขการออกจาก Slide
        bool hasStoppedMovingForward = Input.GetAxisRaw("Vertical") <= 0;

        // หยุดสไลด์เมื่อ: จบระยะเวลาสไลด์ตามกำหนด, กดกระโดด, หรือปล่อยปุ่มเดิน 
        // (ยกเลิกการบังคับหลุดเมื่อไม่โดนพื้น เพื่อให้สไลด์ดิ่งลงมาจากกำแพงหรือสไลด์เหินฟ้าต่อได้ชั่วคราว)
        if (slideTimer <= 0f || Input.GetButtonDown("Jump") || hasStoppedMovingForward)
        {
            StopSlide();
            if (Input.GetButtonDown("Jump")) 
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                PlaySound(jumpSound);
            }
        }
    }

    void StopSlide()
    {
        isSliding = false;
        characterController.height = originalHeight;
        characterController.center = originalCenter;
    }
    #endregion

    #region Wall Running System
    void CheckForWall()
    {
        // เพิ่มระยะห่างในการตรวจจับกำแพงเมื่อกำลัง Wall Running อยู่ (Hysteresis)
        // เพื่อป้องกันการที่สถานะ WallRun หลุดและเข้าใหม่สลับกันรัวๆ จนกล้องกระตุก
        float checkDistance = isWallRunning ? wallDistance * 1.5f : wallDistance;

        wallRight = Physics.Raycast(transform.position, transform.right, out rightHit, checkDistance, wallMask);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftHit, checkDistance, wallMask);
    }

    void HandleWallRunStart()
    {
        if (wallRunCooldownTimer > 0) return;

        // เช็คความสูงจากพื้นด้วย Raycast เพื่อไม่ให้กด Wall Run ทับตอนเตี้ยๆ (ใกล้พื้น)
        bool isHighEnough = !Physics.Raycast(transform.position, Vector3.down, minHeightToWallRun);

        if ((wallLeft || wallRight) && !isGrounded && isHighEnough && Input.GetAxisRaw("Vertical") > 0)
        {
            isWallRunning = true;
            
            // รีเซ็ตแรงแนวราบ แต่กั๊กความเร็วพุ่งขึ้น (Y) เอาไว้ 
            // ทำให้กระโดดอัดกำแพงเพื่อไต่สูงขึ้นต่อได้ลื่นไหล ไม่ชะงักหยุดกึกแบบเวอร์ชันเก่า
            velocity.x = 0;
            velocity.z = 0;
            if (velocity.y < 0) velocity.y = 0; // ถ้าตัวกำลังตก ให้หยุดร่วง
            
            if (movementAudioSource != null && wallRunLoopSound != null)
            {
                movementAudioSource.clip = wallRunLoopSound;
                movementAudioSource.loop = true;
                movementAudioSource.Play();
            }
        }
    }

    void HandleWallRun()
    {
        // ใช้ Normal ของกำแพงที่ถูกต้องตามฝั่ง (ป้องกันการ Bug เมื่อเจอ 2 กำแพงพร้อมกัน)
        RaycastHit activeHit = wallRight ? rightHit : leftHit;
        Vector3 wallNormal = activeHit.normal;

        Vector3 moveDir = Vector3.ProjectOnPlane(transform.forward, wallNormal);

        // "วิ่งแล้วมันลงช้า แต่ถ้าอยู่เฉยจะลงเร็ว"
        float verticalInput = Input.GetAxisRaw("Vertical");
        float currentWallRunGravity = verticalInput > 0 ? wallRunGravity : gravity / 2f; 

        // คำนวณความเร็วรวม: (วิ่งไปตามกำแพง) + (แรงดึงดูด)
        Vector3 wallMove = moveDir * wallRunForce * verticalInput;
        velocity.y = currentWallRunGravity;

        // เพิ่มแรงผลักออกจากกำแพงนิดหน่อย เฉพาะเมื่อเราอยู่ใกล้กำแพงเกินไป (เพื่อไม่ให้เบียด SkinWidth)
        Vector3 pushAway = Vector3.zero;
        if (activeHit.distance < wallDistance) 
        {
            pushAway = wallNormal * 0.12f;
        }

        // รวมทิศทางการเคลื่อนที่ทั้งหมดเข้าด้วยกัน
        characterController.Move((wallMove + pushAway + velocity) * Time.deltaTime);

        if (!(wallLeft || wallRight) || isGrounded || verticalInput <= 0)
        {
            StopWallRun();
        }
    }

    void HandleWallJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            RaycastHit activeHit = wallRight ? rightHit : leftHit;
            Vector3 wallNormal = activeHit.normal;

            // ดีดตัวออกมาแรงๆ (เพิ่มแรงฝั่ง WallNormal ให้มากขึ้น)
            Vector3 jumpDir = (wallNormal * 1.5f + Vector3.up).normalized;
            velocity = jumpDir * wallRunJumpForce;
            
            wallRunCooldownTimer = wallRunCooldown;
            StopWallRun();
            PlaySound(jumpSound);
        }
    }

    void StopWallRun()
    {
        isWallRunning = false;
        if (velocity.y < 0) velocity.y = 0; // รีเซ็ตความเร็วแนวตั้งเมื่อหลุด (ป้องกันการวูบ)
        
        if (movementAudioSource != null && movementAudioSource.clip == wallRunLoopSound)
        {
            movementAudioSource.Stop();
            movementAudioSource.loop = false;
        }
    }
#endregion

    void PlaySound(AudioClip clip)
    {
        if (movementAudioSource != null && clip != null)
        {
            movementAudioSource.PlayOneShot(clip);
        }
    }

    void HandleScreenEffectsVisibility()
    {
        if (speedBurstEffect != null) 
            speedBurstEffect.SetActive(isDashing || isSliding);
        
        if (wallRunEffect != null)
            wallRunEffect.SetActive(isWallRunning);
    }
}
