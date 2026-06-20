using UnityEngine;
using UnityEngine.UI;

public class EquipChoicePopup : MonoBehaviour
{
    public Button slot1Button;
    public Button slot2Button;

    private SkillData pendingSkill;
    private VisualSkillInventoryManager manager;

    public void Open(SkillData skill, VisualSkillInventoryManager mgr, Vector3 worldPosition)
    {
        pendingSkill = skill;
        manager = mgr;

        transform.position = worldPosition + Vector3.up * 60f; // ajustar offset según UI

        gameObject.SetActive(true);

        slot1Button.onClick.RemoveAllListeners();
        slot2Button.onClick.RemoveAllListeners();
        slot1Button.onClick.AddListener(() => Confirm(1));
        slot2Button.onClick.AddListener(() => Confirm(2));
    }

    void Confirm(int slotIndex)
    {
        manager.EquipToSlot(pendingSkill, slotIndex);
        gameObject.SetActive(false);
    }
}