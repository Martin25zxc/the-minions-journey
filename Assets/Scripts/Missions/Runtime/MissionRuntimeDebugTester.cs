using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MissionRuntimeDebugTester : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Asset de misión que querés probar en runtime. No se modifica el asset, solo se crea un estado temporal.")]
    private MissionDefinition missionDefinition;

    [Header("Datos de prueba")]
    [SerializeField, Tooltip("ID del giver original para misiones RequiresTurnIn con OriginalGiver. Ejemplo: survivor_01 o nature_being_01.")]
    private string originalGiverId = "survivor_01";

    [SerializeField, Tooltip("ObjectiveId que se va a progresar con la tecla de progreso.")]
    private string objectiveId;

    [SerializeField, Min(1), Tooltip("Cantidad de progreso que suma cada prueba.")]
    private int progressAmount = 1;

    [SerializeField, Tooltip("Target usado para probar TurnIn. Si la misión usa OriginalGiver, debería coincidir con Original Giver Id.")]
    private string turnInTargetId = "survivor_01";

    [SerializeField, Tooltip("Si está activo, el runtime state arranca como Available al crear/reiniciar.")]
    private bool startAsAvailable = true;

    [Header("Teclas de prueba - Input System")]
    [SerializeField, Tooltip("Crea/reinicia el runtime state desde cero.")]
    private Key resetKey = Key.Digit0;

    [SerializeField, Tooltip("Marca la misión como Available.")]
    private Key markAvailableKey = Key.Digit1;

    [SerializeField, Tooltip("Acepta la misión usando Original Giver Id.")]
    private Key acceptKey = Key.Digit2;

    [SerializeField, Tooltip("Aplica progreso al Objective Id configurado.")]
    private Key progressKey = Key.Digit3;

    [SerializeField, Tooltip("Intenta entregar la misión usando Turn In Target Id.")]
    private Key turnInKey = Key.Digit4;

    [SerializeField, Tooltip("Lista el estado actual en consola.")]
    private Key logStateKey = Key.Digit5;

    [SerializeField, Tooltip("Trackea/destrackea la misión runtime de prueba.")]
    private Key toggleTrackedKey = Key.Digit6;

    [Header("Debug")]
    [SerializeField, Tooltip("Muestra logs simples para entender cada transición.")]
    private bool logActions = true;

    private MissionRuntimeState runtimeState;

    private void Awake()
    {
        CreateRuntimeState();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (WasPressed(keyboard, resetKey))
        {
            CreateRuntimeState();
            return;
        }

        if (runtimeState == null)
        {
            return;
        }

        if (WasPressed(keyboard, markAvailableKey))
        {
            MarkAvailable();
        }

        if (WasPressed(keyboard, acceptKey))
        {
            AcceptMission();
        }

        if (WasPressed(keyboard, progressKey))
        {
            ApplyProgress();
        }

        if (WasPressed(keyboard, turnInKey))
        {
            TurnInMission();
        }

        if (WasPressed(keyboard, logStateKey))
        {
            LogRuntimeState();
        }

        if (WasPressed(keyboard, toggleTrackedKey))
        {
            ToggleTracked();
        }
    }

    private void CreateRuntimeState()
    {
        if (missionDefinition == null)
        {
            Debug.LogWarning("Falta asignar MissionDefinition en el tester de runtime.", this);
            runtimeState = null;
            return;
        }

        runtimeState = new MissionRuntimeState(missionDefinition);

        if (string.IsNullOrWhiteSpace(objectiveId) && runtimeState.Objectives.Count > 0)
        {
            objectiveId = runtimeState.Objectives[0].ObjectiveId;
        }

        if (startAsAvailable)
        {
            runtimeState.MarkAvailable();
        }

        if (logActions)
        {
            Debug.Log($"Runtime creado para misión '{runtimeState.MissionId}'. Estado inicial: {runtimeState.State}", this);
        }
    }

    private void MarkAvailable()
    {
        bool changed = runtimeState.MarkAvailable();
        LogAction(changed ? "Misión marcada como Available." : $"No se pudo marcar Available. Estado actual: {runtimeState.State}");
    }

    private void AcceptMission()
    {
        bool accepted = runtimeState.Accept(originalGiverId, Time.time);
        LogAction(accepted ? $"Misión aceptada. Estado: {runtimeState.State}" : $"No se pudo aceptar. Estado: {runtimeState.State}. Revisar giver si usa OriginalGiver.");
    }

    private void ApplyProgress()
    {
        MissionProgressResult result = runtimeState.ApplyObjectiveProgress(objectiveId, progressAmount, Time.time);

        if (!result.HasAnyChange)
        {
            LogAction($"Sin cambios: {result.Message}");
            return;
        }

        LogAction(result.Message);

        if (result.ObjectiveCompleted)
        {
            LogAction($"ObjectiveCompleted listo para que MissionManager futuro emita evento: {result.ObjectiveState.ObjectiveId}");
        }

        if (result.MissionBecameReadyToTurnIn)
        {
            LogAction("La misión quedó ReadyToTurnIn.");
        }

        if (result.MissionCompleted)
        {
            LogAction("La misión quedó Completed.");
        }
    }

    private void TurnInMission()
    {
        bool completed = runtimeState.TurnIn(turnInTargetId, Time.time);
        LogAction(completed ? "TurnIn correcto. Misión completada." : $"TurnIn rechazado. Estado: {runtimeState.State}. Target usado: '{turnInTargetId}'.");
    }

    private void ToggleTracked()
    {
        bool changed = runtimeState.SetTracked(!runtimeState.IsTracked);
        LogAction(changed ? $"Tracked cambiado a {runtimeState.IsTracked}." : "Tracked no cambió.");
    }

    private void LogRuntimeState()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Misión: {runtimeState.MissionId}");
        builder.AppendLine($"Estado: {runtimeState.State}");
        builder.AppendLine($"Tracked: {runtimeState.IsTracked}");
        builder.AppendLine($"OriginalGiverId: {runtimeState.OriginalGiverId}");
        builder.AppendLine($"AcceptedAtTime: {runtimeState.AcceptedAtTime}");
        builder.AppendLine($"ReadyToTurnInAtTime: {runtimeState.ReadyToTurnInAtTime}");
        builder.AppendLine($"CompletedAtTime: {runtimeState.CompletedAtTime}");
        builder.AppendLine($"Required: {runtimeState.GetCompletedRequiredObjectiveCount()}/{runtimeState.GetRequiredObjectiveCount()}");
        builder.AppendLine($"Bonus: {runtimeState.GetCompletedBonusObjectiveCount()}/{runtimeState.GetBonusObjectiveCount()}");
        builder.AppendLine("Objetivos:");

        for (int i = 0; i < runtimeState.Objectives.Count; i++)
        {
            MissionObjectiveRuntimeState objective = runtimeState.Objectives[i];
            builder.AppendLine($"- {objective.ObjectiveId}: {objective.GetProgressText()} | Completed: {objective.IsCompleted} | CompletionEventEmitted: {objective.CompletionEventEmitted}");
        }

        Debug.Log(builder.ToString(), this);
    }

    private void LogAction(string message)
    {
        if (logActions)
        {
            Debug.Log(message, this);
        }
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        if (key == Key.None)
        {
            return false;
        }

        return keyboard[key].wasPressedThisFrame;
    }
}
