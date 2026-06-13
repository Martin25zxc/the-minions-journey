using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ImpactReceiver : MonoBehaviour, IImpactReceiver
{
    public event Action<ImpactInfo> OnImpactReceived;
    public event Action<ImpactInfo> OnKnockbackStarted;
    public event Action<float> OnStunStarted;
    public event Action<float> OnImpactLockApplied;
    public event Action<ImpactInfo> OnImpactNoEffect;

    [Header("Permisos")]
    [Tooltip("Permite que este objeto sea empujado por impactos. Para jefes o enemigos pesados suele quedar apagado.")]
    [SerializeField]
    private bool canReceiveKnockback = true;

    [Tooltip("Permite que este objeto quede trabado por stun. Para jefes o enemigos especiales suele quedar apagado.")]
    [SerializeField]
    private bool canReceiveStun = true;

    [Tooltip("Reservado para futuro: cortar casteos, channeling o habilidades cargadas. En esta etapa no cancela ataques melee comunes.")]
    [SerializeField]
    private bool canReceiveInterrupt;

    [Header("Movimiento")]
    [Tooltip("Si hay Rigidbody, usa MovePosition para el empuje. Si no hay, mueve el Transform. Mantenerlo activo suele ser lo más seguro.")]
    [SerializeField]
    private bool useRigidbodyWhenAvailable = true;

    [Tooltip("Suaviza el empuje: arranca más fuerte y termina más suave. No cambia la distancia total.")]
    [SerializeField, Range(0.1f, 4f)]
    private float knockbackEasePower = 2f;

    private Rigidbody cachedRigidbody;
    private Coroutine knockbackRoutine;
    private Coroutine stunRoutine;
    private bool isStunned;
    private bool isShuttingDown;

    public bool IsStunned => isStunned;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    private void OnDisable()
    {
        StopActiveImpactRoutines();
    }

    private void OnDestroy()
    {
        isShuttingDown = true;
        StopActiveImpactRoutines();
    }

    public void ReceiveImpact(ImpactInfo impactInfo)
    {
        if (isShuttingDown || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        OnImpactReceived?.Invoke(impactInfo);

        bool shouldKnockback = canReceiveKnockback && impactInfo.HasKnockback;
        bool shouldStun = canReceiveStun && impactInfo.HasStun;
        bool shouldInterrupt = impactInfo.InterruptCurrentAction && canReceiveInterrupt;

        if (!shouldKnockback && !shouldStun && !shouldInterrupt)
        {
            OnImpactNoEffect?.Invoke(impactInfo);
        }

        if (shouldKnockback)
        {
            OnKnockbackStarted?.Invoke(impactInfo);

            if (knockbackRoutine != null)
            {
                StopCoroutine(knockbackRoutine);
            }

            knockbackRoutine = StartCoroutine(KnockbackRoutine(
                impactInfo.Direction,
                impactInfo.KnockbackDistance,
                impactInfo.KnockbackDuration));
        }

        float lockDuration = 0f;
        if (shouldKnockback)
        {
            // Aunque no sea stun, durante el empuje conviene pausar movimiento propio
            // para que la IA o el input no peleen contra el knockback.
            lockDuration = Mathf.Max(lockDuration, impactInfo.KnockbackDuration);
        }

        if (shouldStun)
        {
            lockDuration = Mathf.Max(lockDuration, impactInfo.StunDuration);
            OnStunStarted?.Invoke(impactInfo.StunDuration);
            StartLocalStunTimer(impactInfo.StunDuration);
        }

        if (lockDuration > 0f)
        {
            OnImpactLockApplied?.Invoke(lockDuration);
            NotifyImpactLockables(lockDuration);
        }

        if (shouldInterrupt)
        {
            // Futuro: llamar a un IInterruptible o a una acción cancelable concreta.
            // Hoy queda intencionalmente sin efecto para no cortar ataques melee simples de forma confusa.
        }
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float distance, float duration)
    {
        if (isShuttingDown || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            knockbackRoutine = null;
            yield break;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f || distance <= 0f || duration <= 0f)
        {
            knockbackRoutine = null;
            yield break;
        }

        direction.Normalize();

        float elapsed = 0f;
        Vector3 totalOffset = direction * distance;
        Vector3 appliedOffset = Vector3.zero;

        while (elapsed < duration && !isShuttingDown && isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, knockbackEasePower);
            Vector3 desiredOffset = totalOffset * eased;
            MoveBy(desiredOffset - appliedOffset);
            appliedOffset = desiredOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!isShuttingDown && isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            MoveBy(totalOffset - appliedOffset);
        }

        knockbackRoutine = null;
    }

    private void MoveBy(Vector3 delta)
    {
        if (isShuttingDown || !isActiveAndEnabled || !gameObject.activeInHierarchy || delta.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        if (useRigidbodyWhenAvailable && cachedRigidbody != null && cachedRigidbody.gameObject.activeInHierarchy)
        {
            cachedRigidbody.MovePosition(cachedRigidbody.position + delta);
            return;
        }

        transform.position += delta;
    }

    private void StartLocalStunTimer(float duration)
    {
        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
        }

        stunRoutine = StartCoroutine(LocalStunRoutine(duration));
    }

    private IEnumerator LocalStunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        if (!isShuttingDown)
        {
            isStunned = false;
        }

        stunRoutine = null;
    }

    private void StopActiveImpactRoutines()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }

        isStunned = false;
    }

    private void NotifyImpactLockables(float duration)
    {
        if (isShuttingDown || !gameObject.activeInHierarchy)
        {
            return;
        }

        MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null || !behaviours[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            if (behaviours[i] is IImpactLockable lockable)
            {
                lockable.ApplyImpactLock(duration);
            }
        }
    }
}
