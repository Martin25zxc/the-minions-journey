using UnityEngine;

public class FallAttackController : MonoBehaviour
{
    public float fallSpeed = 10f; // Speed at which the object falls
    public float damage = 20f; // Damage dealt to the player on impact
    public float initialHeight = 10f; // Initial height from which the object starts falling
    public float destroyDelay = 2f; // Time after which the object is destroyed post-impact

    private bool hasCollided = false; // Flag to check if the object has already collided
    
    [Header("Mesh fracturado")]
    public GameObject fracturedMesh;

    void Start()
    {
        // Set the initial position of the object to be at the specified height
        transform.position = new Vector3(transform.position.x, initialHeight, transform.position.z);
        
    }

   
    void Update()
    {
        // Move the object downwards at a constant speed
        if (!hasCollided)
        {   
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }
        
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Assuming the player has a method to take damage
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
        if (collision.gameObject.CompareTag("Marker") || collision.gameObject.CompareTag("Ground"))
        {
            hasCollided = true; // Set the flag to true to stop further movement
            if (collision.gameObject.CompareTag("Marker"))
            {
                // If the object collides with a marker, it will be destroyed immediately
                collision.gameObject.SetActive(false); // Deactivate the marker
            }
           Destroy(gameObject);
           fracturedMesh.SetActive(true); // Activate the fractured mesh
        }

        //Destroy(gameObject, destroyDelay);
    }
}
