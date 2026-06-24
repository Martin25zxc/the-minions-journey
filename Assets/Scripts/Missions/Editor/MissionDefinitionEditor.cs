using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MissionDefinition))]
public sealed class MissionDefinitionEditor : Editor
{
    private SerializedProperty missionId;
    private SerializedProperty category;
    private SerializedProperty displayOrder;
    private SerializedProperty title;
    private SerializedProperty shortDescription;
    private SerializedProperty fullDescription;
    private SerializedProperty startMode;
    private SerializedProperty completionMode;
    private SerializedProperty turnInTargetMode;
    private SerializedProperty turnInTargetId;
    private SerializedProperty requiredForLevelCompletion;
    private SerializedProperty objectives;
    private SerializedProperty rewards;
    private SerializedProperty showInHUD;
    private SerializedProperty showInJournal;
    private SerializedProperty autoTrackOnAccept;

    private void OnEnable()
    {
        missionId = serializedObject.FindProperty("missionId");
        category = serializedObject.FindProperty("category");
        displayOrder = serializedObject.FindProperty("displayOrder");
        title = serializedObject.FindProperty("title");
        shortDescription = serializedObject.FindProperty("shortDescription");
        fullDescription = serializedObject.FindProperty("fullDescription");
        startMode = serializedObject.FindProperty("startMode");
        completionMode = serializedObject.FindProperty("completionMode");
        turnInTargetMode = serializedObject.FindProperty("turnInTargetMode");
        turnInTargetId = serializedObject.FindProperty("turnInTargetId");
        requiredForLevelCompletion = serializedObject.FindProperty("requiredForLevelCompletion");
        objectives = serializedObject.FindProperty("objectives");
        rewards = serializedObject.FindProperty("rewards");
        showInHUD = serializedObject.FindProperty("showInHUD");
        showInJournal = serializedObject.FindProperty("showInJournal");
        autoTrackOnAccept = serializedObject.FindProperty("autoTrackOnAccept");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSectionTitle("Identidad");
        EditorGUILayout.PropertyField(missionId, new GUIContent("Mission Id", "ID estable de la misión. Usar snake_case. Ejemplo: main_01_01_find_hook."));
        EditorGUILayout.PropertyField(category, new GUIContent("Category", "Main para progreso central. Optional para contenido no obligatorio."));
        EditorGUILayout.PropertyField(displayOrder, new GUIContent("Display Order", "Orden sugerido para Journal o listas futuras. Menor aparece antes."));

        DrawSectionTitle("Texto");
        EditorGUILayout.PropertyField(title, new GUIContent("Title", "Título visible de la misión."));
        EditorGUILayout.PropertyField(shortDescription, new GUIContent("Short Description", "Resumen corto para HUD o lista de misiones."));
        EditorGUILayout.PropertyField(fullDescription, new GUIContent("Full Description", "Descripción más completa para Journal futuro."));

        DrawSectionTitle("Flujo");
        EditorGUILayout.PropertyField(startMode, new GUIContent("Start Mode", "Cómo se inicia esta misión. En esta etapa solo se define el dato."));
        EditorGUILayout.PropertyField(completionMode, new GUIContent("Completion Mode", "AutoComplete completa al terminar objetivos required. RequiresTurnIn pasa a ReadyToTurnIn y pide una entrega/interacción final."));

        MissionCompletionMode selectedCompletionMode = (MissionCompletionMode)completionMode.intValue;

        if (selectedCompletionMode == MissionCompletionMode.RequiresTurnIn)
        {
            EditorGUILayout.PropertyField(turnInTargetMode, new GUIContent("Turn In Target Mode", "Dónde se entrega la misión cuando ya está ReadyToTurnIn."));

            MissionTurnInTargetMode selectedTurnInTargetMode = (MissionTurnInTargetMode)turnInTargetMode.intValue;
            if (selectedTurnInTargetMode == MissionTurnInTargetMode.SpecificActor ||
                selectedTurnInTargetMode == MissionTurnInTargetMode.SpecificWorldObject)
            {
                EditorGUILayout.PropertyField(turnInTargetId, new GUIContent("Turn In Target Id", "ID estable del actor u objeto específico de entrega. Ejemplo: nature_being o ancient_gate."));
            }
            else if (selectedTurnInTargetMode == MissionTurnInTargetMode.OriginalGiver)
            {
                EditorGUILayout.HelpBox("La entrega vuelve al giver original. Turn In Target Id no hace falta.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Requires Turn In necesita un target válido. Usá OriginalGiver, SpecificActor o SpecificWorldObject.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Esta misión es Auto Complete: al completar sus objetivos required se cierra sola. No usa Turn In Target Mode ni Turn In Target Id.", MessageType.Info);
        }

        EditorGUILayout.PropertyField(requiredForLevelCompletion, new GUIContent("Required For Level Completion", "Marca si esta misión cuenta para completar el nivel. No usar para fases internas si el nivel continúa."));

        DrawSectionTitle("Objetivos");
        EditorGUILayout.PropertyField(objectives, new GUIContent("Objectives", "Objetivos de autoría. No guardan progreso runtime."), true);

        DrawSectionTitle("Recompensas");
        EditorGUILayout.PropertyField(rewards, new GUIContent("Rewards", "Recompensas que se entregan cuando la misión llega a Completed. También pueden mostrarse en Journal futuro."), true);

        DrawSectionTitle("UI");
        EditorGUILayout.PropertyField(showInHUD, new GUIContent("Show In HUD", "Si está activo, esta misión puede aparecer en el HUD cuando esté trackeada."));
        EditorGUILayout.PropertyField(showInJournal, new GUIContent("Show In Journal", "Si está activo, esta misión puede aparecer en el Journal futuro."));
        EditorGUILayout.PropertyField(autoTrackOnAccept, new GUIContent("Auto Track On Accept", "Si está activo, la misión puede trackearse automáticamente al aceptarse."));

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSectionTitle(string label)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }
}
