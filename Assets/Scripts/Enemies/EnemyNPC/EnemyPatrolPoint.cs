using UnityEngine;

/// <summary>
/// Punto opcional de una ruta de patrulla.
///
/// Si un Transform de la ruta tiene este componente, EnemyDutyController puede
/// leer un tiempo de espera especifico para ese punto. Si no existe, usa el
/// Default Wait Time At Point configurado en EnemyDutyController.
/// </summary>
public sealed class EnemyPatrolPoint : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float waitTime = 0f;

    public float WaitTime => waitTime;

    private void OnValidate()
    {
        waitTime = Mathf.Max(0f, waitTime);
    }
}
