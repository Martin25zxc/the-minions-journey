using System;

/// <summary>
/// Selector puro de acciones para actores de misión.
/// Evalúa todas las misiones asociadas a un actor y elige la mejor acción disponible.
///
/// Prioridad fija MVP:
/// ReadyToTurnIn > Available > ActivePending > CompletedFallback > Unavailable.
///
/// Autoridades:
/// - MissionActorMissionSet solo define qué misiones puede OFRECER este actor.
/// - MissionDefinition + MissionRuntimeState deciden si este actor puede ENTREGAR.
/// </summary>
public static class MissionActorMissionResolver
{
    public static MissionActorResolvedAction ResolveBestAction(
        MissionManager missionManager,
        MissionActorMissionSet missionSet,
        string actorId)
    {
        string cleanedActorId = CleanId(actorId);

        if (missionManager == null)
        {
            return MissionActorResolvedAction.Unavailable("Falta MissionManager.");
        }

        if (missionSet == null || missionSet.EntryCount == 0)
        {
            return MissionActorResolvedAction.Unavailable("El actor no tiene MissionActorMissionSet o no tiene entradas.");
        }

        if (string.IsNullOrEmpty(cleanedActorId))
        {
            return MissionActorResolvedAction.Unavailable("ActorId vacío.");
        }

        MissionActorResolvedAction completedFallback = null;

        // 1) Entrega lista: máxima prioridad.
        // No depende de un checkbox del actor set. La autoridad es runtimeState.CanTurnInAtTarget(actorId).
        for (int i = 0; i < missionSet.EntryCount; i++)
        {
            MissionActorMissionEntry entry = missionSet.GetEntry(i);
            MissionRuntimeState runtimeState = GetRuntimeState(missionManager, entry);

            if (runtimeState == null || entry == null)
            {
                continue;
            }

            if (runtimeState.IsReadyToTurnIn && runtimeState.CanTurnInAtTarget(cleanedActorId))
            {
                return MissionActorResolvedAction.TurnIn(runtimeState, entry);
            }
        }

        // 2) Misión disponible para aceptar.
        // Esta sí depende de CanOfferMission, porque MissionDefinition no sabe qué NPC la ofrece.
        for (int i = 0; i < missionSet.EntryCount; i++)
        {
            MissionActorMissionEntry entry = missionSet.GetEntry(i);
            MissionRuntimeState runtimeState = GetRuntimeState(missionManager, entry);

            if (runtimeState == null || entry == null || !entry.CanOfferMission)
            {
                continue;
            }

            if (runtimeState.IsAvailable)
            {
                return MissionActorResolvedAction.Accept(runtimeState, entry);
            }
        }

        // 3) Misión activa pendiente de objetivo/entrega.
        for (int i = 0; i < missionSet.EntryCount; i++)
        {
            MissionActorMissionEntry entry = missionSet.GetEntry(i);
            MissionRuntimeState runtimeState = GetRuntimeState(missionManager, entry);

            if (runtimeState == null || entry == null)
            {
                continue;
            }

            if (runtimeState.IsActive && IsActorExpectedTurnInTarget(runtimeState, cleanedActorId))
            {
                return MissionActorResolvedAction.Pending(runtimeState, entry);
            }

            if (runtimeState.IsCompleted && completedFallback == null)
            {
                completedFallback = MissionActorResolvedAction.Completed(runtimeState, entry);
            }
        }

        // 4) Completed solo si no hay nada mejor. No debe bloquear Available ni ReadyToTurnIn.
        if (completedFallback != null)
        {
            return completedFallback;
        }

        return MissionActorResolvedAction.Unavailable("No hay acción de misión disponible para este actor.");
    }

