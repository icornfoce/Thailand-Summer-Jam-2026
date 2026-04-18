using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 5f;

    [Header("Camera & Look")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    private float cameraPitch = 0f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 1.1f; 
    public LayerMask groundLayer;
    private bool isGrounded;

    private Rigidbody rb;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Prevent physics from rotating the player
        
        // Lock and hide cursor for first-person control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        CheckGrounded();
        HandleInput();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        // Rotate the camera vertically
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
        
        // Rotate the player horizontally
        transform.Rotate(Vector3.up * mouseX);
    }

    private void CheckGrounded()
    {
        // Simple raycast down from the player's center to check for ground
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void HandleInput()
    {
        // Get movement input
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        
        // Calculate move direction relative to where the player is looking
        moveDirection = (transform.right * x + transform.forward * z).normalized;

        // Handle jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Apply vertical velocity for jump
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }
    }

    private void ApplyMovement()
    {
        // Apply horizontal movement using linearVelocity
        Vector3 targetVelocity = moveDirection * moveSpeed;
        
        // Preserve current vertical velocity (gravity, jumping, etc.)
        targetVelocity.y = rb.linearVelocity.y;
        
        rb.linearVelocity = targetVelocity;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the ground check ray in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
}
