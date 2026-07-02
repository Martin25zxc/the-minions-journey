using System.Collections;
using UnityEngine;

/// <summary>
/// Cura aliados vivos asignados explícitamente. No revive.
/// Recomendado para fase 1 del boss comandante.    
/// </summary>
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyAllyHealAbility : MonoBehaviour, IEnemyAbility
{
    [Header("References")]
    [SerializeField] private EnemyActor actor;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyBossPhaseState phaseState;
    [SerializeField] private EnemyBossAnimatorBridge bossAnimatorBridge;
    [SerializeField] private EnemyAnimator enemyAnimatorFallback;
    [SerializeField] private EnemyBossSpecialCooldownGate specialCooldownGate;

    [Header("Profile")]
    [SerializeField] private EnemyAllyHealProfile profile;

    [Header("Allies")]
    [Tooltip("Aliados vivos que esta habilidad puede curar. No usar para revive.")]
    [SerializeField] private TopDownHealth[] allyHealths;

    [Header("Target Feedback")]
    [Tooltip("Prefab visual que aparece en el aliado seleccionado durante el casteo. Puede ser partículas/glow/runa. No requiere scripts.")]
    [SerializeField] private GameObject healTargetIndicatorPrefab;

    [Tooltip("Offset local respecto del transform del aliado. Para suelo: Y bajo. Para cabeza: Y alto.")]
    [SerializeField] private Vector3 indicatorLocalOffset = new Vector3(0f, 0.15f, 0f);

    [Tooltip("Si está activo, el indicador queda parentado al target y lo sigue. Recomendado para arqueros móviles.")]
    [SerializeField] private bool parentIndicatorToTarget = true;

    [Tooltip("Tiempo extra que el indicador queda visible después de aplicar la cura.")]
    [SerializeField, Min(0f)] private float indicatorExtraLifetimeAfterHeal = 0.35f;

    [Header("Runtime")]
    [SerializeField] private TopDownHealth selectedAlly;
    [SerializeField] private GameObject activeHealIndicator;

    private Coroutine clearIndicatorRoutine;
    private bool isRunning;
    private bool cancelRequested;
    private float nextAvailableTime;

    public bool IsRunning => isRunning;
    public bool IsOnCooldown => Time.time < nextAvailableTime;

    private void Awake()
    {
        if (actor == null) actor = GetComponent<EnemyActor>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (phaseState == null) phaseState = GetComponent<EnemyBossPhaseState>();
        if (bossAnimatorBridge == null) bossAnimatorBridge = GetComponent<EnemyBossAnimatorBridge>();
        if (enemyAnimatorFallback == null) enemyAnimatorFallback = GetComponent<EnemyAnimator>();
        if (specialCooldownGate == null) specialCooldownGate = GetComponent<EnemyBossSpecialCooldownGate>();
    }

    private void OnDisable()
    {
        Cancel();
    }

    public bool CanUse(Transform target)
    {
        if (actor == null || !actor.IsAlive || profile == null || target == null)
        {
            return false;
        }

        if (isRunning || IsOnCooldown)
        {
            return false;
        }

        if (phaseState != null && !phaseState.IsInsideRange(profile.MinPhase, profile.MaxPhase))
        {
            return false;
        }

        if (profile.UseSpecialCooldownGate && specialCooldownGate != null && !specialCooldownGate.CanUseSpecial)
        {
            return false;
        }

        selectedAlly = FindBestAllyToHeal();
        return selectedAlly != null;
    }

    public float GetPriority(Transform target)
    {
        return profile != null ? profile.Priority : 0f;
    }

    public IEnumerator Execute(Transform target)
    {
        if (!CanUse(target))
        {
            yield break;
        }

        isRunning = true;
        cancelRequested = false;
        movement?.Stop();

        if (selectedAlly != null)
        {
            movement?.FaceTarget(selectedAlly.transform.position);
            SpawnHealIndicator(selectedAlly);
        }

        if (bossAnimatorBridge != null)
        {
            bossAnimatorBridge.PlayCast();
        }
        else
        {
            enemyAnimatorFallback?.PlayRangedAttack();
        }

        yield return WaitWhileUsable(profile.TelegraphTime);
        if (!CanContinue(requireSelectedAllyAlive: true))
        {
            Finish(false);
            yield break;
        }

        if (selectedAlly != null && selectedAlly.IsAlive)
        {
            selectedAlly.Heal(profile.HealAmount);
        }

        ClearHealIndicatorDelayed(indicatorExtraLifetimeAfterHeal);

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    public void Cancel()
    {
        cancelRequested = true;
        selectedAlly = null;
        isRunning = false;
        ClearHealIndicatorImmediate();
    }

    private TopDownHealth FindBestAllyToHeal()
    {
        if (allyHealths == null || profile == null)
        {
            return null;
        }

        TopDownHealth best = null;
        float bestMissingPercent = 0f;

        for (int i = 0; i < allyHealths.Length; i++)
        {
            TopDownHealth health = allyHealths[i];
            if (health == null || !health.IsAlive)
            {
                continue;
            }

            if (health == actor.Health)
            {
                continue;
            }

            if (profile.MaxHealRange > 0f && HorizontalDistance(transform.position, health.transform.position) > profile.MaxHealRange)
            {
                continue;
            }

            float missing = health.MaxHealth - health.CurrentHealth;
            if (missing < profile.MinimumMissingHealthToCast)
            {
                continue;
            }

            float missingPercent = health.MaxHealth > 0f ? missing / health.MaxHealth : 0f;
            if (best == null || missingPercent > bestMissingPercent)
            {
                best = health;
                bestMissingPercent = missingPercent;
            }
        }

        return best;
    }

    private IEnumerator WaitWhileUsable(float duration)
    {
        float endTime = Time.time + Mathf.Max(0f, duration);
        while (Time.time < endTime)
        {
            if (!CanContinue(requireSelectedAllyAlive: true))
            {
                yield break;
            }

            if (selectedAlly != null)
            {
                movement?.FaceTarget(selectedAlly.transform.position);
            }

            yield return null;
        }
    }

    private bool CanContinue(bool requireSelectedAllyAlive)
    {
        if (cancelRequested
            || actor == null
            || !actor.IsAlive
            || profile == null
            || !isActiveAndEnabled
            || !gameObject.activeInHierarchy)
        {
            return false;
        }

        if (requireSelectedAllyAlive && (selectedAlly == null || !selectedAlly.IsAlive))
        {
            return false;
        }

        return true;
    }

    private void SpawnHealIndicator(TopDownHealth targetHealth)
    {
        ClearHealIndicatorImmediate();

        if (healTargetIndicatorPrefab == null || targetHealth == null)
        {
            return;
        }

        Transform targetTransform = targetHealth.transform;
        Transform parent = parentIndicatorToTarget ? targetTransform : null;
        Vector3 worldPosition = parentIndicatorToTarget
            ? targetTransform.TransformPoint(indicatorLocalOffset)
            : targetTransform.position + indicatorLocalOffset;

        activeHealIndicator = Instantiate(
            healTargetIndicatorPrefab,
            worldPosition,
            Quaternion.identity,
            parent
        );

        if (parentIndicatorToTarget && activeHealIndicator != null)
        {
            activeHealIndicator.transform.localPosition = indicatorLocalOffset;
            activeHealIndicator.transform.localRotation = Quaternion.identity;
        }
    }

    private void ClearHealIndicatorDelayed(float delay)
    {
        if (clearIndicatorRoutine != null)
        {
            StopCoroutine(clearIndicatorRoutine);
        }

        clearIndicatorRoutine = StartCoroutine(ClearIndicatorAfterDelay(delay));
    }

    private IEnumerator ClearIndicatorAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        ClearHealIndicatorImmediate();
    }

    private void ClearHealIndicatorImmediate()
    {
        if (clearIndicatorRoutine != null)
        {
            StopCoroutine(clearIndicatorRoutine);
            clearIndicatorRoutine = null;
        }

        if (activeHealIndicator != null)
        {
            Destroy(activeHealIndicator);
            activeHealIndicator = null;
        }
    }

    private void Finish(bool completedNormally)
    {
        if (completedNormally && profile != null)
        {
            nextAvailableTime = Time.time + profile.Cooldown;
            if (profile.UseSpecialCooldownGate)
            {
                specialCooldownGate?.NotifySpecialUsed(profile.GlobalCooldownAfterUse);
            }
        }

        if (!completedNormally)
        {
            ClearHealIndicatorImmediate();
        }

        selectedAlly = null;
        isRunning = false;
        cancelRequested = false;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
