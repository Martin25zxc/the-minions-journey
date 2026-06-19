using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/Items/Weapon Data")]
public class WeaponData : ItemData
{
    [Header("Weapon Stats")]
    [SerializeField, FormerlySerializedAs("damage"), Min(0f)]
    float damageBonusMin;

    [SerializeField, Min(0f)]
    float damageBonusMax;

    [SerializeField]
    WeaponType weaponType;

    [Header("Visual")]
    [SerializeField] GameObject equippedModelPrefab;
    
    [Header("Back Left Pose Offset")]
    [SerializeField] EquipmentVisualPose equipmentVisualBackLeftPose;
    
    [Header("Back Right Pose Offset")]
    [SerializeField]
    EquipmentVisualPose equipmentVisualBackRightPose;
    
    [Header("Hand Pose Offset")]
    [SerializeField]
    EquipmentVisualPose equipmentVisualHandPose;

    public WeaponType WeaponType => weaponType;

    public float DamageBonusMin => damageBonusMin;

    public float DamageBonusMax => damageBonusMax;

    public float AverageDamageBonus => (damageBonusMin + damageBonusMax) * 0.5f;

    // Backwards-compatible read-only alias for older scripts/debug UI.
    // In the current combat design this is a bonus, not the full attack damage.
    public float Damage => AverageDamageBonus;

    public GameObject EquippedModelPrefab => equippedModelPrefab;

    public Vector3 HandLocalPosition => equipmentVisualHandPose.LocalPosition;
    public Quaternion HandLocalRotation => Quaternion.Euler(equipmentVisualHandPose.LocalEulerAngles);
    public Vector3 HandLocalScale => equipmentVisualHandPose.LocalScale;

    public Vector3 BackLocalPosition => backLocalPosition;
    public Quaternion BackLocalRotation => Quaternion.Euler(backLocalEulerAngles);
    public Vector3 BackLocalScale => backLocalScale;

    public Vector3 BackLeftLocalPosition => equipmentVisualBackLeftPose.LocalPosition;
    public Quaternion BackLeftLocalRotation => Quaternion.Euler(equipmentVisualBackLeftPose.LocalEulerAngles);
    public Vector3 BackLeftLocalScale => equipmentVisualBackLeftPose.LocalScale;

    public Vector3 BackRightLocalPosition => equipmentVisualBackRightPose.LocalPosition;
    public Quaternion BackRightLocalRotation => Quaternion.Euler(equipmentVisualBackRightPose.LocalEulerAngles);
    public Vector3 BackRightLocalScale => equipmentVisualBackRightPose.LocalScale;

    void OnValidate()
    {
        damageBonusMin = Mathf.Max(0f, damageBonusMin);
        damageBonusMax = Mathf.Max(0f, damageBonusMax);

        if (damageBonusMax < damageBonusMin)
        {
            damageBonusMax = damageBonusMin;
        }
    }

    public float RollDamageBonus()
    {
        if (damageBonusMax <= damageBonusMin)
        {
            return damageBonusMin;
        }

        return Random.Range(damageBonusMin, damageBonusMax);
    }
}
