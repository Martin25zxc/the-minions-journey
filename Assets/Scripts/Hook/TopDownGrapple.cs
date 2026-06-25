using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TopDownPlayerController))]
public sealed class TopDownGrapple : MonoBehaviour, ISkillBehaviour
{
    private enum GrappleState
    {
        Idle,
        HookFlying,
        Pulling
    }

    public string SkillID => "hook";

    [Header("Input")]
    [SerializeField]
    private bool readInputDirectly = true;
    [SerializeField]
    private TopDownPlayerAnimator playerAnimator;

    [SerializeField]
    private bool pressQAgainCancels = true;

    [Header("Hook")]
    [SerializeField]
    private GameObject hookPrefab;

    [SerializeField, Min(1f)]
    private float pullSpeed = 16f;

    [Tooltip("Distancia a la posicion segura final para considerar que el pull termino. Si en una escena vieja estaba alto, el codigo lo limita internamente.")]
    [SerializeField, Min(0.05f)]
    private float arrivalThreshold = 0.25f;

    [SerializeField, Min(0f)]
    private float hookSpawnDistance = 2.2f;

    [Header("Targeting")]
    [Tooltip("Layers que se consideran enemigos para calcular un destino cerca del target y no sobre el punto exacto de impacto.")]
    [SerializeField]
    private LayerMask enemyLayers;

    [Tooltip("Distancia final contra pared/obstaculo. Evita meter al jugador dentro del collider impactado.")]
    [SerializeField, Min(0.1f)]
    private float wallStopDistance = 0.8f;

    [Tooltip("Distancia final contra enemigo. Debe dejar al jugador a rango jugable, no dentro del collider enemigo.")]
    [SerializeField, Min(0.1f)]
    private float enemyStopDistance = 1.2f;

    [Header("Pull Safety")]
    [Tooltip("Layers que bloquean el movimiento del jugador durante el pull. Recomendado: Obstacle, Rock, Boundary, LimitWall. No incluir Ground. No incluir Enemy por ahora si no se filtra el target.")]
    [SerializeField]
    private LayerMask grappleBlockingLayers;

