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
    [Tooltip("Profile used by left-click / light attack.")]
    [SerializeField]
    private PlayerMeleeAttackProfile lightAttackProfile;

    [Tooltip("Profile used by right-click / heavy attack.")]
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

    [Header("Debug Fallbacks")]
    [Tooltip("Permite que los ataques sigan funcionando si todavía no asignaste profiles. Es cómodo para prototipar, pero la ruta recomendada es usar PlayerMeleeAttackProfile assets.")]
    [SerializeField]
    private bool useFallbackProfilesWhenMissing = true;

    private float nextLightAttackTime;
    private float nextHeavyAttackTime;
    private float nextComboAttackTime;

    private void Awake()
    {
        if (weaponLoadout == null)
        {
            weaponLoadout = GetComponent<TMJ_WeaponLoadout>();
        }
    }

    public override bool TryLightAttack(Vector3 facingDirection)
    {
        AttackProfile profile = GetLightProfile();
        return TryAttack(ref nextLightAttackTime, facingDirection, profile, 1f);
    }

    public override bool TryHeavyAttack(Vector3 facingDirection)
    {
        AttackProfile profile = GetHeavyProfile();
        return TryAttack(ref nextHeavyAttackTime, facingDirection, profile, 1f);
    }

    public override bool TryComboAttack(TopDownCombatComboDefinition combo, Vector3 facingDirection)
    {
        if (combo == null || combo.Target != TopDownCombatComboTarget.Weapon)
        {
            return false;
        }

        AttackProfile baseProfile = GetComboBaseProfile(combo);
        AttackProfile comboProfile = baseProfile.WithMultipliers(
            combo.DamageMultiplier,
            combo.RangeMultiplier,
            combo.ArcMultiplier,
            combo.CooldownMultiplier,
            combo.VisualDurationMultiplier,
            combo.VisualWidthMultiplier,
            combo.UseSlashColorOverride ? combo.SlashColorOverride : baseProfile.slashColor);

        return TryAttack(ref nextComboAttackTime, facingDirection, comboProfile, combo.DamageMultiplier);
    }

    private AttackProfile GetComboBaseProfile(TopDownCombatComboDefinition combo)
    {
        // Por ahora el offset de origen solo lo usamos en LeapSlash y solo si ese combo tiene profile propio.
        // Así evitamos que un offset puesto en Light/Heavy afecte ataques normales o combos que caen al fallback.
        bool useImpactOriginOffset = combo.AnimationCue == PlayerComboAnimationCue.LeapSlash;

        if (combo.OverrideAttackProfile != null)
        {
            return AttackProfile.FromAsset(combo.OverrideAttackProfile, useImpactOriginOffset);
        }

        return combo.WeaponAttackStyle == TopDownCombatAttackStyle.Light
            ? GetLightProfile()
            : GetHeavyProfile();
    }

    private bool TryAttack(ref float nextAttackTime, Vector3 facingDirection, AttackProfile profile, float damageMultiplier)
    {
        if (!profile.isValid)
        {
            return false;
        }

        if (Time.time < nextAttackTime)
        {
            return false;
        }

        facingDirection = NormalizeFacingDirection(facingDirection, transform.forward);
        nextAttackTime = Time.time + profile.cooldown;

        // El bonus del arma se tira cuando el ataque queda aceptado, no cuando pega.
        // Así no cambia el daño si el jugador cambia el loadout durante el delay del impacto.
        float damage = CalculateFinalDamage(profile.baseDamage, damageMultiplier, profile.weaponUseSlot);

        if (profile.impactDelaySeconds <= 0f)
        {
            ResolveAttackImpact(facingDirection, damage, profile);
            return true;
        }

        StartCoroutine(ResolveAttackImpactAfterDelay(facingDirection, damage, profile));
        return true;
    }

    private IEnumerator ResolveAttackImpactAfterDelay(Vector3 facingDirection, float damage, AttackProfile profile)
    {
        yield return new WaitForSeconds(profile.impactDelaySeconds);
        ResolveAttackImpact(facingDirection, damage, profile);
    }

    private void ResolveAttackImpact(Vector3 facingDirection, float damage, AttackProfile profile)
    {
        Vector3 impactOrigin = CalculateImpactOrigin(facingDirection, profile);

        ApplyDamage(impactOrigin, facingDirection, damage, profile.attackRange, profile.attackArc);
        StartCoroutine(PlaySlashVisual(impactOrigin, facingDirection, profile.attackRange, profile.attackArc, profile.visualDuration, profile.visualWidth, profile.slashColor));
    }

    private Vector3 CalculateImpactOrigin(Vector3 facingDirection, AttackProfile profile)
    {
        if (!profile.useImpactOriginOffset)
        {
            return transform.position;
        }

        Vector3 localRight = Vector3.Cross(Vector3.up, facingDirection).normalized;
        Vector2 offset = profile.impactOriginOffset;

        // Convención del profile:
        // X = derecha/izquierda local del jugador.
        // Y = adelante/atrás según hacia dónde mira.
        return transform.position + localRight * offset.x + facingDirection * offset.y;
    }

    private float CalculateFinalDamage(float baseDamage, float damageMultiplier, TMJ_WeaponUseSlot weaponUseSlot)
    {
        float safeBaseDamage = Mathf.Max(0f, baseDamage);
        float safeMultiplier = Mathf.Max(0f, damageMultiplier);
        float weaponBonus = weaponLoadout != null ? weaponLoadout.RollDamageBonus(weaponUseSlot) : 0f;

        return safeBaseDamage * safeMultiplier + weaponBonus;
    }

    private AttackProfile GetLightProfile()
    {
        if (lightAttackProfile != null)
        {
            return AttackProfile.FromAsset(lightAttackProfile);
        }

        return useFallbackProfilesWhenMissing
            ? AttackProfile.CreateFallbackLight()
            : AttackProfile.Invalid;
    }

    private AttackProfile GetHeavyProfile()
    {
        if (heavyAttackProfile != null)
        {
            return AttackProfile.FromAsset(heavyAttackProfile);
        }

        return useFallbackProfilesWhenMissing
            ? AttackProfile.CreateFallbackHeavy()
            : AttackProfile.Invalid;
    }

    private void ApplyDamage(Vector3 impactOrigin, Vector3 facingDirection, float damage, float attackRange, float attackArc)
    {
        float safeRange = Mathf.Max(0.1f, attackRange);
        float safeArc = Mathf.Clamp(attackArc, 0f, 360f);
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

            TMJ_DamageUtility.TryDamageCollider(
                hit,
                damage,
                impactOrigin,
                gameObject,
                hittableLayers,
                gameObject,
                processedTargets);
        }
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

    private readonly struct AttackProfile
    {
        public AttackProfile(
            bool isValid,
            TMJ_WeaponUseSlot weaponUseSlot,
            float cooldown,
            float impactDelaySeconds,
            float baseDamage,
            float attackRange,
            float attackArc,
            Vector2 impactOriginOffset,
            bool useImpactOriginOffset,
            float visualDuration,
            float visualWidth,
            Color slashColor)
        {
            this.isValid = isValid;
            this.weaponUseSlot = weaponUseSlot;
            this.cooldown = cooldown;
            this.impactDelaySeconds = impactDelaySeconds;
            this.baseDamage = baseDamage;
            this.attackRange = attackRange;
            this.attackArc = attackArc;
            this.impactOriginOffset = impactOriginOffset;
            this.useImpactOriginOffset = useImpactOriginOffset;
            this.visualDuration = visualDuration;
            this.visualWidth = visualWidth;
            this.slashColor = slashColor;
        }

        public bool isValid { get; }
        public TMJ_WeaponUseSlot weaponUseSlot { get; }
        public float cooldown { get; }
        public float impactDelaySeconds { get; }
        public float baseDamage { get; }
        public float attackRange { get; }
        public float attackArc { get; }
        public Vector2 impactOriginOffset { get; }
        public bool useImpactOriginOffset { get; }
        public float visualDuration { get; }
        public float visualWidth { get; }
        public Color slashColor { get; }

        public static AttackProfile Invalid => new AttackProfile(false, TMJ_WeaponUseSlot.LightAttack, 0f, 0f, 0f, 0f, 0f, Vector2.zero, false, 0f, 0f, Color.white);

        public static AttackProfile FromAsset(PlayerMeleeAttackProfile profile, bool useImpactOriginOffset = false)
        {
            return new AttackProfile(
                true,
                profile.WeaponUseSlot,
                profile.Cooldown,
                profile.ImpactDelaySeconds,
                profile.BaseDamage,
                profile.AttackRange,
                profile.AttackArc,
                profile.ImpactOriginOffset,
                useImpactOriginOffset,
                profile.VisualDuration,
                profile.VisualWidth,
                profile.SlashColor);
        }

        public static AttackProfile CreateFallbackLight()
        {
            return new AttackProfile(
                true,
                TMJ_WeaponUseSlot.LightAttack,
                0.25f,
                0.05f,
                1f,
                1.8f,
                95f,
                Vector2.zero,
                false,
                0.12f,
                0.18f,
                new Color(1f, 0.9f, 0.6f, 1f));
        }

        public static AttackProfile CreateFallbackHeavy()
        {
            return new AttackProfile(
                true,
                TMJ_WeaponUseSlot.HeavyAttack,
                0.65f,
                0.14f,
                2.25f,
                2.25f,
                120f,
                Vector2.zero,
                false,
                0.16f,
                0.24f,
                new Color(1f, 0.75f, 0.35f, 1f));
        }

        public AttackProfile WithMultipliers(
            float damageMultiplier,
            float rangeMultiplier,
            float arcMultiplier,
            float cooldownMultiplier,
            float visualDurationMultiplier,
            float visualWidthMultiplier,
            Color comboSlashColor)
        {
            return new AttackProfile(
                isValid,
                weaponUseSlot,
                cooldown * Mathf.Max(0.01f, cooldownMultiplier),
                impactDelaySeconds,
                baseDamage,
                attackRange * Mathf.Max(0.1f, rangeMultiplier),
                Mathf.Clamp(attackArc * Mathf.Max(0.1f, arcMultiplier), 0f, 360f),
                impactOriginOffset,
                useImpactOriginOffset,
                visualDuration * Mathf.Max(0.01f, visualDurationMultiplier),
                visualWidth * Mathf.Max(0.01f, visualWidthMultiplier),
                comboSlashColor);
        }
    }
}