using UnityEngine;

public class RespawnPointManager : MonoBehaviour
{
    RespawnPointBehaviour _lastActivated;

    [Header("Initial Respawn")]
    [Tooltip("Punto inicial de respawn del nivel. Si el jugador aún no activó ningún checkpoint, reaparece acá.")]
    [SerializeField] Transform initialRespawnPoint;

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
        //Debug.Log("<color=yellow>Respawn point activado</color>");

        if (showActivationNotification)
        {
            TMJNotifications.ShowSystem(activationMessage, NotificationPriority.Normal, "Respawn", "respawn_point_activated", this);
        }
    }

    public bool HasCurrentRespawnPoint => _lastActivated != null;
    public bool HasRespawnPosition => _lastActivated != null || initialRespawnPoint != null;

    public bool TryGetCurrentRespawnPosition(out Vector3 position)
    {
        if (_lastActivated != null)
        {
            position = _lastActivated.GetRespawnPosition();
            return true;
        }

        if (initialRespawnPoint != null)
        {
            position = initialRespawnPoint.position;
            return true;
        }

        position = default;
        return false;
    }

    public Vector3 GetCurrentRespawnPosition(Vector3 fallbackPosition)
    {
        if (TryGetCurrentRespawnPosition(out Vector3 position))
        {
            return position;
        }

        return fallbackPosition;
    }
}
