using UnityEngine;

/// <summary>
/// Controla la rutina fuera de combate del enemigo.
///
/// Responsabilidad:
/// - Resolver Duty cuando el enemigo no esta en combate.
/// - Resolver ReturnToDuty cuando el enemigo debe volver a su rutina.
///
/// No detecta al jugador, no decide ataques, no alerta grupos, no calcula NavMesh
/// y no maneja muerte. EnemyBrain decide el estado general; este componente solo
/// ejecuta el comportamiento fuera de combate configurado.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMovement))]
public sealed class EnemyDutyController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Actor principal del enemigo. Si queda vacio, se busca automaticamente en el mismo GameObject.")]
    [SerializeField]
    private EnemyActor actor;

    [Tooltip("Componente que ejecuta el movimiento fisico con Rigidbody. Si queda vacio, se busca automaticamente en el mismo GameObject.")]
    [SerializeField]
    private EnemyMovement movement;

    [Header("Duty Mode")]
    [Tooltip("Rutina fuera de combate. Guard = se queda custodiando una posicion. Patrol = recorre una ruta de puntos.")]
    [SerializeField]
    private EnemyDutyMode mode = EnemyDutyMode.Guard;

    [Header("Guard")]
    [Tooltip("Si esta activo, el punto de guardia se captura desde la posicion inicial del enemigo al comenzar la escena.")]
    [SerializeField]
    private bool useInitialPositionAsGuardPoint = true;

    [Tooltip("Punto de guardia opcional. Solo se usa si 'Use Initial Position As Guard Point' esta desactivado.")]
    [SerializeField]
    private Transform guardPointOverride;

    [Tooltip("Multiplicador de velocidad al volver a la posicion de guardia durante ReturnToDuty. 1 usa la velocidad normal del EnemyDefinition.")]
    [SerializeField, Min(0f)]
    private float guardReturnSpeedMultiplier = 1f;

    [Tooltip("Si esta activo, al volver a guardia intenta recuperar la orientacion inicial del enemigo.")]
    [SerializeField]
    private bool restoreInitialFacing = true;

    [Header("Patrol")]
    [Tooltip("Ruta de patrulla que el enemigo recorrera cuando Duty Mode sea Patrol.")]
    [SerializeField]
    private EnemyPatrolRoute patrolRoute;

    [Tooltip("Indice del primer punto de la ruta. Si no es valido, se usara el punto mas cercano al enemigo.")]
    [SerializeField, Min(0)]
    private int startPointIndex = 0;

    [Tooltip("Distancia horizontal para considerar que el enemigo llego a un punto de patrulla durante Duty.")]
    [SerializeField, Min(0.05f)]
    private float patrolArrivalDistance = 0.4f;

    [Tooltip("Multiplicador de velocidad mientras patrulla. 1 usa la velocidad normal del EnemyDefinition.")]
    [SerializeField, Min(0f)]
    private float patrolSpeedMultiplier = 1f;

    [Tooltip("Tiempo de espera por defecto al llegar a un punto de patrulla. Puede ser sobrescrito por EnemyPatrolPoint en cada punto.")]
    [SerializeField, Min(0f)]
    private float defaultWaitTimeAtPoint = 0.5f;

    [Tooltip("Al volver desde combate o investigacion, vuelve al punto de patrulla mas cercano en vez de volver al punto que tenia guardado.")]
    [SerializeField]
    private bool returnToClosestPatrolPoint = true;

    [Tooltip("Multiplicador de velocidad al volver a la ruta durante ReturnToDuty. 1 usa la velocidad normal del EnemyDefinition.")]
    [SerializeField, Min(0f)]
    private float patrolReturnSpeedMultiplier = 1f;

    [Tooltip("Si esta activo, despues de volver a la ruta espera el tiempo configurado para ese punto antes de continuar patrullando.")]
    [SerializeField]
    private bool waitAfterReturn = true;

    [Header("Return")]
    [Tooltip("Distancia horizontal para considerar completado ReturnToDuty. Se usa al volver a guardia y al volver a una ruta de patrulla. No es rango de aggro ni rango de ataque.")]
    [SerializeField, Min(0.05f)]
    private float returnArrivalDistance = 0.35f;

    [Header("Debug")]
    [Tooltip("Muestra advertencias cuando la configuracion esta incompleta, por ejemplo Patrol sin ruta valida.")]
    [SerializeField]
    private bool logConfigurationWarnings = true;

    [Tooltip("Dibuja un gizmo del punto de guardia al seleccionar el enemigo en la escena.")]
    [SerializeField]
    private bool drawGuardGizmo = true;

    [Tooltip("Radio visual del gizmo del punto de guardia. Solo afecta la visualizacion en Scene View.")]
    [SerializeField, Min(0.05f)]
    private float guardGizmoRadius = 0.25f;

    [Header("Runtime Debug - Solo lectura conceptual")]
    [Tooltip("Modo de Duty activo en runtime. Solo para inspeccion en Play Mode.")]
    [SerializeField]
    private string debugMode;

    [Tooltip("Indica si el enemigo esta esperando en un punto de patrulla.")]
    [SerializeField]
    private bool debugIsWaiting;

    [Tooltip("Indica si el enemigo esta ejecutando ReturnToDuty.")]
    [SerializeField]
    private bool debugIsReturning;

    [Tooltip("Indice de patrulla actual. -1 significa que no hay ruta valida o aun no se resolvio.")]
    [SerializeField]
    private int debugCurrentPatrolIndex = -1;

    [Tooltip("Destino activo actual: punto de guardia, punto de patrulla o punto de retorno.")]
    [SerializeField]
    private Vector3 debugActiveDestination;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool initialized;

    private int currentPatrolIndex = -1;
    private int patrolDirection = 1;
    private int returnPatrolIndex = -1;

    private bool isWaiting;
    private float waitUntil;

    private bool isReturning;
    private bool returnCompleted;

    public EnemyDutyMode Mode => mode;
    public string DebugLabel => mode.ToString();
    public bool HasActiveDestination => mode == EnemyDutyMode.Guard || HasValidPatrolRoute();
    public Vector3 ActiveDestination => GetActiveDestination();
    public bool HasReturnedToDuty => returnCompleted;

    private Vector3 GuardPoint
    {
        get
        {
            if (!useInitialPositionAsGuardPoint && guardPointOverride != null)
            {
                return guardPointOverride.position;
            }

            return initialPosition;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureInitialPoseIfNeeded();
        RefreshDebugSnapshot();
    }

    public void Initialize(EnemyActor newActor, EnemyMovement newMovement)
    {
        if (newActor != null)
        {
            actor = newActor;
        }

        if (newMovement != null)
        {
            movement = newMovement;
        }

        ResolveReferences();
        CaptureInitialPoseIfNeeded();
        EnsurePatrolIndex();
        RefreshDebugSnapshot();
    }

    public void EnterDuty()
    {
        isReturning = false;
        returnCompleted = false;

        switch (mode)
        {
            case EnemyDutyMode.Patrol:
                EnsurePatrolIndex();
                break;

            case EnemyDutyMode.Guard:
            default:
                isWaiting = false;
                movement?.Stop();
                movement?.ClearIdleResidualPhysics();

                if (restoreInitialFacing)
                {
                    FaceInitialDirection();
                }
                break;
        }

        RefreshDebugSnapshot();
    }

    public void TickDuty()
    {
        switch (mode)
        {
            case EnemyDutyMode.Patrol:
                TickPatrolDuty();
                break;

            case EnemyDutyMode.Guard:
            default:
                movement?.Stop();
                break;
        }

        RefreshDebugSnapshot();
    }

    public void ExitDuty()
    {
        isWaiting = false;
        isReturning = false;
        returnCompleted = false;
        movement?.Stop();
        RefreshDebugSnapshot();
    }

    public void EnterReturnToDuty()
    {
        isWaiting = false;
        isReturning = true;
        returnCompleted = false;

        if (mode == EnemyDutyMode.Patrol)
        {
            ResolveReturnPatrolIndex();
        }

        RefreshDebugSnapshot();
    }

    public void TickReturnToDuty()
    {
        switch (mode)
        {
            case EnemyDutyMode.Patrol:
                TickReturnToPatrol();
                break;

            case EnemyDutyMode.Guard:
            default:
                TickReturnToGuard();
                break;
        }

        RefreshDebugSnapshot();
    }

    public void StopDuty()
    {
        isWaiting = false;
        isReturning = false;
        returnCompleted = false;
        movement?.Stop();
        RefreshDebugSnapshot();
    }

    private void TickPatrolDuty()
    {
        if (!HasValidPatrolRoute())
        {
            if (logConfigurationWarnings)
            {
                Debug.LogWarning($"[{nameof(EnemyDutyController)}] {name} is in Patrol mode but has no valid Patrol Route.", this);
            }

            movement?.Stop();
            return;
        }

        EnsurePatrolIndex();

        if (isWaiting)
        {
            movement?.Stop();
            if (Time.time < waitUntil)
            {
                return;
            }

            isWaiting = false;
            AdvanceToNextPatrolPoint();
        }

        if (HasReachedPatrolPoint(currentPatrolIndex))
        {
            movement?.Stop();
            StartWaitAtCurrentPatrolPoint();
            return;
        }

        movement?.MoveTowards(patrolRoute.GetPointPosition(currentPatrolIndex), patrolArrivalDistance, patrolSpeedMultiplier);
    }

    private void TickReturnToGuard()
    {
        Vector3 guardPoint = GuardPoint;

        if (HasReachedHorizontalPoint(guardPoint, returnArrivalDistance))
        {
            movement?.Stop();
            movement?.ClearIdleResidualPhysics();

            if (restoreInitialFacing)
            {
                FaceInitialDirection();
            }

            isReturning = false;
            returnCompleted = true;
            return;
        }

        movement?.MoveTowards(guardPoint, returnArrivalDistance, guardReturnSpeedMultiplier);
    }

    private void TickReturnToPatrol()
    {
        if (!HasValidPatrolRoute())
        {
            movement?.Stop();
            isReturning = false;
            returnCompleted = true;
            return;
        }

        if (!patrolRoute.IsIndexValid(returnPatrolIndex))
        {
            ResolveReturnPatrolIndex();
        }

        if (!patrolRoute.IsIndexValid(returnPatrolIndex))
        {
            movement?.Stop();
            isReturning = false;
            returnCompleted = true;
            return;
        }

        if (HasReachedHorizontalPoint(patrolRoute.GetPointPosition(returnPatrolIndex), returnArrivalDistance))
        {
            movement?.Stop();
            currentPatrolIndex = returnPatrolIndex;
            isReturning = false;
            returnCompleted = true;

            if (waitAfterReturn)
            {
                StartWaitAtCurrentPatrolPoint();
            }

            return;
        }

        movement?.MoveTowards(patrolRoute.GetPointPosition(returnPatrolIndex), returnArrivalDistance, patrolReturnSpeedMultiplier);
    }

    private void ResolveReferences()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (movement == null)
        {
            movement = GetComponent<EnemyMovement>();
        }
    }

    private void CaptureInitialPoseIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void EnsurePatrolIndex()
    {
        if (!HasValidPatrolRoute())
        {
            currentPatrolIndex = -1;
            return;
        }

        if (patrolRoute.IsIndexValid(currentPatrolIndex))
        {
            return;
        }

        if (patrolRoute.IsIndexValid(startPointIndex))
        {
            currentPatrolIndex = startPointIndex;
            return;
        }

        int closestIndex = patrolRoute.GetClosestPointIndex(transform.position);
        currentPatrolIndex = closestIndex >= 0 ? closestIndex : 0;
    }

    private void ResolveReturnPatrolIndex()
    {
        if (!HasValidPatrolRoute())
        {
            returnPatrolIndex = -1;
            return;
        }

        if (returnToClosestPatrolPoint)
        {
            returnPatrolIndex = patrolRoute.GetClosestPointIndex(transform.position);
        }
        else
        {
            EnsurePatrolIndex();
            returnPatrolIndex = currentPatrolIndex;
        }

        if (!patrolRoute.IsIndexValid(returnPatrolIndex))
        {
            returnPatrolIndex = patrolRoute.IsIndexValid(startPointIndex) ? startPointIndex : 0;
        }
    }

    private bool HasValidPatrolRoute()
    {
        return patrolRoute != null && patrolRoute.IsValid;
    }

    private bool HasReachedPatrolPoint(int pointIndex)
    {
        if (!HasValidPatrolRoute() || !patrolRoute.IsIndexValid(pointIndex))
        {
            return true;
        }

        return HasReachedHorizontalPoint(patrolRoute.GetPointPosition(pointIndex), patrolArrivalDistance);
    }

    private bool HasReachedHorizontalPoint(Vector3 point, float distance)
    {
        Vector3 current = transform.position;
        current.y = 0f;
        point.y = 0f;
        return (current - point).sqrMagnitude <= distance * distance;
    }

    private void StartWaitAtCurrentPatrolPoint()
    {
        if (!HasValidPatrolRoute() || !patrolRoute.IsIndexValid(currentPatrolIndex))
        {
            return;
        }

        float waitDuration = patrolRoute.GetPointWaitTime(currentPatrolIndex, defaultWaitTimeAtPoint);
        if (waitDuration <= 0f)
        {
            AdvanceToNextPatrolPoint();
            return;
        }

        isWaiting = true;
        waitUntil = Time.time + waitDuration;
    }

    private void AdvanceToNextPatrolPoint()
    {
        if (!HasValidPatrolRoute())
        {
            currentPatrolIndex = -1;
            return;
        }

        currentPatrolIndex = patrolRoute.GetNextIndex(currentPatrolIndex, ref patrolDirection);
    }

    private void FaceInitialDirection()
    {
        if (movement == null)
        {
            return;
        }

        Vector3 forward = initialRotation * Vector3.forward;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        movement.FaceTarget(movement.CurrentPosition + forward);
    }

    private Vector3 GetActiveDestination()
    {
        if (mode == EnemyDutyMode.Guard)
        {
            return GuardPoint;
        }

        if (HasValidPatrolRoute())
        {
            int index = isReturning && patrolRoute.IsIndexValid(returnPatrolIndex)
                ? returnPatrolIndex
                : currentPatrolIndex;

            if (patrolRoute.IsIndexValid(index))
            {
                return patrolRoute.GetPointPosition(index);
            }
        }

        return transform.position;
    }

    private void RefreshDebugSnapshot()
    {
        debugMode = mode.ToString();
        debugIsWaiting = isWaiting;
        debugIsReturning = isReturning;
        debugCurrentPatrolIndex = currentPatrolIndex;
        debugActiveDestination = GetActiveDestination();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGuardGizmo || mode != EnemyDutyMode.Guard)
        {
            return;
        }

        Vector3 point;
        if (Application.isPlaying)
        {
            point = GuardPoint;
        }
        else if (!useInitialPositionAsGuardPoint && guardPointOverride != null)
        {
            point = guardPointOverride.position;
        }
        else
        {
            point = transform.position;
        }

        Gizmos.DrawWireSphere(point, guardGizmoRadius);
        Gizmos.DrawLine(transform.position, point);
    }

    private void OnValidate()
    {
        guardReturnSpeedMultiplier = Mathf.Max(0f, guardReturnSpeedMultiplier);
        patrolArrivalDistance = Mathf.Max(0.05f, patrolArrivalDistance);
        patrolSpeedMultiplier = Mathf.Max(0f, patrolSpeedMultiplier);
        defaultWaitTimeAtPoint = Mathf.Max(0f, defaultWaitTimeAtPoint);
        patrolReturnSpeedMultiplier = Mathf.Max(0f, patrolReturnSpeedMultiplier);
        returnArrivalDistance = Mathf.Max(0.05f, returnArrivalDistance);
        guardGizmoRadius = Mathf.Max(0.05f, guardGizmoRadius);
        RefreshDebugSnapshot();
    }
}