    [SerializeField]
    private QueryTriggerInteraction grappleBlockTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Tooltip("CapsuleCollider del jugador usado para validar el trayecto del pull. Si se deja vacio, se busca en el mismo GameObject.")]
    [SerializeField]
    private CapsuleCollider playerCapsule;

    [Tooltip("Margen pequeno para evitar quedar solapado con blockers al final del hook.")]
    [SerializeField, Min(0f)]
    private float grappleSkinWidth = 0.03f;

    [Tooltip("Distancia maxima que el jugador puede recorrer durante el pull. Debe ser igual o menor al maxRange real del projectile. Si un collider devuelve un punto raro, el hook se cancela en vez de arrastrar al jugador fuera del nivel.")]
    [SerializeField, Min(1f)]
    private float maxPullDistance = 18f;

    [Tooltip("Si el destino calculado queda demasiado cerca de una pared/blocker, retrocede en pasos hasta encontrar un punto seguro.")]
    [SerializeField, Min(0.01f)]
    private float destinationBackoffStep = 0.25f;

    [Tooltip("Cantidad maxima de intentos de retroceso para encontrar un destino seguro antes de cancelar el hook.")]
    [SerializeField, Min(0)]
    private int destinationBackoffAttempts = 8;

    [Tooltip("Exige que el volumen del jugador tenga camino libre hasta el destino final al momento de enganchar. Evita atravesar paredes por destinos calculados detras o dentro de colliders.")]
    [SerializeField]
    private bool requireClearInitialPullPath = true;

    [Tooltip("Muestra warnings cuando el hook cancela por seguridad. Util para ajustar layers/distancias en escena.")]
    [SerializeField]
    private bool logGrappleSafetyWarnings;

    [Header("Impact Interaction")]
    [Tooltip("Si un impacto/knockback/stun entra durante el hook, el hook se cancela y ImpactReceiver queda como autoridad.")]
    [SerializeField]
    private bool cancelOnImpact = true;

    [Header("Rope Visual")]
    [SerializeField, Min(0.005f)]
    private float ropeWidth = 0.04f;

    [SerializeField]
    private Color ropeColor = new Color(0.85f, 0.7f, 0.4f, 1f);

    [SerializeField, Min(0f)]
    private float ropeOriginHeightOffset = 0.5f;

    private Rigidbody body;
    private TopDownPlayerController controller;
    private ImpactReceiver impactReceiver;
    private GrapplingHookProjectile activeHook;
    private LineRenderer rope;

    private Vector3 grappleDestination;
    private Vector3 pullStartPosition;
    private GrappleState state = GrappleState.Idle;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        controller = GetComponent<TopDownPlayerController>();
        impactReceiver = GetComponent<ImpactReceiver>();

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<TopDownPlayerAnimator>();
        }
        
        if (playerCapsule == null)
        {
            playerCapsule = GetComponent<CapsuleCollider>();
        }

        rope = BuildRopeRenderer();
    }

    private void OnEnable()
    {
        SubscribeToImpactReceiver();
    }

    private void Update()
    {
        if (readInputDirectly)
        {
            ReadDirectInput();
        }

        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        if (state != GrappleState.Pulling)
        {
            return;
        }

        if (cancelOnImpact && controller != null && controller.IsMovementLockedByImpact)
        {
            CancelGrappleFromImpact();
            return;
        }

        Vector3 currentPosition = body.position;

        if (HasExceededSafePullDistance(currentPosition))
        {
            if (logGrappleSafetyWarnings)
            {
                Debug.LogWarning("TopDownGrapple: pull cancelled because player exceeded maxPullDistance. Check hook targets, enemy roots and blocking layers.", this);
            }

            CancelGrapple(clearPlayerVelocity: true);
            return;
        }

        Vector3 toDestination = grappleDestination - currentPosition;
        toDestination.y = 0f;

        float effectiveArrivalThreshold = Mathf.Clamp(arrivalThreshold, 0.05f, 0.6f);
        if (toDestination.magnitude <= effectiveArrivalThreshold)
        {
            FinishGrapple();
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            grappleDestination,
            pullSpeed * Time.fixedDeltaTime);
        nextPosition.y = currentPosition.y;

        if (IsPullStepBlocked(currentPosition, nextPosition))
        {
            CancelGrapple(clearPlayerVelocity: true);
            return;
        }

        if (controller != null)
        {
            controller.MoveControlledTo(nextPosition, preserveCurrentY: true);
        }
        else
        {
            body.MovePosition(nextPosition);
        }
    }

    private void ReadDirectInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (!keyboard.qKey.wasPressedThisFrame)
        {
            return;
        }

        if (state != GrappleState.Idle || activeHook != null)
        {
            if (pressQAgainCancels)
            {
                CancelGrapple(clearPlayerVelocity: true);
            }

            return;
        }

        FireHook();
    }

    private void FireHook()
    {
        if (state != GrappleState.Idle || activeHook != null)
        {
            return;
        }

        if (controller != null && controller.IsMovementLockedByImpact)
        {
            return;
        }

        if (hookPrefab == null)
        {
            Debug.LogWarning("TopDownGrapple: hookPrefab not assigned.", this);
            return;
        }

        if (!TryGetPlanarAimDirection(out Vector3 direction))
        {
            return;
        }

        playerAnimator?.PlayHookLaunch();
        Vector3 spawnPosition = transform.position
                              + direction * hookSpawnDistance
                              + Vector3.up * ropeOriginHeightOffset;

        GameObject hookObject = Instantiate(
            hookPrefab,
            spawnPosition,
            Quaternion.LookRotation(direction, Vector3.up));

        activeHook = hookObject.GetComponent<GrapplingHookProjectile>();
        if (activeHook == null)
        {
            Debug.LogWarning("TopDownGrapple: hookPrefab does not have GrapplingHookProjectile.", hookObject);
            Destroy(hookObject);
            return;
        }

        activeHook.OnHookLandedDetailed += HandleHookLanded;
        activeHook.OnHookMissed += HandleHookMissed;

        state = GrappleState.HookFlying;
        activeHook.Launch(direction, transform);
    }

    public void Execute()
    {
        FireHook();
        Debug.Log("¡Lanzando el gancho! (Ejecutando Grapple)");
    }

    private bool TryGetPlanarAimDirection(out Vector3 direction)
    {
        direction = controller != null ? controller.AimDirection : transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();
        return true;
    }

    private void HandleHookLanded(Vector3 hitPoint, Collider hitCollider)
    {
        if (activeHook == null || state != GrappleState.HookFlying)
        {
            return;
        }

        if (!TryCalculateDestination(hitPoint, hitCollider, out Vector3 destination))
        {
            CancelGrapple(clearPlayerVelocity: true);
            return;
        }

        if (!TryFindSafeDestination(destination, out Vector3 safeDestination))
        {
            if (logGrappleSafetyWarnings)
            {
                Debug.LogWarning("TopDownGrapple: hook landed but no safe pull destination/path was found. Check wallStopDistance, grappleBlockingLayers and collider setup.", this);
            }

            CancelGrapple(clearPlayerVelocity: true);
            return;
        }

        pullStartPosition = body.position;
        grappleDestination = safeDestination;
        state = GrappleState.Pulling;

        if (controller != null)
        {
            controller.BeginExternalMovement(clearVelocity: true);
        }
        else
        {
            ClearHorizontalVelocity();
        }
    }

    private void HandleHookMissed()
    {
        CancelGrapple(clearPlayerVelocity: true);
    }

    private bool TryCalculateDestination(Vector3 hitPoint, Collider hitCollider, out Vector3 destination)
    {
        if (hitCollider != null && IsInLayerMask(hitCollider.gameObject.layer, enemyLayers))
        {
            return TryCalculateEnemyDestination(hitCollider, out destination);
        }

        return TryCalculateStaticDestination(hitPoint, out destination);
    }

    private bool TryCalculateStaticDestination(Vector3 hitPoint, out Vector3 destination)
    {
        Vector3 currentPosition = body.position;
        Vector3 fromPlayerToHit = hitPoint - currentPosition;
        fromPlayerToHit.y = 0f;

        if (fromPlayerToHit.sqrMagnitude <= 0.001f)
        {
            destination = currentPosition;
            return false;
        }

        fromPlayerToHit.Normalize();

        destination = hitPoint - fromPlayerToHit * wallStopDistance;
        destination.y = currentPosition.y;
        return true;
    }

    private bool TryCalculateEnemyDestination(Collider hitCollider, out Vector3 destination)
    {
        if (hitCollider == null)
        {
            destination = body.position;
            return false;
        }

        Vector3 currentPosition = body.position;

        // Para enemigos usamos el centro del collider impactado, no necesariamente el root.
        // Algunos prefabs tienen pivots/roots lejos del cuerpo visual y eso puede generar destinos absurdamente lejanos.
        Vector3 targetPosition = hitCollider.bounds.center;
        targetPosition.y = currentPosition.y;

        Vector3 fromEnemyToPlayer = currentPosition - targetPosition;
        fromEnemyToPlayer.y = 0f;

        if (fromEnemyToPlayer.sqrMagnitude <= 0.001f)
        {
            fromEnemyToPlayer = -transform.forward;
            fromEnemyToPlayer.y = 0f;
        }

        if (fromEnemyToPlayer.sqrMagnitude <= 0.001f)
        {
            destination = currentPosition;
            return false;
        }

        fromEnemyToPlayer.Normalize();

        destination = targetPosition + fromEnemyToPlayer * enemyStopDistance;
        destination.y = currentPosition.y;
        return true;
    }

    private static Transform ResolveEnemyRoot(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        if (hitCollider.attachedRigidbody != null)
        {
            return hitCollider.attachedRigidbody.transform;
        }

        ImpactReceiver impactReceiverOnTarget = hitCollider.GetComponentInParent<ImpactReceiver>();
        if (impactReceiverOnTarget != null)
        {
            return impactReceiverOnTarget.transform;
        }

        return hitCollider.transform;
    }

    private bool TryFindSafeDestination(Vector3 desiredDestination, out Vector3 safeDestination)
    {
        Vector3 currentPosition = body.position;
        desiredDestination.y = currentPosition.y;

        Vector3 fromCurrentToDestination = desiredDestination - currentPosition;
        fromCurrentToDestination.y = 0f;

        float desiredDistance = fromCurrentToDestination.magnitude;
        if (desiredDistance <= Mathf.Clamp(arrivalThreshold, 0.05f, 0.6f))
        {
            safeDestination = currentPosition;
            return false;
        }

        if (desiredDistance > maxPullDistance)
        {
            safeDestination = currentPosition;
            return false;
        }

        Vector3 pullDirection = fromCurrentToDestination / desiredDistance;

        if (IsDestinationUsable(currentPosition, desiredDestination))
        {
            safeDestination = desiredDestination;
            return true;
        }

        int attempts = Mathf.Max(0, destinationBackoffAttempts);
        float step = Mathf.Max(0.01f, destinationBackoffStep);

        for (int i = 1; i <= attempts; i++)
        {
            Vector3 candidate = desiredDestination - pullDirection * (step * i);
            candidate.y = currentPosition.y;

            Vector3 candidateDelta = candidate - currentPosition;
            candidateDelta.y = 0f;

            if (candidateDelta.magnitude <= Mathf.Clamp(arrivalThreshold, 0.05f, 0.6f))
            {
                break;
            }

            if (IsDestinationUsable(currentPosition, candidate))
            {
                safeDestination = candidate;
                return true;
            }
        }

        safeDestination = currentPosition;
        return false;
    }

    private bool IsDestinationUsable(Vector3 currentPosition, Vector3 destination)
    {
        if (IsDestinationBlocked(destination))
        {
            return false;
        }

        if (requireClearInitialPullPath && IsPullStepBlocked(currentPosition, destination))
        {
            return false;
        }

        return true;
    }

    private bool HasExceededSafePullDistance(Vector3 currentPosition)
    {
        if (maxPullDistance <= 0f)
        {
            return false;
        }

        Vector3 fromStart = currentPosition - pullStartPosition;
        fromStart.y = 0f;

        float safetyMargin = Mathf.Max(0.5f, pullSpeed * Time.fixedDeltaTime * 2f);
        float allowedDistance = maxPullDistance + safetyMargin;
        return fromStart.sqrMagnitude > allowedDistance * allowedDistance;
    }

    private bool IsDestinationBlocked(Vector3 destination)
    {
        if (grappleBlockingLayers.value == 0 || playerCapsule == null)
        {
            return false;
        }

        GetCapsuleWorldDataAt(playerCapsule, destination, transform.rotation, out Vector3 pointA, out Vector3 pointB, out float radius);

        return Physics.CheckCapsule(
            pointA,
            pointB,
            radius + grappleSkinWidth,
            grappleBlockingLayers,
            grappleBlockTriggerInteraction);
    }

    private bool IsPullStepBlocked(Vector3 currentPosition, Vector3 nextPosition)
    {
        if (grappleBlockingLayers.value == 0 || playerCapsule == null)
        {
            return false;
        }

        Vector3 delta = nextPosition - currentPosition;
        delta.y = 0f;

        float distance = delta.magnitude;
        if (distance <= 0.0001f)
        {
            return false;
        }

        Vector3 direction = delta / distance;
        GetCapsuleWorldDataAt(playerCapsule, currentPosition, transform.rotation, out Vector3 pointA, out Vector3 pointB, out float radius);

        return Physics.CapsuleCast(
            pointA,
            pointB,
            radius,
            direction,
            distance + grappleSkinWidth,
            grappleBlockingLayers,
            grappleBlockTriggerInteraction);
    }

    private void FinishGrapple()
    {
        CancelGrapple(clearPlayerVelocity: true);
    }

    private void CancelGrappleFromImpact()
    {
        CancelGrapple(clearPlayerVelocity: false);
    }

    private void CancelGrapple(bool clearPlayerVelocity = true)
    {
        bool wasPulling = state == GrappleState.Pulling;
        state = GrappleState.Idle;

        if (controller != null && wasPulling)
        {
            controller.EndExternalMovement(clearPlayerVelocity);
        }
        else if (clearPlayerVelocity)
        {
            ClearHorizontalVelocity();
        }

        if (activeHook != null)
        {
            activeHook.OnHookLandedDetailed -= HandleHookLanded;
            activeHook.OnHookMissed -= HandleHookMissed;
            activeHook.CancelSilently();
            activeHook = null;
        }

        if (rope != null)
        {
            rope.enabled = false;
        }
    }

    private void ClearHorizontalVelocity()
    {
        if (body == null || body.isKinematic)
        {
            return;
        }

        Vector3 velocity = body.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        body.linearVelocity = velocity;
    }

    private void UpdateRopeVisual()
    {
        if (rope == null)
        {
            return;
        }

        if (activeHook == null)
        {
            rope.enabled = false;
            return;
        }

        rope.enabled = true;
        rope.SetPosition(0, transform.position + Vector3.up * ropeOriginHeightOffset);
        rope.SetPosition(1, activeHook.transform.position);
    }

    private LineRenderer BuildRopeRenderer()
    {
        GameObject ropeObject = new GameObject("GrappleRope");
        ropeObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = ropeObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth * 0.5f;
        lineRenderer.startColor = ropeColor;
        lineRenderer.endColor = ropeColor;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.numCapVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sharedMaterial = BuildRopeMaterial(ropeColor);
        lineRenderer.enabled = false;

        return lineRenderer;
    }

    private static Material BuildRopeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    private void SubscribeToImpactReceiver()
    {
        if (impactReceiver == null)
        {
            return;
        }

        impactReceiver.OnKnockbackStarted += HandleKnockbackStarted;
        impactReceiver.OnStunStarted += HandleStunStarted;
    }

    private void UnsubscribeFromImpactReceiver()
    {
        if (impactReceiver == null)
        {
            return;
        }

        impactReceiver.OnKnockbackStarted -= HandleKnockbackStarted;
        impactReceiver.OnStunStarted -= HandleStunStarted;
    }

    private void HandleKnockbackStarted(ImpactInfo impactInfo)
    {
        if (cancelOnImpact)
        {
            CancelGrappleFromImpact();
        }
    }

    private void HandleStunStarted(float duration)
    {
        if (cancelOnImpact)
        {
            CancelGrappleFromImpact();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromImpactReceiver();
        CancelGrapple(clearPlayerVelocity: false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromImpactReceiver();
    }

    private void OnDrawGizmos()
    {
        if (state == GrappleState.Pulling)
        {
            float effectiveArrivalThreshold = Mathf.Clamp(arrivalThreshold, 0.05f, 0.6f);

            Gizmos.color = new Color(0f, 1f, 0.4f, 0.2f);
            Gizmos.DrawSphere(grappleDestination, effectiveArrivalThreshold);

            Gizmos.color = new Color(0f, 1f, 0.4f, 1f);
            Gizmos.DrawWireSphere(grappleDestination, effectiveArrivalThreshold);

            float crossSize = effectiveArrivalThreshold * 0.3f;
            Gizmos.DrawLine(grappleDestination - Vector3.right * crossSize, grappleDestination + Vector3.right * crossSize);
            Gizmos.DrawLine(grappleDestination - Vector3.forward * crossSize, grappleDestination + Vector3.forward * crossSize);
        }
        else
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            Vector3 preview = transform.position
                            + forward * hookSpawnDistance
                            + Vector3.up * ropeOriginHeightOffset;

            Gizmos.color = new Color(1f, 0.9f, 0f, 0.15f);
            Gizmos.DrawSphere(preview, 0.15f);

            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            Gizmos.DrawWireSphere(preview, 0.15f);
        }
    }

    private static void GetCapsuleWorldDataAt(CapsuleCollider capsule, Vector3 rootPosition, Quaternion rootRotation, out Vector3 pointA, out Vector3 pointB, out float radius)
    {
        Transform capsuleTransform = capsule.transform;
        Vector3 center;

        if (capsuleTransform.parent == null)
        {
            center = rootPosition + rootRotation * capsule.center;
        }
        else
        {
            Vector3 localCenterFromRoot = capsule.transform.localPosition + capsule.center;
            center = rootPosition + rootRotation * localCenterFromRoot;
        }

        Vector3 axis = rootRotation * GetCapsuleAxisLocal(capsule);
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

    private static Vector3 GetCapsuleAxisLocal(CapsuleCollider capsule)
    {
        switch (capsule.direction)
        {
            case 0:
                return Vector3.right;
            case 2:
                return Vector3.forward;
            default:
                return Vector3.up;
        }
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
