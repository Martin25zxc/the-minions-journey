using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class TopDownPlayerController : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    float moveSpeed = 6f;

    [SerializeField, Min(1f)]
    float sprintMultiplier = 4.5f;

    [SerializeField]
    Camera gameplayCamera;

    [SerializeField, Min(0.001f)]
    float aimDeadZone = 0.001f;

    [SerializeField, Min(0.25f)]
    float facingIndicatorLength = 1.5f;

    [SerializeField, Range(0.01f, 0.2f)]
    float facingIndicatorWidth = 0.05f;

    [SerializeField, Range(0f, 10f)]
    float aimRotationDeadZoneDegrees = 2f;

    [SerializeField, Min(0.01f)]
    float movementAcceleration = 40f;

    [SerializeField, Min(0.01f)]
    float movementFriction = 24f;

    [Header("Body Blocking")]
    [Tooltip("Evita que la locomotion normal del jugador empuje cuerpos vivos o blockers. Los impactos/knockback deben venir por ImpactReceiver, no por caminar contra un Rigidbody dinamico.")]
    [SerializeField]
    private bool useBodyBlocking = true;

    [Tooltip("CapsuleCollider usado para castear el volumen del jugador. Si se deja vacio, se busca en el mismo GameObject.")]
    [SerializeField]
    private CapsuleCollider bodyCollider;

    [Tooltip("Layers que bloquean locomotion normal. Recomendado: Enemy, Obstacle, Boundary. No incluir Ground, Loot, Corpse ni Projectile.")]
    [SerializeField]
    private LayerMask bodyBlockingLayers;

    [Tooltip("Distancia minima de chequeo frontal. Ayuda a detectar blockers antes de que la fisica resuelva empujando otro Rigidbody.")]
    [SerializeField, Min(0.001f)]
    private float bodyBlockProbeDistance = 0.08f;

    [Tooltip("Margen pequeno para no quedar pegado al blocker. No debe ser alto.")]
    [SerializeField, Min(0f)]
    private float bodyBlockSkinWidth = 0.03f;

    [Tooltip("Si esta activo, al intentar moverse contra un blocker intenta deslizar por la superficie en vez de frenar completamente.")]
    [SerializeField]
    private bool slideAlongBlockedSurfaces = true;

    [Tooltip("Cuando el jugador intenta avanzar contra un blocker, elimina la parte de la velocidad horizontal que apunta hacia ese blocker. Esto evita empujar enemigos por inercia.")]
    [SerializeField]
    private bool clearVelocityIntoBlocker = true;

    [SerializeField]
    private QueryTriggerInteraction bodyBlockTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Debug")]
    [SerializeField]
    private bool drawBodyBlockDebug;

    [SerializeField]
    Color facingIndicatorColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    Rigidbody body;
    LineRenderer facingIndicator;
    Vector2 moveInput;
    Vector3 aimDirection = Vector3.forward;
    bool isSprinting;

    private readonly Collider[] bodyBlockOverlapBuffer = new Collider[8];

    public Vector3 AimDirection => aimDirection;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CapsuleCollider>();
        }

        //Todo Borrar
        //facingIndicator = CreateFacingIndicator();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    void Update()
    {
        moveInput = ReadMovementInput();
        aimDirection = ReadAimDirection();
        isSprinting = ReadSprintInput();
    }

    void FixedUpdate()
    {
        Vector3 movement = GetCameraRelativeMovement();
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        float currentMoveSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 currentVelocity = body.linearVelocity;
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 desiredAcceleration;

        if (movement.sqrMagnitude > 0.0001f)
        {
            movement = ResolveBodyBlockedMovement(movement, currentMoveSpeed, ref currentHorizontalVelocity);

            Vector3 targetHorizontalVelocity = movement * currentMoveSpeed;
            desiredAcceleration = Vector3.ClampMagnitude((targetHorizontalVelocity - currentHorizontalVelocity) / Time.fixedDeltaTime, movementAcceleration);
        }
        else
        {
            desiredAcceleration = Vector3.ClampMagnitude(-currentHorizontalVelocity / Time.fixedDeltaTime, movementFriction);
        }

        body.AddForce(desiredAcceleration, ForceMode.Acceleration);

        if (aimDirection.sqrMagnitude > aimDeadZone)
        {
            float rotationDelta = Vector3.Angle(transform.forward, aimDirection);
            if (rotationDelta > aimRotationDeadZoneDegrees)
            {
                body.MoveRotation(Quaternion.LookRotation(aimDirection, Vector3.up));
            }
        }
    }

    private Vector3 ResolveBodyBlockedMovement(Vector3 desiredMovement, float currentMoveSpeed, ref Vector3 currentHorizontalVelocity)
    {
        if (!useBodyBlocking || bodyCollider == null || bodyBlockingLayers.value == 0 || desiredMovement.sqrMagnitude <= 0.0001f)
        {
            return desiredMovement;
        }

        Vector3 desiredDirection = desiredMovement.normalized;
        float castDistance = Mathf.Max(bodyBlockProbeDistance, currentMoveSpeed * Time.fixedDeltaTime + bodyBlockSkinWidth);

        if (!TryFindBodyBlock(desiredDirection, castDistance, out MovementBlockInfo blockInfo))
        {
            if (drawBodyBlockDebug)
            {
                Debug.DrawRay(body.position + Vector3.up, desiredDirection * castDistance, Color.green, Time.fixedDeltaTime);
            }

            return desiredMovement;
        }

        if (drawBodyBlockDebug)
        {
            Debug.DrawRay(body.position + Vector3.up, desiredDirection * castDistance, Color.red, Time.fixedDeltaTime);
            Debug.DrawRay(body.position + Vector3.up, blockInfo.Normal, Color.yellow, Time.fixedDeltaTime);
        }

        if (clearVelocityIntoBlocker)
        {
            RemoveVelocityIntoBlocker(blockInfo.Normal, ref currentHorizontalVelocity);
        }

        if (!slideAlongBlockedSurfaces)
        {
            return Vector3.zero;
        }

        Vector3 slideMovement = Vector3.ProjectOnPlane(desiredMovement, blockInfo.Normal);
        slideMovement.y = 0f;

        if (slideMovement.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        slideMovement = Vector3.ClampMagnitude(slideMovement, desiredMovement.magnitude);

        // Segundo chequeo: no deslizar hacia otra pared/enemigo inmediatamente pegado.
        Vector3 slideDirection = slideMovement.normalized;
        if (TryFindBodyBlock(slideDirection, castDistance, out _))
        {
            return Vector3.zero;
        }

        return slideMovement;
    }

    private void RemoveVelocityIntoBlocker(Vector3 blockerNormal, ref Vector3 currentHorizontalVelocity)
    {
        blockerNormal.y = 0f;
        if (blockerNormal.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        blockerNormal.Normalize();

        // Si dot < 0, la velocidad apunta hacia la superficie. La proyectamos para no seguir empujando.
        if (Vector3.Dot(currentHorizontalVelocity, blockerNormal) < 0f)
        {
            currentHorizontalVelocity = Vector3.ProjectOnPlane(currentHorizontalVelocity, blockerNormal);
            Vector3 velocity = body.linearVelocity;
            velocity.x = currentHorizontalVelocity.x;
            velocity.z = currentHorizontalVelocity.z;
            body.linearVelocity = velocity;
        }
    }

    private bool TryFindBodyBlock(Vector3 direction, float castDistance, out MovementBlockInfo blockInfo)
    {
        blockInfo = default;

        if (bodyCollider == null)
        {
            return false;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        direction.Normalize();

        GetCapsuleWorldData(bodyCollider, out Vector3 pointA, out Vector3 pointB, out float radius);

        // Primero resolvemos overlaps actuales. CapsuleCast no siempre reporta colliders ya solapados al inicio.
        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            pointA,
            pointB,
            radius + bodyBlockSkinWidth,
            bodyBlockOverlapBuffer,
            bodyBlockingLayers,
            bodyBlockTriggerInteraction);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider other = bodyBlockOverlapBuffer[i];
            if (other == null || other == bodyCollider)
            {
                continue;
            }

            if (Physics.ComputePenetration(
                bodyCollider,
                bodyCollider.transform.position,
                bodyCollider.transform.rotation,
                other,
                other.transform.position,
                other.transform.rotation,
                out Vector3 separationDirection,
                out float separationDistance))
            {
                separationDirection.y = 0f;
                if (separationDirection.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                separationDirection.Normalize();

                // ComputePenetration devuelve la direccion para sacar al jugador del otro collider.
                // Si la direccion deseada va contra esa separacion, estamos intentando meternos en el blocker.
                if (Vector3.Dot(direction, -separationDirection) > 0.1f)
                {
                    blockInfo = new MovementBlockInfo(other, separationDirection, separationDistance);
                    return true;
                }
            }
        }

        if (Physics.CapsuleCast(
            pointA,
            pointB,
            radius,
            direction,
            out RaycastHit hit,
            castDistance,
            bodyBlockingLayers,
            bodyBlockTriggerInteraction))
        {
            Vector3 normal = hit.normal;
            normal.y = 0f;

            if (normal.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            normal.Normalize();
            blockInfo = new MovementBlockInfo(hit.collider, normal, hit.distance);
            return true;
        }

        return false;
    }

    private static void GetCapsuleWorldData(CapsuleCollider capsule, out Vector3 pointA, out Vector3 pointB, out float radius)
    {
        Transform capsuleTransform = capsule.transform;
        Vector3 center = capsuleTransform.TransformPoint(capsule.center);
        Vector3 axis = GetCapsuleAxisWorld(capsule);

        Vector3 lossyScale = capsuleTransform.lossyScale;
        float heightScale;
        float radiusScale;

        switch (capsule.direction)
        {
            case 0:
                heightScale = Mathf.Abs(lossyScale.x);
                radiusScale = Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
                break;
            case 2:
                heightScale = Mathf.Abs(lossyScale.z);
                radiusScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
                break;
            default:
                heightScale = Mathf.Abs(lossyScale.y);
                radiusScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z));
                break;
        }

        radius = Mathf.Max(0.001f, capsule.radius * radiusScale);
        float height = Mathf.Max(radius * 2f, capsule.height * heightScale);
        float segmentHalfLength = Mathf.Max(0f, (height * 0.5f) - radius);

        pointA = center + axis * segmentHalfLength;
        pointB = center - axis * segmentHalfLength;
    }

    private static Vector3 GetCapsuleAxisWorld(CapsuleCollider capsule)
    {
        switch (capsule.direction)
        {
            case 0:
                return capsule.transform.right;
            case 2:
                return capsule.transform.forward;
            default:
                return capsule.transform.up;
        }
    }

    Vector3 GetCameraRelativeMovement()
    {
        Camera activeCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        if (activeCamera != null)
        {
            cameraForward = Vector3.ProjectOnPlane(activeCamera.transform.forward, Vector3.up);
            cameraRight = Vector3.ProjectOnPlane(activeCamera.transform.right, Vector3.up);

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                cameraForward.Normalize();
            }
            else
            {
                cameraForward = Vector3.forward;
            }

            if (cameraRight.sqrMagnitude > 0.001f)
            {
                cameraRight.Normalize();
            }
            else
            {
                cameraRight = Vector3.right;
            }
        }

        return cameraRight * moveInput.x + cameraForward * moveInput.y;
    }

    LineRenderer CreateFacingIndicator()
    {
        Transform indicatorRoot = transform.Find("FacingIndicator");
        GameObject indicatorObject;

        if (indicatorRoot == null)
        {
            indicatorObject = new GameObject("FacingIndicator");
            indicatorObject.transform.SetParent(transform, false);
            indicatorObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            indicatorObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            indicatorObject = indicatorRoot.gameObject;
        }

        LineRenderer indicator = indicatorObject.GetComponent<LineRenderer>();
        if (indicator == null)
        {
            indicator = indicatorObject.AddComponent<LineRenderer>();
        }

        indicator.useWorldSpace = false;
        indicator.positionCount = 2;
        indicator.startWidth = facingIndicatorWidth;
        indicator.endWidth = facingIndicatorWidth * 0.8f;
        indicator.startColor = facingIndicatorColor;
        indicator.endColor = facingIndicatorColor;
        indicator.alignment = LineAlignment.View;
        indicator.numCapVertices = 4;
        indicator.numCornerVertices = 4;
        indicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        indicator.receiveShadows = false;
        indicator.sharedMaterial = CreateLineMaterial();
        indicator.SetPosition(0, Vector3.zero);
        indicator.SetPosition(1, Vector3.forward * facingIndicatorLength);

        return indicator;
    }

    static Material lineMaterial;

    static Material CreateLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        Shader shader = Shader.Find("TopDown/UnlitVertexColor");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return lineMaterial;
    }

    Vector2 ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    static bool ReadSprintInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
    }

    Vector3 ReadAimDirection()
    {
        Camera activeCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        Mouse mouse = Mouse.current;
        if (activeCamera == null || mouse == null)
        {
            return transform.forward;
        }

        Ray ray = activeCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane aimPlane = new Plane(Vector3.up, transform.position);

        if (!aimPlane.Raycast(ray, out float enter))
        {
            return transform.forward;
        }

        Vector3 aimPoint = ray.GetPoint(enter);
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude > aimDeadZone ? direction.normalized : transform.forward;
    }

    private readonly struct MovementBlockInfo
    {
        public MovementBlockInfo(Collider collider, Vector3 normal, float distance)
        {
            Collider = collider;
            Normal = normal;
            Distance = distance;
        }

        public Collider Collider { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
    }
}
