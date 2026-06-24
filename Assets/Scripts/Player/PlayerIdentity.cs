using System;
using UnityEngine;

[RequireComponent(typeof(TopDownHealth))]
public class PlayerIdentity : MonoBehaviour
{
    public static event Action<TopDownHealth> OnPlayerHealthReady;
    public static TopDownHealth CurrentPlayerHealth { get; private set; }

    public TopDownHealth Health { get; private set; }

    void Awake()
    {
        Health = GetComponent<TopDownHealth>();
        CurrentPlayerHealth = Health;
        OnPlayerHealthReady?.Invoke(Health);
    }

    void OnDestroy()
    {
        if (CurrentPlayerHealth == Health)
            CurrentPlayerHealth = null;
    }
}