    public static MissionIndicatorState ResolveBestIndicatorState(
        MissionManager missionManager,
        MissionActorMissionSet missionSet,
        string actorId,
        bool showPendingTurnInIndicator)
    {
        string cleanedActorId = CleanId(actorId);

        if (missionManager == null || missionSet == null || missionSet.EntryCount == 0 || string.IsNullOrEmpty(cleanedActorId))
        {
            return MissionIndicatorState.None;
        }

        MissionIndicatorState bestState = MissionIndicatorState.None;

        for (int i = 0; i < missionSet.EntryCount; i++)
        {
            MissionActorMissionEntry entry = missionSet.GetEntry(i);
            MissionRuntimeState runtimeState = GetRuntimeState(missionManager, entry);

            if (runtimeState == null || entry == null)
            {
                continue;
            }

            MissionIndicatorState candidate = ResolveIndicatorStateForEntry(runtimeState, entry, cleanedActorId, showPendingTurnInIndicator);

            if (GetIndicatorPriority(candidate) > GetIndicatorPriority(bestState))
            {
                bestState = candidate;
            }
        }

        return bestState;
    }

    /// <summary>
    /// Devuelve true si este actor es el target esperado de entrega para una misión que requiere entrega,
    /// sin exigir que la misión ya esté ReadyToTurnIn.
    /// Sirve para el estado pendiente (? gris) mientras la misión todavía está Active.
    /// </summary>
    public static bool IsActorExpectedTurnInTarget(MissionRuntimeState runtimeState, string actorId)
    {
        if (runtimeState == null || runtimeState.Definition == null)
        {
            return false;
        }

        string cleanedActorId = CleanId(actorId);

        if (string.IsNullOrEmpty(cleanedActorId))
        {
            return false;
        }

        MissionDefinition definition = runtimeState.Definition;

        if (definition.CompletionMode != MissionCompletionMode.RequiresTurnIn)
        {
            return false;
        }

        switch (definition.TurnInTargetMode)
        {
            case MissionTurnInTargetMode.OriginalGiver:
                return !string.IsNullOrEmpty(runtimeState.OriginalGiverId) &&
                       string.Equals(runtimeState.OriginalGiverId, cleanedActorId, StringComparison.Ordinal);

            case MissionTurnInTargetMode.SpecificActor:
            case MissionTurnInTargetMode.SpecificWorldObject:
                return !string.IsNullOrEmpty(definition.TurnInTargetId) &&
                       string.Equals(definition.TurnInTargetId, cleanedActorId, StringComparison.Ordinal);

            case MissionTurnInTargetMode.None:
            default:
                return false;
        }
    }

    private static MissionIndicatorState ResolveIndicatorStateForEntry(
        MissionRuntimeState runtimeState,
        MissionActorMissionEntry entry,
        string cleanedActorId,
        bool showPendingTurnInIndicator)
    {
        if (runtimeState.IsReadyToTurnIn && runtimeState.CanTurnInAtTarget(cleanedActorId))
        {
            return MissionIndicatorState.ReadyToTurnIn;
        }

        if (entry.CanOfferMission && runtimeState.IsAvailable)
        {
            return MissionIndicatorState.MissionAvailable;
        }

        if (showPendingTurnInIndicator &&
            runtimeState.IsActive &&
            IsActorExpectedTurnInTarget(runtimeState, cleanedActorId))
        {
            return MissionIndicatorState.TurnInPending;
        }

        return MissionIndicatorState.None;
    }

    private static MissionRuntimeState GetRuntimeState(MissionManager missionManager, MissionActorMissionEntry entry)
    {
        if (missionManager == null || entry == null || !entry.HasValidMission)
        {
            return null;
        }

        return missionManager.GetMissionState(entry.MissionId);
    }

    private static int GetIndicatorPriority(MissionIndicatorState state)
    {
        switch (state)
        {
            case MissionIndicatorState.ReadyToTurnIn:
                return 30;

            case MissionIndicatorState.MissionAvailable:
                return 20;

            case MissionIndicatorState.TurnInPending:
                return 10;

            case MissionIndicatorState.None:
            default:
                return 0;
        }
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
