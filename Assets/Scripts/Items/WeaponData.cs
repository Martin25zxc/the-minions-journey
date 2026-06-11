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

    [Header("Hand Pose Offset")]
    [SerializeField] Vector3 handLocalPosition;
    [SerializeField] Vector3 handLocalEulerAngles;
    [SerializeField] Vector3 handLocalScale = Vector3.one;

    [Header("Back Pose Offset")]
    [SerializeField] Vector3 backLocalPosition;
    [SerializeField] Vector3 backLocalEulerAngles;
    [SerializeField] Vector3 backLocalScale = Vector3.one;

    public WeaponType WeaponType => weaponType;

    public float DamageBonusMin => damageBonusMin;

    public float DamageBonusMax => damageBonusMax;

    public float AverageDamageBonus => (damageBonusMin + damageBonusMax) * 0.5f;

    // Backwards-compatible read-only alias for older scripts/debug UI.
    // In the current combat design this is a bonus, not the full attack damage.
    public float Damage => AverageDamageBonus;

    public GameObject EquippedModelPrefab => equippedModelPrefab;

    public Vector3 HandLocalPosition => handLocalPosition;
    public Quaternion HandLocalRotation => Quaternion.Euler(handLocalEulerAngles);
    public Vector3 HandLocalScale => handLocalScale;

    public Vector3 BackLocalPosition => backLocalPosition;
    public Quaternion BackLocalRotation => Quaternion.Euler(backLocalEulerAngles);
    public Vector3 BackLocalScale => backLocalScale;

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
