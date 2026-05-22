using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [Header("Equipment Slots")]
    [SerializeField] private Transform rightHandSlot;

    private WeaponData currentWeaponData;
    private GameObject currentWeaponVisual;

    public WeaponData CurrentWeaponData => currentWeaponData;
    public GameObject CurrentWeaponVisual => currentWeaponVisual;

    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            Debug.LogWarning("Tried to equip a null weapon.");
            return;
        }

        UnequipWeapon();

        currentWeaponData = weaponData;

        if (rightHandSlot == null)
        {
            Debug.LogWarning("RightHandSlot is not assigned.");
            return;
        }

        GameObject prefabToEquip = weaponData.EquippedModelPrefab;

        if (prefabToEquip == null)
        {
            Debug.LogWarning($"{weaponData.ItemName} has no equipped model prefab.");
            return;
        }

        currentWeaponVisual = Instantiate(prefabToEquip, rightHandSlot);
        currentWeaponVisual.transform.localPosition = Vector3.zero;
        currentWeaponVisual.transform.localRotation = Quaternion.identity;
        currentWeaponVisual.transform.localScale = Vector3.one;

        DisablePhysicsOnEquippedWeapon(currentWeaponVisual);

        Debug.Log($"Equipped weapon: {weaponData.ItemName}");
    }

    public void UnequipWeapon()
    {
        if (currentWeaponVisual != null)
        {
            Destroy(currentWeaponVisual);
        }

        currentWeaponVisual = null;
        currentWeaponData = null;
    }

    private void DisablePhysicsOnEquippedWeapon(GameObject weaponObject)
    {
        Rigidbody[] rigidbodies = weaponObject.GetComponentsInChildren<Rigidbody>();
        Collider[] colliders = weaponObject.GetComponentsInChildren<Collider>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
        }

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }
}