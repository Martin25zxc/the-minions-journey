using UnityEngine;

/// <summary>
/// Contrato de posicionamiento tactico de combate.
///
/// EnemyBrain decide target y abilities. El posicionamiento decide como moverse
/// cuando el enemigo esta en Combat y no esta ejecutando una ability.
///
/// Ejemplos:
/// - EnemyChasePositioning para melee / melee-leap.
/// - EnemyRangedPositioning para enemigos que mantienen distancia.
/// - Futuro: orbit, flee, flank.
///
/// Patrol/Guard/Wander NO deberian implementarse aca: pertenecen al futuro sistema Duty.
/// </summary>
public interface IEnemyPositioning
{
    void UpdatePositioning(Transform target);
    void StopPositioning();
}
