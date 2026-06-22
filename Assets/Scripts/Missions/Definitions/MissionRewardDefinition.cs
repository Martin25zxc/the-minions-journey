using System;
using UnityEngine;

[Serializable]
public sealed class MissionRewardDefinition
{
    [Header("Identidad")]
    [SerializeField, Tooltip("ID estable de recompensa. Puede apuntar a item, habilidad, curación o evento futuro.")]
    private string rewardId;

    [SerializeField, Tooltip("Tipo general de recompensa. En esta etapa solo se define, todavía no se entrega.")]
    private MissionRewardType rewardType = MissionRewardType.None;

    [Header("Texto")]
    [SerializeField, Tooltip("Nombre visible para HUD/Journal futuro. Ejemplo: Hook, Curación menor, Manzanas benditas.")]
    private string displayName;

    [Header("Cantidad")]
    [SerializeField, Min(1), Tooltip("Cantidad de recompensa. Para desbloqueos únicos, dejar en 1.")]
    private int amount = 1;

    [Header("Opciones")]
    [SerializeField, Tooltip("Si está activo, esta recompensa puede venir de un objetivo bonus o condición extra futura.")]
    private bool bonusReward;

    [SerializeField, Tooltip("Nota corta para diseño. No se muestra al jugador por ahora.")]
    private string designerNote;

    public string RewardId => rewardId;
    public MissionRewardType RewardType => rewardType;
    public string DisplayName => displayName;
    public int Amount => amount;
    public bool BonusReward => bonusReward;
    public string DesignerNote => designerNote;

    internal void Validate(string missionId, int index)
    {
        rewardId = MissionAuthoringValidation.CleanId(rewardId);

        if (amount < 1)
        {
            amount = 1;
        }

        string rewardContext = $"Mission '{missionId}', Reward #{index}";

        if (rewardType == MissionRewardType.None)
        {
            if (!MissionAuthoringValidation.IsNullOrWhiteSpace(rewardId) || !MissionAuthoringValidation.IsNullOrWhiteSpace(displayName))
            {
                Debug.LogWarning($"{rewardContext}: RewardType es None pero tiene datos cargados. Revisá si querías una recompensa real.");
            }

            return;
        }

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(rewardId))
        {
            Debug.LogWarning($"{rewardContext}: falta RewardId. Usá un ID estable, por ejemplo: unlock_heal.");
        }
        else if (!MissionAuthoringValidation.IsStableId(rewardId))
        {
            Debug.LogWarning($"{rewardContext}: {MissionAuthoringValidation.BuildStableIdWarning(nameof(rewardId), rewardId)}");
        }

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogWarning($"{rewardContext}: falta DisplayName. El Journal futuro no va a tener un texto claro para mostrar.");
        }
    }
}
