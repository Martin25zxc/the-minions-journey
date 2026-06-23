using UnityEngine;

/// <summary>
/// Controla el estado visual principal de un NPC humanoide de escena.
///
/// Este componente solo setea parámetros del Animator. No maneja misiones, vida,
/// colisiones, recompensas ni diálogo. La idea es que el NPC pueda arrancar como
/// Idle, debilitado o muerto sin mezclar esa decisión con NpcInteractable.
/// </summary>
[DisallowMultipleComponent]
public sealed class NpcHumanoidSceneStateController : MonoBehaviour
{
    private static readonly int SceneStateHash = Animator.StringToHash("SceneState");

    [Header("Referencias")]
    [Tooltip("Animator del modelo humanoide. Puede estar en el mismo GameObject o en un hijo Visual. Si queda vacío, se intenta buscar en hijos.")]
    [SerializeField]
    private Animator animator;

    [Header("Estado inicial")]
    [Tooltip("Estado visual con el que arranca el NPC al iniciar la escena. Esto es puesta en escena, no estado de misión.")]
    [SerializeField]
    private NpcHumanoidSceneState initialState = NpcHumanoidSceneState.Idle;

    [Tooltip("Si está activo, aplica Initial State en Awake. Mantener activo para NPCs ya colocados en escena.")]
    [SerializeField]
    private bool applyInitialStateOnAwake = true;

    [Header("Animator")]
    [Tooltip("Nombre del parámetro int en el Animator Controller. Recomendado: SceneState. Valores esperados: Idle=0, Weakened=10, DeadA=20, DeadB=30.")]
    [SerializeField]
    private string sceneStateParameterName = "SceneState";

    [Tooltip("Si está activo, avisa en consola cuando falta el Animator o el parámetro SceneState. Útil durante armado de prefabs.")]
    [SerializeField]
    private bool logConfigurationWarnings = true;

    [Header("Debug runtime")]
    [Tooltip("Estado visual actual aplicado por este componente durante Play Mode.")]
    [SerializeField]
    private NpcHumanoidSceneState currentState = NpcHumanoidSceneState.Idle;

    private int sceneStateHash;
    private bool warnedMissingAnimator;
    private bool warnedMissingParameter;

    public NpcHumanoidSceneState CurrentState => currentState;
    public Animator Animator => animator;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        sceneStateParameterName = "SceneState";
        initialState = NpcHumanoidSceneState.Idle;
        currentState = initialState;
    }

    private void Awake()
    {
        ResolveReferences();
        CacheParameterHash();

        if (applyInitialStateOnAwake)
        {
            SetState(initialState, force: true);
        }
    }

    /// <summary>
    /// Cambia el estado visual del NPC y actualiza el Animator.
    /// Puede ser llamado luego por responders, UnityEvents o scripts de escena.
    /// </summary>
    public void SetState(NpcHumanoidSceneState newState)
    {
        SetState(newState, force: false);
    }

    /// <summary>
    /// Reaplica el estado actual al Animator. Útil si se reasigna el Animator o se reinicia visualmente el NPC.
    /// </summary>
    public void ForceApplyCurrentState()
    {
        ApplyStateToAnimator(currentState);
    }

    /// <summary>
    /// Helpers simples para UnityEvent/Inspector si luego se quiere llamar sin pasar enum.
    /// </summary>
    public void SetIdle()
    {
        SetState(NpcHumanoidSceneState.Idle);
    }

    public void SetWeakened()
    {
        SetState(NpcHumanoidSceneState.Weakened);
    }

    public void SetDeadA()
    {
        SetState(NpcHumanoidSceneState.DeadA);
    }

    public void SetDeadB()
    {
        SetState(NpcHumanoidSceneState.DeadB);
    }

    private void SetState(NpcHumanoidSceneState newState, bool force)
    {
        if (!force && currentState == newState)
        {
            return;
        }

        currentState = newState;
        ApplyStateToAnimator(currentState);
    }

    private void ApplyStateToAnimator(NpcHumanoidSceneState state)
    {
        ResolveReferences();
        CacheParameterHash();

        if (animator == null)
        {
            WarnMissingAnimatorOnce();
            return;
        }

        if (!HasAnimatorIntParameter(animator, sceneStateParameterName))
        {
            WarnMissingParameterOnce();
            return;
        }

        animator.SetInteger(sceneStateHash, (int)state);
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void CacheParameterHash()
    {
        string parameterName = string.IsNullOrWhiteSpace(sceneStateParameterName)
            ? "SceneState"
            : sceneStateParameterName;

        sceneStateParameterName = parameterName;
        sceneStateHash = Animator.StringToHash(parameterName);
    }

    private static bool HasAnimatorIntParameter(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Int &&
                parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void WarnMissingAnimatorOnce()
    {
        if (!logConfigurationWarnings || warnedMissingAnimator)
        {
            return;
        }

        warnedMissingAnimator = true;
        Debug.LogWarning($"{name} has no Animator assigned for {nameof(NpcHumanoidSceneStateController)}.", this);
    }

    private void WarnMissingParameterOnce()
    {
        if (!logConfigurationWarnings || warnedMissingParameter)
        {
            return;
        }

        warnedMissingParameter = true;
        Debug.LogWarning(
            $"{name} Animator does not have an int parameter named '{sceneStateParameterName}'. " +
            "Create it in the Animator Controller to drive NPC scene states.",
            this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (string.IsNullOrWhiteSpace(sceneStateParameterName))
        {
            sceneStateParameterName = "SceneState";
        }

        currentState = initialState;
    }

    [ContextMenu("Apply Initial State Now")]
    private void ApplyInitialStateNowFromContextMenu()
    {
        SetState(initialState, force: true);
    }

    [ContextMenu("Find Animator In Children")]
    private void FindAnimatorInChildrenFromContextMenu()
    {
        animator = GetComponentInChildren<Animator>();
    }
#endif
}
