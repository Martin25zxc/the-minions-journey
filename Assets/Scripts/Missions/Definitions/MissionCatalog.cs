using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Missions/Mission Catalog", fileName = "MC_NewMissionCatalog")]
public sealed class MissionCatalog : ScriptableObject
{
    [Header("Misiones")]
    [SerializeField, Tooltip("Misiones disponibles para esta scene, nivel o bloque de prueba. No guarda progreso runtime.")]
    private MissionDefinition[] missions = Array.Empty<MissionDefinition>();

    public IReadOnlyList<MissionDefinition> Missions => missions ?? Array.Empty<MissionDefinition>();

    public bool TryGetMission(string missionId, out MissionDefinition mission)
    {
        string cleanedMissionId = MissionAuthoringValidation.CleanId(missionId);

        if (string.IsNullOrEmpty(cleanedMissionId))
        {
            mission = null;
            return false;
        }

        IReadOnlyList<MissionDefinition> missionList = Missions;

        for (int i = 0; i < missionList.Count; i++)
        {
            MissionDefinition candidate = missionList[i];

            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(candidate.MissionId, cleanedMissionId, StringComparison.Ordinal))
            {
                mission = candidate;
                return true;
            }
        }

        mission = null;
        return false;
    }

    public bool Contains(MissionDefinition mission)
    {
        if (mission == null)
        {
            return false;
        }

        IReadOnlyList<MissionDefinition> missionList = Missions;

        for (int i = 0; i < missionList.Count; i++)
        {
            if (missionList[i] == mission)
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        if (missions == null)
        {
            missions = Array.Empty<MissionDefinition>();
            return;
        }

        HashSet<string> usedMissionIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < missions.Length; i++)
        {
            MissionDefinition mission = missions[i];

            if (mission == null)
            {
                Debug.LogWarning($"{name}: Mission #{i} está vacía/null.", this);
                continue;
            }

            if (MissionAuthoringValidation.IsNullOrWhiteSpace(mission.MissionId))
            {
                Debug.LogWarning($"{name}: Mission #{i} no tiene MissionId. Revisar asset '{mission.name}'.", mission);
                continue;
            }

            if (!usedMissionIds.Add(mission.MissionId))
            {
                Debug.LogWarning($"{name}: MissionId duplicado en el catálogo: '{mission.MissionId}'.", this);
            }
        }
    }
}
