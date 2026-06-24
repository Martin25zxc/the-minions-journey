using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Asset de autoría que define qué misiones puede ofrecer un actor/NPC.
/// Sirve como fuente compartida para MissionActorInteraction y MissionActorIndicator.
///
/// No define si el actor puede entregar: esa regla vive en MissionDefinition + MissionRuntimeState.
/// </summary>
[CreateAssetMenu(fileName = "MAMS_NewActorMissionSet", menuName = "TMJ/Missions/Mission Actor Mission Set")]
public sealed class MissionActorMissionSet : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField, Tooltip("ID interno del set. Útil para debug/autoria. No reemplaza el ActorId del NPC de escena.")]
    private string setId;

    [SerializeField, Tooltip("Nombre legible para diseñadores. No se usa como ID runtime.")]
    private string displayName;

    [Header("Misiones que puede ofrecer el actor")]
    [SerializeField, Tooltip("Misiones relevantes para este actor. El bool de cada entry solo indica si puede ofrecerla cuando esté Available. La entrega se deriva desde la definición/runtime.")]
    private MissionActorMissionEntry[] entries = Array.Empty<MissionActorMissionEntry>();

    public string SetId => setId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<MissionActorMissionEntry> Entries => entries;
    public int EntryCount => entries != null ? entries.Length : 0;

    private void OnValidate()
    {
        setId = CleanId(setId);
    }

    public MissionActorMissionEntry GetEntry(int index)
    {
        if (entries == null || index < 0 || index >= entries.Length)
        {
            return null;
        }

        return entries[index];
    }

    public bool ContainsMission(string missionId)
    {
        string cleanedMissionId = CleanId(missionId);

        if (string.IsNullOrEmpty(cleanedMissionId) || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            MissionActorMissionEntry entry = entries[i];

            if (entry == null || !entry.HasValidMission)
            {
                continue;
            }

            if (string.Equals(entry.MissionId, cleanedMissionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
