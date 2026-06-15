using System.Collections;
using UnityEngine;

/// <summary>
/// Contrato comun para habilidades de enemigos.
///
/// Objetivo:
/// - EnemyBrain no necesita conocer Melee, Leap, Ranged, Barrage, etc.
/// - Cada prefab combina las abilities que necesita.
/// - Cada ability decide si puede usarse, su prioridad y como ejecutarse.
/// </summary>
public interface IEnemyAbility
{
    bool IsRunning { get; }

    /// <summary>
    /// Indica si la habilidad puede usarse ahora contra el target.
    /// Debe validar cooldown, rango, profile asignado, actor vivo, etc.
    /// </summary>
    bool CanUse(Transform target);

    /// <summary>
    /// Prioridad relativa si varias habilidades pueden usarse en el mismo frame.
    /// Ejemplo: Leap > Melee si ambos son posibles, o Barrage > Projectile si esta disponible.
    /// </summary>
    float GetPriority(Transform target);

    /// <summary>
    /// Ejecuta la habilidad. EnemyBrain corre esta corrutina y espera a que termine.
    /// </summary>
    IEnumerator Execute(Transform target);

    /// <summary>
    /// Cancela de forma limpia. Debe apagar flags, hitboxes/logica temporal y animaciones booleanas si aplica.
    /// </summary>
    void Cancel();
}
