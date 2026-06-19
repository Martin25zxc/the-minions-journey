using UnityEngine;

/// <summary>
/// Contrato de pathfinding para enemigos.
///
/// Responsabilidad:
/// - Recibir origen/destino.
/// - Calcular o devolver el proximo punto a seguir.
///
/// No mueve al enemigo, no modifica Rigidbody, no decide combate,
/// no ejecuta patrol y no selecciona abilities.
/// </summary>
public interface IEnemyPathProvider
{
    bool TryGetNextPoint(
        Vector3 currentPosition,
        Vector3 destination,
        float destinationArrivalDistance,
        float waypointArrivalDistance,
        out Vector3 nextPoint,
        out bool nextPointIsFinalDestination,
        out EnemyPathStatus status);

    void ClearPath();
}
