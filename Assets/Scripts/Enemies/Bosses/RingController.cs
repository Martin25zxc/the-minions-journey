using UnityEngine;

/// <summary>
/// Vive en el prefab del anillo.
/// Se expande desde 0 hasta maxScale en expandDuration segundos y luego se destruye.
/// El collider trigger escala junto con el mesh automáticamente.
/// </summary>
public class RingController : MonoBehaviour
{
    public float damage        = 20f;
    public float maxScale      = 8f;
    public float expandDuration = 1f;

    private float   elapsed = 0f;
    private bool    hit     = false;  // para dañar solo una vez por anillo

    private void Start()
    {
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Asegura que el anillo esté plano en el suelo
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / expandDuration);
        transform.localScale = Vector3.one * Mathf.Lerp(0f, maxScale, t);

        if (t >= 1f) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hit) return;
        if (other.CompareTag("Player"))
        {
            hit = true;
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }
}