using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coordina alertas simples de un grupo de enemigos.
///
/// Responsabilidad:
/// - Registrar miembros del grupo.
/// - Recibir alertas locales desde un EnemyGroupMember.
/// - Reenviar la alerta a los demas miembros validos.
/// - Opcionalmente reenviar la alerta a otros grupos vinculados.
///
/// No mueve enemigos, no selecciona habilidades, no cambia estados directamente
/// y no reemplaza a EnemyBrain ni EnemyAwareness.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyGroupController : MonoBehaviour
{
    [Header("Members")]
    [Tooltip("Si esta activo, al iniciar registra EnemyGroupMember encontrados como hijos. Es util si usas el grupo como contenedor visual, pero la pertenencia real sigue siendo la referencia del EnemyGroupMember.")]
    [SerializeField]
    private bool registerChildMembersOnAwake;

    [Header("Alert Rules")]
    [Tooltip("Distancia horizontal maxima desde la posicion de la alerta para que un miembro la reciba. 0 significa alertar a todo el grupo.")]
    [SerializeField, Min(0f)]
    private float alertRadius = 12f;

    [Tooltip("Si esta activo, los miembros muertos o deshabilitados no reciben alertas.")]
    [SerializeField]
    private bool ignoreDeadMembers = true;

    [Tooltip("Tiempo durante el cual este grupo recuerda IDs de alerta ya procesados. Evita loops, sobre todo con Linked Groups.")]
    [SerializeField, Min(0.1f)]
    private float processedAlertIdLifetime = 4f;

    [Header("Linked Groups - Opcional")]
    [Tooltip("Si esta activo, este grupo puede reenviar alertas a los grupos vinculados en Linked Groups. Con la lista vacia no tiene efecto.")]
    [SerializeField]
    private bool forwardAlertsToLinkedGroups = true;

    [Tooltip("Otros grupos que tambien deberian enterarse de las alertas de este grupo. Mantener vacio si no se necesita.")]
    [SerializeField]
    private List<EnemyGroupController> linkedGroups = new List<EnemyGroupController>();

    [Header("Debug")]
    [Tooltip("Muestra logs cuando el grupo recibe o reenvia alertas.")]
    [SerializeField]
    private bool logAlerts;

    [Tooltip("Dibuja el radio de alerta del grupo cuando se selecciona el objeto. Solo ayuda visual; no afecta gameplay.")]
    [SerializeField]
    private bool drawAlertRadiusGizmo = true;

    [Header("Runtime Debug - Solo lectura conceptual")]
    [Tooltip("Cantidad de miembros registrados actualmente en este grupo.")]
    [SerializeField]
    private int debugMemberCount;

    [Tooltip("Ultima razon de alerta procesada por este grupo.")]
    [SerializeField]
    private string debugLastAlertReason = "None";

    [Tooltip("Ultimo miembro que origino una alerta procesada por este grupo.")]
    [SerializeField]
    private EnemyGroupMember debugLastAlertSource;

    [Tooltip("Ultimo target de alerta procesado por este grupo.")]
    [SerializeField]
    private Transform debugLastAlertTarget;

    [Tooltip("Ultima posicion de alerta procesada por este grupo.")]
    [SerializeField]
    private Vector3 debugLastAlertPosition;

    private readonly List<EnemyGroupMember> members = new List<EnemyGroupMember>();
    private readonly Dictionary<int, float> processedAlertIds = new Dictionary<int, float>();

    public int MemberCount => members.Count;
    public float AlertRadius => alertRadius;

    private void Awake()
    {
        if (registerChildMembersOnAwake)
        {
            RegisterChildMembers();
        }

        RefreshDebugSnapshot();
    }

    public void RegisterMember(EnemyGroupMember member)
    {
        if (member == null || members.Contains(member))
        {
            RefreshDebugSnapshot();
            return;
        }

        members.Add(member);
        RefreshDebugSnapshot();
    }

    public void UnregisterMember(EnemyGroupMember member)
    {
        if (member == null)
        {
            return;
        }

        members.Remove(member);
        RefreshDebugSnapshot();
    }

    public void ReceiveAlert(EnemyAlertContext context)
    {
        ProcessAlert(context, propagateToLinkedGroups: true);
    }

    public void ReceiveLinkedAlert(EnemyAlertContext context)
    {
        ProcessAlert(context, propagateToLinkedGroups: true);
    }

    private void ProcessAlert(EnemyAlertContext context, bool propagateToLinkedGroups)
    {
        CleanupProcessedAlertIds();

        if (!TryRememberAlert(context.AlertId))
        {
            return;
        }

        debugLastAlertReason = context.Reason.ToString();
        debugLastAlertSource = context.SourceMember;
        debugLastAlertTarget = context.Target;
        debugLastAlertPosition = context.TryGetReferencePosition(out Vector3 referencePosition)
            ? referencePosition
            : Vector3.zero;

        if (logAlerts)
        {
            string sourceName = context.SourceMember != null ? context.SourceMember.name : "Unknown";
            string targetName = context.Target != null ? context.Target.name : "None";
            Debug.Log($"[{nameof(EnemyGroupController)}] {name} processed alert {context.AlertId} ({context.Reason}) from {sourceName}. Target: {targetName}.", this);
        }

        SendAlertToMembers(context);

        if (propagateToLinkedGroups && forwardAlertsToLinkedGroups)
        {
            ForwardAlertToLinkedGroups(context);
        }

        RefreshDebugSnapshot();
    }

    private void SendAlertToMembers(EnemyAlertContext context)
    {
        for (int i = members.Count - 1; i >= 0; i--)
        {
            EnemyGroupMember member = members[i];
            if (member == null)
            {
                members.RemoveAt(i);
                continue;
            }

            if (member == context.SourceMember)
            {
                continue;
            }

            if (ignoreDeadMembers && !member.IsAlive)
            {
                continue;
            }

            if (!IsInsideAlertRadius(member, context))
            {
                continue;
            }

            member.ReceiveGroupAlert(context);
        }
    }

    private void ForwardAlertToLinkedGroups(EnemyAlertContext context)
    {
        for (int i = 0; i < linkedGroups.Count; i++)
        {
            EnemyGroupController linkedGroup = linkedGroups[i];
            if (linkedGroup == null || linkedGroup == this)
            {
                continue;
            }

            linkedGroup.ReceiveLinkedAlert(context);
        }
    }

    private bool IsInsideAlertRadius(EnemyGroupMember member, EnemyAlertContext context)
    {
        if (alertRadius <= 0f)
        {
            return true;
        }

        if (!context.TryGetReferencePosition(out Vector3 referencePosition))
        {
            return true;
        }

        Vector3 memberPosition = member.transform.position;
        memberPosition.y = 0f;
        referencePosition.y = 0f;
        return (memberPosition - referencePosition).sqrMagnitude <= alertRadius * alertRadius;
    }

    private bool TryRememberAlert(int alertId)
    {
        if (processedAlertIds.ContainsKey(alertId))
        {
            return false;
        }

        processedAlertIds.Add(alertId, Time.time + processedAlertIdLifetime);
        return true;
    }

    private void CleanupProcessedAlertIds()
    {
        if (processedAlertIds.Count == 0)
        {
            return;
        }

        List<int> expiredIds = null;
        foreach (KeyValuePair<int, float> pair in processedAlertIds)
        {
            if (Time.time <= pair.Value)
            {
                continue;
            }

            if (expiredIds == null)
            {
                expiredIds = new List<int>();
            }

            expiredIds.Add(pair.Key);
        }

        if (expiredIds == null)
        {
            return;
        }

        for (int i = 0; i < expiredIds.Count; i++)
        {
            processedAlertIds.Remove(expiredIds[i]);
        }
    }

    private void RegisterChildMembers()
    {
        EnemyGroupMember[] childMembers = GetComponentsInChildren<EnemyGroupMember>(includeInactive: true);
        for (int i = 0; i < childMembers.Length; i++)
        {
            if (childMembers[i] == null)
            {
                continue;
            }

            childMembers[i].SetGroup(this);
        }
    }

    private void RefreshDebugSnapshot()
    {
        debugMemberCount = members.Count;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawAlertRadiusGizmo || alertRadius <= 0f)
        {
            return;
        }

        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }

    private void OnValidate()
    {
        alertRadius = Mathf.Max(0f, alertRadius);
        processedAlertIdLifetime = Mathf.Max(0.1f, processedAlertIdLifetime);
        RefreshDebugSnapshot();
    }
}
