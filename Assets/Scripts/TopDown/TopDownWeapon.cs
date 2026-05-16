using UnityEngine;

[DisallowMultipleComponent]
public abstract class TopDownWeapon : MonoBehaviour
{
    public abstract bool TryLightAttack(Vector3 facingDirection);

    public abstract bool TryHeavyAttack(Vector3 facingDirection);

    public virtual bool TryComboAttack(TopDownCombatComboDefinition combo, Vector3 facingDirection)
    {
        return false;
    }

    protected static Vector3 NormalizeFacingDirection(Vector3 facingDirection, Vector3 fallbackDirection)
    {
        if (facingDirection.sqrMagnitude < 0.0001f)
        {
            facingDirection = fallbackDirection;
        }

        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return facingDirection.normalized;
    }
}