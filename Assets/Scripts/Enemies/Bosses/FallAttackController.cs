using System.Collections.Generic;
using UnityEngine;

public class FallAttackController : MonoBehaviour
{
    [Header("Movimiento")]
    public float fallSpeed = 10f;
    public float initialHeight = 10f;
    public float destroyDelay = 2f;

    [Header("Daño")]
    public float damage = 20f;
    public LayerMask targetLayers;
    public LayerMask impactLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Mesh fracturado")]
    public GameObject fracturedMesh;

    bool hasCollided;
    readonly HashSet<ITopDownDamageable> damagedTargets = new HashSet<ITopDownDamageable>();

    void Start()
    {
        transform.position = new Vector3(transform.position.x, initialHeight, transform.position.z);
    }

    void Update()
    {
        if (!hasCollided)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }
    }

    public void Configure(GameObject owner, LayerMask targets, LayerMask impacts)
    {
        damageOwner = owner;
        targetLayers = targets;
        impactLayers = impacts;
    }

    void OnTriggerEnter(Collider collision)
    {
        TMJ_DamageUtility.TryDamageCollider(
            collision,
            damage,
            transform.position,
            gameObject,
            targetLayers,
            damageOwner,
            damagedTargets);

        if (TMJ_DamageUtility.IsInLayerMask(collision.gameObject.layer, impactLayers))
        {
            Impact(collision.gameObject);
        }
    }

    void Impact(GameObject impactedObject)
    {
        if (hasCollided)
        {
            return;
        }

        hasCollided = true;

        if (impactedObject != null && impactedObject.CompareTag("Marker"))
        {
            impactedObject.SetActive(false);
        }

        if (fracturedMesh != null)
        {
            fracturedMesh.SetActive(true);
        }

        Destroy(gameObject, destroyDelay);
    }
}
