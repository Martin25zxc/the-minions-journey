using System;
using UnityEngine;

[RequireComponent(typeof(TopDownHealth))]
public class PlayerIdentity : MonoBehaviour
{
    public static event Action<TopDownHealth> OnPlayerHealthReady;

    public TopDownHealth Health { get; private set; }

    void Awake()
    {
        Health = GetComponent<TopDownHealth>();
        OnPlayerHealthReady?.Invoke(Health);
    }
}