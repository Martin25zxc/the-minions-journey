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

        acquiredSet = new HashSet<string>(acquiredSkillIDs);
    }

    public bool IsAcquired(string skillID) => acquiredSet.Contains(skillID);

    public void Acquire(string skillID)
    {
        if (!acquiredSet.Add(skillID)) return; // ya la tenía, no notifica de nuevo

        acquiredSkillIDs.Add(skillID);
        OnSkillAcquired?.Invoke(skillID);
    }

    public void SetRespawnPoint(Vector3 point) => respawnPoint = point;

    public Vector3 GetRespawnPoint() => respawnPoint;

    // TODO: acá después conectás tu sistema de save/load real
    // (JSON, PlayerPrefs, lo que uses) para persistir acquiredSkillIDs y respawnPoint.
}