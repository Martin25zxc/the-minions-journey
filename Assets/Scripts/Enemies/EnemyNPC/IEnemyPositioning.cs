using UnityEngine;

/// <summary>
/// Contrato de posicionamiento para enemigos.
///
/// EnemyBrain decide target y abilities. El posicionamiento decide como moverse
/// cuando no se esta ejecutando una ability.
///
/// Ejemplos:
/// - EnemyChasePositioning para melee / melee-leap.
/// - EnemyRangedPositioning para enemigos que mantienen distancia.
/// - Futuro: patrol, orbit, guard, flee, etc.
/// </summary>
public interface IEnemyPositioning
{
    void UpdatePositioning(Transform target);
    void StopPositioning();
}
