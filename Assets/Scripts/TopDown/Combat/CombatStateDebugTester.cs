using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class CombatStateDebugTester : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Estado de combate del jugador que queremos probar.")]
    [SerializeField] private PlayerCombatState playerCombatState;

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
        playerCombatState = FindFirstObjectByType<PlayerCombatState>();
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
        if (playerCombatState == null)
        {
            Debug.LogWarning($"Falta asignar {nameof(PlayerCombatState)}.", this);
            return;
        }

        fakeEnemyHasAggro = !fakeEnemyHasAggro;

        if (fakeEnemyHasAggro)
        {
            playerCombatState.RegisterAggroSource(this);

            if (logActions)
            {
                Debug.Log("Debug: enemigo falso registró aggro.", this);
            }
        }
        else
        {
            playerCombatState.UnregisterAggroSource(this);

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
