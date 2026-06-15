using UnityEngine;

/// <summary>
/// Datos de un evento de daño dentro de The Minion's Journey.
///
/// Separación importante:
/// - Instigator: actor responsable del daño. Ej: Ranger, Barbarian, Player.
/// - DamageCauser: objeto que físicamente causó el daño. Ej: flecha, hitbox, arma, explosión.
/// - OriginPosition: punto espacial/técnico desde donde se originó o aplicó el daño.
/// - HitPoint: punto aproximado de impacto sobre el objetivo, si lo conocemos.
/// - DirectionFromSourceToTarget: dirección real del golpe/proyectil/onda hacia el objetivo.
///
/// </summary>
public readonly struct TMJ_DamageInfo
{
    public TMJ_DamageInfo(
        float damage,
        Vector3 originPosition,
        GameObject instigator = null,
        GameObject damageCauser = null,
        Vector3? hitPoint = null,
        Vector3? directionFromSourceToTarget = null)
    {
        Damage = damage;
        OriginPosition = originPosition;
        Instigator = instigator;
        DamageCauser = damageCauser != null ? damageCauser : instigator;

        HasHitPoint = hitPoint.HasValue;
        HitPoint = hitPoint.GetValueOrDefault();

        if (directionFromSourceToTarget.HasValue && directionFromSourceToTarget.Value.sqrMagnitude > 0.0001f)
        {
            HasDirectionFromSourceToTarget = true;
            DirectionFromSourceToTarget = directionFromSourceToTarget.Value.normalized;
        }
        else
        {
            HasDirectionFromSourceToTarget = false;
            DirectionFromSourceToTarget = Vector3.zero;
        }
    }

    public float Damage { get; }

    /// <summary>
    /// Punto espacial/técnico del daño: centro de hitbox, centro de explosión, punto del proyectil, etc.
    /// No necesariamente es la posición del atacante.
    /// </summary>
    public Vector3 OriginPosition { get; }

    /// <summary>
    /// Actor responsable del daño. Sirve para atribución, aggro, kill credit y fallback de reacción.
    /// </summary>
    public GameObject Instigator { get; }

    /// <summary>
    /// Objeto que físicamente causó el daño. Ej: proyectil, hitbox, arma o el mismo atacante.
    /// </summary>
    public GameObject DamageCauser { get; }

    public bool HasHitPoint { get; }
    public Vector3 HitPoint { get; }

    public bool HasDirectionFromSourceToTarget { get; }
    public Vector3 DirectionFromSourceToTarget { get; }
}
