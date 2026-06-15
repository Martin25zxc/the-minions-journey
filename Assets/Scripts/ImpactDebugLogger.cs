using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ImpactReceiver))]
public sealed class ImpactDebugLogger : MonoBehaviour
{
    [SerializeField]
    private bool logImpactReceived = true;

    [SerializeField]
    private bool logKnockback = true;

    [SerializeField]
    private bool logStun = true;

    [SerializeField]
    private bool logImpactLock = true;

    [SerializeField]
    private bool logNoEffect = true;

    private ImpactReceiver impactReceiver;

    private void Awake()
    {
        EnsureImpactReceiverReference();
    }

    private void OnEnable()
    {
        EnsureImpactReceiverReference();

        if (impactReceiver == null)
        {
            return;
        }

        impactReceiver.OnImpactReceived += HandleImpactReceived;
        impactReceiver.OnKnockbackStarted += HandleKnockbackStarted;
        impactReceiver.OnStunStarted += HandleStunStarted;
        impactReceiver.OnImpactLockApplied += HandleImpactLockApplied;
        impactReceiver.OnImpactNoEffect += HandleImpactNoEffect;
    }

    private void OnDisable()
    {
        if (impactReceiver == null)
        {
            return;
        }

        impactReceiver.OnImpactReceived -= HandleImpactReceived;
        impactReceiver.OnKnockbackStarted -= HandleKnockbackStarted;
        impactReceiver.OnStunStarted -= HandleStunStarted;
        impactReceiver.OnImpactLockApplied -= HandleImpactLockApplied;
        impactReceiver.OnImpactNoEffect -= HandleImpactNoEffect;
    }

    private void EnsureImpactReceiverReference()
    {
        if (impactReceiver == null)
        {
            impactReceiver = GetComponent<ImpactReceiver>();
        }
    }

    private void HandleImpactReceived(ImpactInfo impactInfo)
    {
        if (!logImpactReceived)
        {
            return;
        }

        string sourceName = impactInfo.Source != null ? impactInfo.Source.name : "Unknown Source";
        Debug.Log(
            $"[Impact] {name} received impact from {sourceName}. " +
            $"Direction={impactInfo.Direction}, Knockback={impactInfo.KnockbackDistance:0.##}/{impactInfo.KnockbackDuration:0.##}s, " +
            $"Stun={impactInfo.StunDuration:0.##}s, Interrupt={impactInfo.InterruptCurrentAction}.",
            this);
    }

    private void HandleKnockbackStarted(ImpactInfo impactInfo)
    {
        if (!logKnockback)
        {
            return;
        }

        Debug.Log(
            $"[Knockback] {name}: distance={impactInfo.KnockbackDistance:0.##}, " +
            $"duration={impactInfo.KnockbackDuration:0.##}s, direction={impactInfo.Direction}.",
            this);
    }

    private void HandleStunStarted(float duration)
    {
        if (!logStun)
        {
            return;
        }

        Debug.Log($"[Stun] {name}: duration={duration:0.##}s.", this);
    }

    private void HandleImpactLockApplied(float duration)
    {
        if (!logImpactLock)
        {
            return;
        }

        Debug.Log($"[ImpactLock] {name}: duration={duration:0.##}s.", this);
    }

    private void HandleImpactNoEffect(ImpactInfo impactInfo)
    {
        if (!logNoEffect)
        {
            return;
        }

        Debug.Log(
            $"[Impact:NoEffect] {name} received an impact but permissions/settings produced no knockback, stun or interrupt. " +
            $"HasKnockback={impactInfo.HasKnockback}, HasStun={impactInfo.HasStun}, Interrupt={impactInfo.InterruptCurrentAction}.",
            this);
    }
}
