using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Castea orbes orbitantes alrededor del boss.
/// Pensado como habilidad de fase 2 para obligar al jugador a alejarse/reubicarse.
/// </summary>
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyOrbitingOrbsAbility : MonoBehaviour, IEnemyAbility, IEnemyEncounterResettable
{
    [Header("References")]
    [SerializeField] private EnemyActor actor;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyBossPhaseState phaseState;
    [SerializeField] private EnemyBossAnimatorBridge bossAnimatorBridge;
    [SerializeField] private EnemyAnimator enemyAnimatorFallback;
    [SerializeField] private EnemyBossSpecialCooldownGate specialCooldownGate;
    [SerializeField] private Transform orbitAnchor;

    [Header("Profile")]
    [SerializeField] private EnemyOrbitingOrbsProfile profile;

    [Header("Runtime")]
    [SerializeField] private List<EnemyOrbitingOrbHazard> activeOrbs = new List<EnemyOrbitingOrbHazard>();

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
        if (orbitAnchor == null) orbitAnchor = transform;
    }

    private void OnDisable()
    {
        Cancel();
        ClearActiveOrbs();
    }

    public bool CanUse(Transform target)
    {
        CleanupNullOrbs();

        if (actor == null || !actor.IsAlive || profile == null || target == null)
        {
            return false;
        }

        if (isRunning || IsOnCooldown || profile.OrbPrefab == null)
        {
            return false;
        }

        if (profile.PreventCastWhileOrbsActive && activeOrbs.Count > 0)
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

        float distance = HorizontalDistance(transform.position, target.position);
        return profile.IsDistanceInRange(distance);
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

        if (target != null)
        {
            movement?.FaceTarget(target.position);
        }

        if (bossAnimatorBridge != null)
        {
            bossAnimatorBridge.PlayCast();
        }
        else
        {
            enemyAnimatorFallback?.PlayRangedAttack();
        }

        yield return WaitAiming(profile.TelegraphTime, target);
        if (!CanContinue(target))
        {
            Finish(false);
            yield break;
        }

        SpawnOrbs();

        yield return WaitWhileUsable(profile.RecoveryTime);
        Finish(true);
    }

    public void Cancel()
    {
        cancelRequested = true;
        isRunning = false;
    }

    public void ResetForEncounter()
    {
        Cancel();
        ClearActiveOrbs();
    }

    private void SpawnOrbs()
    {
        ClearActiveOrbs();

        int count = Mathf.Max(1, profile.OrbCount);
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = step * i;
            EnemyOrbitingOrbHazard orb = Instantiate(profile.OrbPrefab, orbitAnchor.position, Quaternion.identity, null);
            if (orb == null)
            {
                continue;
            }

            orb.Initialize(
                orbitAnchor,
                gameObject,
                angle,
                profile.OrbitRadius,
                profile.OrbitDegreesPerSecond,
                profile.Duration,
                profile.HeightOffset,
                profile.HitRadius,
                profile.Damage,
                profile.DamageCooldownPerTarget,
                profile.TargetLayers,
                profile.ApplyImpact,
                profile.KnockbackDistance,
                profile.KnockbackDuration,
                profile.StunDuration,
                profile.InterruptCurrentAction);

            activeOrbs.Add(orb);
        }
    }

    private IEnumerator WaitAiming(float duration, Transform target)
    {
        float endTime = Time.time + Mathf.Max(0f, duration);
        while (Time.time < endTime)
        {
            if (!CanContinue(target))
            {
                yield break;
            }

            if (target != null)
            {
                movement?.FaceTarget(target.position);
            }

            yield return null;
        }
    }

    private IEnumerator WaitWhileUsable(float duration)
    {
        float endTime = Time.time + Mathf.Max(0f, duration);
        while (Time.time < endTime)
        {
            if (cancelRequested || actor == null || !actor.IsAlive)
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool CanContinue(Transform target)
    {
        return !cancelRequested
            && actor != null
            && actor.IsAlive
            && profile != null
            && target != null
            && isActiveAndEnabled
            && gameObject.activeInHierarchy;
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

        isRunning = false;
        cancelRequested = false;
    }

    private void ClearActiveOrbs()
    {
        for (int i = activeOrbs.Count - 1; i >= 0; i--)
        {
            EnemyOrbitingOrbHazard orb = activeOrbs[i];
            if (orb != null)
            {
                Destroy(orb.gameObject);
            }
        }

        activeOrbs.Clear();
    }

    private void CleanupNullOrbs()
    {
        for (int i = activeOrbs.Count - 1; i >= 0; i--)
        {
            if (activeOrbs[i] == null)
            {
                activeOrbs.RemoveAt(i);
            }
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
