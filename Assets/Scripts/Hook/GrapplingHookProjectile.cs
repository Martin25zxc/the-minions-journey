using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class GrapplingHookProjectile : MonoBehaviour
{
    public event Action<Vector3> OnHookLanded;
    public event Action OnHookMissed;

    [SerializeField, Min(1f)] float speed;
    [SerializeField, Min(1f)] float maxRange;

    Rigidbody body;
    Vector3 spawnPosition;
    bool landed;

    void Awake()
    {

    }

    public void Launch(Vector3 direction)
    {
        spawnPosition = transform.position;
        body.linearVelocity = direction.normalized * speed;
    }

    void Update()
    {
        if (!landed && Vector3.Distance(transform.position, spawnPosition) > maxRange)
        {
            landed = true;
            OnHookMissed?.Invoke();
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (landed) return;
        if (!collision.gameObject.CompareTag("wall")) return;

        landed = true;
        body.isKinematic = true;

        Vector3 hitPoint = collision.contacts[0].point;
        OnHookLanded?.Invoke(hitPoint);
    }
}
