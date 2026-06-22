public readonly struct WorldFlagChangedEventArgs
{
    public string FlagId { get; }
    public bool IsSet { get; }
    public bool PreviousValue { get; }

    public WorldFlagChangedEventArgs(string flagId, bool isSet, bool previousValue)
    {
        FlagId = flagId;
        IsSet = isSet;
        PreviousValue = previousValue;
    }
}
