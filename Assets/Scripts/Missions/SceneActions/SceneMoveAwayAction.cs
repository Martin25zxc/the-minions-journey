using System.Collections;
using UnityEngine;

/// <summary>
/// Acción de escena para retirar un objeto/NPC in-game moviéndolo durante unos segundos
/// o hacia un punto destino, y opcionalmente desactivándolo al finalizar.
///
/// Uso típico: invocar BeginMoveAway() desde MissionSceneResponseBlock.Custom Scene Actions.
/// No reproduce clips ni Timeline; solo mueve un Transform en gameplay.
/// </summary>
[DisallowMultipleComponent]
public sealed class SceneMoveAwayAction : MonoBehaviour
{
    private enum MovementMode
    {
        Direction = 0,
        TargetPoint = 1
    }

    [Header("Target")]
    [SerializeField, Tooltip("Transform que se moverá. Si queda vacío, se mueve este GameObject.")]
    private Transform targetToMove;

    [Header("Movement")]
    [SerializeField]
    private MovementMode movementMode = MovementMode.Direction;

    [SerializeField, Tooltip("Dirección usada en modo Direction. Puede incluir Y para simular vuelo/ascenso.")]
    private Vector3 direction = Vector3.forward;

    [SerializeField, Tooltip("Si está activo, Direction se interpreta en espacio local del target. Si está desactivado, se interpreta como dirección de mundo.")]
    private bool useLocalDirection;

    [SerializeField, Tooltip("Destino usado en modo TargetPoint.")]
    private Transform targetPoint;

    [SerializeField, Min(0f)]
    private float speed = 4f;

    [SerializeField, Min(0f), Tooltip("Duración máxima del movimiento. En modo Direction debería ser mayor a 0 para evitar movimiento infinito.")]
    private float maxDuration = 5f;

    [SerializeField, Min(0f), Tooltip("Distancia a partir de la cual se considera alcanzado el Target Point.")]
    private float stoppingDistance = 0.1f;

    [Header("Rotation")]
    [SerializeField, Tooltip("Si está activo, el target orienta su forward hacia la dirección de movimiento.")]
    private bool alignForwardToMovement = true;

    [SerializeField, Min(0f), Tooltip("Velocidad de rotación en grados por segundo. 0 = rotación instantánea.")]
    private float rotationSpeed = 720f;

    [Header("Finish")]
    [SerializeField]
    private bool disableOnFinish = true;

    [SerializeField, Tooltip("GameObject que se desactivará al terminar. Si queda vacío, se desactiva el GameObject del target movido.")]
    private GameObject objectToDisable;

    [Header("Options")]
    [SerializeField]
    private bool useUnscaledTime;

    [SerializeField, Tooltip("Si la acción se invoca mientras ya se está ejecutando, reinicia el movimiento.")]
    private bool restartIfAlreadyRunning = true;

    [SerializeField]
    private bool logDebug;

    private Coroutine moveRoutine;
    private bool isMoving;

    private void Reset()
    {
        targetToMove = transform;
        objectToDisable = gameObject;
    }

    /// <summary>
    /// Alias corto para facilitar selección desde UnityEvent.
    /// </summary>
    public void Begin()
    {
        BeginMoveAway();
    }

    public void BeginMoveAway()
    {
        if (isMoving)
        {
            if (!restartIfAlreadyRunning)
            {
                return;
            }

            StopMoveAway();
        }

        Transform target = ResolveTarget();
        if (target == null)
        {
            Debug.LogWarning($"{nameof(SceneMoveAwayAction)} '{name}' has no target to move.", this);
            return;
        }

        if (speed <= 0f)
        {
            Debug.LogWarning($"{nameof(SceneMoveAwayAction)} '{name}' cannot move because Speed is 0.", this);
            return;
        }

        moveRoutine = StartCoroutine(MoveRoutine(target));
    }

    public void StopMoveAway()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isMoving = false;
    }

    private IEnumerator MoveRoutine(Transform target)
    {
        isMoving = true;
        float elapsed = 0f;

        if (logDebug)
        {
            Debug.Log($"{nameof(SceneMoveAwayAction)} '{name}' started moving '{target.name}'.", this);
        }

        while (true)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Vector3 moveDirection;
            float stepDistance = speed * deltaTime;

            if (movementMode == MovementMode.TargetPoint && targetPoint != null)
            {
                Vector3 deltaToTarget = targetPoint.position - target.position;
                float distanceToTarget = deltaToTarget.magnitude;

                if (distanceToTarget <= stoppingDistance)
                {
                    break;
                }

                moveDirection = deltaToTarget.normalized;

                if (stepDistance >= distanceToTarget)
                {
                    target.position = targetPoint.position;
                    AlignIfNeeded(target, moveDirection, deltaTime);
                    break;
                }

                target.position += moveDirection * stepDistance;
                AlignIfNeeded(target, moveDirection, deltaTime);
            }
            else
            {
                moveDirection = ResolveDirection(target);

                if (moveDirection.sqrMagnitude <= 0.0001f)
                {
                    Debug.LogWarning($"{nameof(SceneMoveAwayAction)} '{name}' has an empty Direction.", this);
                    break;
                }

                target.position += moveDirection * stepDistance;
                AlignIfNeeded(target, moveDirection, deltaTime);
            }

            elapsed += deltaTime;
            if (maxDuration > 0f && elapsed >= maxDuration)
            {
                break;
            }

            if (movementMode == MovementMode.Direction && maxDuration <= 0f)
            {
                Debug.LogWarning($"{nameof(SceneMoveAwayAction)} '{name}' is in Direction mode with Max Duration = 0. Stopping after one frame to avoid infinite movement.", this);
                break;
            }

            yield return null;
        }

        Finish(target);
    }

    private Transform ResolveTarget()
    {
        return targetToMove != null ? targetToMove : transform;
    }

    private Vector3 ResolveDirection(Transform target)
    {
        Vector3 resolvedDirection = direction;

        if (useLocalDirection && target != null)
        {
            resolvedDirection = target.TransformDirection(direction);
        }

        return resolvedDirection.sqrMagnitude > 0.0001f ? resolvedDirection.normalized : Vector3.zero;
    }

    private void AlignIfNeeded(Transform target, Vector3 moveDirection, float deltaTime)
    {
        if (!alignForwardToMovement || target == null || moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);

        if (rotationSpeed <= 0f)
        {
            target.rotation = desiredRotation;
            return;
        }

        target.rotation = Quaternion.RotateTowards(
            target.rotation,
            desiredRotation,
            rotationSpeed * deltaTime);
    }

    private void Finish(Transform target)
    {
        isMoving = false;
        moveRoutine = null;

        if (logDebug && target != null)
        {
            Debug.Log($"{nameof(SceneMoveAwayAction)} '{name}' finished moving '{target.name}'.", this);
        }

        if (!disableOnFinish)
        {
            return;
        }

        GameObject targetObject = objectToDisable != null
            ? objectToDisable
            : target != null ? target.gameObject : gameObject;

        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }
}
