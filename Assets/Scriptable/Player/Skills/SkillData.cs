using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/SkillData")]
public class SkillData : ScriptableObject
{
    [Tooltip("Debe coincidir exactamente con el SkillID del script ISkillBehaviour correspondiente")]
    public string skillID;

    public string skillName;
    [TextArea] public string description;
    public Sprite icon;
    public float cooldown;
}