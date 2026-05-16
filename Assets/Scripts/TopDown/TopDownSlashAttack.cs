using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownSlashAttack : TopDownWeapon
{
    [SerializeField, Min(0.05f)]
    float lightCooldown = 0.25f;

    [SerializeField, Min(0.1f)]
    float lightDamage = 1f;

    [SerializeField, Min(0.1f)]
    float lightAttackRange = 1.8f;

    [SerializeField, Range(20f, 180f)]
    float lightAttackArc = 95f;

    [SerializeField, Min(0.01f)]
    float lightVisualDuration = 0.12f;

    [SerializeField, Range(0.05f, 0.35f)]
    float lightVisualWidth = 0.18f;

    [SerializeField]
    Color lightSlashColor = new Color(1f, 0.9f, 0.6f, 1f);

    [SerializeField, Min(0.05f)]
    float heavyCooldown = 0.65f;

    [SerializeField, Min(0.1f)]
    float heavyDamage = 2.25f;

    [SerializeField, Min(0.1f)]
    float heavyAttackRange = 2.25f;

    [SerializeField, Range(20f, 180f)]
    float heavyAttackArc = 120f;

    [SerializeField, Min(0.01f)]
    float heavyVisualDuration = 0.16f;

    [SerializeField, Range(0.05f, 0.4f)]
    float heavyVisualWidth = 0.24f;

    [SerializeField]
    Color heavySlashColor = new Color(1f, 0.75f, 0.35f, 1f);

    [SerializeField, Min(4)]
    int visualSegments = 18;

    [SerializeField, Min(0.01f)]
    float hitHeight = 1f;

    [SerializeField]
    LayerMask hittableLayers = ~0;

    float nextLightAttackTime;
    float nextHeavyAttackTime;

    public override bool TryLightAttack(Vector3 facingDirection)
    {
        return TryAttack(ref nextLightAttackTime, facingDirection, lightCooldown, lightDamage, lightAttackRange, lightAttackArc, lightVisualDuration, lightVisualWidth, lightSlashColor);
    }

    public override bool TryHeavyAttack(Vector3 facingDirection)
    {
        return TryAttack(ref nextHeavyAttackTime, facingDirection, heavyCooldown, heavyDamage, heavyAttackRange, heavyAttackArc, heavyVisualDuration, heavyVisualWidth, heavySlashColor);
    }

    bool TryAttack(ref float nextAttackTime, Vector3 facingDirection, float cooldown, float damage, float attackRange, float attackArc, float visualDuration, float visualWidth, Color slashColor)
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        facingDirection = NormalizeFacingDirection(facingDirection, transform.forward);
        nextAttackTime = Time.time + cooldown;

        ApplyDamage(facingDirection, damage, attackRange, attackArc);
        StartCoroutine(PlaySlashVisual(facingDirection, attackRange, attackArc, visualDuration, visualWidth, slashColor));
        return true;
    }

    void ApplyDamage(Vector3 facingDirection, float damage, float attackRange, float attackArc)
    {
        Vector3 center = transform.position + Vector3.up * hitHeight + facingDirection * (attackRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(center, attackRange * 0.75f, hittableLayers, QueryTriggerInteraction.Ignore);
        HashSet<Transform> processedRoots = new HashSet<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            Transform root = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform.root;
            if (!processedRoots.Add(root))
            {
                continue;
            }

            Vector3 toTarget = root.position - transform.position;
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

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ITopDownDamageable damageable)
                {
                    damageable.TakeDamage(damage);
                    break;
                }
            }
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
}
