using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 12f;
    [Tooltip("Camera used to make W/S move vertically on screen. Leave empty to use Main Camera.")]
    [SerializeField] private Camera movementCamera;

    [Header("Input")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";

    [Header("Mouse Facing")]
    [SerializeField] private LayerMask mouseAimMask = ~0;
    [SerializeField] private float mouseRayDistance = 500f;

    private CharacterController characterController;
    private Camera mainCamera;
    private PlayerInventory playerInventory;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInventory = GetComponent<PlayerInventory>();
        mainCamera = movementCamera != null ? movementCamera : Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        float horizontal = Input.GetAxisRaw(horizontalAxis);
        float vertical = Input.GetAxisRaw(verticalAxis);

        Vector3 inputDirection = GetMovementDirection(horizontal, vertical);

        float speedMultiplier = playerInventory != null
            ? playerInventory.GetEquippedWalkSpeedMultiplier()
            : 1f;
        characterController.SimpleMove(inputDirection * moveSpeed * speedMultiplier);

        RotateTowardMouse(inputDirection);
    }

    private Vector3 GetMovementDirection(float horizontal, float vertical)
    {
        Vector3 rawInput = new Vector3(horizontal, 0f, vertical);
        if (rawInput.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        if (mainCamera == null)
        {
            return rawInput.normalized;
        }

        // Project the camera's on-screen axes onto the floor. Using camera.up for
        // vertical input also works with a camera looking almost straight down,
        // where a flattened camera.forward can become a zero vector.
        Vector3 screenUp = Vector3.ProjectOnPlane(mainCamera.transform.up, Vector3.up);
        Vector3 screenRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up);

        if (screenUp.sqrMagnitude <= 0.001f)
        {
            screenUp = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
        }

        if (screenUp.sqrMagnitude <= 0.001f || screenRight.sqrMagnitude <= 0.001f)
        {
            return rawInput.normalized;
        }

        screenUp.Normalize();
        screenRight.Normalize();

        Vector3 moveDirection = (screenRight * rawInput.x) + (screenUp * rawInput.z);
        return moveDirection.normalized;
    }

    private void RotateTowardMouse(Vector3 fallbackDirection)
    {
        if (mainCamera == null)
        {
            RotateTowards(fallbackDirection);
            return;
        }

        Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseRay, out RaycastHit hit, mouseRayDistance, mouseAimMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 lookDirection = hit.point - transform.position;
            lookDirection.y = 0f;
            RotateTowards(lookDirection.normalized);
            return;
        }

        RotateTowards(fallbackDirection);
    }

    private void RotateTowards(Vector3 lookDirection)
    {
        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }
}
