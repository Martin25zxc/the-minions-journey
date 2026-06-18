using UnityEngine;

/// <summary>
/// Path provider directo.
///
/// Devuelve siempre el destino final como proximo punto.
/// No requiere NavMesh y es la opcion recomendada para escenas simples,
/// pruebas iniciales o arenas abiertas.
/// </summary>
[DisallowMultipleComponent]
public sealed class DirectPathProvider : MonoBehaviour, IEnemyPathProvider
{
    public bool TryGetNextPoint(
        Vector3 currentPosition,
        Vector3 destination,
        float destinationArrivalDistance,
        float waypointArrivalDistance,
        out Vector3 nextPoint,
        out bool nextPointIsFinalDestination,
        out EnemyPathStatus status)
    {
        nextPoint = destination;
        nextPointIsFinalDestination = true;
        status = EnemyPathStatus.Valid;
        return true;
    }

    public void ClearPath()
    {
        // No hay cache que limpiar.
    }
}
