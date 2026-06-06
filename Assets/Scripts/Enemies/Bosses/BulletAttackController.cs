using UnityEngine;

public class BulletAttackController : MonoBehaviour
{
    public float projectileSpeed = 5f;
    public float damage = 10f;
    public bool canBounce = false;
    public int maxBounces = 1;

    private Vector3 moveDirection;
    private int bouncesLeft;
   

    private void Start()
    {
        moveDirection = transform.forward;
        bouncesLeft = maxBounces;
    }

    private void Update()
    {
        transform.Translate(moveDirection * projectileSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("LimitWall"))
        {
            if (canBounce && bouncesLeft > 0)
            {
                moveDirection = -moveDirection;
                bouncesLeft--;

                //Destroy(gameObject, 4f); // Destruir después de un tiempo para evitar bounces infinitos
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
