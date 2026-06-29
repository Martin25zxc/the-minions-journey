using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionMusicTriggerMoment
{
    OnAccepted = 0,
    OnCompleted = 1
}

[DisallowMultipleComponent]
public sealed class MissionMusicResponder : MonoBehaviour
{
    [Serializable]
    private sealed class MissionMusicRule
    {
        [Header("Identidad")]
        [SerializeField]
        private string ruleName = "Music Rule";

        [Header("Trigger")]
        [SerializeField]
        private MissionDefinition mission;

        [SerializeField]
        private MissionMusicTriggerMoment triggerMoment = MissionMusicTriggerMoment.OnAccepted;

        [Header("Música")]
        [SerializeField]
        private AudioClip musicClip;

        [SerializeField, Min(0f)]
        private float fadeDuration = 1f;

        [Header("Ejecución")]
        [SerializeField]
        private bool executeOnlyOnce = true;

        [SerializeField, Tooltip("Solo lectura en Play Mode. Se resetea al cargar la escena.")]
        private bool hasExecuted;

        public string RuleName => string.IsNullOrWhiteSpace(ruleName) ? "Music Rule" : ruleName;
        public bool ExecuteOnlyOnce => executeOnlyOnce;
        public bool HasExecuted => hasExecuted;
        public AudioClip MusicClip => musicClip;
        public float FadeDuration => fadeDuration;

        public void ResetRuntimeState()
        {
            hasExecuted = false;
        }

        public bool Matches(MissionRuntimeState missionState, MissionMusicTriggerMoment incomingMoment)
        {
            if (missionState == null || mission == null)
            {
                return false;
            }

            if (triggerMoment != incomingMoment)
            {
                return false;
            }

            return string.Equals(missionState.MissionId, mission.MissionId, StringComparison.Ordinal);
        }

        public void MarkExecuted()
        {
            hasExecuted = true;
        }
    }

    [Header("Referencias")]
    [SerializeField, Tooltip("MissionManager de la escena. Si queda vacío, se intenta resolver automáticamente.")]
    private MissionManager missionManager;

    [Header("Reglas")]
    [SerializeField]
    private List<MissionMusicRule> rules = new List<MissionMusicRule>();

    [Header("Debug")]
    [SerializeField]
    private bool logDebug;

    private bool subscribed;

    private void Reset()
    {
        missionManager = FindFirstObjectByType<MissionManager>();
    }

    private void Awake()
    {
        ResetRuntimeRuleState();
    }

    private void OnEnable()
    {
        if (missionManager == null)
        {
            missionManager = FindFirstObjectByType<MissionManager>();
        }

        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void ResetRuntimeRuleState()
    {
        if (rules == null)
        {
            return;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            rules[i]?.ResetRuntimeState();
        }
    }

    private void Subscribe()
    {
        if (subscribed || missionManager == null)
        {
            return;
        }

        missionManager.MissionAccepted += HandleMissionAccepted;
        missionManager.MissionCompleted += HandleMissionCompleted;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || missionManager == null)
        {
            return;
        }

        missionManager.MissionAccepted -= HandleMissionAccepted;
        missionManager.MissionCompleted -= HandleMissionCompleted;
        subscribed = false;
    }

    private void HandleMissionAccepted(MissionRuntimeState missionState)
    {
        ProcessRules(missionState, MissionMusicTriggerMoment.OnAccepted);
    }

    private void HandleMissionCompleted(MissionRuntimeState missionState)
    {
        ProcessRules(missionState, MissionMusicTriggerMoment.OnCompleted);
    }

    private void ProcessRules(MissionRuntimeState missionState, MissionMusicTriggerMoment triggerMoment)
    {
        if (rules == null || missionState == null)
        {
            return;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            MissionMusicRule rule = rules[i];
            if (rule == null)
            {
                continue;
            }

            if (rule.ExecuteOnlyOnce && rule.HasExecuted)
            {
                continue;
            }

            if (!rule.Matches(missionState, triggerMoment))
            {
                continue;
            }

            ExecuteRule(rule, missionState);
        }
    }

    private void ExecuteRule(MissionMusicRule rule, MissionRuntimeState missionState)
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(MissionMusicResponder)} no encontró AudioManager para ejecutar regla '{rule.RuleName}'.", this);
            return;
        }

        if (rule.MusicClip == null)
        {
            Debug.LogWarning($"{nameof(MissionMusicResponder)}: la regla '{rule.RuleName}' no tiene Music Clip asignado.", this);
            return;
        }

        AudioManager.Instance.PlayMusic(rule.MusicClip, rule.FadeDuration);
        rule.MarkExecuted();

        if (logDebug)
        {
            Debug.Log($"{nameof(MissionMusicResponder)} ejecutó regla '{rule.RuleName}' por misión '{missionState.MissionId}'.", this);
        }
    }
}
