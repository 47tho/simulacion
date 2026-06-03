using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5.0f;
    [SerializeField] private float minDistance = 1.0f;
    [SerializeField] private float sensitivity = 0.2f;
    [SerializeField] private float minY = -20f;
    [SerializeField] private float maxY = 80f;
    [SerializeField] private LayerMask collisionLayers = ~0; // Default to all layers
    [SerializeField] private float cameraRadius = 0.2f;
    [SerializeField] private float smoothTime = 0.1f;

    private Vector2 rotation;
    private InputAction lookAction;
    private float currentDistance;
    private float distanceVelocity;

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
        
        currentDistance = distance;
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
        float targetDist = distance;
        RaycastHit hit;
        if (Physics.SphereCast(targetPos, cameraRadius, (desiredPos - targetPos).normalized, out hit, distance, collisionLayers))
        {
            targetDist = Mathf.Clamp(hit.distance - 0.1f, minDistance, distance);
        }

        // Smoothly adjust camera distance
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distanceVelocity, smoothTime);
        
        transform.position = targetPos - (rot * Vector3.forward * currentDistance);
        transform.rotation = rot;
    }
}

