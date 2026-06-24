using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MissionRewardDefinition))]
public sealed class MissionRewardDefinitionDrawer : PropertyDrawer
{
    private const float HelpBoxHeight = 38f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect rect = position;
        rect.height = EditorGUIUtility.singleLineHeight;

        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, BuildFoldoutLabel(property, label), true);
        rect.y += LineHeight();

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        SerializedProperty rewardType = property.FindPropertyRelative("rewardType");
        DrawProperty(ref rect, property, "rewardType", "Reward Type");

        MissionRewardType selectedType = GetSelectedRewardType(rewardType);

        switch (selectedType)
        {
            case MissionRewardType.None:
                DrawHelpBox(ref rect, "Sin recompensa. Usar solo si querés dejar un slot vacío temporalmente.", MessageType.Info);
                break;

            case MissionRewardType.SkillUnlock:
                DrawProperty(ref rect, property, "skill", "Skill");
                DrawHelpIfMissingObject(ref rect, property, "skill", "SkillUnlock necesita un SkillData asignado.");
                break;

            case MissionRewardType.Item:
                DrawProperty(ref rect, property, "itemId", "Item Id");
                DrawProperty(ref rect, property, "quantity", "Quantity");
                break;

            case MissionRewardType.Currency:
                DrawProperty(ref rect, property, "currencyId", "Currency Id");
                DrawProperty(ref rect, property, "quantity", "Quantity");
                break;

            case MissionRewardType.InstantHeal:
                DrawProperty(ref rect, property, "fullHeal", "Full Heal");

                SerializedProperty fullHeal = property.FindPropertyRelative("fullHeal");
                if (fullHeal != null && !fullHeal.boolValue)
                {
                    DrawProperty(ref rect, property, "healAmount", "Heal Amount");
                }
                break;

            case MissionRewardType.WorldEvent:
                DrawProperty(ref rect, property, "worldEventId", "World Event Id");
                break;

            default:
                DrawHelpBox(ref rect, $"RewardType no soportado: {selectedType}", MessageType.Warning);
                break;
        }

        DrawSeparator(ref rect);

        EditorGUI.LabelField(rect, "Presentación", EditorStyles.boldLabel);
        rect.y += LineHeight();

        DrawProperty(ref rect, property, "showInJournal", "Show In Journal");
        DrawProperty(ref rect, property, "notifyOnGrant", "Notify On Grant");
        DrawProperty(ref rect, property, "displayNameOverride", "Display Name Override");
        DrawProperty(ref rect, property, "iconOverride", "Icon Override");
        DrawProperty(ref rect, property, "descriptionOverride", "Description Override", true);

        DrawSeparator(ref rect);

        EditorGUI.LabelField(rect, "Diseño", EditorStyles.boldLabel);
        rect.y += LineHeight();

        DrawProperty(ref rect, property, "designerNote", "Designer Note", true);

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = LineHeight();

        if (!property.isExpanded)
        {
            return height;
        }

        SerializedProperty rewardType = property.FindPropertyRelative("rewardType");
        MissionRewardType selectedType = GetSelectedRewardType(rewardType);

        height += PropertyHeight(property, "rewardType");

        switch (selectedType)
        {
            case MissionRewardType.None:
                height += HelpBoxHeight + Spacing();
                break;

            case MissionRewardType.SkillUnlock:
                height += PropertyHeight(property, "skill");

                SerializedProperty skill = property.FindPropertyRelative("skill");
                if (skill != null && skill.objectReferenceValue == null)
                {
                    height += HelpBoxHeight + Spacing();
                }
                break;

            case MissionRewardType.Item:
                height += PropertyHeight(property, "itemId");
                height += PropertyHeight(property, "quantity");
                break;

            case MissionRewardType.Currency:
                height += PropertyHeight(property, "currencyId");
                height += PropertyHeight(property, "quantity");
                break;

            case MissionRewardType.InstantHeal:
                height += PropertyHeight(property, "fullHeal");

                SerializedProperty fullHeal = property.FindPropertyRelative("fullHeal");
                if (fullHeal != null && !fullHeal.boolValue)
                {
                    height += PropertyHeight(property, "healAmount");
                }
                break;

            case MissionRewardType.WorldEvent:
                height += PropertyHeight(property, "worldEventId");
                break;
        }

        height += Spacing() + LineHeight();
        height += PropertyHeight(property, "showInJournal");
        height += PropertyHeight(property, "notifyOnGrant");
        height += PropertyHeight(property, "displayNameOverride");
        height += PropertyHeight(property, "iconOverride");
        height += PropertyHeight(property, "descriptionOverride");

        height += Spacing() + LineHeight();
        height += PropertyHeight(property, "designerNote");

        return height;
    }

    private static MissionRewardType GetSelectedRewardType(SerializedProperty rewardType)
    {
        if (rewardType == null)
        {
            return MissionRewardType.None;
        }

        // Para enums con valores explícitos, enumValueIndex devuelve el índice visual del dropdown.
        // intValue devuelve el valor real del enum: SkillUnlock = 10, Item = 20, etc.
        return (MissionRewardType)rewardType.intValue;
    }

    private static GUIContent BuildFoldoutLabel(SerializedProperty property, GUIContent fallback)
    {
        SerializedProperty rewardType = property.FindPropertyRelative("rewardType");
        SerializedProperty displayNameOverride = property.FindPropertyRelative("displayNameOverride");
        SerializedProperty skill = property.FindPropertyRelative("skill");

        string title = fallback.text;

        if (rewardType != null && rewardType.enumDisplayNames.Length > rewardType.enumValueIndex)
        {
            title = rewardType.enumDisplayNames[rewardType.enumValueIndex];
        }

        if (displayNameOverride != null && !string.IsNullOrWhiteSpace(displayNameOverride.stringValue))
        {
            title += $" - {displayNameOverride.stringValue}";
        }
        else if (skill != null && skill.objectReferenceValue != null)
        {
            title += $" - {skill.objectReferenceValue.name}";
        }

        return new GUIContent(title);
    }

    private static void DrawProperty(
        ref Rect rect,
        SerializedProperty root,
        string propertyName,
        string label,
        bool includeChildren = false)
    {
        SerializedProperty child = root.FindPropertyRelative(propertyName);
        if (child == null)
        {
            return;
        }

        float height = EditorGUI.GetPropertyHeight(child, includeChildren);
        rect.height = height;

        EditorGUI.PropertyField(rect, child, new GUIContent(label), includeChildren);
        rect.y += height + Spacing();
    }

    private static void DrawHelpIfMissingObject(
        ref Rect rect,
        SerializedProperty root,
        string propertyName,
        string message)
    {
        SerializedProperty child = root.FindPropertyRelative(propertyName);
        if (child == null || child.objectReferenceValue != null)
        {
            return;
        }

        DrawHelpBox(ref rect, message, MessageType.Warning);
    }

    private static void DrawHelpBox(ref Rect rect, string message, MessageType messageType)
    {
        rect.height = HelpBoxHeight;
        EditorGUI.HelpBox(rect, message, messageType);
        rect.y += HelpBoxHeight + Spacing();
    }

    private static void DrawSeparator(ref Rect rect)
    {
        rect.y += Spacing();
    }

    private static float PropertyHeight(SerializedProperty root, string propertyName)
    {
        SerializedProperty child = root.FindPropertyRelative(propertyName);
        if (child == null)
        {
            return 0f;
        }

        return EditorGUI.GetPropertyHeight(child, true) + Spacing();
    }

    private static float LineHeight()
    {
        return EditorGUIUtility.singleLineHeight + Spacing();
    }

    private static float Spacing()
    {
        return EditorGUIUtility.standardVerticalSpacing;
    }
}
