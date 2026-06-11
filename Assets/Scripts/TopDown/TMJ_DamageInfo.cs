using UnityEngine;

/// <summary>
/// Datos mínimos de un golpe/daño dentro de The Minion's Journey.
/// Por ahora transporta daño, origen y fuente. Más adelante puede crecer para stun,
/// knockback, interrupciones o instigator sin cambiar el contrato de daño otra vez.
/// </summary>
public readonly struct TMJ_DamageInfo
{
    public TMJ_DamageInfo(float damage, Vector3 sourcePosition, GameObject source = null)
    {
        Damage = damage;
        SourcePosition = sourcePosition;
        Source = source;
    }

    public float Damage { get; }

    public Vector3 SourcePosition { get; }

    public GameObject Source { get; }
}
