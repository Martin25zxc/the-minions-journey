using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownEquipmentVisualManager : MonoBehaviour
{
    [Header("Sockets")]
    [SerializeField] Transform rightHandSocket;
    [SerializeField] Transform backSocket;

    [Header("Weapon Loadout")]
    [SerializeField] TMJ_WeaponLoadout weaponLoadout;

    [Header("Behaviour")]
    [SerializeField] TopDownWeaponVisualPose startingPose = TopDownWeaponVisualPose.LightInHand;

    WeaponVisualInstance lightWeapon;
    WeaponVisualInstance heavyWeapon;
    TopDownWeaponVisualPose currentPose;

    public WeaponData CurrentLightWeapon => weaponLoadout != null ? weaponLoadout.CurrentLightAttackWeapon : lightWeapon.weaponData;
    public WeaponData CurrentHeavyWeapon => weaponLoadout != null ? weaponLoadout.CurrentHeavyAttackWeapon : heavyWeapon.weaponData;

    bool UsesSharedWeapon => CurrentLightWeapon != null && CurrentLightWeapon == CurrentHeavyWeapon;

    void Awake()
    {
        if (weaponLoadout == null)
        {
            weaponLoadout = GetComponent<TMJ_WeaponLoadout>();
        }
    }

    void OnEnable()
    {
        if (weaponLoadout != null)
        {
            weaponLoadout.OnLoadoutChanged += RefreshVisuals;
        }
    }

    void OnDisable()
    {
        if (weaponLoadout != null)
        {
            weaponLoadout.OnLoadoutChanged -= RefreshVisuals;
        }
    }

    void Start()
    {
        currentPose = startingPose;
        RefreshVisuals();
    }

    public void SetWeaponInHand(TMJ_WeaponUseSlot useSlot)
    {
        SetVisualPose(useSlot == TMJ_WeaponUseSlot.LightAttack
            ? TopDownWeaponVisualPose.LightInHand
            : TopDownWeaponVisualPose.HeavyInHand);
    }

    public void SheatheWeapons()
    {
        SetVisualPose(TopDownWeaponVisualPose.BothOnBack);
    }

    public void SetVisualPose(TopDownWeaponVisualPose pose)
    {
        currentPose = pose;

        switch (pose)
        {
            case TopDownWeaponVisualPose.LightInHand:
                AttachToHand(ref lightWeapon);
                AttachToBack(ref heavyWeapon);
                break;

            case TopDownWeaponVisualPose.HeavyInHand:
                if (UsesSharedWeapon)
                {
                    AttachToHand(ref lightWeapon);
                }
                else
                {
                    AttachToHand(ref heavyWeapon);
                    AttachToBack(ref lightWeapon);
                }
                break;

            case TopDownWeaponVisualPose.BothOnBack:
                AttachToBack(ref lightWeapon);
                AttachToBack(ref heavyWeapon);
                break;
        }
    }

    public void EquipLightWeapon(WeaponData weaponData)
    {
        if (weaponLoadout != null)
        {
            weaponLoadout.EquipWeapon(TMJ_WeaponUseSlot.LightAttack, weaponData);
            return;
        }

        DestroyVisual(ref lightWeapon);
        lightWeapon = CreateVisual(weaponData);
        SetVisualPose(currentPose);
    }

    public void EquipHeavyWeapon(WeaponData weaponData)
    {
        if (weaponLoadout != null)
        {
            weaponLoadout.EquipWeapon(TMJ_WeaponUseSlot.HeavyAttack, weaponData);
            return;
        }

        DestroyVisual(ref heavyWeapon);
        heavyWeapon = CreateVisual(weaponData);
        SetVisualPose(currentPose);
    }

    void RefreshVisuals()
    {
        DestroyVisual(ref lightWeapon);
        DestroyVisual(ref heavyWeapon);

        WeaponData lightWeaponData = weaponLoadout.CurrentLightAttackWeapon;
        WeaponData heavyWeaponData = weaponLoadout.CurrentHeavyAttackWeapon;

        lightWeapon = CreateVisual(lightWeaponData);

        if (heavyWeaponData != null && heavyWeaponData != lightWeaponData)
        {
            heavyWeapon = CreateVisual(heavyWeaponData);
        }
        else
        {
            heavyWeapon = default;
        }

        SetVisualPose(currentPose);
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
