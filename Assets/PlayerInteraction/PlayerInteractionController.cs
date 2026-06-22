using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerInteractionController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Tecla usada para la intención de interactuar. Por ahora seguimos el estilo actual del player usando Input System directo.")]
    [SerializeField]
    private Key interactKey = Key.F;

    [Header("Player Reaction")]
    [Tooltip("Reproduce la animación Interact del jugador al presionar la tecla, aunque no haya un objeto cerca.")]
    [SerializeField]
    private bool playReactionOnInput = true;

    [Tooltip("Guarda las armas en la espalda al presionar interact. Se recomienda combinarlo con 'solo fuera de combate'.")]
    [SerializeField]
    private bool sheatheWeaponsOnInput = true;

    [Tooltip("Si está activo, no mueve armas a la espalda mientras PlayerThreatTracker indique combate.")]
    [SerializeField]
    private bool sheatheWeaponsOnlyOutsideCombat = true;

    [Tooltip("Si está apagado y no hay PlayerThreatTracker asignado, no guarda armas. Es más seguro para no esconder armas por una referencia mal configurada.")]
    [SerializeField]
    private bool allowSheatheWhenCombatStateMissing;

    [SerializeField]
    private TopDownPlayerAnimator playerAnimator;

    [SerializeField]
    private TopDownEquipmentVisualManager equipmentVisuals;

    [SerializeField]
    private PlayerThreatTracker combatState;

    [Header("Contextual Interaction")]
    [Tooltip("Si está activo, además de la reacción visual intenta interactuar con el mejor IPlayerInteractable cercano.")]
    [SerializeField]
    private bool enableContextualInteraction = true;

    [Tooltip("Origen de la búsqueda. Si queda vacío, se usa el transform del player.")]
    [SerializeField]
    private Transform interactionOrigin;

    [SerializeField, Min(0.1f)]
    private float interactionRadius = 1.8f;

    [Tooltip("Layers consideradas para interacción contextual. Recomendado: solo la layer Interactable.")]
    [SerializeField]
    private LayerMask interactableLayers;

    [SerializeField]
    private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [SerializeField, Min(1)]
    private int maxCandidates = 16;

    [Tooltip("Evita que colliders del propio Player sean considerados como candidatos si por error están en una layer incluida.")]
    [SerializeField]
    private bool ignoreOwnColliders = true;

    [Header("Debug")]
    [SerializeField]
    private bool logInteractions;

    [SerializeField]
    private bool logConfigurationWarnings = true;

    [SerializeField]
    private bool logWhenBufferIsFull = true;

    [SerializeField]
    private bool drawGizmos = true;

    private Collider[] candidateBuffer;
    private readonly HashSet<IPlayerInteractable> processedInteractables = new();
    private bool warnedMissingCombatState;
    private bool warnedEmptyLayerMask;

    public float InteractionRadius => interactionRadius;
    public Transform InteractionOrigin => interactionOrigin != null ? interactionOrigin : transform;

    private void Reset()
    {
        playerAnimator = GetComponent<TopDownPlayerAnimator>();
        equipmentVisuals = GetComponent<TopDownEquipmentVisualManager>();
        combatState = GetComponent<PlayerThreatTracker>();
        interactionOrigin = transform;

        int interactableMask = LayerMask.GetMask("Interactable");
        if (interactableMask != 0)
        {
            interactableLayers = interactableMask;
        }
    }

    private void Awake()
    {
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<TopDownPlayerAnimator>();
        }

        if (equipmentVisuals == null)
        {
            equipmentVisuals = GetComponent<TopDownEquipmentVisualManager>();
        }

        if (combatState == null)
        {
            combatState = GetComponent<PlayerThreatTracker>();
        }

        if (interactionOrigin == null)
        {
            interactionOrigin = transform;
        }

        ValidateConfiguration();
        EnsureBuffer();
    }

    private void OnValidate()
    {
        if (maxCandidates < 1)
        {
            maxCandidates = 1;
        }

        if (interactionRadius < 0.1f)
        {
            interactionRadius = 0.1f;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || interactKey == Key.None)
        {
            return;
        }

        if (keyboard[interactKey].wasPressedThisFrame)
        {
            HandleInteractInput();
        }
    }

    public void HandleInteractInput()
    {
        PlayPlayerReaction();

        if (!enableContextualInteraction)
        {
            return;
        }

        if (TryFindBestInteractable(out IPlayerInteractable interactable, out InteractionContext context))
        {
            if (logInteractions)
            {
                Debug.Log($"{name} interacts with {GetDebugName(interactable)}.", this);
            }

            interactable.Interact(context);
        }
        else if (logInteractions)
        {
            Debug.Log($"{name} pressed interact, but no valid interactable was found.", this);
        }
    }

    public void PlayPlayerReaction()
    {
        if (sheatheWeaponsOnInput && CanSheatheWeaponsNow())
        {
            equipmentVisuals?.SheatheWeapons();
        }

        if (playReactionOnInput)
        {
            playerAnimator?.PlayInteract();
        }
    }

    public bool TryFindBestInteractable(out IPlayerInteractable bestInteractable, out InteractionContext context)
    {
        EnsureBuffer();

        bestInteractable = null;
        context = BuildContext();

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

            if (!interactable.CanInteract(context))
            {
                continue;
            }

            int priority = interactable is IPlayerInteractablePriority priorityProvider
                ? priorityProvider.InteractionPriority
                : 0;

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

    private InteractionContext BuildContext()
    {
        return new InteractionContext(
            gameObject,
            transform,
            InteractionOrigin,
            this);
    }

    private bool CanSheatheWeaponsNow()
    {
        if (equipmentVisuals == null)
        {
            return false;
        }

        if (!sheatheWeaponsOnlyOutsideCombat)
        {
            return true;
        }

        if (combatState == null)
        {
            WarnMissingCombatStateOnce();
            return allowSheatheWhenCombatStateMissing;
        }

        return !combatState.IsInCombat;
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

        if (playReactionOnInput && playerAnimator == null)
        {
            Debug.LogWarning($"{name} has Play Reaction enabled but no {nameof(TopDownPlayerAnimator)} assigned.", this);
        }

        if (sheatheWeaponsOnInput && equipmentVisuals == null)
        {
            Debug.LogWarning($"{name} has Sheathe Weapons enabled but no {nameof(TopDownEquipmentVisualManager)} assigned.", this);
        }

        if (sheatheWeaponsOnInput && sheatheWeaponsOnlyOutsideCombat && combatState == null && !allowSheatheWhenCombatStateMissing)
        {
            Debug.LogWarning(
                $"{name} is configured to sheathe only outside combat, but no {nameof(PlayerThreatTracker)} is assigned. " +
                "Weapons will not be sheathed until the reference is assigned or Allow Sheathe When Combat State Missing is enabled.",
                this);
        }

        if (enableContextualInteraction && interactableLayers.value == 0)
        {
            Debug.LogWarning(
                $"{name} has contextual interaction enabled but Interactable Layers is empty. " +
                "Assign the Interactable layer or disable contextual interaction.",
                this);
        }
    }

    private void WarnMissingCombatStateOnce()
    {
        if (!logConfigurationWarnings || warnedMissingCombatState)
        {
            return;
        }

        warnedMissingCombatState = true;
        Debug.LogWarning(
            $"{name} cannot know whether the player is in combat because {nameof(PlayerThreatTracker)} is missing. " +
            "Current configuration will not sheathe weapons.",
            this);
    }

    private void WarnEmptyLayerMaskOnce()
    {
        if (!logConfigurationWarnings || warnedEmptyLayerMask)
        {
            return;
        }

        warnedEmptyLayerMask = true;
        Debug.LogWarning(
            $"{name} tried contextual interaction, but Interactable Layers is empty.",
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

    private static string GetDebugName(IPlayerInteractable interactable)
    {
        return interactable is Component component && component != null
            ? component.name
            : interactable.ToString();
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
