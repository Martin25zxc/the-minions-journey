using UnityEngine;

public class FireBallSkill : MonoBehaviour, ISkillBehaviour
{
    public string SkillID => "fireball";

    [Tooltip("Prefab de la bola de fuego que se instanciará al usar la habilidad.")]
    [SerializeField] AttackBallBehavoir fireBallPrefab;
    [SerializeField] Transform firePoint; // Punto desde donde se lanzará la bola de fuego
    [SerializeField] float fireBallSpeed = 10f;
    [SerializeField] float fireBallDamage = 20f;


    public void Execute()
    {
        AttackBallBehavoir fireBall = Instantiate(fireBallPrefab, firePoint.position, firePoint.rotation);
        fireBall.Initialize(fireBallSpeed, fireBallDamage);
        Debug.Log("¡Lanzar bola de fuego!");
    }
}
