using UnityEngine;

public class ProjectileAttackController : MonoBehaviour
{
    public float projectileSpeed = 5f;
    public float damage    = 10f;

    void Update()
    {
        transform.Translate(transform.forward * projectileSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ProjectileAttackController] Colision con {other.gameObject.name}");
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }

        if (other.CompareTag("LimitWall"))
        {
            Destroy(gameObject);
        }
    }
}