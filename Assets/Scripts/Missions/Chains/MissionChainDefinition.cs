using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TMJ/Missions/Mission Chain Definition", fileName = "MCD_NewMissionChain")]
public sealed class MissionChainDefinition : ScriptableObject
{
    [Header("Identidad")]
    [SerializeField, Tooltip("ID estable de la cadena. Usar snake_case. Ejemplo: level01_main_chain.")]
    private string chainId;

    [SerializeField, Tooltip("Nombre visible para identificar esta cadena en Inspector o logs.")]
    private string displayName;

    [Header("Reglas")]
    [SerializeField, Tooltip("Lista de conexiones entre misiones. Una misma misión fuente puede tener varias reglas salientes.")]
    private MissionChainRule[] rules = Array.Empty<MissionChainRule>();

    public string ChainId => CleanId(chainId);
    public string DisplayName => displayName;
    public IReadOnlyList<MissionChainRule> Rules => rules ?? Array.Empty<MissionChainRule>();

    private void OnValidate()
    {
        chainId = CleanId(chainId);

        if (rules == null)
        {
            rules = Array.Empty<MissionChainRule>();
        }

        if (string.IsNullOrEmpty(chainId))
        {
            Debug.LogWarning($"{name}: falta ChainId. Usá un ID estable, por ejemplo: level01_main_chain.", this);
        }

        HashSet<string> duplicateCheck = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < rules.Length; i++)
        {
            MissionChainRule rule = rules[i];

            if (rule == null)
            {
                Debug.LogWarning($"{name}: Rule #{i} está vacío/null.", this);
                continue;
            }

            rule.Validate(this, name, i);

            if (!rule.IsValid)
            {
                continue;
            }

            string duplicateKey = $"{rule.SourceMissionId}|{rule.Trigger}|{rule.Action}|{rule.TargetMissionId}";
            if (!duplicateCheck.Add(duplicateKey))
            {
                Debug.LogWarning($"{name}: regla duplicada detectada: {duplicateKey}.", this);
            }
        }
    }

    private static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
