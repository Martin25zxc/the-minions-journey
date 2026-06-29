using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton de progreso de partida.
///
/// [DefaultExecutionOrder] garantiza que su Awake() corra antes que el de
/// cualquier script sin el atributo, sin depender de la posición en la
/// jerarquía (que Unity NO garantiza como orden de inicialización).
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Habilidades adquiridas")]
    [SerializeField] private List<string> acquiredSkillIDs = new();
    private HashSet<string> acquiredSet;

    [Header("Respawn")]
    public Vector3 respawnPoint;

    /// <summary>Se dispara cuando se adquiere una skill nueva (ej. recompensa de misión).</summary>
    public event Action<string> OnSkillAcquired;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RebuildAcquiredSetFromSerializedList();
    }

    public bool IsAcquired(string skillID)
    {
        EnsureAcquiredSet();
        string normalizedId = NormalizeSkillId(skillID);
        return !string.IsNullOrEmpty(normalizedId) && acquiredSet.Contains(normalizedId);
    }

    /// <summary>
    /// Intenta adquirir una skill.
    /// Devuelve true solo cuando la skill se agrega por primera vez.
    /// Esto permite que rewards/notificaciones eviten mensajes duplicados.
    /// </summary>
    public bool TryAcquire(string skillID)
    {
        EnsureAcquiredSet();

        string normalizedId = NormalizeSkillId(skillID);
        if (string.IsNullOrEmpty(normalizedId))
        {
            Debug.LogWarning("GameProgressManager.TryAcquire recibió un skillID vacío o inválido.", this);
            return false;
        }

        if (!acquiredSet.Add(normalizedId))
        {
            return false; // ya la tenía, no notifica de nuevo
        }

        acquiredSkillIDs.Add(normalizedId);
        OnSkillAcquired?.Invoke(normalizedId);
        return true;
    }

    /// <summary>
    /// Compatibilidad con llamadas existentes y posibles UnityEvents.
    /// Si necesitás saber si realmente se adquirió algo nuevo, usá TryAcquire.
    /// </summary>
    public void Acquire(string skillID)
    {
        TryAcquire(skillID);
    }

    public void SetRespawnPoint(Vector3 point) => respawnPoint = point;

    public Vector3 GetRespawnPoint() => respawnPoint;

    private void EnsureAcquiredSet()
    {
        if (acquiredSet == null)
        {
            RebuildAcquiredSetFromSerializedList();
        }
    }

    private void RebuildAcquiredSetFromSerializedList()
    {
        acquiredSet = new HashSet<string>(StringComparer.Ordinal);

        if (acquiredSkillIDs == null)
        {
            acquiredSkillIDs = new List<string>();
            return;
        }

        // Limpia entradas vacías/duplicadas preservando el orden autoral de la lista serializada.
        for (int i = acquiredSkillIDs.Count - 1; i >= 0; i--)
        {
            string normalizedId = NormalizeSkillId(acquiredSkillIDs[i]);

            if (string.IsNullOrEmpty(normalizedId) || acquiredSet.Contains(normalizedId))
            {
                acquiredSkillIDs.RemoveAt(i);
                continue;
            }

            acquiredSkillIDs[i] = normalizedId;
            acquiredSet.Add(normalizedId);
        }

        acquiredSkillIDs.Reverse();
    }

    private static string NormalizeSkillId(string skillID)
    {
        return string.IsNullOrWhiteSpace(skillID) ? string.Empty : skillID.Trim();
    }

    // TODO: acá después conectás tu sistema de save/load real
    // (JSON, PlayerPrefs, lo que uses) para persistir acquiredSkillIDs y respawnPoint.
}
