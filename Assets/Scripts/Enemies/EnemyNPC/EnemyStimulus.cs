using UnityEngine;

/// <summary>
/// Evento de informacion que puede cambiar la awareness del enemigo.
///
/// Importante:
/// - No mueve al enemigo.
/// - No dispara animaciones.
/// - No decide estados directamente.
/// Solo representa algo que el enemigo percibio o recibio.
/// </summary>
public readonly struct EnemyStimulus
{
    public EnemyStimulus(
        EnemyStimulusType type,
        Transform target,
        Vector3 position,
        bool hasPosition,
        GameObject instigator,
        GameObject damageCauser,
        float memoryDuration,
        bool forceCombat)
    {
        Type = type;
        Target = target;
        Position = position;
        HasPosition = hasPosition;
        Instigator = instigator;
        DamageCauser = damageCauser;
        MemoryDuration = Mathf.Max(0f, memoryDuration);
        ForceCombat = forceCombat;
    }

    public EnemyStimulusType Type { get; }
    public Transform Target { get; }
    public bool HasTarget => Target != null;
    public Vector3 Position { get; }
    public bool HasPosition { get; }
    public GameObject Instigator { get; }
    public GameObject DamageCauser { get; }
    public float MemoryDuration { get; }
    public bool ForceCombat { get; }

    public static EnemyStimulus Sight(Transform target, float memoryDuration)
    {
        Vector3 position = target != null ? target.position : Vector3.zero;
        return new EnemyStimulus(
            EnemyStimulusType.Sight,
            target,
            position,
            target != null,
            target != null ? target.gameObject : null,
            null,
            memoryDuration,
            true);
    }

    public static EnemyStimulus DamageWithTarget(
        Transform target,
        Vector3 originPosition,
        GameObject instigator,
        GameObject damageCauser,
        float memoryDuration)
    {
        Vector3 position = target != null ? target.position : originPosition;
        return new EnemyStimulus(
            EnemyStimulusType.Damage,
            target,
            position,
            true,
            instigator,
            damageCauser,
            memoryDuration,
            true);
    }

    public static EnemyStimulus DamageAtPosition(
        Vector3 originPosition,
        GameObject instigator,
        GameObject damageCauser,
        float memoryDuration)
    {
        return new EnemyStimulus(
            EnemyStimulusType.Damage,
            null,
            originPosition,
            true,
            instigator,
            damageCauser,
            memoryDuration,
            false);
    }

    public static EnemyStimulus GroupAlert(
        Transform target,
        Vector3 position,
        bool hasPosition,
        GameObject instigator,
        float memoryDuration,
        bool forceCombat)
    {
        return new EnemyStimulus(
            EnemyStimulusType.GroupAlert,
            target,
            position,
            hasPosition,
            instigator,
            null,
            memoryDuration,
            forceCombat);
    }
}
