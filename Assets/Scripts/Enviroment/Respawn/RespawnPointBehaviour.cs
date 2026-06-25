using UnityEngine;
using System;

public class RespawnPointBehaviour : MonoBehaviour
{
    public static event Action<RespawnPointBehaviour> OnRespawnPointTriggered; // renombrado, más claro

    [Header("Respawn Point Settings")]
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject inactiveVisual;
    [SerializeField] private Transform respawnPosition;

    public bool isCurrentRespawnPoint { get; private set; }

    private void Awake()
    {
        if (activeVisual == null || inactiveVisual == null || respawnPosition == null)
        {
            Debug.LogError("RespawnPointBehaviour: One or more serialized fields are not assigned in the inspector.", this);
            enabled = false;
            return;
        }

        inactiveVisual.SetActive(true);
        activeVisual.SetActive(false);
    }

    // Ya NO invoca el evento. Solo aplica estado visual.
    public void SetAsCurrent(bool isCurrent)
    {
        if (isCurrentRespawnPoint == isCurrent) return; // guarda extra contra llamadas redundantes
        isCurrentRespawnPoint = isCurrent;
        activeVisual.SetActive(isCurrent);
        inactiveVisual.SetActive(!isCurrent);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCurrentRespawnPoint) return;
        if (other.CompareTag("Player"))
        {
            OnRespawnPointTriggered?.Invoke(this); // solo avisa, no decide nada
        }
    }

    public Vector3 GetRespawnPosition()
    {
        return respawnPosition.position;
    }
}