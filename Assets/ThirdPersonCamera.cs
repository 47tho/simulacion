using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5.0f;
    [SerializeField] private float sensitivity = 0.2f;
    [SerializeField] private float minY = -20f;
    [SerializeField] private float maxY = 80f;
    [SerializeField] private LayerMask collisionLayers = ~0; // Default to all layers
    [SerializeField] private float cameraRadius = 0.2f;

    private Vector2 rotation;
    private InputAction lookAction;

    private void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
        
        // Ignore Player layer for camera collision to avoid snapping to character head
        if (target != null)
        {
            collisionLayers &= ~(1 << target.gameObject.layer);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        rotation.x += lookInput.x * sensitivity;
        rotation.y -= lookInput.y * sensitivity;
        rotation.y = Mathf.Clamp(rotation.y, minY, maxY);

        Quaternion rot = Quaternion.Euler(rotation.y, rotation.x, 0);
        Vector3 targetPos = target.position + Vector3.up * 1.5f;
        Vector3 desiredPos = targetPos - (rot * Vector3.forward * distance);

        // Collision Check
        RaycastHit hit;
        if (Physics.SphereCast(targetPos, cameraRadius, (desiredPos - targetPos).normalized, out hit, distance, collisionLayers))
        {
            transform.position = targetPos + (desiredPos - targetPos).normalized * (hit.distance - 0.1f);
        }
        else
        {
            transform.position = desiredPos;
        }

        transform.rotation = rot;
    }
}

