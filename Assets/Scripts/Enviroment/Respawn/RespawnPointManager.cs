using UnityEngine;
using System.Collections;

public class RespawnPointManager : MonoBehaviour
{
    TopDownHealth _playerHealth;
    RespawnPointBehaviour _lastActivated;
    Coroutine _respawnCoroutine;

    [SerializeField] float respawnDelay = 2f; // Delay before respawning the player

    void OnEnable()
    {
        PlayerIdentity.OnPlayerHealthReady += HandlePlayerReady;
        RespawnPointBehaviour.OnRespawnPointTriggered += HandleRespawnPointActivated;

        // "Late join": si el Player ya hizo Awake antes de que nosotros nos
        // suscribiéramos (orden Awake -> OnEnable de Unity), lo atrapamos acá.
        if (PlayerIdentity.CurrentPlayerHealth != null)
            HandlePlayerReady(PlayerIdentity.CurrentPlayerHealth);
    }

    void OnDisable()
    {
        PlayerIdentity.OnPlayerHealthReady -= HandlePlayerReady;
        RespawnPointBehaviour.OnRespawnPointTriggered -= HandleRespawnPointActivated;

        if (_playerHealth != null)
            _playerHealth.OnDied -= HandlePlayerDied;

        if (_respawnCoroutine != null)
            StopCoroutine(_respawnCoroutine);
    }

    void HandlePlayerReady(TopDownHealth health)
    {
        if (_playerHealth == health) return; // ya conectado, evita doble suscripción

        if (_playerHealth != null)
            _playerHealth.OnDied -= HandlePlayerDied; // por si el player se reemplaza

        _playerHealth = health;
        _playerHealth.OnDied += HandlePlayerDied;
        Debug.Log("<color=yellow>Player attached to respawn system</color>");
    }

    void HandleRespawnPointActivated(RespawnPointBehaviour point)
    {
        if (point == _lastActivated) return;
        _lastActivated?.SetAsCurrent(false);
        point.SetAsCurrent(true);
        _lastActivated = point;
        Debug.Log("<color=yellow>New respawn point</color>");
    }

    void HandlePlayerDied()
    {
        if (_lastActivated == null || _playerHealth == null) return;

        if (_respawnCoroutine != null)
            StopCoroutine(_respawnCoroutine); // evita coroutines duplicadas si muere rápido

        _respawnCoroutine = StartCoroutine(RespawnPlayerDelayed());
    }

    IEnumerator RespawnPlayerDelayed()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    void RespawnPlayer()
    {
        Vector3 spawnPos = _lastActivated.GetRespawnPosition();
        Rigidbody rb = _playerHealth.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos;
        }
        else
        {
            _playerHealth.transform.position = spawnPos;
        }

        
    }
}