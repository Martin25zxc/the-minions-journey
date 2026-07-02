using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Herramienta de debug para medir el rango real de una arena/área de boss.
///
/// Sirve para estimar LoseAggroRange y validar que el boss no salga de combate por distancia
/// mientras el jugador sigue dentro del área.
///
/// Uso:
/// - Crear un Empty en la escena: ArenaRangeProbe.
/// - Asignar Origin = Boss_Start o el transform del boss.
/// - Asignar Manual Points = esquinas/puntos extremos del área.
/// - Opcional: asignar Bounds Collider = collider trigger del volumen del encounter.
/// - Leer en Scene View: Max Distance y Recommended Lose Aggro.
/// </summary>
[ExecuteAlways]
public sealed class ArenaRangeProbe : MonoBehaviour
{
    [Header("Origin")]
    [SerializeField]
    private Transform origin;

    [Header("Manual Points")]
    [SerializeField]
    private Transform[] manualPoints;

    [Header("Optional Bounds Collider")]
    [Tooltip("Si se asigna, también mide contra las esquinas del bounds del collider. Útil para BoxCollider del volumen.")]
    [SerializeField]
    private Collider boundsCollider;

    [Header("Measurement")]
    [SerializeField]
    private bool ignoreY = true;

    [SerializeField, Min(0f)]
    private float recommendedExtraMargin = 5f;

    [Header("Gizmos")]
    [SerializeField]
    private bool drawLines = true;

    [SerializeField]
    private bool drawRecommendedRadius = true;

    [SerializeField]
    private Color lineColor = Color.yellow;

    [SerializeField]
    private Color farthestColor = Color.red;

    [SerializeField]
    private Color recommendedRadiusColor = new Color(1f, 0.4f, 0f, 0.2f);

    [Header("Debug Read Only")]
    [SerializeField]
    private float debugMaxDistance;

    [SerializeField]
    private float debugRecommendedLoseAggroRange;

    [SerializeField]
    private string debugFarthestPointName;

    public float MaxDistance => debugMaxDistance;
    public float RecommendedLoseAggroRange => debugRecommendedLoseAggroRange;

    private void OnDrawGizmos()
    {
        RecalculateAndDraw(true);
    }

    private void OnValidate()
    {
        RecalculateAndDraw(false);
    }

    private void RecalculateAndDraw(bool draw)
    {
        debugMaxDistance = 0f;
        debugRecommendedLoseAggroRange = 0f;
        debugFarthestPointName = string.Empty;

        if (origin == null)
        {
            return;
        }

        Vector3 originPos = origin.position;
        Vector3 farthestPos = originPos;

        EvaluateManualPoints(originPos, ref farthestPos, draw);
        EvaluateBoundsCollider(originPos, ref farthestPos, draw);

        debugRecommendedLoseAggroRange = debugMaxDistance + recommendedExtraMargin;

        if (!draw)
        {
            return;
        }

        Gizmos.color = farthestColor;
        Gizmos.DrawSphere(farthestPos, 0.35f);

        if (drawRecommendedRadius && debugRecommendedLoseAggroRange > 0f)
        {
            Gizmos.color = recommendedRadiusColor;
            DrawWireCircle(origin.position, debugRecommendedLoseAggroRange, 96);
        }

#if UNITY_EDITOR
        Handles.Label(
            origin.position + Vector3.up * 2f,
            $"Max arena distance: {debugMaxDistance:F2}\nRecommended LoseAggro: {debugRecommendedLoseAggroRange:F2}\nFarthest: {debugFarthestPointName}"
        );
#endif
    }

    private void EvaluateManualPoints(Vector3 originPos, ref Vector3 farthestPos, bool draw)
    {
        if (manualPoints == null)
        {
            return;
        }

        for (int i = 0; i < manualPoints.Length; i++)
        {
            Transform point = manualPoints[i];
            if (point == null)
            {
                continue;
            }

            EvaluatePoint(originPos, point.position, point.name, ref farthestPos, draw);
        }
    }

    private void EvaluateBoundsCollider(Vector3 originPos, ref Vector3 farthestPos, bool draw)
    {
        if (boundsCollider == null)
        {
            return;
        }

        Bounds bounds = boundsCollider.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        EvaluatePoint(originPos, new Vector3(min.x, min.y, min.z), "Bounds Min", ref farthestPos, draw);
        EvaluatePoint(originPos, new Vector3(min.x, min.y, max.z), "Bounds Corner", ref farthestPos, draw);
        EvaluatePoint(originPos, new Vector3(max.x, min.y, min.z), "Bounds Corner", ref farthestPos, draw);
        EvaluatePoint(originPos, new Vector3(max.x, min.y, max.z), "Bounds Max", ref farthestPos, draw);
    }

    private void EvaluatePoint(Vector3 originPos, Vector3 pointPos, string pointName, ref Vector3 farthestPos, bool draw)
    {
        Vector3 a = originPos;
        Vector3 b = pointPos;

        if (ignoreY)
        {
            a.y = 0f;
            b.y = 0f;
        }

        float distance = Vector3.Distance(a, b);

        if (draw && drawLines)
        {
            Gizmos.color = lineColor;
            Gizmos.DrawLine(origin.position, pointPos);
        }

        if (distance > debugMaxDistance)
        {
            debugMaxDistance = distance;
            farthestPos = pointPos;
            debugFarthestPointName = pointName;
        }
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        if (segments < 8)
        {
            segments = 8;
        }

        Vector3 previous = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
