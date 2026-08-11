using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Isometric View")]
    [SerializeField] private Vector3 offset = new Vector3(9f, 12f, -9f);
    [SerializeField] private float followSmoothTime = 0.15f;
    [SerializeField] private bool lookAtTarget = true;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            followSmoothTime);

        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
