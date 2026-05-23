using UnityEngine;

/// <summary>
/// Solo datos puros del jefe: prefab, stats y fases.
/// Los ataques viven como MonoBehaviour en el prefab, no aquí.
/// Crear desde: Assets > Create > Boss/Boss Data
/// </summary>
[CreateAssetMenu(fileName = "New Boss", menuName = "Boss/Boss Data")]
public class BossDataSO : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Estadísticas")]
    public float maxHealth    = 1000f;
    public float moveSpeed    = 4f;
    public float damageAmount = 20f;

    [Header("Fases (opcional)")]
    [Tooltip("healthThreshold = porcentaje de vida (0-1) al que empieza la fase.")]
    public BossPhase[] phases;
}

[System.Serializable]
public struct BossPhase
{
    public string phaseName;
    [Range(0f, 1f)] public float healthThreshold;
}