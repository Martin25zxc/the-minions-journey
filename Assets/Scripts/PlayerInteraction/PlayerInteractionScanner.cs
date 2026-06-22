using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerInteractionScanner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Controller que inició la interacción. Se usa para completar InteractionContext y centralizar datos del player.")]
    [SerializeField]
    private PlayerInteractionController controller;

    [Tooltip("Origen de la búsqueda. Si queda vacío, se usa el transform del player.")]
    [SerializeField]
    private Transform interactionOrigin;

    [Header("Scan")]
    [SerializeField, Min(0.1f)]
    private float interactionRadius = 1.8f;

    [Tooltip("Layers consideradas para interacción contextual. Recomendado: solo la layer Interactable. No usar Everything.")]
    [SerializeField]
    private LayerMask interactableLayers;

    [Tooltip("Permite decidir si se detectan colliders trigger, colliders sólidos o ambos. Collide suele ser lo más flexible para interactuables.")]
    [SerializeField]
    private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [SerializeField, Min(1)]
    private int maxCandidates = 16;

    [Tooltip("Evita que colliders del propio Player sean considerados si por error están en una layer incluida.")]
    [SerializeField]
    private bool ignoreOwnColliders = true;

    [Header("Debug")]
    [SerializeField]
    private bool logConfigurationWarnings = true;

    [SerializeField]
    private bool logWhenBufferIsFull = true;

    [SerializeField]
    private bool drawGizmos = true;

    private Collider[] candidateBuffer;
    private readonly HashSet<IPlayerInteractable> processedInteractables = new();
    private PlayerThreatTracker threatTracker;
    private bool warnedEmptyLayerMask;

    public float InteractionRadius => interactionRadius;
    public LayerMask InteractableLayers => interactableLayers;
    public Transform InteractionOrigin => interactionOrigin != null ? interactionOrigin : transform;

    private void Reset()
    {
        controller = GetComponent<PlayerInteractionController>();
        threatTracker = GetComponent<PlayerThreatTracker>();
        interactionOrigin = transform;

        int interactableMask = LayerMask.GetMask("Interactable");
        if (interactableMask != 0)
        {
            interactableLayers = interactableMask;
        }
    }

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<PlayerInteractionController>();
        }

        if (threatTracker == null)
        {
            threatTracker = GetComponent<PlayerThreatTracker>();
        }

        if (interactionOrigin == null)
        {
            interactionOrigin = transform;
        }

        EnsureBuffer();
        ValidateConfiguration();
    }

    private void OnValidate()
    {
        if (interactionRadius < 0.1f)
        {
            interactionRadius = 0.1f;
        }

        if (maxCandidates < 1)
        {
            maxCandidates = 1;
        }
    }

    public InteractionContext BuildContext()
    {
        PlayerThreatTracker contextThreatTracker = controller != null
            ? controller.ThreatTracker
            : threatTracker;

        return new InteractionContext(
            gameObject,
            transform,
            InteractionOrigin,
            controller,
            contextThreatTracker);
    }

    public bool TryFindBestInteractable(out IPlayerInteractable bestInteractable, out InteractionContext context)
    {
        context = BuildContext();
        return TryFindBestInteractable(context, out bestInteractable);
    }

    public bool TryFindBestInteractable(InteractionContext context, out IPlayerInteractable bestInteractable)
    {
        EnsureBuffer();
        bestInteractable = null;

        if (interactableLayers.value == 0)
        {
            WarnEmptyLayerMaskOnce();
            return false;
        }

        Vector3 origin = context.OriginPosition;
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            interactionRadius,
            candidateBuffer,
            interactableLayers,
            triggerInteraction);

        if (logWhenBufferIsFull && hitCount >= candidateBuffer.Length)
        {
            Debug.LogWarning(
                $"{name} filled the interaction candidate buffer ({candidateBuffer.Length}). " +
                "If this happens often, increase Max Candidates.",
                this);
        }

        processedInteractables.Clear();

        int bestPriority = int.MinValue;
        float bestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider candidateCollider = candidateBuffer[i];
            if (candidateCollider == null)
            {
                continue;
            }

            if (ignoreOwnColliders && IsOwnCollider(candidateCollider))
            {
                continue;
            }

            IPlayerInteractable interactable = candidateCollider.GetComponentInParent<IPlayerInteractable>();
            if (interactable == null)
            {
                continue;
            }

            if (!processedInteractables.Add(interactable))
            {
                continue;
            }

            if (!IsFocusable(interactable, context))
            {
                continue;
            }

            int priority = GetPriority(interactable);
            float distanceSqr = GetDistanceSqr(origin, candidateCollider, interactable);

            if (priority > bestPriority ||
                priority == bestPriority && distanceSqr < bestDistanceSqr)
            {
                bestInteractable = interactable;
                bestPriority = priority;
                bestDistanceSqr = distanceSqr;
            }
        }

        return bestInteractable != null;
    }

    public bool IsFocusable(IPlayerInteractable interactable, InteractionContext context)
    {
        if (interactable == null)
        {
            return false;
        }

        if (interactable is Component component)
        {
            if (component == null || !component.gameObject.activeInHierarchy)
            {
                return false;
            }

            float maxDistance = interactionRadius + 0.15f;
            float distanceSqr = (component.transform.position - context.OriginPosition).sqrMagnitude;
            if (distanceSqr > maxDistance * maxDistance)
            {
                return false;
            }
        }

        return interactable.CanInteract(context);
    }

    public int GetPriority(IPlayerInteractable interactable)
    {
        return interactable is IPlayerInteractablePriority priorityProvider
            ? priorityProvider.InteractionPriority
            : 0;
    }

    private bool IsOwnCollider(Collider candidateCollider)
    {
        Transform candidateTransform = candidateCollider.transform;
        return candidateTransform == transform || candidateTransform.IsChildOf(transform);
    }

    private void EnsureBuffer()
    {
        if (candidateBuffer == null || candidateBuffer.Length != maxCandidates)
        {
            candidateBuffer = new Collider[maxCandidates];
        }
    }

    private void ValidateConfiguration()
    {
        if (!logConfigurationWarnings)
        {
            return;
        }

        if (interactableLayers.value == 0)
        {
            Debug.LogWarning(
                $"{name} has an empty Interactable Layers mask. " +
                "Assign the Interactable layer so contextual interaction can find candidates.",
                this);
        }
    }

    private void WarnEmptyLayerMaskOnce()
    {
        if (!logConfigurationWarnings || warnedEmptyLayerMask)
        {
            return;
        }

        warnedEmptyLayerMask = true;
        Debug.LogWarning(
            $"{name} tried to scan interactables, but Interactable Layers is empty.",
            this);
    }

    private static float GetDistanceSqr(
        Vector3 origin,
        Collider candidateCollider,
        IPlayerInteractable interactable)
    {
        if (interactable is Component interactableComponent && interactableComponent != null)
        {
            return (interactableComponent.transform.position - origin).sqrMagnitude;
        }

        Vector3 closestPoint = candidateCollider.ClosestPoint(origin);
        return (closestPoint - origin).sqrMagnitude;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Transform origin = interactionOrigin != null ? interactionOrigin : transform;
        if (origin == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(origin.position, interactionRadius);
    }
}
