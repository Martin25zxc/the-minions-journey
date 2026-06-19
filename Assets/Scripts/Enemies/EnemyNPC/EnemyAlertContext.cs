using UnityEngine;

/// <summary>
/// Datos que viajan entre EnemyGroupMember y EnemyGroupController cuando un enemigo alerta a su grupo.
///
/// No decide estados, no mueve enemigos y no aplica daño. Solo describe que ocurrio,
/// quien lo reporto y que informacion tactica deberian recibir los aliados.
/// </summary>
public readonly struct EnemyAlertContext
{
    private static int nextAlertId = 1;

    public EnemyAlertContext(
        int alertId,
        EnemyAlertReason reason,
        EnemyGroupMember sourceMember,
        EnemyGroupController sourceGroup,
        Transform target,
        Vector3 position,
        bool hasPosition,
        GameObject instigator,
        GameObject damageCauser,
        float memoryDuration,
        bool forceCombat)
    {
        AlertId = alertId;
        Reason = reason;
        SourceMember = sourceMember;
        SourceGroup = sourceGroup;
        Target = target;
        Position = position;
        HasPosition = hasPosition;
        Instigator = instigator;
        DamageCauser = damageCauser;
        MemoryDuration = Mathf.Max(0f, memoryDuration);
        ForceCombat = forceCombat;
    }

    public int AlertId { get; }
    public EnemyAlertReason Reason { get; }
    public EnemyGroupMember SourceMember { get; }
    public EnemyGroupController SourceGroup { get; }
    public Transform Target { get; }
    public bool HasTarget => Target != null;
    public Vector3 Position { get; }
    public bool HasPosition { get; }
    public GameObject Instigator { get; }
    public GameObject DamageCauser { get; }
    public float MemoryDuration { get; }
    public bool ForceCombat { get; }

    public static EnemyAlertContext FromStimulus(
        EnemyAlertReason reason,
        EnemyGroupMember sourceMember,
        EnemyGroupController sourceGroup,
        EnemyStimulus stimulus,
        float fallbackMemoryDuration,
        bool forceCombat)
    {
        Transform target = stimulus.Target;
        bool hasPosition = stimulus.HasPosition || target != null;
        Vector3 position = stimulus.HasPosition
            ? stimulus.Position
            : target != null ? target.position : Vector3.zero;

        float memoryDuration = stimulus.MemoryDuration > 0f
            ? stimulus.MemoryDuration
            : fallbackMemoryDuration;

        return new EnemyAlertContext(
            GenerateAlertId(),
            reason,
            sourceMember,
            sourceGroup,
            target,
            position,
            hasPosition,
            stimulus.Instigator,
            stimulus.DamageCauser,
            memoryDuration,
            forceCombat);
    }

    public EnemyStimulus ToGroupStimulus()
    {
        return EnemyStimulus.GroupAlert(
            Target,
            HasPosition ? Position : Target != null ? Target.position : Vector3.zero,
            HasPosition || Target != null,
            Instigator,
            MemoryDuration,
            ForceCombat);
    }

    public bool TryGetReferencePosition(out Vector3 referencePosition)
    {
        if (HasPosition)
        {
            referencePosition = Position;
            return true;
        }

        if (Target != null)
        {
            referencePosition = Target.position;
            return true;
        }

        if (SourceMember != null)
        {
            referencePosition = SourceMember.transform.position;
            return true;
        }

        referencePosition = Vector3.zero;
        return false;
    }

    private static int GenerateAlertId()
    {
        int id = nextAlertId;
        nextAlertId++;

        if (nextAlertId == int.MaxValue)
        {
            nextAlertId = 1;
        }

        return id;
    }
}
