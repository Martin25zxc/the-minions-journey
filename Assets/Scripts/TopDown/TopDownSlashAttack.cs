using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownSlashAttack : TopDownWeapon
{
    [Header("Weapon Loadout")]
    [SerializeField]
    private TMJ_WeaponLoadout weaponLoadout;

    [Header("Attack Profiles")]
    [Tooltip("Profile usado por click izquierdo / ataque liviano. Es obligatorio: si falta, el ataque no se ejecuta para evitar valores ocultos en código.")]
    [SerializeField]
    private PlayerMeleeAttackProfile lightAttackProfile;

    [Tooltip("Profile usado por click derecho / ataque pesado. Es obligatorio: si falta, el ataque no se ejecuta para evitar valores ocultos en código.")]
    [SerializeField]
    private PlayerMeleeAttackProfile heavyAttackProfile;

    [Header("Hit Detection")]
    [SerializeField, Min(4)]
    private int visualSegments = 18;

    [Tooltip("Altura desde donde se calcula/dibuja el golpe. Sirve para que el slash salga más o menos desde la zona de la mano/torso, no desde el piso.")]
    [SerializeField, Min(0.01f)]
    private float hitHeight = 1f;

    [SerializeField]
    private LayerMask hittableLayers = ~0;

    private float nextLightAttackTime;
    private float nextHeavyAttackTime;
    private float nextComboAttackTime;
    private bool missingLightProfileWarningShown;
    private bool missingHeavyProfileWarningShown;

    private void Awake()
    {
        if (weaponLoadout == null)
        {
            weaponLoadout = GetComponent<TMJ_WeaponLoadout>();
        }
    }

    public override bool TryLightAttack(Vector3 facingDirection)
    {
        ResolvedMeleeAttack attack = GetLightAttack();
        return TryAttack(ref nextLightAttackTime, facingDirection, attack, 1f);
    }

    public override bool TryHeavyAttack(Vector3 facingDirection)
    {
        ResolvedMeleeAttack attack = GetHeavyAttack();
        return TryAttack(ref nextHeavyAttackTime, facingDirection, attack, 1f);
    }

    public override bool TryComboAttack(TopDownCombatComboDefinition combo, Vector3 facingDirection)
    {
        if (combo == null || combo.Target != TopDownCombatComboTarget.Weapon)
        {
            return false;
        }

        ResolvedMeleeAttack baseAttack = GetComboBaseAttack(combo);
        ResolvedMeleeAttack comboAttack = baseAttack.WithMultipliers(
            combo.RangeMultiplier,
            combo.ArcMultiplier,
            combo.CooldownMultiplier,
            combo.VisualDurationMultiplier,
            combo.VisualWidthMultiplier,
            combo.UseSlashColorOverride ? combo.SlashColorOverride : baseAttack.SlashColor);

        return TryAttack(ref nextComboAttackTime, facingDirection, comboAttack, combo.DamageMultiplier);
    }

    private ResolvedMeleeAttack GetComboBaseAttack(TopDownCombatComboDefinition combo)
    {
        if (combo.OverrideAttackProfile != null)
        {
            return ResolvedMeleeAttack.FromAsset(combo.OverrideAttackProfile);
        }

        // Si el combo no tiene profile propio, cae al Light/Heavy configurado.
        // Esto mantiene compatibilidad con combos simples, pero para combos con identidad fuerte
        // como LeapSlash o SpinSlash conviene asignar Override Attack Profile.
        return combo.WeaponAttackStyle == TopDownCombatAttackStyle.Light
            ? GetLightAttack()
            : GetHeavyAttack();
    }

    private bool TryAttack(ref float nextAttackTime, Vector3 facingDirection, ResolvedMeleeAttack attack, float damageMultiplier)
    {
        if (!attack.IsValid)
        {
            return false;
        }

        if (Time.time < nextAttackTime)
        {
            return false;
        }

        facingDirection = NormalizeFacingDirection(facingDirection, transform.forward);
        nextAttackTime = Time.time + attack.Cooldown;

        // El bonus del arma se tira cuando el ataque queda aceptado, no cuando pega.
        // Así no cambia el daño si el jugador cambia el loadout durante el delay del impacto.
        float damage = CalculateFinalDamage(attack.BaseDamage, damageMultiplier, attack.WeaponUseSlot);

        if (attack.ImpactDelaySeconds <= 0f)
        {
            ResolveAttackImpact(facingDirection, damage, attack);
            return true;
        }

        StartCoroutine(ResolveAttackImpactAfterDelay(facingDirection, damage, attack));
        return true;
    }

    private IEnumerator ResolveAttackImpactAfterDelay(Vector3 facingDirection, float damage, ResolvedMeleeAttack attack)
    {
        yield return new WaitForSeconds(attack.ImpactDelaySeconds);
        ResolveAttackImpact(facingDirection, damage, attack);
    }

    private void ResolveAttackImpact(Vector3 facingDirection, float damage, ResolvedMeleeAttack attack)
    {
        Vector3 impactOrigin = CalculateImpactOrigin(facingDirection, attack.ImpactOriginOffset);

        ResolveHits(impactOrigin, facingDirection, damage, attack);
        StartCoroutine(PlaySlashVisual(impactOrigin, facingDirection, attack.AttackRange, attack.AttackArc, attack.VisualDuration, attack.VisualWidth, attack.SlashColor));
    }

    private Vector3 CalculateImpactOrigin(Vector3 facingDirection, Vector2 impactOriginOffset)
    {
        Vector3 localRight = Vector3.Cross(Vector3.up, facingDirection).normalized;

        // Convención del PlayerMeleeAttackProfile:
        // X = derecha/izquierda local del jugador.
        // Y = adelante/atrás según hacia dónde mira.
        // Si el offset es 0,0, esto no cambia nada; por eso no necesitamos un bool extra.
        return transform.position + localRight * impactOriginOffset.x + facingDirection * impactOriginOffset.y;
    }

    private float CalculateFinalDamage(float baseDamage, float damageMultiplier, TMJ_WeaponUseSlot weaponUseSlot)
    {
        float safeBaseDamage = Mathf.Max(0f, baseDamage);
        float safeMultiplier = Mathf.Max(0f, damageMultiplier);
        float weaponBonus = weaponLoadout != null ? weaponLoadout.RollDamageBonus(weaponUseSlot) : 0f;

        return safeBaseDamage * safeMultiplier + weaponBonus;
    }

    private ResolvedMeleeAttack GetLightAttack()
    {
        if (lightAttackProfile != null)
        {
            return ResolvedMeleeAttack.FromAsset(lightAttackProfile);
        }

        LogMissingProfileOnce(ref missingLightProfileWarningShown, nameof(lightAttackProfile));
        return ResolvedMeleeAttack.Invalid;
    }

    private ResolvedMeleeAttack GetHeavyAttack()
    {
        if (heavyAttackProfile != null)
        {
            return ResolvedMeleeAttack.FromAsset(heavyAttackProfile);
        }

        LogMissingProfileOnce(ref missingHeavyProfileWarningShown, nameof(heavyAttackProfile));
        return ResolvedMeleeAttack.Invalid;
    }

    private void LogMissingProfileOnce(ref bool warningWasShown, string fieldName)
    {
        if (warningWasShown)
        {
            return;
        }

        warningWasShown = true;
        Debug.LogWarning($"{nameof(TopDownSlashAttack)} on '{name}' needs '{fieldName}' assigned. Attack cancelled instead of using hidden fallback values.", this);
    }

    private void ResolveHits(Vector3 impactOrigin, Vector3 facingDirection, float damage, ResolvedMeleeAttack attack)
    {
        float safeRange = Mathf.Max(0.1f, attack.AttackRange);
        float safeArc = Mathf.Clamp(attack.AttackArc, 0f, 360f);
        bool isFullCircle = safeArc >= 359.9f;

        Vector3 impactOriginAtHeight = impactOrigin + Vector3.up * hitHeight;
        Vector3 center = isFullCircle
            ? impactOriginAtHeight
            : impactOriginAtHeight + facingDirection * (safeRange * 0.5f);

        float overlapRadius = isFullCircle ? safeRange : safeRange * 0.75f;
        Collider[] hits = Physics.OverlapSphere(center, overlapRadius, hittableLayers, QueryTriggerInteraction.Ignore);
        HashSet<ITopDownDamageable> processedTargets = new HashSet<ITopDownDamageable>();

        foreach (Collider hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!TMJ_DamageUtility.TryGetDamageable(hit, hittableLayers, gameObject, out ITopDownDamageable damageable))
            {
                continue;
            }

            if (processedTargets.Contains(damageable))
            {
                continue;
            }

            Transform targetTransform = damageable is MonoBehaviour damageableBehaviour
                ? damageableBehaviour.transform
                : hit.transform;

            Vector3 toTarget = targetTransform.position - impactOrigin;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance <= 0.001f || distance > safeRange)
            {
                continue;
            }

            if (!isFullCircle && Vector3.Angle(facingDirection, toTarget) > safeArc * 0.5f)
            {
                continue;
            }

            Vector3 directionFromSourceToTarget = targetTransform.position - transform.position;
            directionFromSourceToTarget.y = 0f;

            TMJ_DamageInfo damageInfo = new TMJ_DamageInfo(
                damage,
                impactOrigin,
                gameObject,
                gameObject,
                TMJ_DamageUtility.GetSafeClosestPoint(hit, impactOrigin),
                directionFromSourceToTarget);

            bool damaged = TMJ_DamageUtility.TryDamageCollider(
                hit,
                damageInfo,
                hittableLayers,
                gameObject,
                processedTargets);

            if (damaged && CanApplyImpactAfterDamage(hit, damageable))
            {
                TryApplyImpact(hit, damageable, impactOrigin, facingDirection, attack);
            }
        }
    }

    private void TryApplyImpact(Collider hit, ITopDownDamageable damageable, Vector3 impactOrigin, Vector3 fallbackDirection, ResolvedMeleeAttack attack)
    {
        if (!attack.ApplyImpact || !CanApplyImpactAfterDamage(hit, damageable))
        {
            return;
        }

        IImpactReceiver impactReceiver = FindImpactReceiver(hit, damageable);
        if (impactReceiver == null)
        {
            return;
        }

        Transform targetTransform = damageable is MonoBehaviour damageableBehaviour
            ? damageableBehaviour.transform
            : hit.transform;

        Vector3 impactDirection = targetTransform.position - impactOrigin;
        impactDirection.y = 0f;

        if (impactDirection.sqrMagnitude <= 0.0001f)
        {
            impactDirection = fallbackDirection;
        }

        ImpactInfo impactInfo = new ImpactInfo(
            gameObject,
            impactOrigin,
            impactDirection,
            attack.KnockbackDistance,
            attack.KnockbackDuration,
            attack.StunDuration,
            attack.InterruptCurrentAction);

        impactReceiver.ReceiveImpact(impactInfo);
    }


    private static bool CanApplyImpactAfterDamage(Collider hit, ITopDownDamageable damageable)
    {
        // El daño puede matar al enemigo y disparar su lógica de muerte antes de que intentemos aplicar knockback/stun.
        // Si el target murió, fue destruido o quedó desactivado, no le aplicamos impacto: la muerte manda.
        if (hit == null || damageable == null)
        {
            return false;
        }

        if (damageable is Object damageableObject && damageableObject == null)
        {
            return false;
        }

        if (damageable is MonoBehaviour damageableBehaviour)
        {
            if (damageableBehaviour == null || !damageableBehaviour.gameObject.activeInHierarchy)
            {
                return false;
            }

            TopDownHealth health = damageableBehaviour.GetComponentInParent<TopDownHealth>();
            if (health != null && health.CurrentHealth <= 0f)
            {
                return false;
            }
        }

        TopDownHealth hitHealth = hit.GetComponentInParent<TopDownHealth>();
        if (hitHealth != null && hitHealth.CurrentHealth <= 0f)
        {
            return false;
        }

        return true;
    }

    private static IImpactReceiver FindImpactReceiver(Collider hit, ITopDownDamageable damageable)
    {
        if (damageable is Object damageableObject && damageableObject == null)
        {
            return null;
        }

        if (damageable is MonoBehaviour damageableBehaviour)
        {
            if (damageableBehaviour == null || !damageableBehaviour.gameObject.activeInHierarchy)
            {
                return null;
            }

            IImpactReceiver receiver = FindInterfaceInParents<IImpactReceiver>(damageableBehaviour.transform);
            if (receiver != null)
            {
                return receiver;
            }
        }

        return hit != null ? FindInterfaceInParents<IImpactReceiver>(hit.transform) : null;
    }

    private static TInterface FindInterfaceInParents<TInterface>(Transform start) where TInterface : class
    {
        if (start == null)
        {
            return null;
        }

        MonoBehaviour[] behaviours = start.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is TInterface found)
            {
                return found;
            }
        }

        return null;
    }

    private IEnumerator PlaySlashVisual(Vector3 impactOrigin, Vector3 facingDirection, float attackRange, float attackArc, float visualDuration, float visualWidth, Color slashColor)
    {
        float safeRange = Mathf.Max(0.1f, attackRange);
        float safeArc = Mathf.Clamp(attackArc, 0f, 360f);
        bool isFullCircle = safeArc >= 359.9f;

        GameObject visual = new GameObject("SlashVisual");
        visual.hideFlags = HideFlags.DontSave;
        visual.transform.SetPositionAndRotation(impactOrigin + Vector3.up * hitHeight, Quaternion.LookRotation(facingDirection, Vector3.up));

        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = isFullCircle;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.positionCount = visualSegments;
        line.startWidth = visualWidth;
        line.endWidth = isFullCircle ? visualWidth : visualWidth * 0.15f;
        line.sharedMaterial = CreateLineMaterial();
        line.startColor = slashColor;
        line.endColor = new Color(slashColor.r, slashColor.g, slashColor.b, 0f);

        float halfArc = safeArc * 0.5f;
        for (int i = 0; i < visualSegments; i++)
        {
            float t = visualSegments == 1 ? 0f : (float)i / (visualSegments - 1);
            float angle = isFullCircle ? t * 360f : Mathf.Lerp(-halfArc, halfArc, t);
            Vector3 point = Quaternion.AngleAxis(angle, Vector3.up) * (Vector3.forward * safeRange);
            point.y = Mathf.Sin(t * Mathf.PI) * 0.18f;
            line.SetPosition(i, point);
        }

        float elapsed = 0f;
        while (elapsed < visualDuration)
        {
            float progress = elapsed / visualDuration;
            line.widthMultiplier = Mathf.Lerp(1f, 0f, progress);

            Color fadedColor = slashColor;
            fadedColor.a = Mathf.Lerp(1f, 0f, progress);
            line.startColor = fadedColor;
            line.endColor = new Color(fadedColor.r, fadedColor.g, fadedColor.b, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(visual);
    }

    private static Material lineMaterial;

    private static Material CreateLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        Shader shader = Shader.Find("TopDown/UnlitVertexColor");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return lineMaterial;
    }

    private readonly struct ResolvedMeleeAttack
    {
        private ResolvedMeleeAttack(
            bool isValid,
            TMJ_WeaponUseSlot weaponUseSlot,
            float cooldown,
            float impactDelaySeconds,
            float baseDamage,
            float attackRange,
            float attackArc,
            Vector2 impactOriginOffset,
            bool applyImpact,
            float knockbackDistance,
            float knockbackDuration,
            float stunDuration,
            bool interruptCurrentAction,
            float visualDuration,
            float visualWidth,
            Color slashColor)
        {
            IsValid = isValid;
            WeaponUseSlot = weaponUseSlot;
            Cooldown = cooldown;
            ImpactDelaySeconds = impactDelaySeconds;
            BaseDamage = baseDamage;
            AttackRange = attackRange;
            AttackArc = attackArc;
            ImpactOriginOffset = impactOriginOffset;
            ApplyImpact = applyImpact;
            KnockbackDistance = knockbackDistance;
            KnockbackDuration = knockbackDuration;
            StunDuration = stunDuration;
            InterruptCurrentAction = interruptCurrentAction;
            VisualDuration = visualDuration;
            VisualWidth = visualWidth;
            SlashColor = slashColor;
        }

        public bool IsValid { get; }
        public TMJ_WeaponUseSlot WeaponUseSlot { get; }
        public float Cooldown { get; }
        public float ImpactDelaySeconds { get; }
        public float BaseDamage { get; }
        public float AttackRange { get; }
        public float AttackArc { get; }
        public Vector2 ImpactOriginOffset { get; }
        public bool ApplyImpact { get; }
        public float KnockbackDistance { get; }
        public float KnockbackDuration { get; }
        public float StunDuration { get; }
        public bool InterruptCurrentAction { get; }
        public float VisualDuration { get; }
        public float VisualWidth { get; }
        public Color SlashColor { get; }

        public static ResolvedMeleeAttack Invalid => new ResolvedMeleeAttack(
            false,
            TMJ_WeaponUseSlot.LightAttack,
            0f,
            0f,
            0f,
            0f,
            0f,
            Vector2.zero,
            false,
            0f,
            0f,
            0f,
            false,
            0f,
            0f,
            Color.white);

        public static ResolvedMeleeAttack FromAsset(PlayerMeleeAttackProfile profile)
        {
            if (profile == null)
            {
                return Invalid;
            }

            return new ResolvedMeleeAttack(
                true,
                profile.WeaponUseSlot,
                profile.Cooldown,
                profile.ImpactDelaySeconds,
                profile.BaseDamage,
                profile.AttackRange,
                profile.AttackArc,
                profile.ImpactOriginOffset,
                profile.ApplyImpact,
                profile.KnockbackDistance,
                profile.KnockbackDuration,
                profile.StunDuration,
                profile.InterruptCurrentAction,
                profile.VisualDuration,
                profile.VisualWidth,
                profile.SlashColor);
        }

        public ResolvedMeleeAttack WithMultipliers(
            float rangeMultiplier,
            float arcMultiplier,
            float cooldownMultiplier,
            float visualDurationMultiplier,
            float visualWidthMultiplier,
            Color comboSlashColor)
        {
            if (!IsValid)
            {
                return Invalid;
            }

            return new ResolvedMeleeAttack(
                IsValid,
                WeaponUseSlot,
                Cooldown * Mathf.Max(0.01f, cooldownMultiplier),
                ImpactDelaySeconds,
                BaseDamage,
                AttackRange * Mathf.Max(0.1f, rangeMultiplier),
                Mathf.Clamp(AttackArc * Mathf.Max(0.1f, arcMultiplier), 0f, 360f),
                ImpactOriginOffset,
                ApplyImpact,
                KnockbackDistance,
                KnockbackDuration,
                StunDuration,
                InterruptCurrentAction,
                VisualDuration * Mathf.Max(0.01f, visualDurationMultiplier),
                VisualWidth * Mathf.Max(0.01f, visualWidthMultiplier),
                comboSlashColor);
        }
    }
}
