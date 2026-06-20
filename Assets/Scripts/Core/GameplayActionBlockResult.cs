public readonly struct GameplayActionBlockResult
{
    public bool IsAllowed { get; }
    public string Reason { get; }

    private GameplayActionBlockResult(bool isAllowed, string reason)
    {
        IsAllowed = isAllowed;
        Reason = reason;
    }

    public static GameplayActionBlockResult Allowed()
    {
        return new GameplayActionBlockResult(true, string.Empty);
    }

    public static GameplayActionBlockResult Blocked(string reason)
    {
        return new GameplayActionBlockResult(false, reason);
    }
}
