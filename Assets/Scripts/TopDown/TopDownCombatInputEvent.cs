public readonly struct TopDownCombatInputEvent
{
    public TopDownCombatInputEvent(TopDownCombatInputAction action, float time)
    {
        Action = action;
        Time = time;
    }

    public TopDownCombatInputAction Action { get; }

    public float Time { get; }
}