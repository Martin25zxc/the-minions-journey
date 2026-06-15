using UnityEngine;

/// <summary>
/// Utilidades para traducir un TMJ_DamageInfo a una reacción visual.
///
/// La reacción de animación no debería adivinar usando siempre OriginPosition:
/// en proyectiles o áreas, OriginPosition puede ser el punto técnico del impacto,
/// no una referencia estable del lado desde donde llegó la amenaza.
/// </summary>
public static class TMJ_DamageReactionUtility
{
    public static bool TryGetDirectionToDamageSource(
        TMJ_DamageInfo damageInfo,
        Transform receiver,
        out Vector3 directionToSource)
    {
        directionToSource = Vector3.zero;

        if (receiver == null)
        {
            return false;
        }

        // Prioridad 1: dirección explícita del golpe.
        // DirectionFromSourceToTarget apunta desde la fuente hacia el receptor.
        // Para saber dónde está la fuente respecto del receptor, invertimos la dirección.
        if (damageInfo.HasDirectionFromSourceToTarget)
        {
            directionToSource = -damageInfo.DirectionFromSourceToTarget;
            directionToSource.y = 0f;

            if (directionToSource.sqrMagnitude > 0.0001f)
            {
                directionToSource.Normalize();
                return true;
            }
        }

        // Prioridad 2: actor responsable. Es estable para melee/proyectiles simples.
        if (damageInfo.Instigator != null)
        {
            directionToSource = damageInfo.Instigator.transform.position - receiver.position;
            directionToSource.y = 0f;

            if (directionToSource.sqrMagnitude > 0.0001f)
            {
                directionToSource.Normalize();
                return true;
            }
        }

        // Prioridad 3: punto técnico del daño. Fallback para daño ambiental/legacy.
        directionToSource = damageInfo.OriginPosition - receiver.position;
        directionToSource.y = 0f;

        if (directionToSource.sqrMagnitude > 0.0001f)
        {
            directionToSource.Normalize();
            return true;
        }

        return false;
    }

    public static bool IsDamageSourceInFront(
        TMJ_DamageInfo damageInfo,
        Transform receiver,
        float frontBackThreshold = 0f)
    {
        if (receiver == null)
        {
            return true;
        }

        if (!TryGetDirectionToDamageSource(damageInfo, receiver, out Vector3 directionToSource))
        {
            return true;
        }

        Vector3 forward = receiver.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        forward.Normalize();
        float dot = Vector3.Dot(forward, directionToSource);
        return dot >= frontBackThreshold;
    }
}
