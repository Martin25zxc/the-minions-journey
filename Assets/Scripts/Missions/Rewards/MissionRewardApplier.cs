using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MissionRewardApplier : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager que emite el evento MissionCompleted. Este applier se suscribe a ese evento y aplica las recompensas declaradas en MissionDefinition.")]
    private MissionManager missionManager;

    [Header("Debug")]
    [SerializeField]
    private bool logRewards;

    // Runtime guard: evita aplicar dos veces las recompensas de la misma misión
    // si el MissionManager emite completion más de una vez por refresh/debug/reentrada.
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
        else if (logRewards)
        {
            Debug.LogWarning("MissionRewardApplier: no hay MissionManager asignado/encontrado. No se aplicarán rewards por evento.", this);
        }
    }

    private void OnDisable()
    {
        if (missionManager != null)
        {
            missionManager.MissionCompleted -= HandleMissionCompleted;
        }
    }

    private void HandleMissionCompleted(MissionRuntimeState runtimeState)
    {
        if (runtimeState == null)
        {
            Debug.LogWarning("MissionRewardApplier: recibió MissionCompleted con runtimeState null.", this);
            return;
        }

        ApplyRewards(runtimeState.Definition);
    }

    /// <summary>
    /// Llamar una sola vez cuando la misión llega a Completed.
    /// No llamar en ReadyToTurnIn, ni durante refreshes de HUD/Journal.
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

        // GameProgressManager.Acquire todavía es void por compatibilidad con el equipo de habilidades.
        // Cuando se coordine el cambio a bool/TryAcquire, acá se podrá evitar notificaciones/logs duplicados.
        GameProgressManager.Instance.Acquire(skillId);

        if (logRewards)
        {
            Debug.Log($"MissionRewardApplier: misión '{mission.MissionId}' otorgó SkillUnlock '{skillId}'.", this);
        }
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
