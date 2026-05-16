using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField]
    Vector3 offset = new Vector3(0f, 12f, -10f);

    [SerializeField, Min(0.01f)]
    float rotationSensitivity = 0.25f;

    [SerializeField, Min(0.01f)]
    float followSharpness = 12f;

    TopDownPlayerController target;
    float yaw;

    void Awake()
    {
        target = FindFirstObjectByType<TopDownPlayerController>();
        yaw = transform.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            target = FindFirstObjectByType<TopDownPlayerController>();
            if (target == null)
            {
                return;
            }
        }

        ReadOrbitInput();

        Vector3 targetPosition = target.transform.position;
        Quaternion orbitRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 desiredPosition = targetPosition + orbitRotation * offset;
        float smoothing = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothing);
        Vector3 lookTarget = targetPosition + Vector3.up * 1f;
        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    void ReadOrbitInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse?.middleButton.isPressed != true)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * rotationSensitivity;
    }
}