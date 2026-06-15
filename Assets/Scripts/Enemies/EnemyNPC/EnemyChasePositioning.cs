using UnityEngine;

/// <summary>
/// Posicionamiento basico para enemigos que persiguen al target hasta una distancia segura.
///
/// Este componente reemplaza el uso implicito de una distancia fallback dentro de EnemyDefinition.
/// La distancia de frenado queda explicitamente en el prefab que la necesita.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMovement))]
public sealed class EnemyChasePositioning : MonoBehaviour, IEnemyPositioning
{
    [Header("References")]
    [SerializeField]
    private EnemyMovement movement;

    [Header("Chase")]
    [Tooltip("Distancia a la que el enemigo deja de avanzar. Debe estar cerca o por debajo del rango real del ataque melee.")]
    [SerializeField, Min(0.05f)]
    private float stopDistance = 1.2f;

    [SerializeField, Min(0f)]
    private float speedMultiplier = 1f;

    [Tooltip("Cuando queda dentro de stop distance, mira al target.")]
    [SerializeField]
    private bool faceTargetAtStop = true;

    public float StopDistance => stopDistance;

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

        movement.MoveTowards(target.position, stopDistance, speedMultiplier);

        if (faceTargetAtStop)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
            {
                movement.FaceTarget(target.position);
            }
        }
    }

    public void StopPositioning()
    {
        movement?.Stop();
    }

    private void OnValidate()
    {
        stopDistance = Mathf.Max(0.05f, stopDistance);
        speedMultiplier = Mathf.Max(0f, speedMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
