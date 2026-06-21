using System;

public readonly struct GameWorldEvent
{
    public GameWorldEvent(GameWorldEventType type, string targetId, int amount = 1, string sourceId = "")
    {
        Type = type;
        TargetId = CleanId(targetId);
        Amount = Math.Max(1, amount);
        SourceId = CleanId(sourceId);
    }

    public GameWorldEventType Type { get; }
    public string TargetId { get; }
    public int Amount { get; }
    public string SourceId { get; }

    public bool IsValid => !string.IsNullOrWhiteSpace(TargetId) && Amount > 0;
    public bool HasSource => !string.IsNullOrWhiteSpace(SourceId);

    public static GameWorldEvent AreaReached(string areaId, string sourceId = "")
    {
        return new GameWorldEvent(GameWorldEventType.AreaReached, areaId, 1, sourceId);
    }

    public static GameWorldEvent ItemCollected(string itemId, int amount = 1, string sourceId = "")
    {
        return new GameWorldEvent(GameWorldEventType.ItemCollected, itemId, amount, sourceId);
    }

    public static GameWorldEvent EnemyDefeated(string enemyId, string sourceId = "")
    {
        return new GameWorldEvent(GameWorldEventType.EnemyDefeated, enemyId, 1, sourceId);
    }

    public static GameWorldEvent ObjectInteracted(string objectId, string sourceId = "")
    {
        return new GameWorldEvent(GameWorldEventType.ObjectInteracted, objectId, 1, sourceId);
    }

    public static GameWorldEvent ArtifactAcquired(string artifactId, string sourceId = "")
    {
        return new GameWorldEvent(GameWorldEventType.ArtifactAcquired, artifactId, 1, sourceId);
    }

    public static GameWorldEvent ActorTalkedTo(string actorId, string sourceId = "")
    {
        return new GameWorldEvent(GameWorldEventType.ActorTalkedTo, actorId, 1, sourceId);
    }

    public override string ToString()
    {
        string sourceText = HasSource ? $", SourceId: {SourceId}" : string.Empty;
        return $"Type: {Type}, TargetId: {TargetId}, Amount: {Amount}{sourceText}";
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
