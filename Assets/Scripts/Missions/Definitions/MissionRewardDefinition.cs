using System;
using UnityEngine;

[Serializable]
public sealed class MissionRewardDefinition
{
    [Header("Tipo")]
    [SerializeField, Tooltip("Tipo general de recompensa entregada cuando la misión llega a Completed.")]
    private MissionRewardType rewardType = MissionRewardType.None;

    [Header("Skill Unlock")]
    [SerializeField, Tooltip("Skill/habilidad asignable que se desbloquea. Usar cuando Reward Type es SkillUnlock.")]
    private SkillData skill;

    [Header("Item")]
    [SerializeField, Tooltip("ID estable del item a entregar. Usar cuando Reward Type es Item.")]
    private string itemId;

    [Header("Currency")]
    [SerializeField, Tooltip("ID estable de la moneda/recurso. Ejemplo: gold, souls, fragments.")]
    private string currencyId;

    [Header("Cantidad")]
    [SerializeField, Min(1), Tooltip("Cantidad para Item o Currency. Para SkillUnlock, InstantHeal y WorldEvent se ignora.")]
    private int quantity = 1;

    [Header("Instant Heal")]
    [SerializeField, Tooltip("Si está activo, la recompensa cura toda la vida. Usar cuando Reward Type es InstantHeal.")]
    private bool fullHeal;

    [SerializeField, Min(0f), Tooltip("Cantidad de vida restaurada si Full Heal está desactivado.")]
    private float healAmount;

    [Header("World Event")]
    [SerializeField, Tooltip("ID estable del evento de mundo a disparar. Usar cuando Reward Type es WorldEvent.")]
    private string worldEventId;

    [Header("Presentación")]
    [SerializeField, Tooltip("Si está activo, esta recompensa puede mostrarse en Journal futuro.")]
    private bool showInJournal = true;

    [SerializeField, Tooltip("Si está activo, esta recompensa puede disparar una notificación al entregarse.")]
    private bool notifyOnGrant = true;

    [SerializeField, Tooltip("Nombre visible opcional. Si queda vacío, se intenta usar el nombre del asset o un fallback.")]
    private string displayNameOverride;

    [SerializeField, TextArea(2, 4), Tooltip("Descripción opcional para Journal futuro. Si queda vacía, se intenta usar la descripción del asset.")]
    private string descriptionOverride;

    [SerializeField, Tooltip("Ícono opcional para Journal/notificación. Si queda vacío, se intenta usar el ícono del asset.")]
    private Sprite iconOverride;

    [Header("Diseño")]
    [SerializeField, TextArea(2, 4), Tooltip("Nota interna para diseño. No se muestra al jugador.")]
    private string designerNote;

    public MissionRewardType RewardType => rewardType;
    public SkillData Skill => skill;

    public string ItemId => itemId;
    public string CurrencyId => currencyId;
    public int Quantity => quantity;

    public bool FullHeal => fullHeal;
    public float HealAmount => healAmount;

    public string WorldEventId => worldEventId;

    public bool ShowInJournal => showInJournal;
    public bool NotifyOnGrant => notifyOnGrant;

    public string DisplayNameOverride => displayNameOverride;
    public string DescriptionOverride => descriptionOverride;
    public Sprite IconOverride => iconOverride;

    public string DesignerNote => designerNote;

    public string RewardId
    {
        get
        {
            return rewardType switch
            {
                MissionRewardType.SkillUnlock => skill != null ? skill.skillID : string.Empty,
                MissionRewardType.Item => itemId,
                MissionRewardType.Currency => currencyId,
                MissionRewardType.InstantHeal => fullHeal ? "full_heal" : "instant_heal",
                MissionRewardType.WorldEvent => worldEventId,
                _ => string.Empty
            };
        }
    }

    public string DisplayName
    {
        get
        {
            if (!MissionAuthoringValidation.IsNullOrWhiteSpace(displayNameOverride))
            {
                return displayNameOverride;
            }

            if (rewardType == MissionRewardType.SkillUnlock && skill != null)
            {
                return skill.skillName;
            }

            return rewardType switch
            {
                MissionRewardType.Item => itemId,
                MissionRewardType.Currency => currencyId,
                MissionRewardType.InstantHeal => fullHeal ? "Curación completa" : "Curación",
                MissionRewardType.WorldEvent => worldEventId,
                _ => string.Empty
            };
        }
    }

