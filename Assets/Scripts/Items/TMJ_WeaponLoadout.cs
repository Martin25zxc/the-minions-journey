using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TMJ_WeaponLoadout : MonoBehaviour
{
    [Header("Attack Use Assignments")]
    [Tooltip("Weapon used by the left-click / light attack. This is a tactical use, not a physical slot restriction.")]
    [SerializeField] WeaponData startingLightAttackWeapon;

    [Tooltip("Weapon used by the right-click / heavy attack. This is a tactical use, not a physical slot restriction.")]
    [SerializeField] WeaponData startingHeavyAttackWeapon;

    WeaponData lightAttackWeapon;
    WeaponData heavyAttackWeapon;

    public event Action OnLoadoutChanged;
    public event Action<TMJ_WeaponUseSlot, WeaponData> OnWeaponAssigned;

    public WeaponData CurrentLightAttackWeapon => lightAttackWeapon;
    public WeaponData CurrentHeavyAttackWeapon => heavyAttackWeapon;

    void Awake()
    {
        lightAttackWeapon = startingLightAttackWeapon;
        heavyAttackWeapon = startingHeavyAttackWeapon;
    }

    public WeaponData GetWeapon(TMJ_WeaponUseSlot useSlot)
    {
        return useSlot == TMJ_WeaponUseSlot.LightAttack
            ? lightAttackWeapon
            : heavyAttackWeapon;
    }

    public void EquipWeapon(TMJ_WeaponUseSlot useSlot, WeaponData weaponData)
    {
        switch (useSlot)
        {
            case TMJ_WeaponUseSlot.LightAttack:
                lightAttackWeapon = weaponData;
                break;
            case TMJ_WeaponUseSlot.HeavyAttack:
                heavyAttackWeapon = weaponData;
                break;
        }

        OnWeaponAssigned?.Invoke(useSlot, weaponData);
        OnLoadoutChanged?.Invoke();
    }

    public void SwapAttackAssignments()
    {
        (lightAttackWeapon, heavyAttackWeapon) = (heavyAttackWeapon, lightAttackWeapon);
        OnLoadoutChanged?.Invoke();
    }

    public float RollDamageBonus(TMJ_WeaponUseSlot useSlot)
    {
        WeaponData weaponData = GetWeapon(useSlot);
        return weaponData != null ? weaponData.RollDamageBonus() : 0f;
    }
}
