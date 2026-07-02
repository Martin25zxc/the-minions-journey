using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Volumen simple de encuentro para bosses/arenas sin BossArenaManager.
///
/// Responsabilidad:
/// - Saber si el Player sigue dentro del área del encounter.
/// - Evitar que un reset anti-cheese ocurra por una pérdida temporal de aggro/line of sight mientras el jugador sigue dentro.
/// - Opcionalmente disparar reset cuando el Player sale físicamente del área.
///
/// Uso recomendado:
/// - Crear un GameObject con BoxCollider/SphereCollider/CapsuleCollider en IsTrigger.
/// - Cubrir toda el área cerrada del boss.
/// - Asignar el Player Transform, o usar tag "Player" si no se asigna.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class EnemyEncounterCombatVolume : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private Transform playerTransform;

    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private bool useTagFallback = true;

    [Header("Debug")]
    [SerializeField]
    private bool logChanges;

    [SerializeField]
    private int debugPlayerColliderCount;

    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();
    private Collider triggerCollider;
    private bool wasPlayerInside;

    public bool IsPlayerInside => playerCollidersInside.Count > 0;

    public event Action PlayerEnteredVolume;
    public event Action PlayerExitedVolume;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[{nameof(EnemyEncounterCombatVolume)}] {name} requiere un Collider con Is Trigger activo.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        bool wasInside = IsPlayerInside;
        playerCollidersInside.Add(other);
        RefreshDebug();

        if (!wasInside && IsPlayerInside)
        {
            wasPlayerInside = true;
            if (logChanges)
            {
                Debug.Log($"[{nameof(EnemyEncounterCombatVolume)}] Player entered {name}.", this);
            }
            PlayerEnteredVolume?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        bool wasInside = IsPlayerInside;
        playerCollidersInside.Remove(other);
        RemoveDestroyedColliders();
        RefreshDebug();

        if (wasInside && !IsPlayerInside)
        {
            wasPlayerInside = false;
            if (logChanges)
            {
                Debug.Log($"[{nameof(EnemyEncounterCombatVolume)}] Player exited {name}.", this);
            }
            PlayerExitedVolume?.Invoke();
        }
    }

    private void Update()
    {
        if (playerCollidersInside.Count == 0)
        {
            return;
        }

        bool wasInside = IsPlayerInside;
        RemoveDestroyedColliders();
        RefreshDebug();

        if (wasInside && !IsPlayerInside && wasPlayerInside)
        {
            wasPlayerInside = false;
            PlayerExitedVolume?.Invoke();
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (playerTransform != null)
        {
            Transform otherTransform = other.transform;
            return otherTransform == playerTransform
                || otherTransform.root == playerTransform
                || otherTransform.IsChildOf(playerTransform);
        }

        if (!useTagFallback || string.IsNullOrWhiteSpace(playerTag))
        {
            return false;
        }

        return other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag);
    }

    private void RemoveDestroyedColliders()
    {
        playerCollidersInside.RemoveWhere(colliderRef => colliderRef == null);
    }

    private void RefreshDebug()
    {
        debugPlayerColliderCount = playerCollidersInside.Count;
    }

    private void OnDisable()
    {
        playerCollidersInside.Clear();
        RefreshDebug();
        wasPlayerInside = false;
    }

    private void OnValidate()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
}
