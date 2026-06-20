using System;
using System.Collections.Generic;
using UnityEngine;

public class EquippedSkillState
{
    public SkillData data;
    public float cooldownRemaining;
}

public class VisualSkillInventoryManager : MonoBehaviour
{
    [Header("Datos (TODAS las habilidades, adquiridas o no)")]
    public List<SkillData> allSkills = new();

    [Header("Fuentes de ejecutores de habilidades (objetos con componentes ISkillBehaviour)")]
    public MonoBehaviour[] skillExecutorSources;

    [Header("UI")]
    public Transform skillsContainer; // un solo contenedor (HorizontalLayoutGroup), siempre activo
    public SkillSlotView slotPrefab;  // un solo prefab para todo

    [Header("Etiquetas de tecla mostradas con el inventario cerrado")]
    public string slot1KeyLabel = "Q";
    public string slot2KeyLabel = "E";

    public EquippedSkillState slot1;
    public EquippedSkillState slot2;
    public event Action OnEquipChanged;

    private readonly Dictionary<string, ISkillBehaviour> executorsByID = new();
    private readonly Dictionary<SkillData, SkillSlotView> viewsBySkill = new();
    private readonly List<SkillSlotView> views = new();
    private bool inventoryOpen;

    void Awake() => CacheExecutors();

    void Start()
    {
        BuildViews();
        SetInventoryOpen(false);
        GameProgressManager.Instance.OnSkillAcquired += HandleSkillAcquired;
    }

    void OnDestroy()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.OnSkillAcquired -= HandleSkillAcquired;
    }

    void HandleSkillAcquired(string skillID)
    {
        // simple y suficiente para pocas skills: refresca el candado de todas.
        foreach (var view in views)
            view.RefreshAcquiredVisual();
    }

    void Update()
    {
        TickCooldown(slot1);
        TickCooldown(slot2);
    }

    void TickCooldown(EquippedSkillState state)
    {
        if (state != null && state.cooldownRemaining > 0)
            state.cooldownRemaining -= Time.deltaTime;
    }

    void CacheExecutors()
    {
        executorsByID.Clear();
        foreach (var source in skillExecutorSources)
        {
            if (source == null) continue;
            foreach (var behaviour in source.GetComponents<ISkillBehaviour>())
                executorsByID[behaviour.SkillID] = behaviour;
        }
    }

    void BuildViews()
    {
        foreach (var skill in allSkills)
        {
            var view = Instantiate(slotPrefab, skillsContainer);
            view.Setup(skill, this);
            views.Add(view);
            viewsBySkill[skill] = view;
        }
    }

    /// <summary>
    /// Abierto: se ven todas las instancias en el orden de allSkills.
    /// Cerrado: cada instancia se autodesactiva salvo que esté equipada, y
    /// además fuerza el orden de los siblings para que slot2 quede a la
    /// izquierda y slot1 a la derecha, sin importar el orden del inventario.
    /// </summary>
    public void SetInventoryOpen(bool open)
    {
        inventoryOpen = open;
        foreach (var view in views)
            view.SetInventoryOpen(open);

        if (!open)
            ApplyClosedOrder();
    }

    void ApplyClosedOrder()
    {
        if (slot1 != null && viewsBySkill.TryGetValue(slot1.data, out var view1))
            view1.transform.SetSiblingIndex(0);

        if (slot2 != null && viewsBySkill.TryGetValue(slot2.data, out var view2))
            view2.transform.SetSiblingIndex(skillsContainer.childCount - 1);
    }

    public string GetKeyLabelForSlot(int slotIndex) => slotIndex == 1 ? slot1KeyLabel : slot2KeyLabel;

    public void ToggleInventory() => SetInventoryOpen(!inventoryOpen);

    public bool IsEquipped(SkillData skill, out int slotIndex)
    {
        if (slot1?.data == skill) { slotIndex = 1; return true; }
        if (slot2?.data == skill) { slotIndex = 2; return true; }
        slotIndex = 0;
        return false;
    }

    public void EquipToSlot(SkillData skill, int slotIndex)
    {
        if (!GameProgressManager.Instance.IsAcquired(skill.skillID)) return;

        var state = new EquippedSkillState { data = skill, cooldownRemaining = 0 };
        if (slotIndex == 1) slot1 = state; else slot2 = state;
        RefreshAllViews();
        OnEquipChanged?.Invoke();
    }

    public void Unequip(int slotIndex)
    {
        if (slotIndex == 1) slot1 = null; else slot2 = null;
        RefreshAllViews();
        OnEquipChanged?.Invoke();
    }

    void RefreshAllViews()
    {
        foreach (var view in views) view.RefreshEquippedState();
    }

    /// <summary>
    /// Único punto de entrada para USAR una habilidad. Si el slot está vacío
    /// o en cooldown, no pasa nada: esto garantiza que el player solo pueda
    /// usar habilidades equipadas.
    /// </summary>
    public void TryUseSkill(int slotIndex)
    {
        var state = slotIndex == 1 ? slot1 : slot2;
        if (state == null || state.cooldownRemaining > 0) return;

        if (executorsByID.TryGetValue(state.data.skillID, out var executor))
            executor.Execute();
        else
            Debug.LogWarning($"No se encontró ejecutor para skillID '{state.data.skillID}'");

        state.cooldownRemaining = state.data.cooldown;
    }
}