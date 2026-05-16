using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownSlashAttack : MonoBehaviour
{
    [SerializeField, Min(0.05f)]
    float cooldown = 0.35f;

    [SerializeField, Min(0.1f)]
    float damage = 1f;

    [SerializeField, Min(0.1f)]
    float attackRange = 1.8f;

    [SerializeField, Range(20f, 180f)]
    float attackArc = 100f;

    [SerializeField, Min(4)]
    int visualSegments = 18;

    [SerializeField, Min(0.01f)]
    float visualDuration = 0.12f;

    [SerializeField, Min(0.01f)]
    float hitHeight = 1f;

    [SerializeField]
    Color slashColor = new Color(1f, 0.9f, 0.6f, 1f);

    [SerializeField]
    LayerMask hittableLayers = ~0;

    float nextAttackTime;

    public bool TryAttack(Vector3 facingDirection)
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        if (facingDirection.sqrMagnitude < 0.0001f)
        {
            facingDirection = transform.forward;
        }

        facingDirection.y = 0f;
        facingDirection.Normalize();

        nextAttackTime = Time.time + cooldown;

        ApplyDamage(facingDirection);
        StartCoroutine(PlaySlashVisual(facingDirection));
        return true;
    }

    void ApplyDamage(Vector3 facingDirection)
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

    IEnumerator PlaySlashVisual(Vector3 facingDirection)
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
        line.startWidth = 0.18f;
        line.endWidth = 0.02f;
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

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
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
