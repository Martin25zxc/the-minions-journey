using UnityEngine;

public class ProjectileAttackController : MonoBehaviour
{
    [Header("Movimiento")]
    public float projectileSpeed = 10f;

    [Header("Daño")]
    public float damage = 10f;
    public LayerMask targetLayers;
    public LayerMask blockingLayers;
    public bool destroyOnTargetHit = true;

    [SerializeField]
    GameObject damageOwner;

    void Update()
    {
        transform.Translate(transform.forward * projectileSpeed * Time.deltaTime, Space.World);
    }

    public void Configure(GameObject owner, LayerMask targets, LayerMask blockers, float speed)
    {
        damageOwner = owner;
        targetLayers = targets;
        blockingLayers = blockers;
        projectileSpeed = speed;
    }

    void OnTriggerEnter(Collider other)
    {
        if (TMJ_DamageUtility.TryDamageCollider(other, damage, transform.position, gameObject, targetLayers, damageOwner))
        {
            if (destroyOnTargetHit)
            {
                Destroy(gameObject);
            }

            return;
        }

        if (TMJ_DamageUtility.IsInLayerMask(other.gameObject.layer, blockingLayers))
        {
            Destroy(gameObject);
        }
    }
}
