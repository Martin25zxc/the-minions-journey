using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraShake : MonoBehaviour
{
    public enum ShakeDirection { All, Horizontal, Vertical }

    public static event System.Action<float, float, ShakeDirection> OnShake;

    private float shakeTimer;
    private float shakeDuration;
    private float initialIntensity;
    private ShakeDirection currentDirection;

    void Awake()
    {
        OnShake += Shake;
    }

    void OnDestroy()
    {
        OnShake -= Shake;
    }

    void LateUpdate()
    {
        if (shakeTimer <= 0f) return;

        shakeTimer -= Time.deltaTime;

        // Decaimiento gradual
        float t = shakeTimer / shakeDuration;
        float intensity = Mathf.Lerp(0f, initialIntensity, t);

        transform.position += CalculateOffset(intensity);
    }

    private Vector3 CalculateOffset(float intensity)
    {
        return currentDirection switch
        {
            ShakeDirection.Horizontal => new Vector3(
                Random.Range(-1f, 1f) * intensity, 0f, 0f),

            ShakeDirection.Vertical => new Vector3(
                0f, Random.Range(-1f, 1f) * intensity, 0f),

            _ => new Vector3(
                Random.Range(-1f, 1f) * intensity,
                Random.Range(-1f, 1f) * intensity,
                0f)
        };
    }

    private void Shake(float intensity, float duration, ShakeDirection direction)
    {
        initialIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
        currentDirection = direction;
    }

    public static void Trigger(float intensity, float duration, ShakeDirection direction = ShakeDirection.All)
    {
        OnShake?.Invoke(intensity, duration, direction);
    }
}