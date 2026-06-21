using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class WorldFlagDebugTester : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Registro de flags de mundo. Debería estar en _GameSystems.")]
    [SerializeField] private WorldFlagRegistry worldFlagRegistry;

    [Header("Teclas de prueba")]
    [Tooltip("Activa/desactiva hook_received.")]
    [SerializeField] private Key toggleHookReceivedKey = Key.H;

    [Tooltip("Activa/desactiva old_gate_opened.")]
    [SerializeField] private Key toggleOldGateOpenedKey = Key.O;

    [Tooltip("Activa/desactiva dying_ally_dead.")]
    [SerializeField] private Key toggleDyingAllyDeadKey = Key.Y;

    [Tooltip("Lista en consola todos los flags activos.")]
    [SerializeField] private Key listFlagsKey = Key.L;

    [Tooltip("Limpia todos los flags activos.")]
    [SerializeField] private Key clearFlagsKey = Key.K;

    [Header("Debug")]
    [Tooltip("Muestra logs de las acciones de este tester.")]
    [SerializeField] private bool logActions = true;

    private void Reset()
    {
        worldFlagRegistry = FindFirstObjectByType<WorldFlagRegistry>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (WasPressed(keyboard, toggleHookReceivedKey))
        {
            ToggleFlag(WorldFlagIds.HookReceived);
        }

        if (WasPressed(keyboard, toggleOldGateOpenedKey))
        {
            ToggleFlag(WorldFlagIds.OldGateOpened);
        }

        if (WasPressed(keyboard, toggleDyingAllyDeadKey))
        {
            ToggleFlag(WorldFlagIds.DyingAllyDead);
        }

        if (WasPressed(keyboard, listFlagsKey))
        {
            LogActiveFlags();
        }

        if (WasPressed(keyboard, clearFlagsKey))
        {
            ClearFlags();
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

    private void ToggleFlag(string flagId)
    {
        if (!HasRegistry())
        {
            return;
        }

        bool wasSet = worldFlagRegistry.HasFlag(flagId);
        bool changed = worldFlagRegistry.ToggleFlag(flagId);

        if (!logActions)
        {
            return;
        }

        if (!changed)
        {
            Debug.Log($"No se pudo cambiar el flag: {flagId}", this);
            return;
        }

        string result = wasSet ? "desactivado" : "activado";
        Debug.Log($"WorldFlag de prueba {result}: {flagId}", this);
    }

    private void LogActiveFlags()
    {
        if (!HasRegistry())
        {
            return;
        }

        var activeFlags = worldFlagRegistry.GetActiveFlagsSnapshot();

        if (activeFlags.Count == 0)
        {
            Debug.Log("No hay WorldFlags activos.", this);
            return;
        }

        StringBuilder builder = new();
        builder.AppendLine($"WorldFlags activos ({activeFlags.Count}):");

        for (int i = 0; i < activeFlags.Count; i++)
        {
            builder.Append("- ");
            builder.AppendLine(activeFlags[i]);
        }

        Debug.Log(builder.ToString(), this);
    }

    private void ClearFlags()
    {
        if (!HasRegistry())
        {
            return;
        }

        worldFlagRegistry.ClearAllFlags();

        if (logActions)
        {
            Debug.Log("WorldFlags limpiados desde tester.", this);
        }
    }

    private bool HasRegistry()
    {
        if (worldFlagRegistry != null)
        {
            return true;
        }

        Debug.LogWarning($"Falta asignar {nameof(WorldFlagRegistry)} en {nameof(WorldFlagDebugTester)}.", this);
        return false;
    }
}