    public string Description
    {
        get
        {
            if (!MissionAuthoringValidation.IsNullOrWhiteSpace(descriptionOverride))
            {
                return descriptionOverride;
            }

            if (rewardType == MissionRewardType.SkillUnlock && skill != null)
            {
                return skill.description;
            }

            return string.Empty;
        }
    }

    public Sprite Icon
    {
        get
        {
            if (iconOverride != null)
            {
                return iconOverride;
            }

            if (rewardType == MissionRewardType.SkillUnlock && skill != null)
            {
                return skill.icon;
            }

            return null;
        }
    }

    internal void Validate(string missionId, int index)
    {
        itemId = MissionAuthoringValidation.CleanId(itemId);
        currencyId = MissionAuthoringValidation.CleanId(currencyId);
        worldEventId = MissionAuthoringValidation.CleanId(worldEventId);

        if (quantity < 1)
        {
            quantity = 1;
        }

        if (healAmount < 0f)
        {
            healAmount = 0f;
        }

        string rewardContext = $"Mission '{missionId}', Reward #{index}";

        switch (rewardType)
        {
            case MissionRewardType.None:
                ValidateNone(rewardContext);
                break;

            case MissionRewardType.SkillUnlock:
                ValidateSkillUnlock(rewardContext);
                break;

            case MissionRewardType.Item:
                ValidateStableId(rewardContext, nameof(itemId), itemId);
                break;

            case MissionRewardType.Currency:
                ValidateStableId(rewardContext, nameof(currencyId), currencyId);
                break;

            case MissionRewardType.InstantHeal:
                ValidateInstantHeal(rewardContext);
                break;

            case MissionRewardType.WorldEvent:
                ValidateStableId(rewardContext, nameof(worldEventId), worldEventId);
                break;

            default:
                Debug.LogWarning($"{rewardContext}: RewardType no soportado: {rewardType}.");
                break;
        }
    }

    private void ValidateNone(string rewardContext)
    {
        bool hasData =
            skill != null ||
            !MissionAuthoringValidation.IsNullOrWhiteSpace(itemId) ||
            !MissionAuthoringValidation.IsNullOrWhiteSpace(currencyId) ||
            !MissionAuthoringValidation.IsNullOrWhiteSpace(worldEventId) ||
            !MissionAuthoringValidation.IsNullOrWhiteSpace(displayNameOverride) ||
            !MissionAuthoringValidation.IsNullOrWhiteSpace(descriptionOverride) ||
            iconOverride != null;

        if (hasData)
        {
            Debug.LogWarning($"{rewardContext}: RewardType es None pero tiene datos cargados. Revisá si querías una recompensa real.");
        }
    }

    private void ValidateSkillUnlock(string rewardContext)
    {
        if (skill == null)
        {
            Debug.LogWarning($"{rewardContext}: RewardType SkillUnlock necesita SkillData.");
            return;
        }

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(skill.skillID))
        {
            Debug.LogWarning($"{rewardContext}: SkillData no tiene skillID.");
        }
        else if (!MissionAuthoringValidation.IsStableId(skill.skillID))
        {
            Debug.LogWarning($"{rewardContext}: {MissionAuthoringValidation.BuildStableIdWarning("skill.skillID", skill.skillID)}");
        }

        if (MissionAuthoringValidation.IsNullOrWhiteSpace(skill.skillName) &&
            MissionAuthoringValidation.IsNullOrWhiteSpace(displayNameOverride))
        {
            Debug.LogWarning($"{rewardContext}: SkillData no tiene skillName y no hay DisplayNameOverride.");
        }

        if (quantity != 1)
        {
            Debug.LogWarning($"{rewardContext}: SkillUnlock ignora Quantity. Dejá Quantity en 1 para evitar confusión.");
        }
    }

    private void ValidateInstantHeal(string rewardContext)
    {
        if (!fullHeal && healAmount <= 0f)
        {
            Debug.LogWarning($"{rewardContext}: InstantHeal necesita FullHeal activo o HealAmount mayor a 0.");
        }

        if (quantity != 1)
        {
            Debug.LogWarning($"{rewardContext}: InstantHeal ignora Quantity. Dejá Quantity en 1 para evitar confusión.");
        }
    }

    private static void ValidateStableId(string rewardContext, string fieldName, string value)
    {
        if (MissionAuthoringValidation.IsNullOrWhiteSpace(value))
        {
            Debug.LogWarning($"{rewardContext}: falta {fieldName}.");
            return;
        }

        if (!MissionAuthoringValidation.IsStableId(value))
        {
            Debug.LogWarning($"{rewardContext}: {MissionAuthoringValidation.BuildStableIdWarning(fieldName, value)}");
        }
    }
}
