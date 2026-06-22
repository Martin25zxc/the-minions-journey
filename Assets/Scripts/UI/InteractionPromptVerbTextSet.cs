using System;
using UnityEngine;

[Serializable]
public sealed class InteractionPromptVerbTextSet
{
    [SerializeField] private string interact = "Interactuar";
    [SerializeField] private string talk = "Hablar";
    [SerializeField] private string examine = "Examinar";
    [SerializeField] private string open = "Abrir";
    [SerializeField] private string activate = "Activar";
    [SerializeField] private string read = "Leer";
    [SerializeField] private string use = "Usar";
    [SerializeField] private string fallback = "Interactuar";

    public string GetLabel(InteractionPromptData promptData)
    {
        if (promptData.Verb == InteractionPromptVerb.Custom)
        {
            return promptData.HasCustomLabel ? promptData.CustomLabel : fallback;
        }

        return promptData.Verb switch
        {
            InteractionPromptVerb.Interact => interact,
            InteractionPromptVerb.Talk => talk,
            InteractionPromptVerb.Examine => examine,
            InteractionPromptVerb.Open => open,
            InteractionPromptVerb.Activate => activate,
            InteractionPromptVerb.Read => read,
            InteractionPromptVerb.Use => use,
            _ => fallback
        };
    }
}
