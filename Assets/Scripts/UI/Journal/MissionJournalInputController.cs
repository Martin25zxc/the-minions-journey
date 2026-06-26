using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Input simple para abrir/cerrar el Mission Journal con una tecla.
/// 
/// Scope de esta etapa:
/// - J abre/cierra el Journal.
/// - No maneja ESC completo.
/// - No reemplaza un futuro InputActions asset.
/// - No activa/desactiva UI directamente; delega en MissionJournalController.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionJournalInputController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Controller del Mission Journal.")]
    private MissionJournalController journalController;

    [Header("Tecla")]
    [SerializeField, Tooltip("Tecla usada para abrir/cerrar el Journal durante gameplay.")]
    private Key journalKey = Key.J;

    [Header("Debug")]
    [SerializeField] private bool logDebug;

    private void Reset()
    {
        journalController = FindFirstObjectByType<MissionJournalController>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || journalController == null)
        {
            return;
        }

        if (WasPressed(keyboard, journalKey))
        {
            journalController.RequestToggleFromGameplayKey();

            if (logDebug)
            {
                Debug.Log($"MissionJournalInputController: tecla {journalKey} presionada.", this);
            }
        }
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        if (key == Key.None)
        {
            return false;
        }

        KeyControl keyControl = keyboard[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }
}
