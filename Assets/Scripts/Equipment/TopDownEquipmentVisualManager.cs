using UnityEngine;

[DisallowMultipleComponent]
public sealed class TopDownEquipmentVisualManager : MonoBehaviour
{
    [Header("Sockets")]
    [SerializeField] private Transform rightHandSocket;
    [SerializeField] private Transform backSocket;

    [Header("Weapon Loadout")]
    [SerializeField] private TMJ_WeaponLoadout weaponLoadout;

    [Header("Behaviour")]
    [SerializeField] private TopDownWeaponVisualPose startingPose = TopDownWeaponVisualPose.LightInHand;

    private WeaponVisualInstance lightWeapon;
    private WeaponVisualInstance heavyWeapon;
    private TopDownWeaponVisualPose currentPose;

    public WeaponData CurrentLightWeapon =>
        weaponLoadout != null ? weaponLoadout.CurrentLightAttackWeapon : lightWeapon.weaponData;

    public WeaponData CurrentHeavyWeapon =>
        weaponLoadout != null ? weaponLoadout.CurrentHeavyAttackWeapon : heavyWeapon.weaponData;

    private void Awake()
    {
        if (weaponLoadout == null)
        {
            weaponLoadout = GetComponent<TMJ_WeaponLoadout>();
        }
    }

    private void OnEnable()
    {
        if (weaponLoadout != null)
        {
            weaponLoadout.OnLoadoutChanged += RefreshVisuals;
        }
    }

    private void OnDisable()
    {
        if (weaponLoadout != null)
        {
            weaponLoadout.OnLoadoutChanged -= RefreshVisuals;
        }
    }

    private void Start()
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
                AttachToBack(ref heavyWeapon, TMJ_WeaponUseSlot.HeavyAttack);
                break;

            case TopDownWeaponVisualPose.HeavyInHand:
                AttachToHand(ref heavyWeapon);
                AttachToBack(ref lightWeapon, TMJ_WeaponUseSlot.LightAttack);
                break;

            case TopDownWeaponVisualPose.BothOnBack:
                AttachToBack(ref lightWeapon, TMJ_WeaponUseSlot.LightAttack);
                AttachToBack(ref heavyWeapon, TMJ_WeaponUseSlot.HeavyAttack);
                break;

            default:
                Debug.LogWarning($"Unhandled weapon visual pose: {pose}");
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
        lightWeapon = CreateVisual(weaponData, TMJ_WeaponUseSlot.LightAttack);

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
        heavyWeapon = CreateVisual(weaponData, TMJ_WeaponUseSlot.HeavyAttack);

        SetVisualPose(currentPose);
    }

    private void RefreshVisuals()
    {
        DestroyVisual(ref lightWeapon);
        DestroyVisual(ref heavyWeapon);

        WeaponData lightWeaponData = weaponLoadout != null
            ? weaponLoadout.CurrentLightAttackWeapon
            : null;

        WeaponData heavyWeaponData = weaponLoadout != null
            ? weaponLoadout.CurrentHeavyAttackWeapon
            : null;

        lightWeapon = CreateVisual(lightWeaponData, TMJ_WeaponUseSlot.LightAttack);
        heavyWeapon = CreateVisual(heavyWeaponData, TMJ_WeaponUseSlot.HeavyAttack);

        SetVisualPose(currentPose);
    }

    private WeaponVisualInstance CreateVisual(
        WeaponData weaponData,
        TMJ_WeaponUseSlot useSlot)
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
        instance.name = $"Visual_{useSlot}_{weaponData.ItemName}";

        DisablePhysics(instance);

        return new WeaponVisualInstance
        {
            weaponData = weaponData,
            useSlot = useSlot,
            instance = instance
        };
    }

    private void AttachToHand(ref WeaponVisualInstance visual)
    {
        if (!visual.HasInstance)
        {
            return;
        }

        if (rightHandSocket == null)
        {
            Debug.LogWarning($"{name} has no right hand socket assigned.");
            visual.instance.SetActive(false);
            return;
        }

        Transform visualTransform = visual.instance.transform;

        visualTransform.SetParent(rightHandSocket, false);
        visualTransform.localPosition = visual.weaponData.HandLocalPosition;
        visualTransform.localRotation = visual.weaponData.HandLocalRotation;
        visualTransform.localScale = visual.weaponData.HandLocalScale;

        visual.instance.SetActive(true);
    }

    private void AttachToBack(
        ref WeaponVisualInstance visual,
        TMJ_WeaponUseSlot useSlot)
    {
        if (!visual.HasInstance)
        {
            return;
        }

        if (backSocket == null)
        {
            Debug.LogWarning($"{name} has no back socket assigned.");
            visual.instance.SetActive(false);
            return;
        }

        Transform visualTransform = visual.instance.transform;

        visualTransform.SetParent(backSocket, false);
        ApplyBackPose(visual.weaponData, visualTransform, useSlot);

        visual.instance.SetActive(true);
    }

    private static void ApplyBackPose(
        WeaponData weaponData,
        Transform visualTransform,
        TMJ_WeaponUseSlot useSlot)
    {
        switch (useSlot)
        {
            case TMJ_WeaponUseSlot.LightAttack:
                visualTransform.localPosition = weaponData.BackLeftLocalPosition;
                visualTransform.localRotation = weaponData.BackLeftLocalRotation;
                visualTransform.localScale = weaponData.BackLeftLocalScale;
                break;

            case TMJ_WeaponUseSlot.HeavyAttack:
                visualTransform.localPosition = weaponData.BackRightLocalPosition;
                visualTransform.localRotation = weaponData.BackRightLocalRotation;
                visualTransform.localScale = weaponData.BackRightLocalScale;
                break;

            default:
                visualTransform.localPosition = weaponData.BackLocalPosition;
                visualTransform.localRotation = weaponData.BackLocalRotation;
                visualTransform.localScale = weaponData.BackLocalScale;
                break;
        }
    }

    private void DestroyVisual(ref WeaponVisualInstance visual)
    {
        if (visual.instance != null)
        {
            Destroy(visual.instance);
        }

        visual = default;
    }

    private static void DisablePhysics(GameObject weaponObject)
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

    private struct WeaponVisualInstance
    {
        public WeaponData weaponData;
        public TMJ_WeaponUseSlot useSlot;
        public GameObject instance;

        public bool HasInstance => weaponData != null && instance != null;
    }
}