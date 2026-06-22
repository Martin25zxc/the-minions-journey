using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class MissionManagerDebugTester : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager que queremos probar.")]
    private MissionManager missionManager;

    [SerializeField, Tooltip("Misión usada para aceptar, trackear, entregar o loguear desde este tester.")]
    private MissionDefinition selectedMission;

    [Header("Datos de prueba")]
    [SerializeField, Tooltip("ID del giver original usado al aceptar misiones RequiresTurnIn + OriginalGiver.")]
    private string originalGiverId = "survivor_01";

    [SerializeField, Tooltip("Target usado para intentar entregar la misión seleccionada.")]
    private string turnInTargetId = "survivor_01";

    [SerializeField, Tooltip("Tipo de evento del mundo que se va a reportar con la tecla configurada.")]
    private GameWorldEventType eventType = GameWorldEventType.ArtifactAcquired;

    [SerializeField, Tooltip("TargetId del GameWorldEvent de prueba. Debe coincidir con el TargetId del objetivo.")]
    private string eventTargetId = "hook_artifact";

    [SerializeField, Min(1), Tooltip("Cantidad del GameWorldEvent de prueba.")]
    private int eventAmount = 1;

    [Header("Teclas")]
    [SerializeField, Tooltip("Marca la misión seleccionada como Available.")]
    private Key makeAvailableKey = Key.Digit1;

    [SerializeField, Tooltip("Acepta la misión seleccionada.")]
    private Key acceptMissionKey = Key.Digit2;

    [SerializeField, Tooltip("Reporta el GameWorldEvent configurado.")]
    private Key reportEventKey = Key.Digit3;

    [SerializeField, Tooltip("Intenta entregar la misión seleccionada.")]
    private Key turnInMissionKey = Key.Digit4;

    [SerializeField, Tooltip("Trackea la misión seleccionada.")]
    private Key trackMissionKey = Key.Digit5;

    [SerializeField, Tooltip("Loguea el estado de la misión seleccionada.")]
    private Key logSelectedMissionKey = Key.Digit6;

    [SerializeField, Tooltip("Loguea todas las misiones registradas.")]
    private Key logAllMissionsKey = Key.Digit7;

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (WasPressed(keyboard, makeAvailableKey))
        {
            TryMakeAvailable();
        }

        if (WasPressed(keyboard, acceptMissionKey))
        {
            TryAcceptSelectedMission();
        }

        if (WasPressed(keyboard, reportEventKey))
        {
            ReportConfiguredWorldEvent();
        }

        if (WasPressed(keyboard, turnInMissionKey))
        {
            TryTurnInSelectedMission();
        }

        if (WasPressed(keyboard, trackMissionKey))
        {
            TryTrackSelectedMission();
        }

        if (WasPressed(keyboard, logSelectedMissionKey))
        {
            LogSelectedMissionState();
        }

        if (WasPressed(keyboard, logAllMissionsKey))
        {
            LogAllMissionStates();
        }
    }

    private void TryMakeAvailable()
    {
        if (!CanUseTester())
        {
            return;
        }

        bool changed = missionManager.TryMakeMissionAvailable(selectedMission.MissionId);
        Debug.Log($"TryMakeMissionAvailable({selectedMission.MissionId}) -> {changed}", this);
    }

    private void TryAcceptSelectedMission()
    {
        if (!CanUseTester())
        {
            return;
        }

        bool accepted = missionManager.TryAcceptMission(selectedMission, originalGiverId);
        Debug.Log($"TryAcceptMission({selectedMission.MissionId}) -> {accepted}", this);
    }

    private void ReportConfiguredWorldEvent()
    {
        if (missionManager == null)
        {
            Debug.LogWarning("Falta MissionManager en el tester.", this);
            return;
        }

        GameWorldEvent worldEvent = new GameWorldEvent(eventType, eventTargetId, eventAmount, name);
        bool changed = missionManager.TryReportWorldEvent(worldEvent);
        Debug.Log($"TryReportWorldEvent({worldEvent}) -> {changed}", this);
    }

    private void TryTurnInSelectedMission()
    {
        if (!CanUseTester())
        {
            return;
        }

        bool completed = missionManager.TryTurnInMission(selectedMission.MissionId, turnInTargetId);
        Debug.Log($"TryTurnInMission({selectedMission.MissionId}, {turnInTargetId}) -> {completed}", this);
    }

    private void TryTrackSelectedMission()
    {
        if (!CanUseTester())
        {
            return;
        }

        bool tracked = missionManager.TrySetTrackedMission(selectedMission.MissionId);
        Debug.Log($"TrySetTrackedMission({selectedMission.MissionId}) -> {tracked}", this);
    }

    private void LogSelectedMissionState()
    {
        if (!CanUseTester())
        {
            return;
        }

        MissionRuntimeState state = missionManager.GetMissionState(selectedMission.MissionId);
        LogMissionState(state);
    }

    private void LogAllMissionStates()
    {
        if (missionManager == null)
        {
            Debug.LogWarning("Falta MissionManager en el tester.", this);
            return;
        }

        IReadOnlyList<MissionRuntimeState> missions = missionManager.Missions;

        if (missions.Count == 0)
        {
            Debug.Log("No hay misiones registradas en MissionManager.", this);
            return;
        }

        for (int i = 0; i < missions.Count; i++)
        {
            LogMissionState(missions[i]);
        }
    }

    private void LogMissionState(MissionRuntimeState state)
    {
        if (state == null)
        {
            Debug.Log("MissionRuntimeState null.", this);
            return;
        }

        Debug.Log($"Misión {state.MissionId} | State: {state.State} | Tracked: {state.IsTracked} | Required: {state.GetCompletedRequiredObjectiveCount()}/{state.GetRequiredObjectiveCount()} | Bonus: {state.GetCompletedBonusObjectiveCount()}/{state.GetBonusObjectiveCount()}", this);

        IReadOnlyList<MissionObjectiveRuntimeState> objectives = state.Objectives;

        for (int i = 0; i < objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objective = objectives[i];
            Debug.Log($" - {objective.ObjectiveId}: {objective.GetProgressText()} | Completed: {objective.IsCompleted}", this);
        }
    }

    private bool CanUseTester()
    {
        if (missionManager == null)
        {
            Debug.LogWarning("Falta MissionManager en el tester.", this);
            return false;
        }

        if (selectedMission == null)
        {
            Debug.LogWarning("Falta Selected Mission en el tester.", this);
            return false;
        }

        return true;
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        KeyControl keyControl = keyboard[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }
}
