using UnityEngine;

public class GhostShieldSkill : MonoBehaviour, ISkillBehaviour
{
    public string SkillID => "ghostshield";

    public void Execute()
    {
        Debug.Log("¡Ejecutando Ghost Shield!");
        // Aquí iría la lógica real de la habilidad, como aplicar un escudo al jugador, etc.
    }
}
