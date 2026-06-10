using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Weapon Data")]
public class WeaponData : ItemData
{
    [Header("Weapon Stats")]
    [SerializeField] 
    private float damage;
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
    public float Damage => damage;
    public GameObject EquippedModelPrefab => equippedModelPrefab;

    public Vector3 HandLocalPosition => handLocalPosition;
    public Quaternion HandLocalRotation => Quaternion.Euler(handLocalEulerAngles);
    public Vector3 HandLocalScale => handLocalScale;

    public Vector3 BackLocalPosition => backLocalPosition;
    public Quaternion BackLocalRotation => Quaternion.Euler(backLocalEulerAngles);
    public Vector3 BackLocalScale => backLocalScale;
    
}