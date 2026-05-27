using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(TopDownPlayerController))]
public sealed class TopDownPlayerCombatInput : MonoBehaviour
{
    [SerializeField]
    TopDownWeapon equippedWeapon;

    [SerializeField]
    TopDownPower powerQ;

    [SerializeField]
    TopDownPower powerE;

    [SerializeField, Min(0.25f)]
    float comboHistoryWindow = 1.5f;

    [SerializeField]
    List<TopDownCombatComboDefinition> comboDefinitions = new List<TopDownCombatComboDefinition>();

    readonly List<TopDownCombatInputEvent> inputHistory = new List<TopDownCombatInputEvent>();

    TopDownPlayerController playerController;

    [SerializeField]
    TopDownPlayerAnimator playerAnimator;

    void Awake()
    {
        playerController = GetComponent<TopDownPlayerController>();

        if (equippedWeapon == null)
        {
            equippedWeapon = GetComponent<TopDownWeapon>();
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<TopDownPlayerAnimator>();
        }
    }

    void OnValidate()
    {
        if (comboDefinitions == null)
        {
            comboDefinitions = new List<TopDownCombatComboDefinition>();
        }

        if (comboDefinitions.Count == 0)
        {
            comboDefinitions.Add(TopDownCombatComboDefinition.CreateDefaultTripleSlash());
        }
    }

    void Update()
    {
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

    void HandleInput(TopDownCombatInputAction action, Vector3 facingDirection)
    {
        float currentTime = Time.time;
        inputHistory.Add(new TopDownCombatInputEvent(action, currentTime));
        TrimInputHistory(currentTime);

        if (TryResolveCombo(facingDirection, currentTime, out TopDownCombatComboDefinition comboDefinition) && comboDefinition.ConsumeMatchedInput)
        {
            return;
        }

        ExecuteBaseAction(action, facingDirection);
    }

    bool TryResolveCombo(Vector3 facingDirection, float currentTime, out TopDownCombatComboDefinition comboDefinition)
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
                    playerAnimator?.PlayComboAttack();
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

    void ExecuteBaseAction(TopDownCombatInputAction action, Vector3 facingDirection)
    {
        switch (action)
        {
            case TopDownCombatInputAction.LightAttack:
                equippedWeapon?.TryLightAttack(facingDirection);
                playerAnimator?.PlayLightAttack();
                break;
            case TopDownCombatInputAction.HeavyAttack:
                equippedWeapon?.TryHeavyAttack(facingDirection);
                playerAnimator?.PlayHeavyAttack();
                break;
            case TopDownCombatInputAction.PowerQ:
                powerQ?.TryActivate(facingDirection);
                break;
            case TopDownCombatInputAction.PowerE:
                powerE?.TryActivate(facingDirection);
                break;
        }
    }

    void TrimInputHistory(float currentTime)
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