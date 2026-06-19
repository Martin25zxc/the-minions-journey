using UnityEngine;

/// <summary>
/// Posicionamiento basico para enemigos melee que persiguen al target hasta una distancia segura.
///
/// Usa EnemyNavigator para poder funcionar con DirectPathProvider o NavMeshPathProvider.
/// EnemyMovement sigue siendo el dueno fisico del movimiento.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyNavigator))]
public sealed class EnemyChasePositioning : MonoBehaviour, IEnemyPositioning
{
    [Header("References")]
    [SerializeField]
    private EnemyMovement movement;

    [SerializeField]
    private EnemyNavigator navigator;

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

        if (navigator == null)
        {
            navigator = GetComponent<EnemyNavigator>();
        }
    }

    public void UpdatePositioning(Transform target)
    {
        if (target == null)
        {
            StopPositioning();
            return;
        }

        if (navigator == null)
        {
            movement?.Stop();
            return;
        }

        bool reached = navigator.MoveTo(target.position, stopDistance, speedMultiplier);
        if (faceTargetAtStop && reached)
        {
            movement?.FaceTarget(target.position);
        }
    }

    public void StopPositioning()
    {
        navigator?.StopNavigation();
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
