using UnityEngine;

/// <summary>
/// Define los datos de un ataque individual del jefe.
/// Crea assets desde: Assets > Create > Boss/Attack
/// </summary>
[CreateAssetMenu(fileName = "New Attack", menuName = "Boss/Attack")]
public class AttackSO : ScriptableObject
{
    [Header("Identificación")]
    public string attackName = "Unnamed Attack";
    [TextArea] public string description;

    [Header("Timing")]
    public float cooldown       = 3f;   // segundos entre usos
    public float startupTime    = 0.5f; // anticipación antes de ejecutar
    public float duration       = 1.5f; // duración total de la acción

    [Header("Coordinación con arena")]
    [Tooltip("Si es true, el BossArenaManager prepara el entorno antes de ordenar la ejecución.")]
    public bool needsArena = false;

    [Tooltip("Segundos que el manager espera a que el entorno esté listo antes de dar la orden.")]
    public float arenaSetupDelay = 0.8f;

    [Header("Animación")]
    [Tooltip("Nombre del trigger en el Animator del jefe.")]
    public string animatorTrigger = "";
}