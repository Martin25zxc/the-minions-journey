using UnityEngine;

public class ThunderBoltSkill : MonoBehaviour, ISkillBehaviour
{

    public string SkillID => "thunderbolt";

    public void Execute()
    {
        Debug.Log("Pikaaaachuuuuu! (Ejecutando ThunderBolt)");
        // Aquí iría la lógica real de la habilidad, como instanciar un proyectil, aplicar daño, etc.
    }
}
