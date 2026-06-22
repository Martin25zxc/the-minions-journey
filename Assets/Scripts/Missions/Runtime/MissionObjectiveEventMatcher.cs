using System;

public static class MissionObjectiveEventMatcher
{
    public static bool Matches(MissionObjectiveDefinition objectiveDefinition, GameWorldEvent worldEvent)
    {
        if (objectiveDefinition == null || !worldEvent.IsValid)
        {
            return false;
        }

        if (!MatchesType(objectiveDefinition.ObjectiveType, worldEvent.Type))
        {
            return false;
        }

        string objectiveTargetId = CleanId(objectiveDefinition.TargetId);
        string eventTargetId = CleanId(worldEvent.TargetId);

        if (string.IsNullOrEmpty(objectiveTargetId) || string.IsNullOrEmpty(eventTargetId))
        {
            return false;
        }

        return string.Equals(objectiveTargetId, eventTargetId, StringComparison.Ordinal);
    }

    public static bool MatchesType(MissionObjectiveType objectiveType, GameWorldEventType eventType)
    {
        switch (objectiveType)
        {
            case MissionObjectiveType.ReachArea:
                return eventType == GameWorldEventType.AreaReached;

            case MissionObjectiveType.CollectItem:
                return eventType == GameWorldEventType.ItemCollected;

            case MissionObjectiveType.DefeatEnemies:
                return eventType == GameWorldEventType.EnemyDefeated;

            case MissionObjectiveType.InteractWithObject:
                return eventType == GameWorldEventType.ObjectInteracted;

            case MissionObjectiveType.AcquireItem:
                return eventType == GameWorldEventType.ArtifactAcquired ||
                       eventType == GameWorldEventType.ItemCollected;

            case MissionObjectiveType.TalkToActor:
                return eventType == GameWorldEventType.ActorTalkedTo;

            default:
                return false;
        }
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
