using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class CombatStateDebugTester : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Estado de combate del jugador que queremos probar.")]
    [SerializeField] private PlayerThreatTracker PlayerThreatTracker;

#if ENABLE_INPUT_SYSTEM
    [Header("Input System - Debug")]
    [Tooltip("Tecla para simular que un enemigo entra o sale de aggro. Solo es para pruebas.")]
    [SerializeField] private Key toggleCombatKey = Key.C;
#endif

    [Header("Debug")]
    [Tooltip("Muestra logs para entender qué está pasando durante la prueba.")]
    [SerializeField] private bool logActions = true;

    private bool fakeEnemyHasAggro;

    private void Reset()
    {
        PlayerThreatTracker = FindFirstObjectByType<PlayerThreatTracker>();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (WasKeyPressedThisFrame(toggleCombatKey))
        {
            ToggleFakeAggro();
        }
#endif
    }

    public void ToggleFakeAggro()
    {
        if (PlayerThreatTracker == null)
        {
            Debug.LogWarning($"Falta asignar {nameof(PlayerThreatTracker)}.", this);
            return;
        }

        fakeEnemyHasAggro = !fakeEnemyHasAggro;

        if (fakeEnemyHasAggro)
        {
            PlayerThreatTracker.RegisterAggroSource(this);

            if (logActions)
            {
                Debug.Log("Debug: enemigo falso registró aggro.", this);
            }
        }
        else
        {
            PlayerThreatTracker.UnregisterAggroSource(this);

            if (logActions)
            {
                Debug.Log("Debug: enemigo falso quitó aggro.", this);
            }
        }
    }

#if ENABLE_INPUT_SYSTEM
    private static bool WasKeyPressedThisFrame(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[key].wasPressedThisFrame;
    }
#endif
}
