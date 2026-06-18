using UnityEngine;

/// <summary>
/// Traduce daño recibido en estimulos de IA.
///
/// Ejemplos:
/// - Si el enemigo recibe daño con Instigator = Player, entra en combat contra Player.
/// - Si recibe daño sin Instigator claro, investiga OriginPosition.
///
/// No reproduce hits: eso sigue siendo responsabilidad de EnemyAnimator.
/// No aplica knockback/stun: eso sigue siendo responsabilidad de ImpactReceiver.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
[RequireComponent(typeof(EnemyAwareness))]
public sealed class EnemyDamageReply : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [SerializeField]
    private EnemyAwareness awareness;

    [Header("Aggro")]
    [SerializeField]
    private bool useInstigatorAsCombatTarget = true;

    [Tooltip("Layers validas para convertir un Instigator en combat target. Por defecto acepta todo; si queres ser estricto, dejalo solo en Player.")]
    [SerializeField]
    private LayerMask validCombatTargetLayers = ~0;

    [Tooltip("Si el Instigator es un hijo/hitbox, intenta subir hasta un TopDownHealth padre para usar el actor real como target.")]
    [SerializeField]
    private bool resolveInstigatorHealthRoot = true;

    [SerializeField, Min(0f)]
    private float damageCombatMemoryDuration = 4f;

    [Header("Investigation")]
    [SerializeField]
    private bool investigateUnknownDamage = true;

    [SerializeField, Min(0f)]
    private float unknownDamageInvestigationDuration = 4f;

    [Header("Debug")]
    [SerializeField]
    private bool logDamageStimuli;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (actor != null)
        {
            actor.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (actor != null)
        {
            actor.Damaged -= HandleDamaged;
        }
    }

    private void HandleDamaged(EnemyActor damagedActor, TMJ_DamageInfo damageInfo)
    {
        if (awareness == null || actor == null || !actor.IsAlive)
        {
            return;
        }

        Transform combatTarget = useInstigatorAsCombatTarget
            ? ResolveCombatTarget(damageInfo.Instigator)
            : null;

        if (combatTarget != null)
        {
            if (logDamageStimuli)
            {
                Debug.Log($"[{nameof(EnemyDamageReply)}] {name} aggro by damage. Target: {combatTarget.name}.", this);
            }

            awareness.ReceiveStimulus(EnemyStimulus.DamageWithTarget(
                combatTarget,
                damageInfo.OriginPosition,
                damageInfo.Instigator,
                damageInfo.DamageCauser,
                damageCombatMemoryDuration));

            return;
        }

        if (!investigateUnknownDamage)
        {
            return;
        }

        if (logDamageStimuli)
        {
            Debug.Log($"[{nameof(EnemyDamageReply)}] {name} investigates unknown damage at {damageInfo.OriginPosition}.", this);
        }

        awareness.ReceiveStimulus(EnemyStimulus.DamageAtPosition(
            damageInfo.OriginPosition,
            damageInfo.Instigator,
            damageInfo.DamageCauser,
            unknownDamageInvestigationDuration));
    }

    private Transform ResolveCombatTarget(GameObject instigator)
    {
        if (instigator == null)
        {
            return null;
        }

        Transform candidate = instigator.transform;

        if (resolveInstigatorHealthRoot)
        {
            TopDownHealth healthRoot = instigator.GetComponentInParent<TopDownHealth>();
            if (healthRoot != null)
            {
                candidate = healthRoot.transform;
            }
        }

        if (candidate == null || IsSelfOrChild(candidate))
        {
            return null;
        }

        if (!IsLayerAllowed(candidate.gameObject.layer, validCombatTargetLayers))
        {
            return null;
        }

        return candidate;
    }

    private bool IsSelfOrChild(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        Transform self = transform;
        return candidate == self || candidate.IsChildOf(self) || self.IsChildOf(candidate);
    }

    private static bool IsLayerAllowed(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void ResolveReferences()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (awareness == null)
        {
            awareness = GetComponent<EnemyAwareness>();
        }
    }

    private void OnValidate()
    {
        damageCombatMemoryDuration = Mathf.Max(0f, damageCombatMemoryDuration);
        unknownDamageInvestigationDuration = Mathf.Max(0f, unknownDamageInvestigationDuration);
    }
}
