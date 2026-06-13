using UnityEngine;

public readonly struct ImpactInfo
{
    public ImpactInfo(
        GameObject source,
        Vector3 sourcePosition,
        Vector3 direction,
        float knockbackDistance,
        float knockbackDuration,
        float stunDuration,
        bool interruptCurrentAction)
    {
        Source = source;
        SourcePosition = sourcePosition;

        direction.y = 0f;
        Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;

        KnockbackDistance = Mathf.Max(0f, knockbackDistance);
        KnockbackDuration = Mathf.Max(0f, knockbackDuration);
        StunDuration = Mathf.Max(0f, stunDuration);

        // Preparado para enemigos futuros con casteos/channeling/habilidades cargadas.
        // En esta etapa no lo usamos para cancelar ataques melee comunes.
        InterruptCurrentAction = interruptCurrentAction;
    }

    public GameObject Source { get; }
    public Vector3 SourcePosition { get; }
    public Vector3 Direction { get; }
    public float KnockbackDistance { get; }
    public float KnockbackDuration { get; }
    public float StunDuration { get; }
    public bool InterruptCurrentAction { get; }

    public bool HasKnockback => KnockbackDistance > 0f && KnockbackDuration > 0f && Direction.sqrMagnitude > 0.0001f;
    public bool HasStun => StunDuration > 0f;
}
