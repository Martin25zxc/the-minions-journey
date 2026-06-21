using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Un solo prefab para todo: en la lista (inventario abierto) muestra data
/// con hover y permite equipar/desequipar con click. En el HUD (inventario
/// cerrado) la misma instancia queda activa solo si está equipada, mostrando
/// ícono, cooldown y la tecla asignada.
/// "Adquirida o no" se consulta a GameProgressManager, no al SO.
/// </summary>
public class SkillSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Visual base")]
    public Image icon;
    public Image cooldownOverlay;
    public GameObject lockedOverlay;      // visible si no está adquirida (solo importa con inventario abierto)
    public GameObject equippedHighlight;

    [Header("Tecla (solo con inventario cerrado)")]
    public TMP_Text keyLabel;

    [Header("Tooltip (solo con inventario abierto)")]
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    [Header("Mini menú de equipar (hijo del mismo prefab)")]
    public GameObject slotChoicePanel;
    public Button slot1Button;
    public Button slot2Button;

    private SkillData data;
    private VisualSkillInventoryManager manager;
    private bool inventoryOpen;

    private bool Acquired => GameProgressManager.Instance.IsAcquired(data.skillID);

    public void Setup(SkillData skillData, VisualSkillInventoryManager mgr)
    {
        data = skillData;
        manager = mgr;

        icon.sprite = data.icon;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        if (slotChoicePanel != null) slotChoicePanel.SetActive(false);

        slot1Button.onClick.RemoveAllListeners();
        slot2Button.onClick.RemoveAllListeners();
        slot1Button.onClick.AddListener(() => ChooseSlot(1));
        slot2Button.onClick.AddListener(() => ChooseSlot(2));

        RefreshAcquiredVisual();
        RefreshEquippedState();
    }

    public void RefreshAcquiredVisual()
    {
        lockedOverlay?.SetActive(!Acquired);
    }

    /// <summary>Llamado por el manager cada vez que se abre/cierra el inventario.</summary>
    public void SetInventoryOpen(bool open)
    {
        inventoryOpen = open;

        if (open)
        {
            RefreshAcquiredVisual(); // por si se adquirió algo mientras estaba cerrado
            keyLabel?.gameObject.SetActive(false);
            gameObject.SetActive(true); // abierto: se ven todas, adquiridas o no
        }
        else
        {
            tooltipPanel?.SetActive(false);
            slotChoicePanel?.SetActive(false);
            ApplyClosedVisual();
        }
    }

    /// <summary>Llamado por el manager cada vez que cambia algún slot equipado.</summary>
    public void RefreshEquippedState()
    {
        bool equipped = manager.IsEquipped(data, out _);
        equippedHighlight?.SetActive(equipped);

        if (!inventoryOpen)
            ApplyClosedVisual();
    }

    void ApplyClosedVisual()
    {
        bool equipped = manager.IsEquipped(data, out int slot);
        gameObject.SetActive(equipped); // cerrado: solo sobreviven visualmente las equipadas

        if (keyLabel != null)
        {
            keyLabel.gameObject.SetActive(equipped);
            if (equipped) keyLabel.text = manager.GetKeyLabelForSlot(slot);
        }
    }

    void Update()
    {
        if (!manager.IsEquipped(data, out int slot))
        {
            cooldownOverlay.gameObject.SetActive(false);
            return;
        }

        var state = slot == 1 ? manager.slot1 : manager.slot2;
        bool onCooldown = state.cooldownRemaining > 0;
        cooldownOverlay.gameObject.SetActive(onCooldown);
        if (onCooldown)
            cooldownOverlay.fillAmount = state.cooldownRemaining / data.cooldown;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!inventoryOpen) return;

        tooltipText.text = Acquired
            ? $"{data.skillName}\n{data.description}\nCooldown: {data.cooldown}s"
            : $"{data.skillName}\n(Bloqueada)";

        tooltipPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!inventoryOpen) return;
        tooltipPanel.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!inventoryOpen || !Acquired) return;

        if (manager.IsEquipped(data, out int slot))
        {
            manager.Unequip(slot);
            slotChoicePanel.SetActive(false);
        }
        else
        {
            slotChoicePanel.SetActive(!slotChoicePanel.activeSelf);
        }
    }

    void ChooseSlot(int slotIndex)
    {
        manager.EquipToSlot(data, slotIndex);
        slotChoicePanel.SetActive(false);
    }
}