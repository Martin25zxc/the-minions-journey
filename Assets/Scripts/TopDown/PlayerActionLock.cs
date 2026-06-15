using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerActionLock : MonoBehaviour, IImpactLockable
{
    [Header("Lock Permissions")]
    [Tooltip("Bloquea el movimiento por input mientras dura un impacto/stun recibido.")]
    [SerializeField]
    private bool lockMovement = true;

    [Tooltip("Bloquea nuevos inputs de combate mientras dura un impacto/stun recibido.")]
    [SerializeField]
    private bool lockCombat = true;

    [Header("Timing")]
    [Tooltip("Duración mínima del lock recibido. Normalmente puede quedar en 0.")]
    [SerializeField, Min(0f)]
    private float minimumLockDuration = 0f;

    [Tooltip("Duración máxima de seguridad. Evita locks accidentales demasiado largos. Usar 0 para no limitar.")]
    [SerializeField, Min(0f)]
    private float maximumLockDuration = 2f;

    [Tooltip("Usar tiempo sin escalar. Para combate normal conviene dejarlo apagado.")]
    [SerializeField]
    private bool useUnscaledTime;

    [Header("Debug")]
    [SerializeField]
    private bool logLocks;

    private float movementLockedUntil;
    private float combatLockedUntil;

    public bool IsMovementLocked => lockMovement && CurrentTime < movementLockedUntil;
    public bool IsCombatLocked => lockCombat && CurrentTime < combatLockedUntil;
    public bool IsAnyLocked => IsMovementLocked || IsCombatLocked;

    private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

    public void ApplyImpactLock(float duration)
    {
        if (!isActiveAndEnabled || duration <= 0f)
        {
            return;
        }

        float finalDuration = Mathf.Max(duration, minimumLockDuration);
        if (maximumLockDuration > 0f)
        {
            finalDuration = Mathf.Min(finalDuration, maximumLockDuration);
        }

        if (finalDuration <= 0f)
        {
            return;
        }

        float lockedUntil = CurrentTime + finalDuration;

        if (lockMovement)
        {
            movementLockedUntil = Mathf.Max(movementLockedUntil, lockedUntil);
        }

        if (lockCombat)
        {
            combatLockedUntil = Mathf.Max(combatLockedUntil, lockedUntil);
        }

        if (logLocks)
        {
            Debug.Log($"{name} action locked for {finalDuration:0.00}s", this);
        }
    }

    public void ClearAllLocks()
    {
        movementLockedUntil = 0f;
        combatLockedUntil = 0f;
    }

    public void ClearMovementLock()
    {
        movementLockedUntil = 0f;
    }

    public void ClearCombatLock()
    {
        combatLockedUntil = 0f;
    }

    private void OnDisable()
    {
        ClearAllLocks();
    }
}
