using UnityEngine;

public class MeshBreak : MonoBehaviour
{
    [Header("Fuerza de explosión")]
    [SerializeField] float forceMagnitude = 5f;
    [SerializeField] float torque = 3f;
    [SerializeField] float upwardBias = 0.3f;

    [Header("Tiempo de vida")]
    [SerializeField] float lifetime = 2f;

    Rigidbody[] pieces;

    void Awake()
    {
        pieces = GetComponentsInChildren<Rigidbody>();
    }

    void OnEnable()
    {
        foreach (var rb in pieces)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;

            // Dirección desde el centro hacia cada pieza
            Vector3 direction = (rb.transform.position - transform.position).normalized;

            // Si la pieza está exactamente en el centro, fallback random
            if (direction.sqrMagnitude < 0.001f)
                direction = Random.onUnitSphere;

            // Bias hacia arriba opcional para que no vayan al piso
            direction += Vector3.up * upwardBias;
            direction.Normalize();

            rb.AddForce(direction * forceMagnitude, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
        }
        Destroy(gameObject, lifetime);
    }
}