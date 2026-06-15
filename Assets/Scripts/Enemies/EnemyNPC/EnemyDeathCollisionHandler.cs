using UnityEngine;

/// <summary>
/// Politica simple para que un enemigo muerto deje de comportarse como entidad de combate.
///
/// No maneja vida, animacion ni loot. Solo responde a EnemyActor.Died y ajusta colision/layer.
/// Esto evita mezclar la muerte visual con la fisica del enemigo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyActor))]
public sealed class EnemyDeathCollisionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private EnemyActor actor;

    [Header("Disable Colliders On Death")]
    [Tooltip("Si esta activo, desactiva los colliders listados al morir. Recomendado para enemigos comunes si el cadaver no debe bloquear combate.")]
    [SerializeField]
    private bool disableCollidersOnDeath = true;

    [Tooltip("Si esta activo y la lista esta vacia, toma colliders del root y sus hijos en Awake.")]
    [SerializeField]
    private bool autoCollectCollidersIfEmpty = true;

    [Tooltip("Normalmente conviene dejarlo apagado para no tocar triggers visuales/debug o hitboxes que se apaguen por otra via.")]
    [SerializeField]
    private bool includeTriggerColliders;

    [SerializeField]
    private Collider[] collidersToDisable;

    [Header("Layer On Death - Optional")]
    [Tooltip("Opcional. Si preferis no apagar colliders, podes mover el objeto a una layer Corpse configurada para no colisionar con Player/Enemy/Projectiles.")]
    [SerializeField]
    private bool changeLayerOnDeath;

    [SerializeField]
    private string corpseLayerName = "Corpse";

    [SerializeField]
    private bool applyLayerToChildren = true;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (autoCollectCollidersIfEmpty && (collidersToDisable == null || collidersToDisable.Length == 0))
        {
            Collider[] allColliders = GetComponentsInChildren<Collider>(true);
            if (includeTriggerColliders)
            {
                collidersToDisable = allColliders;
            }
            else
            {
                int count = 0;
                for (int i = 0; i < allColliders.Length; i++)
                {
                    if (!allColliders[i].isTrigger)
                    {
                        count++;
                    }
                }

                collidersToDisable = new Collider[count];
                int writeIndex = 0;
                for (int i = 0; i < allColliders.Length; i++)
                {
                    if (!allColliders[i].isTrigger)
                    {
                        collidersToDisable[writeIndex] = allColliders[i];
                        writeIndex++;
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        if (actor == null)
        {
            actor = GetComponent<EnemyActor>();
        }

        if (actor != null)
        {
            actor.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (actor != null)
        {
            actor.Died -= HandleDied;
        }
    }

    private void HandleDied(EnemyActor deadActor)
    {
        if (disableCollidersOnDeath)
        {
            DisableConfiguredColliders();
        }

        if (changeLayerOnDeath)
        {
            ApplyCorpseLayer();
        }
    }

    private void DisableConfiguredColliders()
    {
        if (collidersToDisable == null)
        {
            return;
        }

        for (int i = 0; i < collidersToDisable.Length; i++)
        {
            Collider targetCollider = collidersToDisable[i];
            if (targetCollider == null)
            {
                continue;
            }

            if (!includeTriggerColliders && targetCollider.isTrigger)
            {
                continue;
            }

            targetCollider.enabled = false;
        }
    }

    private void ApplyCorpseLayer()
    {
        int corpseLayer = LayerMask.NameToLayer(corpseLayerName);
        if (corpseLayer < 0)
        {
            Debug.LogWarning($"[{nameof(EnemyDeathCollisionHandler)}] Layer '{corpseLayerName}' does not exist. Corpse layer was not applied.", this);
            return;
        }

        if (applyLayerToChildren)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.layer = corpseLayer;
            }

            return;
        }

        gameObject.layer = corpseLayer;
    }
}
