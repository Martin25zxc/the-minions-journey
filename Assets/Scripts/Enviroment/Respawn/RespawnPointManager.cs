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
    }

    void OnDisable()
    {
        PlayerIdentity.OnPlayerHealthReady -= HandlePlayerReady;
        RespawnPointBehaviour.OnRespawnPointTriggered -= HandleRespawnPointActivated;

        if (_playerHealth != null)
            _playerHealth.OnDied -= HandlePlayerDied;
    }

    void HandlePlayerReady(TopDownHealth health)
    {
        if (_playerHealth != null)
            _playerHealth.OnDied -= HandlePlayerDied; // por si el player se reemplaza

        _playerHealth = health;
        _playerHealth.OnDied += HandlePlayerDied;
    }

    void HandleRespawnPointActivated(RespawnPointBehaviour point) // suscripto a OnRespawnPointTriggered ahora
    {
        if (point == _lastActivated) return;

        _lastActivated?.SetAsCurrent(false);
        point.SetAsCurrent(true);   
        _lastActivated = point;
    }

    void HandlePlayerDied()
    {
        if (_lastActivated == null || _playerHealth == null) return;
        _respawnCoroutine = StartCoroutine(RespawnPlayerDelayed());

    }

    IEnumerator RespawnPlayerDelayed()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    void RespawnPlayer()
    {
        _playerHealth.transform.position = _lastActivated.GetRespawnPosition();

    }
}