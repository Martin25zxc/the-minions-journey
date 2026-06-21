using UnityEngine;

public class FireBallSkill : MonoBehaviour, ISkillBehaviour
{
    public string SkillID => "fireball";
    
    public void Execute()
    {
        Debug.Log("¡Lanzar bola de fuego!");
        // Aquí iría la lógica real de la habilidad: instanciar proyectil, aplicar daño, etc.
    }
}
