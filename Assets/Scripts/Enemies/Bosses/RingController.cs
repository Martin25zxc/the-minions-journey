using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vive en el prefab del anillo.
/// Se expande desde 0 hasta maxScale en expandDuration segundos y luego se destruye.
/// El collider trigger escala junto con el mesh automáticamente.
/// Cada objetivo válido recibe daño una sola vez por anillo.
/// </summary>
public class RingController : MonoBehaviour
{
    [Header("Daño")]
    public float damage = 20f;
    public LayerMask targetLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Expansión")]
    public float maxScale = 8f;
    public float expandDuration = 1f;

    float elapsed;
    readonly HashSet<ITopDownDamageable> damagedTargets = new HashSet<ITopDownDamageable>();

    void Start()
    {
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / expandDuration);
        transform.localScale = Vector3.one * Mathf.Lerp(0f, maxScale, t);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    public void Configure(GameObject owner, LayerMask targets)
    {
        damageOwner = owner;
        targetLayers = targets;
    }

    void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    void TryDamage(Collider other)
    {
        TMJ_DamageUtility.TryDamageCollider(
            other,
            damage,
            transform.position,
            gameObject,
            targetLayers,
            damageOwner,
            damagedTargets);
    }
}
