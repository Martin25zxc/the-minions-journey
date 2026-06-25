using UnityEngine;

public class RespawnPointManager : MonoBehaviour
{
    RespawnPointBehaviour _lastActivated;

    [Header("Notifications")]
    [Tooltip("Muestra una notificación cuando el jugador activa un nuevo punto de respawn.")]
    [SerializeField] bool showActivationNotification = true;

    [Tooltip("Texto que se muestra al activar un nuevo punto de respawn.")]
    [SerializeField] string activationMessage = "Punto de respawn activado.";

    void OnEnable()
    {
        RespawnPointBehaviour.OnRespawnPointTriggered += HandleRespawnPointActivated;
    }

    void OnDisable()
    {
        RespawnPointBehaviour.OnRespawnPointTriggered -= HandleRespawnPointActivated;
    }

    void HandleRespawnPointActivated(RespawnPointBehaviour point)
    {
        if (point == _lastActivated) return;
        _lastActivated?.SetAsCurrent(false);
        point.SetAsCurrent(true);
        _lastActivated = point;

        if (showActivationNotification)
        {
            TMJNotifications.ShowSystem(activationMessage, NotificationPriority.Normal, "Respawn", "respawn_point_activated", this);
        }
    }

    public bool HasCurrentRespawnPoint => _lastActivated != null;

    public Vector3 GetCurrentRespawnPosition(Vector3 fallbackPosition)
    {
        if (_lastActivated == null)
        {
            return fallbackPosition;
        }

        return _lastActivated.GetRespawnPosition();
    }
}
