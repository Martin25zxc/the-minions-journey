using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class EnemyMovement : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField]
    private EnemyDefinition definition;

    [Header("References")]
    [SerializeField]
    private Rigidbody rb;

    [Header("Residual Physics")]
    [Tooltip("Cuando el brain entra en Idle, limpia velocidad horizontal residual. No toca Y para no pelear contra gravedad, rampas o escaleras.")]
    [SerializeField]
    private bool clearPlanarVelocityOnIdle = true;

    [Tooltip("Cuando el brain entra en Idle, corta giro fisico residual generado por colisiones/knockback.")]
    [SerializeField]
    private bool clearAngularVelocityOnIdle = true;

    [Tooltip("Al morir, limpia velocidad horizontal residual. No toca Y.")]
    [SerializeField]
    private bool clearPlanarVelocityOnDeath = true;

    [Tooltip("Al morir, corta giro fisico residual.")]
    [SerializeField]
    private bool clearAngularVelocityOnDeath = true;

    [Tooltip("Para enemigos sin ragdoll, deja el cuerpo quieto al morir y evita deslizamientos. Apagar si mas adelante se implementa ragdoll/cadaver fisico.")]
    [SerializeField]
    private bool makeRigidbodyKinematicOnDeath = true;

    private Vector3 desiredVelocity;
    private bool hasMoveRequest;
    private Vector3 desiredFacingDirection;
    private bool hasRotationRequest;

    public bool IsMoving => hasMoveRequest && desiredVelocity.sqrMagnitude > 0.0001f;
    public Vector3 DesiredVelocity => desiredVelocity;
    public Vector3 CurrentPosition => rb != null ? rb.position : transform.position;
    public Rigidbody Rigidbody => rb;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    public void Initialize(EnemyDefinition newDefinition)
    {
        if (newDefinition != null)
        {
            definition = newDefinition;
        }
    }

    public void MoveTowards(Vector3 targetPosition, float stopDistance)
    {
        MoveTowards(targetPosition, stopDistance, 1f);
    }

    public void MoveTowards(Vector3 targetPosition, float stopDistance, float speedMultiplier)
    {
        if (definition == null)
        {
            Stop();
            return;
        }

        Vector3 currentPosition = CurrentPosition;
        Vector3 toTarget = targetPosition - currentPosition;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
        {
            Stop();
            FaceTarget(targetPosition);
            return;
        }

        MoveInDirection(toTarget.normalized, speedMultiplier);
    }

    public void MoveInDirection(Vector3 direction, float speedMultiplier = 1f)
    {
        if (definition == null)
        {
            Stop();
            return;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            Stop();
            return;
        }

        direction.Normalize();
        desiredVelocity = direction * definition.MoveSpeed * Mathf.Max(0f, speedMultiplier);
        hasMoveRequest = desiredVelocity.sqrMagnitude > 0.0001f;

        if (hasMoveRequest)
        {
            RequestRotation(direction);
        }
    }

    public void MoveAwayFrom(Vector3 threatPosition, float speedMultiplier = 1f)
    {
        Vector3 away = CurrentPosition - threatPosition;
        away.y = 0f;
        MoveInDirection(away, speedMultiplier);
    }

    public void Stop()
    {
        desiredVelocity = Vector3.zero;
        hasMoveRequest = false;
        hasRotationRequest = false;
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - CurrentPosition;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        RequestRotation(toTarget.normalized);
    }

    /// <summary>
    /// Movimiento controlado inmediato para abilities como Leap.
    /// No mantiene una orden de movimiento persistente.
    /// Por defecto preserva la Y actual para no pelear contra gravedad ni forzar una altura global.
    /// </summary>
    public void MoveControlledTo(Vector3 worldPosition, bool preserveCurrentY = true)
    {
        Stop();

        Vector3 nextPosition = worldPosition;
        if (preserveCurrentY)
        {
            nextPosition.y = CurrentPosition.y;
        }

        rb.MovePosition(nextPosition);
    }

    public void ClearIdleResidualPhysics()
    {
        ClearResidualPhysics(clearPlanarVelocityOnIdle, clearAngularVelocityOnIdle);
    }

    public void ApplyDeathPhysics()
    {
        Stop();
        ClearResidualPhysics(clearPlanarVelocityOnDeath, clearAngularVelocityOnDeath);

        if (makeRigidbodyKinematicOnDeath && rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public void ClearResidualPhysics(bool clearPlanarVelocity = true, bool clearAngularVelocity = true)
    {
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        if (clearPlanarVelocity)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }

        if (clearAngularVelocity)
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyRotation();
    }

    private void ApplyMovement()
    {
        if (!hasMoveRequest || rb == null)
        {
            return;
        }

        Vector3 delta = desiredVelocity * Time.fixedDeltaTime;
        Vector3 nextPosition = rb.position + delta;
        rb.MovePosition(nextPosition);
    }

    private void ApplyRotation()
    {
        if (!hasRotationRequest || definition == null || desiredFacingDirection.sqrMagnitude <= 0.0001f || rb == null)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(desiredFacingDirection, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            rb.rotation,
            targetRotation,
            definition.RotateSpeed * Time.fixedDeltaTime);

        rb.MoveRotation(nextRotation);

        if (Quaternion.Angle(nextRotation, targetRotation) <= 0.1f)
        {
            hasRotationRequest = false;
        }
    }

    private void RequestRotation(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        desiredFacingDirection = direction.normalized;
        hasRotationRequest = true;
    }
}
