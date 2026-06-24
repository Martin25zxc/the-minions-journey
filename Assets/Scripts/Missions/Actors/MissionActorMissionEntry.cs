using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Entrada de autoría que define qué misión puede OFRECER este actor/NPC.
/// No guarda progreso runtime y no decide entrega.
///
/// Regla importante:
/// - El actor set dice qué misiones puede ofrecer el actor.
/// - La entrega se deriva desde MissionDefinition + MissionRuntimeState.CanTurnInAtTarget(actorId).
/// </summary>
[Serializable]
public sealed class MissionActorMissionEntry
{
    [SerializeField, Tooltip("Misión asociada a este actor. Debe estar en el MissionCatalog del nivel para contenido estable.")]
    private MissionDefinition mission;

    [FormerlySerializedAs("canGiveMission")]
    [SerializeField, Tooltip("Si está activo, este actor puede ofrecer/aceptar esta misión cuando esté Available. La entrega NO se configura acá: se deriva desde MissionDefinition + runtime.")]
    private bool canOfferMission = true;

    public MissionDefinition Mission => mission;
    public bool CanOfferMission => canOfferMission;

    public string MissionId
    {
        get
        {
            if (mission == null)
            {
                return string.Empty;
            }

            return CleanId(mission.MissionId);
        }
    }

    public bool HasValidMission => !string.IsNullOrEmpty(MissionId);

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
