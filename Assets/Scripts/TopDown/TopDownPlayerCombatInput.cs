using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(TopDownPlayerController))]
public sealed class TopDownPlayerCombatInput : MonoBehaviour
{
    [Header("Combat References")]
    [SerializeField]
    private TopDownWeapon equippedWeapon;

    [SerializeField]
    private TopDownPower powerQ;

    [SerializeField]
    private TopDownPower powerE;

    [SerializeField]
    private TopDownPlayerAnimator playerAnimator;

    [SerializeField]
    private TopDownEquipmentVisualManager equipmentVisuals;

    [Header("Impact Lock")]
    [SerializeField]
    private PlayerActionLock actionLock;

    [Tooltip("Limpia la secuencia de combo cuando el jugador queda bloqueado por impacto/stun. Evita que inputs viejos ejecuten combos al recuperar control.")]
    [SerializeField]
    private bool clearComboHistoryWhenCombatLocked = true;

    [Header("Combos")]
    [SerializeField, Min(0.25f)]
    private float comboHistoryWindow = 1.5f;

    [Tooltip("Combos disponibles para el jugador. El orden importa si dos combos empatan en largo y prioridad.")]
    [SerializeField]
    private List<TopDownCombatComboDefinition> comboDefinitions = new List<TopDownCombatComboDefinition>();

    [Tooltip("Agrega los combos default si faltan. Desactivalo si querés administrar la lista completamente a mano.")]
    [SerializeField]
    private bool autoEnsureDefaultCombos = true;

    private readonly List<TopDownCombatInputEvent> inputHistory = new List<TopDownCombatInputEvent>();
    private TopDownPlayerController playerController;
    private bool wasCombatLocked;
    private TopDownHealth playerHealth;

    private void Awake()
    {
        playerController = GetComponent<TopDownPlayerController>();
        playerHealth = GetComponent<TopDownHealth>();

        if (equippedWeapon == null)
        {
            equippedWeapon = GetComponent<TopDownWeapon>();
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<TopDownPlayerAnimator>();
        }

        if (equipmentVisuals == null)
        {
            equipmentVisuals = GetComponent<TopDownEquipmentVisualManager>();
        }

        if (actionLock == null)
        {
            actionLock = GetComponent<PlayerActionLock>();
        }
    }

    private void OnValidate()
    {
        if (comboDefinitions == null)
        {
            comboDefinitions = new List<TopDownCombatComboDefinition>();
        }

        NormalizeComboDefinitions();

        if (autoEnsureDefaultCombos)
        {
            EnsureDefaultCombos();
        }
        else if (comboDefinitions.Count == 0)
        {
            comboDefinitions.Add(TopDownCombatComboDefinition.CreateDefaultLeapSlash());
        }
    }

    private void NormalizeComboDefinitions()
    {
        for (int i = 0; i < comboDefinitions.Count; i++)
        {
            comboDefinitions[i]?.NormalizeForInspector();
        }
    }

    private void EnsureDefaultCombos()
    {
        IReadOnlyList<TopDownCombatComboDefinition> defaultCombos = TopDownCombatComboDefinition.CreateDefaultWeaponCombos();
        for (int i = 0; i < defaultCombos.Count; i++)
        {
            TopDownCombatComboDefinition defaultCombo = defaultCombos[i];
            if (defaultCombo == null || string.IsNullOrWhiteSpace(defaultCombo.ComboId) || HasCombo(defaultCombo.ComboId))
            {
                continue;
            }

            comboDefinitions.Add(defaultCombo);
        }
    }

