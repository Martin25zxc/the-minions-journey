using UnityEngine;

[DisallowMultipleComponent]
public abstract class TopDownPower : MonoBehaviour
{
    public abstract bool TryActivate(Vector3 facingDirection);
}