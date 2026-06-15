using UnityEngine;

/// <summary>
/// Componente opcional para enemigos ranged.
///
/// Si esta presente, EnemyBrain lo usa para mantener distancia en vez de perseguir como melee.
/// No dispara, no decide habilidades y no conoce perfiles de proyectiles.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMovement))]
public sealed class EnemyRangedPositioning : MonoBehaviour, IEnemyPositioning
{
    [Header("References")]
    [SerializeField]
    private EnemyMovement movement;

    [Header("Distances")]
    [SerializeField, Min(0.05f)]
    private float tooCloseDistance = 2.5f;

    [SerializeField, Min(0.05f)]
    private float preferredDistance = 6f;

    [SerializeField, Min(0f)]
    private float preferredDistanceTolerance = 1f;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float approachSpeedMultiplier = 1f;

    [SerializeField, Min(0f)]
    private float retreatSpeedMultiplier = 0.8f;

    [Tooltip("Si esta activo, al retroceder sigue mirando al jugador. Suele sentirse mejor para ranged.")]
    [SerializeField]
    private bool faceTargetWhileRetreating = true;

    [Tooltip("Si esta activo, cuando esta en distancia comoda queda quieto mirando al jugador.")]
    [SerializeField]
    private bool stopAtComfortRange = true;

    public float TooCloseDistance => tooCloseDistance;
    public float PreferredDistance => preferredDistance;
    public float PreferredDistanceTolerance => preferredDistanceTolerance;

    private void Awake()
    {
        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }
    }

    public void UpdatePositioning(Transform target)
    {
        if (movement == null || target == null)
        {
            movement?.Stop();
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance <= 0.0001f)
        {
            movement.Stop();
            return;
        }

        if (distance < tooCloseDistance)
        {
            Vector3 away = -toTarget.normalized;
            movement.MoveInDirection(away, retreatSpeedMultiplier);

            if (faceTargetWhileRetreating)
            {
                movement.FaceTarget(target.position);
            }

            return;
        }

        if (distance > preferredDistance + preferredDistanceTolerance)
        {
            movement.MoveTowards(target.position, preferredDistance, approachSpeedMultiplier);
            return;
        }

        if (stopAtComfortRange)
        {
            movement.Stop();
        }

        movement.FaceTarget(target.position);
    }

    public void StopPositioning()
    {
        movement?.Stop();
    }

    private void OnValidate()
    {
        tooCloseDistance = Mathf.Max(0.05f, tooCloseDistance);
        preferredDistance = Mathf.Max(tooCloseDistance + 0.05f, preferredDistance);
        preferredDistanceTolerance = Mathf.Max(0f, preferredDistanceTolerance);
        approachSpeedMultiplier = Mathf.Max(0f, approachSpeedMultiplier);
        retreatSpeedMultiplier = Mathf.Max(0f, retreatSpeedMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, preferredDistance + preferredDistanceTolerance);
    }
}
