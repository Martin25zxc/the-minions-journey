using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Minion Data")]
public class MinionDataSO : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth      = 50f;
    public float moveSpeed      = 3.5f;
    public float damageAmount   = 10f;

    [Header("Rangos")]
    public float detectionRange = 12f;
    public float detectionAngle = 90f;  // angulo total del cono de vision
    public float attackRange    = 1.5f;

    [Header("Ataque")]
    public float attackDuration = 0.4f;

    [Header("Cooldown entre ataques")]
    public float minCooldown    = 0.8f;
    public float maxCooldown    = 1.6f;
}
