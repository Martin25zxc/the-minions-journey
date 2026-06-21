using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameWorldEventDebugTester : MonoBehaviour
{
    [Header("IDs de prueba")]
    [Tooltip("ID del artefacto que simula el Hook. Es un ejemplo de prueba, no definitivo.")]
    [SerializeField] private string hookArtifactId = "hook_artifact";

    [Tooltip("ID del item recolectable. Es un ejemplo de prueba, no definitivo.")]
    [SerializeField] private string appleItemId = "apple";

    [Tooltip("ID del área alcanzada. Es un ejemplo de prueba, no definitivo.")]
    [SerializeField] private string valleyHeightsAreaId = "valley_heights";

    [Tooltip("ID del enemigo jefe. Es un ejemplo de prueba, no definitivo.")]
    [SerializeField] private string valleyLeaderEnemyId = "valley_leader";

    [Tooltip("ID de un objeto interactuable de prueba.")]
    [SerializeField] private string objectId = "ancient_gate";

    [Tooltip("ID de un actor/NPC de prueba.")]
    [SerializeField] private string actorId = "survivor_01";

    [Header("Cantidades")]
    [Tooltip("Cantidad que se usa para probar ItemCollected. Para manzanas suele ser 1.")]
    [SerializeField, Min(1)] private int itemAmount = 1;

    [Header("Teclas de prueba - Input System")]
    [Tooltip("Simula ArtifactAcquired con el ID del Hook.")]
    [SerializeField] private Key hookArtifactKey = Key.H;

    [Tooltip("Simula ItemCollected con el ID de manzana.")]
    [SerializeField] private Key appleCollectedKey = Key.A;

    [Tooltip("Simula AreaReached con el ID del alto del valle.")]
    [SerializeField] private Key areaReachedKey = Key.V;

    [Tooltip("Simula EnemyDefeated con el ID del líder del valle.")]
    [SerializeField] private Key enemyDefeatedKey = Key.B;

    [Tooltip("Simula ObjectInteracted con el ID del objeto.")]
    [SerializeField] private Key objectInteractedKey = Key.O;

    [Tooltip("Simula ActorTalkedTo con el ID del actor.")]
    [SerializeField] private Key actorTalkedToKey = Key.T;

    [Header("Debug")]
    [Tooltip("Muestra en consola cada evento generado.")]
    [SerializeField] private bool logEvents = true;

    [Tooltip("Texto visible en Inspector con el último evento generado.")]
    [SerializeField, TextArea(2, 4)] private string lastEventDebug = "Todavía no se generó ningún evento.";

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (WasPressed(keyboard, hookArtifactKey))
        {
            LogEvent(GameWorldEvent.ArtifactAcquired(hookArtifactId, name));
        }

        if (WasPressed(keyboard, appleCollectedKey))
        {
            LogEvent(GameWorldEvent.ItemCollected(appleItemId, itemAmount, name));
        }

        if (WasPressed(keyboard, areaReachedKey))
        {
            LogEvent(GameWorldEvent.AreaReached(valleyHeightsAreaId, name));
        }

        if (WasPressed(keyboard, enemyDefeatedKey))
        {
            LogEvent(GameWorldEvent.EnemyDefeated(valleyLeaderEnemyId, name));
        }

        if (WasPressed(keyboard, objectInteractedKey))
        {
            LogEvent(GameWorldEvent.ObjectInteracted(objectId, name));
        }

        if (WasPressed(keyboard, actorTalkedToKey))
        {
            LogEvent(GameWorldEvent.ActorTalkedTo(actorId, name));
        }
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        if (key == Key.None)
        {
            return false;
        }

        return keyboard[key].wasPressedThisFrame;
    }

    private void LogEvent(GameWorldEvent worldEvent)
    {
        lastEventDebug = worldEvent.ToString();

        if (!worldEvent.IsValid)
        {
            Debug.LogWarning($"GameWorldEvent inválido: {worldEvent}", this);
            return;
        }

        if (logEvents)
        {
            Debug.Log($"GameWorldEvent generado -> {worldEvent}", this);
        }
    }
}
