using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Definition", fileName = "EnemyDefinition_NewEnemy")]
public sealed class EnemyDefinition : ScriptableObject
{
    [Header("Stats")]
    [SerializeField, Min(1f)]
    private float maxHealth = 60f;

    [SerializeField, Min(0f)]
    private float moveSpeed = 3.2f;

    [SerializeField, Min(0f)]
    private float rotateSpeed = 720f;

    [Header("Detection - Vision principal")]
    [Tooltip("Distancia maxima para detectar al objetivo dentro del cono frontal.")]
    [SerializeField, Min(0f)]
    private float detectionRange = 10f;

    [Tooltip("Angulo total del cono de deteccion. Ejemplo: 140 = 70 grados hacia cada lado. Usar 360 para vision circular.")]
    [SerializeField, Range(1f, 360f)]
    private float detectionAngle = 140f;

    [Header("Detection - Proximidad")]
    [Tooltip("Mini deteccion circular alrededor del enemigo. Sirve para que no ignore al jugador si se acerca por la espalda o un costado.")]
    [SerializeField, Min(0f)]
    private float proximityDetectionRange = 2f;

    [Header("Detection - Memoria / Aggro")]
    [Tooltip("Distancia a partir de la cual el enemigo olvida al objetivo aunque ya lo hubiera detectado.")]
    [SerializeField, Min(0f)]
    private float loseAggroRange = 15f;

    [Tooltip("Tiempo que el enemigo conserva el target si lo pierde momentaneamente por angulo o Line of Sight. Evita flicker entre perseguir/no perseguir.")]
    [SerializeField, Min(0f)]
    private float targetMemoryDuration = 1.5f;

    [Header("Loot")]
    [Tooltip("Tabla de loot por tipo de enemigo. LootDropper puede usarla si no tiene override local.")]
    [SerializeField]
    private LootDropTable lootDropTable;

    [Header("Body")]
    [Tooltip("Radio aproximado del cuerpo. Se usa para validaciones fisicas/logicas, por ejemplo aterrizajes del Leap.")]
    [SerializeField, Min(0.05f)]
    private float bodyRadius = 0.45f;

    public float MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float RotateSpeed => rotateSpeed;

    public float DetectionRange => detectionRange;
    public float DetectionAngle => detectionAngle;
    public float DetectionHalfAngle => detectionAngle * 0.5f;
    public float ProximityDetectionRange => proximityDetectionRange;
    public float LoseAggroRange => loseAggroRange;
    public float TargetMemoryDuration => targetMemoryDuration;

    public LootDropTable LootDropTable => lootDropTable;

    public float BodyRadius => bodyRadius;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotateSpeed = Mathf.Max(0f, rotateSpeed);

        detectionRange = Mathf.Max(0f, detectionRange);
        detectionAngle = Mathf.Clamp(detectionAngle, 1f, 360f);
        proximityDetectionRange = Mathf.Max(0f, proximityDetectionRange);

        float minimumLoseAggro = Mathf.Max(detectionRange, proximityDetectionRange);
        loseAggroRange = Mathf.Max(minimumLoseAggro, loseAggroRange);
        targetMemoryDuration = Mathf.Max(0f, targetMemoryDuration);

        bodyRadius = Mathf.Max(0.05f, bodyRadius);
    }
}

