using UnityEngine;

public class BulletAttackController : MonoBehaviour
{
    [Header("Movimiento")]
    public float projectileSpeed = 5f;
    public bool canBounce = false;
    public int maxBounces = 1;

    [Header("Daño")]
    public float damage = 10f;
    public LayerMask targetLayers;
    public LayerMask wallLayers;

    [SerializeField]
    GameObject damageOwner;

    Vector3 moveDirection;
    int bouncesLeft;

    void Start()
    {
        if (moveDirection.sqrMagnitude < 0.001f)
        {
            moveDirection = transform.forward;
        }

        bouncesLeft = maxBounces;
    }

    void Update()
    {
        transform.Translate(moveDirection * projectileSpeed * Time.deltaTime, Space.World);
    }

    public void Configure(GameObject owner, LayerMask targets, LayerMask walls, float speed)
    {
        damageOwner = owner;
        targetLayers = targets;
        wallLayers = walls;
        projectileSpeed = speed;
        moveDirection = transform.forward;
        bouncesLeft = maxBounces;
    }

    void OnTriggerEnter(Collider other)
    {
        if (TMJ_DamageUtility.TryDamageCollider(other, damage, transform.position, gameObject, targetLayers, damageOwner))
        {
            Destroy(gameObject);
            return;
        }

        if (TMJ_DamageUtility.IsInLayerMask(other.gameObject.layer, wallLayers))
        {
            HandleWallCollision();
        }
    }

    void HandleWallCollision()
    {
        if (canBounce && bouncesLeft > 0)
        {
            moveDirection = -moveDirection;
            bouncesLeft--;
            return;
        }

        Destroy(gameObject);
    }
}
