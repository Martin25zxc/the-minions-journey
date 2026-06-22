/// <summary>
/// Datos semánticos del prompt de interacción.
/// Evita que los interactuables devuelvan strings completos como "F - Hablar".
/// </summary>
public readonly struct InteractionPromptData
{
    public InteractionPromptData(InteractionPromptVerb verb, string customLabel = null)
    {
        Verb = verb;
        CustomLabel = customLabel;
    }

    public InteractionPromptVerb Verb { get; }
    public string CustomLabel { get; }

    public bool HasCustomLabel => !string.IsNullOrWhiteSpace(CustomLabel);

    public static InteractionPromptData Default => new(InteractionPromptVerb.Interact);

    public static InteractionPromptData Custom(string customLabel)
    {
        return new InteractionPromptData(InteractionPromptVerb.Custom, customLabel);
    }
}
