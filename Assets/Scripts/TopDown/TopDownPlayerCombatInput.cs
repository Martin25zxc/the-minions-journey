using UnityEngine;
using UnityEngine.InputSystem;

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

    TopDownPlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<TopDownPlayerController>();

        if (equippedWeapon == null)
        {
            equippedWeapon = GetComponent<TopDownWeapon>();
        }
    }

    void Update()
    {
        Vector3 facingDirection = playerController != null ? playerController.AimDirection : transform.forward;

        Mouse mouse = Mouse.current;
        if (mouse?.leftButton.wasPressedThisFrame == true)
        {
            equippedWeapon?.TryLightAttack(facingDirection);
        }

        if (mouse?.rightButton.wasPressedThisFrame == true)
        {
            equippedWeapon?.TryHeavyAttack(facingDirection);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard?.qKey.wasPressedThisFrame == true)
        {
            powerQ?.TryActivate(facingDirection);
        }

        if (keyboard?.eKey.wasPressedThisFrame == true)
        {
            powerE?.TryActivate(facingDirection);
        }
    }
}