using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float jumpImpulseDelay = 1.2f; // Time for the "anticipation" animation

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;
    private float airTimeCounter = 0f;
    private float jumpTimer = 0f;
    private bool isWaitingToJump = false;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;

    private Transform cameraTransform;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");

        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
            
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;
        
        // If we are waiting for the jump impulse, we should stay "grounded" for animation purposes
        bool animatorGrounded = isGrounded || isWaitingToJump;
        
        if (isGrounded)
        {
            if (velocity.y < 0) velocity.y = -2f;
            
            // Only reset air time and falling if we aren't about to jump
            if (!isWaitingToJump)
            {
                airTimeCounter = 0f;
                if (animator != null) animator.SetBool("IsFalling", false);
            }
        }
        else
        {
            // If we are in the air (and not just waiting for the jump impulse)
            if (!isWaitingToJump)
            {
                airTimeCounter += Time.deltaTime;
                if (airTimeCounter > 0.5f) // Reduced from 2s to 0.5s for better responsiveness
                {
                    if (animator != null) animator.SetBool("IsFalling", true);
                }
            }
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = Vector3.zero;
        
        // Block movement during jump impulse for better feel
        if (moveInput.sqrMagnitude > 0.01f && !isWaitingToJump)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            moveDirection = forward * moveInput.y + right * moveInput.x;

            gameObject.transform.forward = Vector3.Slerp(gameObject.transform.forward, moveDirection, Time.deltaTime * rotationSpeed);
        }

        controller.Move(moveDirection * Time.deltaTime * moveSpeed);

        // Jump Logic with Impulse Delay
        if (jumpAction.WasPressedThisFrame() && isGrounded && !isWaitingToJump)
        {
            if (animator != null) animator.SetTrigger("Jump");
            isWaitingToJump = true;
            jumpTimer = jumpImpulseDelay;
        }

        if (isWaitingToJump)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isWaitingToJump = false;
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animation Parameters
        if (animator != null)
        {
            animator.SetFloat("Speed", moveInput.magnitude);
            // We set IsGrounded to false as soon as we start waiting for the jump
            // to prevent the animator from snapping back to Idle.
            animator.SetBool("IsGrounded", isGrounded && !isWaitingToJump);
        }
    }
}
