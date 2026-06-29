using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionRewardApplier : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager que emite MissionCompleted. Si queda vacío, se intenta resolver automáticamente al habilitarse.")]
    private MissionManager missionManager;

    [Header("Notificaciones")]
    [SerializeField, Tooltip("Muestra una notificación cuando una reward SkillUnlock desbloquea una habilidad nueva.")]
    private bool notifySkillUnlocks = true;

    [SerializeField, Tooltip("Título usado para notificaciones de habilidades desbloqueadas.")]
    private string skillUnlockTitle = "Nueva habilidad desbloqueada";

    [SerializeField, Tooltip("Texto final para guiar al jugador hacia la UI de habilidades/inventario.")]
    private string skillUnlockHint = "Revisa el inventario.";

    [Header("Debug")]
    [SerializeField]
    private bool logRewards;

    // Runtime guard: evita aplicar dos veces las recompensas de la misma misión
    // si el MissionManager llama este método más de una vez por refresh/debug/reentrada.
    // Cuando exista Save/Load real, decidir si este estado debe persistirse o si
    // el MissionManager garantiza que solo emite completion una vez por misión.
    private readonly HashSet<string> appliedMissionIds = new();

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
    }

    private void OnEnable()
    {
        if (missionManager == null)
        {
            missionManager = FindFirstObjectByType<MissionManager>();
        }

        if (missionManager != null)
        {
            missionManager.MissionCompleted += HandleMissionCompleted;
        }
    }

    private void OnDisable()
    {
        if (missionManager != null)
        {
            missionManager.MissionCompleted -= HandleMissionCompleted;
        }
    }

    private void HandleMissionCompleted(MissionRuntimeState missionState)
    {
        if (missionState == null)
        {
            return;
        }

        ApplyRewards(missionState.Definition);
    }

    /// <summary>
    /// Llamar una sola vez cuando la misión llega a Completed.
    /// No llamar en ReadyToTurnIn, ni durante refreshes de HUD/Journal.
    /// Se mantiene público para herramientas/debug o integración manual puntual.
    /// </summary>
    public void ApplyRewards(MissionDefinition mission)
    {
        TryApplyRewards(mission);
    }

    /// <summary>
    /// Aplica recompensas si todavía no fueron aplicadas para esta misión.
    /// Devuelve true si intentó aplicar rewards, false si no hizo nada.
    /// </summary>
    public bool TryApplyRewards(MissionDefinition mission)
    {
        if (mission == null)
        {
            Debug.LogWarning("MissionRewardApplier: misión null. No se aplicaron recompensas.", this);
            return false;
        }

        string missionId = mission.MissionId;
        if (string.IsNullOrWhiteSpace(missionId))
        {
            Debug.LogWarning("MissionRewardApplier: misión sin MissionId. No se aplicaron recompensas.", this);
            return false;
        }

        if (!appliedMissionIds.Add(missionId))
        {
            if (logRewards)
            {
                Debug.Log($"MissionRewardApplier: rewards de misión '{missionId}' ya fueron aplicadas en esta ejecución. Se ignora llamada duplicada.", this);
            }

            return false;
        }

        if (mission.Rewards == null || mission.Rewards.Count == 0)
        {
            if (logRewards)
            {
                Debug.Log($"MissionRewardApplier: misión '{missionId}' completada sin rewards.", this);
            }

            return true;
        }

        foreach (MissionRewardDefinition reward in mission.Rewards)
        {
            ApplyReward(mission, reward);
        }

        return true;
    }

    /// <summary>
    /// Solo para herramientas/debug. No usar como parte normal del flujo de gameplay.
    /// </summary>
    public void ClearRuntimeAppliedRewards()
    {
        appliedMissionIds.Clear();
    }

    private void ApplyReward(MissionDefinition mission, MissionRewardDefinition reward)
    {
        if (reward == null)
        {
            Debug.LogWarning($"MissionRewardApplier: misión '{mission.MissionId}' tiene una reward null.", this);
            return;
        }

        switch (reward.RewardType)
        {
            case MissionRewardType.None:
                LogIgnoredNone(mission);
                break;

            case MissionRewardType.SkillUnlock:
                ApplySkillUnlock(mission, reward);
                break;

            case MissionRewardType.Item:
                WarnNotImplemented(mission, reward, "Item rewards todavía no están conectadas a inventario.");
                break;

            case MissionRewardType.Currency:
                WarnNotImplemented(mission, reward, "Currency rewards todavía no están conectadas a un sistema de moneda/recurso.");
                break;

            case MissionRewardType.InstantHeal:
                WarnNotImplemented(mission, reward, "InstantHeal rewards todavía no están conectadas al TopDownHealth del player.");
                break;

            case MissionRewardType.WorldEvent:
                WarnNotImplemented(mission, reward, "WorldEvent rewards todavía no están conectadas a un bus/registry de eventos de mundo.");
                break;

            default:
                Debug.LogWarning($"MissionRewardApplier: RewardType no soportado '{reward.RewardType}' en misión '{mission.MissionId}'.", this);
                break;
        }
    }

    private void ApplySkillUnlock(MissionDefinition mission, MissionRewardDefinition reward)
    {
        string skillId = reward.RewardId;

        if (string.IsNullOrWhiteSpace(skillId))
        {
            Debug.LogWarning($"MissionRewardApplier: SkillUnlock sin skillId en misión '{mission.MissionId}'.", this);
            return;
        }

        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning($"MissionRewardApplier: no existe GameProgressManager. No se pudo otorgar la skill '{skillId}' de la misión '{mission.MissionId}'.", this);
            return;
        }

        bool acquiredNow = GameProgressManager.Instance.TryAcquire(skillId);

        if (logRewards)
        {
            string result = acquiredNow ? "otorgó" : "ya tenía";
            Debug.Log($"MissionRewardApplier: misión '{mission.MissionId}' {result} SkillUnlock '{skillId}'.", this);
        }

        if (acquiredNow && notifySkillUnlocks)
        {
            ShowSkillUnlockNotification(reward, skillId);
        }
    }

    private void ShowSkillUnlockNotification(MissionRewardDefinition reward, string skillId)
    {
        string displayName = !string.IsNullOrWhiteSpace(reward.DisplayName)
            ? reward.DisplayName
            : skillId;

        string message = $"Has desbloqueado una nueva habilidad: {displayName}. {skillUnlockHint}";

        TMJNotifications.ShowInventory(
            message: message,
            priority: NotificationPriority.High,
            title: skillUnlockTitle,
            groupKey: $"skill_unlocked:{skillId}",
            context: this);
    }

    private void WarnNotImplemented(MissionDefinition mission, MissionRewardDefinition reward, string reason)
    {
        Debug.LogWarning($"MissionRewardApplier: RewardType '{reward.RewardType}' declarado pero no implementado. Misión '{mission.MissionId}', RewardId '{reward.RewardId}'. {reason}", this);
    }

    private void LogIgnoredNone(MissionDefinition mission)
    {
        if (logRewards)
        {
            Debug.Log($"MissionRewardApplier: misión '{mission.MissionId}' tiene una reward None. Se ignora.", this);
        }
    }
}
