using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Weapon Data")]
public class WeaponData : ItemData
{
    [SerializeField] 
    private WeaponType weaponType;
    [Header("Weapon Stats")]
    [SerializeField] 
    private float damage = 10f;

    [Header("Equipped Model")]
    [SerializeField] 
    private GameObject equippedModelPrefab;

    public WeaponType WeaponType => weaponType;
    public float Damage => damage;
    public GameObject EquippedModelPrefab => equippedModelPrefab;
}