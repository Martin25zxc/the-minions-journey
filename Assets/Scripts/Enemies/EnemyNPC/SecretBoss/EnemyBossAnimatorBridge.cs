using UnityEngine;

/// <summary>
/// Bridge liviano para triggers exclusivos del boss.
/// No reemplaza EnemyAnimator: usa el mismo Animator Component.
///
/// v2: agrega restart explícito de estados para habilidades que necesitan repetir
/// el mismo clip varias veces dentro de una sola ejecución, como SpinShockwave.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyBossAnimatorBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Trigger Names")]
    [SerializeField] private string phaseTransitionTrigger = "BossPhaseTransition";
    [SerializeField] private string slashSpecialTrigger = "BossSlashSpecial";
    [SerializeField] private string spinSlashTrigger = "BossSpinSlash";
    [SerializeField] private string castTrigger = "BossCast";
    [SerializeField] private string throwTrigger = "BossThrow";

    [Header("State Restart")]
    [Tooltip("Nombre exacto del estado de Animator que contiene el clip de spin del boss.")]
    [SerializeField] private string spinSlashStateName = "BossSpinSlash";

    [Tooltip("Layer del Animator donde está el estado BossSpinSlash.")]
    [SerializeField, Min(0)] private int spinSlashLayerIndex = 0;

    [Tooltip("Si está activo, PlaySpinSlash(true) reinicia el estado con Animator.Play en normalizedTime 0.")]
    [SerializeField] private bool allowDirectSpinStateRestart = true;

    [Header("Fallback")]
    [Tooltip("Si falta un trigger de boss, intenta usar RangedAttack para casts/slashes y ShieldThrow para throw.")]
    [SerializeField] private bool useBaseFallbackTriggers = true;

    [SerializeField] private string rangedFallbackTrigger = "RangedAttack";
    [SerializeField] private string shieldThrowFallbackTrigger = "ShieldThrow";

    [Header("Debug")]
    [SerializeField] private bool warnMissingParameters = true;

    private void Awake()
    {
        ResolveReferences();
    }

    public void PlayPhaseTransition()
    {
        Trigger(phaseTransitionTrigger, null);
    }

    public void PlaySlashSpecial()
    {
        Trigger(slashSpecialTrigger, useBaseFallbackTriggers ? rangedFallbackTrigger : null);
    }

    public void PlaySpinSlash()
    {
        PlaySpinSlash(false);
    }

    public void PlaySpinSlash(bool restartState)
    {
        ResolveReferences();

        if (restartState && allowDirectSpinStateRestart && TryRestartState(spinSlashStateName, spinSlashLayerIndex))
        {
            return;
        }

        Trigger(spinSlashTrigger, null);
    }

    public void PlayCast()
    {
        Trigger(castTrigger, useBaseFallbackTriggers ? rangedFallbackTrigger : null);
    }

    public void PlayThrow()
    {
        Trigger(throwTrigger, useBaseFallbackTriggers ? shieldThrowFallbackTrigger : null);
    }

    private void Trigger(string primaryName, string fallbackName)
    {
        ResolveReferences();
        if (animator == null)
        {
            return;
        }

        if (TryTrigger(primaryName))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            TryTrigger(fallbackName);
        }
    }

    private bool TryTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return false;
        }

        int hash = Animator.StringToHash(triggerName);
        if (!HasParameter(hash, AnimatorControllerParameterType.Trigger, triggerName))
        {
            return false;
        }

        animator.ResetTrigger(hash);
        animator.SetTrigger(hash);
        return true;
    }

    private bool TryRestartState(string stateName, int layerIndex)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        if (layerIndex < 0 || layerIndex >= animator.layerCount)
        {
            if (warnMissingParameters)
            {
                Debug.LogWarning($"[{nameof(EnemyBossAnimatorBridge)}] Invalid Animator layer {layerIndex} for state '{stateName}'.", this);
            }
            return false;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(layerIndex, stateHash))
        {
            if (warnMissingParameters)
            {
                Debug.LogWarning($"[{nameof(EnemyBossAnimatorBridge)}] Missing Animator state '{stateName}' in layer {layerIndex} of {animator.runtimeAnimatorController?.name}.", this);
            }
            return false;
        }

        animator.Play(stateHash, layerIndex, 0f);
        animator.Update(0f);
        return true;
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType expectedType, string parameterName)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.nameHash != hash)
            {
                continue;
            }

            if (parameter.type == expectedType)
            {
                return true;
            }

            if (warnMissingParameters)
            {
                Debug.LogWarning($"[{nameof(EnemyBossAnimatorBridge)}] Parameter '{parameterName}' exists in {animator.runtimeAnimatorController?.name}, but type is {parameter.type} instead of {expectedType}.", this);
            }

            return false;
        }

        if (warnMissingParameters)
        {
            Debug.LogWarning($"[{nameof(EnemyBossAnimatorBridge)}] Missing Animator trigger '{parameterName}' in {animator.runtimeAnimatorController?.name}.", this);
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }
}
