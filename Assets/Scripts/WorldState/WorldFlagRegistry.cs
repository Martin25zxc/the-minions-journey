using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldFlagRegistry : MonoBehaviour
{
    private static WorldFlagRegistry activeRegistry;

    [Header("Flags iniciales")]
    [Tooltip("Flags que arrancan activas al iniciar la escena. Usalo solo para pruebas o estado inicial claro de la escena.")]
    [SerializeField] private List<string> initialFlags = new();

    [Header("Validación")]
    [Tooltip("Muestra advertencias si un flag no sigue la convención snake_case recomendada.")]
    [SerializeField] private bool warnIfFlagIdIsNotSnakeCase = true;

    [Header("Debug")]
    [Tooltip("Muestra logs cuando cambia un flag. Útil mientras armamos misiones y responders.")]
    [SerializeField] private bool logChanges;

    [Tooltip("Lista solo para mirar en el Inspector. La fuente real es el HashSet interno, no edites esto a mano.")]
    [SerializeField] private List<string> activeFlagsDebug = new();

    private readonly HashSet<string> activeFlags = new(StringComparer.Ordinal);

    public int ActiveFlagCount => activeFlags.Count;

    public event Action<WorldFlagChangedEventArgs> FlagChanged;
    public event Action<string> FlagSet;
    public event Action<string> FlagRemoved;

    private void Awake()
    {
        ValidateSingleActiveRegistry();
        LoadInitialFlags();
    }

    private void OnDestroy()
    {
        if (activeRegistry == this)
        {
            activeRegistry = null;
        }
    }

    public bool HasFlag(string flagId)
    {
        return TryNormalizeFlagId(flagId, out string normalizedFlagId) && activeFlags.Contains(normalizedFlagId);
    }

    public bool TryGetFlag(string flagId, out bool value)
    {
        value = HasFlag(flagId);
        return TryNormalizeFlagId(flagId, out _);
    }

    public bool SetFlag(string flagId, bool value = true)
    {
        if (!value)
        {
            return RemoveFlag(flagId);
        }

        if (!TryNormalizeFlagId(flagId, out string normalizedFlagId))
        {
            return false;
        }

        if (!activeFlags.Add(normalizedFlagId))
        {
            return false;
        }

        RefreshDebugList();
        LogFlagChange(normalizedFlagId, true);

        WorldFlagChangedEventArgs eventArgs = new(normalizedFlagId, true, false);
        FlagChanged?.Invoke(eventArgs);
        FlagSet?.Invoke(normalizedFlagId);

        return true;
    }

    public bool RemoveFlag(string flagId)
    {
        if (!TryNormalizeFlagId(flagId, out string normalizedFlagId))
        {
            return false;
        }

        if (!activeFlags.Remove(normalizedFlagId))
        {
            return false;
        }

        RefreshDebugList();
        LogFlagChange(normalizedFlagId, false);

        WorldFlagChangedEventArgs eventArgs = new(normalizedFlagId, false, true);
        FlagChanged?.Invoke(eventArgs);
        FlagRemoved?.Invoke(normalizedFlagId);

        return true;
    }

    public bool ToggleFlag(string flagId)
    {
        if (!TryNormalizeFlagId(flagId, out string normalizedFlagId))
        {
            return false;
        }

        return activeFlags.Contains(normalizedFlagId)
            ? RemoveFlag(normalizedFlagId)
            : SetFlag(normalizedFlagId);
    }

    public void ClearAllFlags()
    {
        if (activeFlags.Count == 0)
        {
            return;
        }

        string[] flagsToRemove = new string[activeFlags.Count];
        activeFlags.CopyTo(flagsToRemove);

        foreach (string flagId in flagsToRemove)
        {
            RemoveFlag(flagId);
        }
    }

    public IReadOnlyList<string> GetActiveFlagsSnapshot()
    {
        RefreshDebugList();
        return activeFlagsDebug.AsReadOnly();
    }

    [ContextMenu("Debug/Listar flags activos")]
    public void DebugLogActiveFlags()
    {
        RefreshDebugList();

        if (activeFlagsDebug.Count == 0)
        {
            Debug.Log("No hay WorldFlags activos.", this);
            return;
        }

        Debug.Log($"WorldFlags activos ({activeFlagsDebug.Count}):\n- {string.Join("\n- ", activeFlagsDebug)}", this);
    }

    [ContextMenu("Debug/Limpiar flags activos")]
    private void DebugClearAllFlags()
    {
        ClearAllFlags();
    }

    private void LoadInitialFlags()
    {
        activeFlags.Clear();

        for (int i = 0; i < initialFlags.Count; i++)
        {
            if (!TryNormalizeFlagId(initialFlags[i], out string normalizedFlagId))
            {
                continue;
            }

            activeFlags.Add(normalizedFlagId);
        }

        RefreshDebugList();
    }

    private void ValidateSingleActiveRegistry()
    {
        if (activeRegistry != null && activeRegistry != this)
        {
            Debug.LogError(
                "Hay más de un WorldFlagRegistry activo en la escena. " +
                "Para esta etapa usamos uno solo en _GameSystems y referencias por Inspector, no singleton global.",
                this);
            return;
        }

        activeRegistry = this;
    }

    private bool TryNormalizeFlagId(string flagId, out string normalizedFlagId)
    {
        normalizedFlagId = string.Empty;

        if (string.IsNullOrWhiteSpace(flagId))
        {
            Debug.LogWarning("Se intentó usar un WorldFlag vacío o null. Se ignoró.", this);
            return false;
        }

        normalizedFlagId = flagId.Trim();

        if (warnIfFlagIdIsNotSnakeCase && !LooksLikeSnakeCase(normalizedFlagId))
        {
            Debug.LogWarning(
                $"El WorldFlag '{normalizedFlagId}' no parece seguir snake_case. " +
                "Recomendado: hook_received, old_gate_opened, nature_being_helped.",
                this);
        }

        return true;
    }

    private static bool LooksLikeSnakeCase(string value)
    {
        bool previousWasUnderscore = false;

        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];

            if (character == '_')
            {
                if (i == 0 || i == value.Length - 1 || previousWasUnderscore)
                {
                    return false;
                }

                previousWasUnderscore = true;
                continue;
            }

            previousWasUnderscore = false;

            if (character >= 'a' && character <= 'z')
            {
                continue;
            }

            if (character >= '0' && character <= '9')
            {
                continue;
            }

            return false;
        }

        return value.Length > 0;
    }

    private void RefreshDebugList()
    {
        activeFlagsDebug.Clear();
        activeFlagsDebug.AddRange(activeFlags);
        activeFlagsDebug.Sort(StringComparer.Ordinal);
    }

    private void LogFlagChange(string flagId, bool isSet)
    {
        if (!logChanges)
        {
            return;
        }

        string stateText = isSet ? "activado" : "desactivado";
        Debug.Log($"WorldFlag {stateText}: {flagId}", this);
    }

    private void OnValidate()
    {
        NormalizeInitialFlagsInInspector();
    }

    private void NormalizeInitialFlagsInInspector()
    {
        if (initialFlags == null)
        {
            initialFlags = new List<string>();
            return;
        }

        HashSet<string> seenFlags = new(StringComparer.Ordinal);

        for (int i = initialFlags.Count - 1; i >= 0; i--)
        {
            string rawFlagId = initialFlags[i];

            if (string.IsNullOrWhiteSpace(rawFlagId))
            {
                initialFlags.RemoveAt(i);
                continue;
            }

            string normalizedFlagId = rawFlagId.Trim();
            initialFlags[i] = normalizedFlagId;

            if (!seenFlags.Add(normalizedFlagId))
            {
                initialFlags.RemoveAt(i);
            }
        }
    }
}