    private bool HasCombo(string comboId)
    {
        for (int i = 0; i < comboDefinitions.Count; i++)
        {
            TopDownCombatComboDefinition combo = comboDefinitions[i];
            if (combo != null && combo.ComboId == comboId)
            {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (playerHealth != null && !playerHealth.IsAlive)
        {
            return; // No procesar inputs si el jugador está muerto
        }
        if (IsCombatLocked())
        {
            if (!wasCombatLocked && clearComboHistoryWhenCombatLocked)
            {
                inputHistory.Clear();
            }

            wasCombatLocked = true;
            return;
        }

        if (wasCombatLocked)
        {
            if (clearComboHistoryWhenCombatLocked)
            {
                inputHistory.Clear();
            }

            wasCombatLocked = false;
        }

        Vector3 facingDirection = playerController != null ? playerController.AimDirection : transform.forward;

        Mouse mouse = Mouse.current;
        if (mouse?.leftButton.wasPressedThisFrame == true)
        {
            HandleInput(TopDownCombatInputAction.LightAttack, facingDirection);
        }

        if (mouse?.rightButton.wasPressedThisFrame == true)
        {
            HandleInput(TopDownCombatInputAction.HeavyAttack, facingDirection);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard?.qKey.wasPressedThisFrame == true)
        {
            HandleInput(TopDownCombatInputAction.PowerQ, facingDirection);
        }

        if (keyboard?.eKey.wasPressedThisFrame == true)
        {
            HandleInput(TopDownCombatInputAction.PowerE, facingDirection);
        }
    }

    private void HandleInput(TopDownCombatInputAction action, Vector3 facingDirection)
    {
        if (IsCombatLocked())
        {
            return;
        }

        float currentTime = Time.time;
        inputHistory.Add(new TopDownCombatInputEvent(action, currentTime));
        TrimInputHistory(currentTime);

        if (TryResolveCombo(facingDirection, currentTime, out TopDownCombatComboDefinition comboDefinition) && comboDefinition.ConsumeMatchedInput)
        {
            return;
        }

        ExecuteBaseAction(action, facingDirection);
    }

    private bool TryResolveCombo(Vector3 facingDirection, float currentTime, out TopDownCombatComboDefinition comboDefinition)
    {
        if (!TopDownCombatComboMatcher.TryFindBestMatch(inputHistory, comboDefinitions, currentTime, out comboDefinition))
        {
            return false;
        }

        bool executed = false;
        switch (comboDefinition.Target)
        {
            case TopDownCombatComboTarget.Weapon:
                executed = equippedWeapon != null && equippedWeapon.TryComboAttack(comboDefinition, facingDirection);

                if (executed)
                {
                    equipmentVisuals?.SetWeaponInHand(comboDefinition.EffectiveWeaponUseSlot);
                    playerAnimator?.PlayCombo(comboDefinition.AnimationCue);
                }

                break;

            case TopDownCombatComboTarget.PowerQ:
                executed = powerQ != null && powerQ.TryComboActivate(comboDefinition, facingDirection);
                break;

            case TopDownCombatComboTarget.PowerE:
                executed = powerE != null && powerE.TryComboActivate(comboDefinition, facingDirection);
                break;
        }

        if (!executed)
        {
            comboDefinition = null;
        }

        return executed;
    }

    private void ExecuteBaseAction(TopDownCombatInputAction action, Vector3 facingDirection)
    {
        switch (action)
        {
            case TopDownCombatInputAction.LightAttack:
                if (equippedWeapon != null && equippedWeapon.TryLightAttack(facingDirection))
                {
                    equipmentVisuals?.SetWeaponInHand(TMJ_WeaponUseSlot.LightAttack);
                    playerAnimator?.PlayLightAttack();
                }

                break;

            case TopDownCombatInputAction.HeavyAttack:
                if (equippedWeapon != null && equippedWeapon.TryHeavyAttack(facingDirection))
                {
                    equipmentVisuals?.SetWeaponInHand(TMJ_WeaponUseSlot.HeavyAttack);
                    playerAnimator?.PlayHeavyAttack();
                }

                break;

            case TopDownCombatInputAction.PowerQ:
                powerQ?.TryActivate(facingDirection);
                break;

            case TopDownCombatInputAction.PowerE:
                powerE?.TryActivate(facingDirection);
                break;
        }
    }


    private bool IsCombatLocked()
    {
        return actionLock != null && actionLock.IsCombatLocked;
    }

    private void TrimInputHistory(float currentTime)
    {
        float window = Mathf.Max(comboHistoryWindow, 0.25f);
        for (int i = inputHistory.Count - 1; i >= 0; i--)
        {
            if (currentTime - inputHistory[i].Time <= window)
            {
                continue;
            }

            inputHistory.RemoveAt(i);
        }

        if (inputHistory.Count > 32)
        {
            inputHistory.RemoveRange(0, inputHistory.Count - 32);
        }
    }
}
