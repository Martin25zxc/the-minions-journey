using UnityEngine;

[DisallowMultipleComponent]
public abstract class TopDownPower : MonoBehaviour
{
    public abstract bool TryActivate(Vector3 facingDirection);

    public virtual bool TryComboActivate(TopDownCombatComboDefinition combo, Vector3 facingDirection)
    {
        return false;
    }
}