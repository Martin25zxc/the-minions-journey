using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownSlashAttack : TopDownWeapon
{
    [Header("Weapon Loadout")]
    [SerializeField]
    TMJ_WeaponLoadout weaponLoadout;

    [Header("Attack Profiles")]
    [Tooltip("Profile used by left-click / light attack.")]
    [SerializeField]
    PlayerMeleeAttackProfile lightAttackProfile;

    [Tooltip("Profile used by right-click / heavy attack.")]
    [SerializeField]
    PlayerMeleeAttackProfile heavyAttackProfile;

    [Header("Hit Detection")]
    [SerializeField, Min(4)]
    int visualSegments = 18;

    [SerializeField, Min(0.01f)]
    float hitHeight = 1f;

    [SerializeField]
    LayerMask hittableLayers = ~0;

    [Header("Debug Fallbacks")]
    [Tooltip("Allows attacks to keep working while profiles are not assigned yet. Assign PlayerMeleeAttackProfile assets to use the recommended workflow.")]
    [SerializeField]
    bool useFallbackProfilesWhenMissing = true;

    float nextLightAttackTime;
    float nextHeavyAttackTime;
    float nextComboAttackTime;

    void Awake()
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

        AttackProfile baseProfile = combo.WeaponAttackStyle == TopDownCombatAttackStyle.Light
            ? GetLightProfile()
            : GetHeavyProfile();

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

    bool TryAttack(ref float nextAttackTime, Vector3 facingDirection, AttackProfile profile, float damageMultiplier)
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

        float damage = CalculateFinalDamage(profile.baseDamage, damageMultiplier, profile.weaponUseSlot);
        ApplyDamage(facingDirection, damage, profile.attackRange, profile.attackArc);
        StartCoroutine(PlaySlashVisual(facingDirection, profile.attackRange, profile.attackArc, profile.visualDuration, profile.visualWidth, profile.slashColor));
        return true;
    }

    float CalculateFinalDamage(float baseDamage, float damageMultiplier, TMJ_WeaponUseSlot weaponUseSlot)
    {
        float safeBaseDamage = Mathf.Max(0f, baseDamage);
        float safeMultiplier = Mathf.Max(0f, damageMultiplier);
        float weaponBonus = weaponLoadout != null ? weaponLoadout.RollDamageBonus(weaponUseSlot) : 0f;

        return safeBaseDamage * safeMultiplier + weaponBonus;
    }

    AttackProfile GetLightProfile()
    {
        if (lightAttackProfile != null)
        {
            return AttackProfile.FromAsset(lightAttackProfile);
        }

        return useFallbackProfilesWhenMissing
            ? AttackProfile.CreateFallbackLight()
            : AttackProfile.Invalid;
    }

    AttackProfile GetHeavyProfile()
    {
        if (heavyAttackProfile != null)
        {
            return AttackProfile.FromAsset(heavyAttackProfile);
        }

        return useFallbackProfilesWhenMissing
            ? AttackProfile.CreateFallbackHeavy()
            : AttackProfile.Invalid;
    }

    void ApplyDamage(Vector3 facingDirection, float damage, float attackRange, float attackArc)
    {
        Vector3 center = transform.position + Vector3.up * hitHeight + facingDirection * (attackRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(center, attackRange * 0.75f, hittableLayers, QueryTriggerInteraction.Ignore);
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

            Vector3 toTarget = targetTransform.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance <= 0.001f || distance > attackRange)
            {
                continue;
            }

            if (Vector3.Angle(facingDirection, toTarget) > attackArc * 0.5f)
            {
                continue;
            }

            TMJ_DamageUtility.TryDamageCollider(
                hit,
                damage,
                transform.position,
                gameObject,
                hittableLayers,
                gameObject,
                processedTargets);
        }
    }

    IEnumerator PlaySlashVisual(Vector3 facingDirection, float attackRange, float attackArc, float visualDuration, float visualWidth, Color slashColor)
    {
        GameObject visual = new GameObject("SlashVisual");
        visual.hideFlags = HideFlags.DontSave;
        visual.transform.SetPositionAndRotation(transform.position + Vector3.up * hitHeight, Quaternion.LookRotation(facingDirection, Vector3.up));

        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = false;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.positionCount = visualSegments;
        line.startWidth = visualWidth;
        line.endWidth = visualWidth * 0.15f;
        line.sharedMaterial = CreateLineMaterial();
        line.startColor = slashColor;
        line.endColor = new Color(slashColor.r, slashColor.g, slashColor.b, 0f);

        float halfArc = attackArc * 0.5f;
        for (int i = 0; i < visualSegments; i++)
        {
            float t = visualSegments == 1 ? 0f : (float)i / (visualSegments - 1);
            float angle = Mathf.Lerp(-halfArc, halfArc, t);
            Vector3 point = Quaternion.AngleAxis(angle, Vector3.up) * (Vector3.forward * attackRange);
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

    static Material lineMaterial;

    static Material CreateLineMaterial()
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

    readonly struct AttackProfile
    {
        public AttackProfile(
            bool isValid,
            TMJ_WeaponUseSlot weaponUseSlot,
            float cooldown,
            float baseDamage,
            float attackRange,
            float attackArc,
            float visualDuration,
            float visualWidth,
            Color slashColor)
        {
            this.isValid = isValid;
            this.weaponUseSlot = weaponUseSlot;
            this.cooldown = cooldown;
            this.baseDamage = baseDamage;
            this.attackRange = attackRange;
            this.attackArc = attackArc;
            this.visualDuration = visualDuration;
            this.visualWidth = visualWidth;
            this.slashColor = slashColor;
        }

        public bool isValid { get; }
        public TMJ_WeaponUseSlot weaponUseSlot { get; }
        public float cooldown { get; }
        public float baseDamage { get; }
        public float attackRange { get; }
        public float attackArc { get; }
        public float visualDuration { get; }
        public float visualWidth { get; }
        public Color slashColor { get; }

        public static AttackProfile Invalid => new AttackProfile(false, TMJ_WeaponUseSlot.LightAttack, 0f, 0f, 0f, 0f, 0f, 0f, Color.white);

        public static AttackProfile FromAsset(PlayerMeleeAttackProfile profile)
        {
            return new AttackProfile(
                true,
                profile.WeaponUseSlot,
                profile.Cooldown,
                profile.BaseDamage,
                profile.AttackRange,
                profile.AttackArc,
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
                1f,
                1.8f,
                95f,
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
                2.25f,
                2.25f,
                120f,
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
                baseDamage,
                attackRange * Mathf.Max(0.1f, rangeMultiplier),
                attackArc * Mathf.Max(0.1f, arcMultiplier),
                visualDuration * Mathf.Max(0.01f, visualDurationMultiplier),
                visualWidth * Mathf.Max(0.01f, visualWidthMultiplier),
                comboSlashColor);
        }
    }
}