using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillInput : MonoBehaviour
{
    public VisualSkillInventoryManager skillManager;

    [Header("Bindings")]
    public Key slot1Key = Key.Q;
    public Key slot2Key = Key.E;
    public Key toggleInventoryKey = Key.I;

    void Update()
    {
        if (Keyboard.current == null) return; // seguridad por si no hay teclado (ej. en editor con Input System desactivado)
        if (Keyboard.current[slot1Key].wasPressedThisFrame) skillManager.TryUseSkill(1);
        if (Keyboard.current[slot2Key].wasPressedThisFrame) skillManager.TryUseSkill(2);
        if (Keyboard.current[toggleInventoryKey].wasPressedThisFrame) skillManager.ToggleInventory();
        
    }

}