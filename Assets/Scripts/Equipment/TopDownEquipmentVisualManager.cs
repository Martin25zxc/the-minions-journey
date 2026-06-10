using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownEquipmentVisualManager : MonoBehaviour
{
    [Header("Sockets")]
    [SerializeField] Transform rightHandSocket;
    [SerializeField] Transform backSocket;

    [Header("Starting Equipment")]
    [SerializeField] WeaponData startingLightWeapon;
    [SerializeField] WeaponData startingHeavyWeapon;

    [Header("Behaviour")]
    [SerializeField] TopDownWeaponVisualPose startingPose = TopDownWeaponVisualPose.LightInHand;

    WeaponVisualInstance lightWeapon;
    WeaponVisualInstance heavyWeapon;

    public WeaponData CurrentLightWeapon => lightWeapon.weaponData;
    public WeaponData CurrentHeavyWeapon => heavyWeapon.weaponData;

    void Start()
    {
        EquipLightWeapon(startingLightWeapon);
        EquipHeavyWeapon(startingHeavyWeapon);
        SetVisualPose(startingPose);
    }

    public void EquipLightWeapon(WeaponData weaponData)
    {
        DestroyVisual(ref lightWeapon);
        lightWeapon = CreateVisual(weaponData);
        SetVisualPose(startingPose);
    }

    public void EquipHeavyWeapon(WeaponData weaponData)
    {
        DestroyVisual(ref heavyWeapon);
        heavyWeapon = CreateVisual(weaponData);
        SetVisualPose(startingPose);
    }

    public void SetWeaponInHand(TopDownWeaponEquipSlot slot)
    {
        if (slot == TopDownWeaponEquipSlot.Light)
        {
            SetVisualPose(TopDownWeaponVisualPose.LightInHand);
        }
        else
        {
            SetVisualPose(TopDownWeaponVisualPose.HeavyInHand);
        }
    }

    public void SheatheWeapons()
    {
        SetVisualPose(TopDownWeaponVisualPose.BothOnBack);
    }

    public void SetVisualPose(TopDownWeaponVisualPose pose)
    {
        switch (pose)
        {
            case TopDownWeaponVisualPose.LightInHand:
                AttachToHand(ref lightWeapon);
                AttachToBack(ref heavyWeapon);
                break;

            case TopDownWeaponVisualPose.HeavyInHand:
                AttachToHand(ref heavyWeapon);
                AttachToBack(ref lightWeapon);
                break;

            case TopDownWeaponVisualPose.BothOnBack:
                AttachToBack(ref lightWeapon);
                AttachToBack(ref heavyWeapon);
                break;
        }
    }

    WeaponVisualInstance CreateVisual(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            return default;
        }

        GameObject prefab = weaponData.EquippedModelPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"{weaponData.ItemName} has no equipped visual prefab.");
            return default;
        }

        GameObject instance = Instantiate(prefab);
        instance.name = $"Visual_{weaponData.ItemName}";

        DisablePhysics(instance);

        return new WeaponVisualInstance
        {
            weaponData = weaponData,
            instance = instance
        };
    }

    void AttachToHand(ref WeaponVisualInstance visual)
    {
        if (!visual.HasInstance || rightHandSocket == null)
        {
            return;
        }

        visual.instance.transform.SetParent(rightHandSocket, false);
        visual.instance.transform.localPosition = visual.weaponData.HandLocalPosition;
        visual.instance.transform.localRotation = visual.weaponData.HandLocalRotation;
        visual.instance.transform.localScale = visual.weaponData.HandLocalScale;
        visual.instance.SetActive(true);
    }

    void AttachToBack(ref WeaponVisualInstance visual)
    {
        if (!visual.HasInstance || backSocket == null)
        {
            return;
        }

        visual.instance.transform.SetParent(backSocket, false);
        visual.instance.transform.localPosition = visual.weaponData.BackLocalPosition;
        visual.instance.transform.localRotation = visual.weaponData.BackLocalRotation;
        visual.instance.transform.localScale = visual.weaponData.BackLocalScale;
        visual.instance.SetActive(true);
    }

    void DestroyVisual(ref WeaponVisualInstance visual)
    {
        if (visual.instance != null)
        {
            Destroy(visual.instance);
        }

        visual = default;
    }

    static void DisablePhysics(GameObject weaponObject)
    {
        Rigidbody[] rigidbodies = weaponObject.GetComponentsInChildren<Rigidbody>(true);
        Collider[] colliders = weaponObject.GetComponentsInChildren<Collider>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    struct WeaponVisualInstance
    {
        public WeaponData weaponData;
        public GameObject instance;

        public bool HasInstance => weaponData != null && instance != null;
    }
}