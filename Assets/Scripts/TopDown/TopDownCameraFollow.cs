using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField]
    Vector3 offset = new Vector3(0f, 12f, -10f);

    [SerializeField]
    Vector3 rotationEuler = new Vector3(55f, 0f, 0f);

    [SerializeField, Min(0.01f)]
    float followSharpness = 12f;

    TopDownPlayerController target;

    void Awake()
    {
        target = FindFirstObjectByType<TopDownPlayerController>();
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

        Vector3 targetPosition = target.transform.position;
        Vector3 desiredPosition = targetPosition + offset;
        float smoothing = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothing);
        transform.rotation = Quaternion.Euler(rotationEuler);
    }
}