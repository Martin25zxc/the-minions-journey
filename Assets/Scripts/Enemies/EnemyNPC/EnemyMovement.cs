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

    [Header("Rigidbody Setup - Provisorio / Revisar")]
    [Tooltip("Ayuda inicial para estabilizar pruebas top-down. Revisar antes de cerrar arquitectura, especialmente cuando se implemente Leap Attack.")]
    [SerializeField]
    private bool configureRigidbodyForTopDownOnAwake = true;

    [Tooltip("Provisorio. Si el Leap mueve el root fisico en Y, esto debera apagarse. Si solo salta el Visual, podria quedar activo.")]
    [SerializeField]
    private bool freezePositionY = true;

    [Tooltip("Provisorio. Evita rotaciones fisicas raras en arenas planas, pero revisar con colisiones reales.")]
    [SerializeField]
    private bool freezeRotationXAndZ = true;

    [Header("Movement")]
    [SerializeField]
    private bool useRigidbodyMovePosition = true;

    [Tooltip("Provisorio. Mantiene la altura inicial del root. Revisar cuando se decida si el salto mueve root o solo visual.")]
    [SerializeField]
    private bool keepInitialY = true;

    private Vector3 desiredVelocity;
    private bool hasMoveRequest;
    private float initialY;

    public bool IsMoving => hasMoveRequest && desiredVelocity.sqrMagnitude > 0.0001f;
    public Vector3 DesiredVelocity => desiredVelocity;
    public Vector3 CurrentPosition => rb != null ? rb.position : transform.position;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        initialY = transform.position.y;

        if (configureRigidbodyForTopDownOnAwake && rb != null)
        {
            ConfigureRigidbodyForTopDown();
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
        if (definition == null)
        {
            Stop();
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 toTarget = targetPosition - currentPosition;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
        {
            Stop();
            FaceTarget(targetPosition);
            return;
        }

        Vector3 direction = toTarget.normalized;
        desiredVelocity = direction * definition.MoveSpeed;
        hasMoveRequest = true;
        RotateTowards(direction);
    }

    public void Stop()
    {
        desiredVelocity = Vector3.zero;
        hasMoveRequest = false;
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        RotateTowards(toTarget.normalized);
    }

    /// <summary>
    /// Movimiento controlado inmediato para abilities como Leap.
    /// No deja una orden de movimiento persistente: solo coloca el root en la posicion indicada.
    /// </summary>
    public void MoveControlledTo(Vector3 worldPosition, bool preserveCurrentY = true)
    {
        Stop();

        Vector3 nextPosition = worldPosition;
        if (preserveCurrentY)
        {
            nextPosition.y = CurrentPosition.y;
        }
        else if (keepInitialY)
        {
            nextPosition.y = initialY;
        }

        if (useRigidbodyMovePosition && rb != null)
        {
            rb.MovePosition(nextPosition);
            return;
        }

        transform.position = nextPosition;
    }

    public void SetInitialYFromCurrentPosition()
    {
        initialY = CurrentPosition.y;
    }

    private void FixedUpdate()
    {
        if (keepInitialY)
        {
            KeepGroundHeight();
        }

        if (!hasMoveRequest)
        {
            return;
        }

        Vector3 delta = desiredVelocity * Time.fixedDeltaTime;
        Vector3 nextPosition = rb != null ? rb.position + delta : transform.position + delta;

        if (keepInitialY)
        {
            nextPosition.y = initialY;
        }

        if (useRigidbodyMovePosition && rb != null)
        {
            rb.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    private void RotateTowards(Vector3 direction)
    {
        if (definition == null || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            definition.RotateSpeed * Time.deltaTime);
    }

    private void ConfigureRigidbodyForTopDown()
    {
        rb.useGravity = false;

        RigidbodyConstraints constraints = rb.constraints;

        if (freezePositionY)
        {
            constraints |= RigidbodyConstraints.FreezePositionY;
        }

        if (freezeRotationXAndZ)
        {
            constraints |= RigidbodyConstraints.FreezeRotationX;
            constraints |= RigidbodyConstraints.FreezeRotationZ;
        }

        rb.constraints = constraints;
    }

    private void KeepGroundHeight()
    {
        if (rb != null)
        {
            Vector3 position = rb.position;
            if (!Mathf.Approximately(position.y, initialY))
            {
                position.y = initialY;
                rb.MovePosition(position);
            }

            return;
        }

        Vector3 transformPosition = transform.position;
        if (!Mathf.Approximately(transformPosition.y, initialY))
        {
            transformPosition.y = initialY;
            transform.position = transformPosition;
        }
    }
}
