using System.Collections;
using UnityEngine;

/// <summary>
/// Acción de escena para mover un objeto/NPC siguiendo una lista de puntos definidos en la scene.
///
/// Uso típico: invocar BeginPath() desde MissionSceneResponseBlock.Custom Scene Actions.
/// No usa NavMesh, Timeline ni clips; solo mueve un Transform de waypoint en waypoint.
/// </summary>
[DisallowMultipleComponent]
public sealed class ScenePathMoveAction : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("Transform que se moverá. Si queda vacío, se mueve este GameObject.")]
    private Transform targetToMove;

    [Header("Path")]
    [SerializeField, Tooltip("Puntos que seguirá el target, en orden. Deben ser objetos de la escena, idealmente Empty GameObjects.")]
    private Transform[] waypoints;

    [SerializeField, Tooltip("Si está activo, los waypoints null se ignoran. Si está desactivado, un waypoint null corta la acción con warning.")]
    private bool skipNullWaypoints = true;

    [SerializeField, Tooltip("Si está activo, al llegar a cada waypoint ajusta la posición exacta al punto.")]
    private bool snapToWaypoint = true;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float speed = 4f;

    [SerializeField, Min(0f), Tooltip("Distancia a partir de la cual se considera alcanzado un waypoint.")]
    private float arrivalDistance = 0.15f;

    [SerializeField, Min(0f), Tooltip("Duración máxima de toda la ruta. 0 = sin límite. Recomendado usar un valor mayor a 0 como seguro anti-bugs.")]
    private float maxDuration = 10f;

    [Header("Rotation")]
    [SerializeField, Tooltip("Si está activo, el target orienta su forward hacia la dirección de movimiento.")]
    private bool alignForwardToMovement = true;

    [SerializeField, Tooltip("Si está activo, rota solo en horizontal, manteniendo el objeto derecho. Útil para humanoides. Para vuelo inclinado, desactivarlo.")]
    private bool keepUpright = true;

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

    [SerializeField, Tooltip("Si la acción se invoca mientras ya se está ejecutando, reinicia la ruta desde el primer waypoint.")]
    private bool restartIfAlreadyRunning = true;

    [SerializeField]
    private bool logDebug;

    private Coroutine pathRoutine;
    private bool isMoving;

    private void Reset()
    {
        targetToMove = transform;
        objectToDisable = gameObject;
    }

    private void OnDisable()
    {
        StopPath();
    }

    /// <summary>
    /// Alias corto para facilitar selección desde UnityEvent.
    /// </summary>
    public void Begin()
    {
        BeginPath();
    }

    public void BeginPath()
    {
        if (isMoving)
        {
            if (!restartIfAlreadyRunning)
            {
                return;
            }

            StopPath();
        }

        Transform target = ResolveTarget();
        if (target == null)
        {
            Debug.LogWarning($"{nameof(ScenePathMoveAction)} '{name}' has no target to move.", this);
            return;
        }

        if (speed <= 0f)
        {
            Debug.LogWarning($"{nameof(ScenePathMoveAction)} '{name}' cannot move because Speed is 0.", this);
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"{nameof(ScenePathMoveAction)} '{name}' has no waypoints.", this);
            return;
        }

        pathRoutine = StartCoroutine(PathRoutine(target));
    }

    public void StopPath()
    {
        if (pathRoutine != null)
        {
            StopCoroutine(pathRoutine);
            pathRoutine = null;
        }

        isMoving = false;
    }

    private IEnumerator PathRoutine(Transform target)
    {
        isMoving = true;
        float elapsed = 0f;
        int waypointIndex = 0;

        if (logDebug)
        {
            Debug.Log($"{nameof(ScenePathMoveAction)} '{name}' started moving '{target.name}' through {waypoints.Length} waypoint(s).", this);
        }

        while (waypointIndex < waypoints.Length)
        {
            Transform waypoint = waypoints[waypointIndex];

            if (waypoint == null)
            {
                if (!skipNullWaypoints)
                {
                    Debug.LogWarning($"{nameof(ScenePathMoveAction)} '{name}' found a null waypoint at index {waypointIndex}.", this);
                    break;
                }

                waypointIndex++;
                continue;
            }

            Vector3 deltaToWaypoint = waypoint.position - target.position;
            float distanceToWaypoint = deltaToWaypoint.magnitude;

            if (distanceToWaypoint <= arrivalDistance)
            {
                if (snapToWaypoint)
                {
                    target.position = waypoint.position;
                }

                waypointIndex++;
                continue;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float stepDistance = speed * deltaTime;
            Vector3 moveDirection = deltaToWaypoint.normalized;

            if (stepDistance >= distanceToWaypoint)
            {
                target.position = waypoint.position;
                AlignIfNeeded(target, moveDirection, deltaTime);
                waypointIndex++;
            }
            else
            {
                target.position += moveDirection * stepDistance;
                AlignIfNeeded(target, moveDirection, deltaTime);
            }

            elapsed += deltaTime;
            if (maxDuration > 0f && elapsed >= maxDuration)
            {
                if (logDebug)
                {
                    Debug.Log($"{nameof(ScenePathMoveAction)} '{name}' reached Max Duration before finishing the path.", this);
                }

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

    private void AlignIfNeeded(Transform target, Vector3 moveDirection, float deltaTime)
    {
        if (!alignForwardToMovement || target == null || moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 rotationDirection = moveDirection.normalized;

        if (keepUpright)
        {
            rotationDirection.y = 0f;

            if (rotationDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            rotationDirection.Normalize();
        }

        Quaternion desiredRotation = Quaternion.LookRotation(rotationDirection, Vector3.up);

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
        pathRoutine = null;

        if (logDebug && target != null)
        {
            Debug.Log($"{nameof(ScenePathMoveAction)} '{name}' finished moving '{target.name}'.", this);
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
