using UnityEngine;

/// <summary>
/// Cooldown global para habilidades especiales de boss.
///
/// Evita cadenas injustas como Orbes -> SpinShockwave -> TripleShockwave sin respiración.
/// Las abilities especiales consultan CanUseSpecial y notifican NotifySpecialUsed al terminar/castear.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyBossSpecialCooldownGate : MonoBehaviour
{
    [Header("Global Special Cooldown")]
    [SerializeField, Min(0f)]
    private float defaultCooldownAfterSpecial = 3f;

    [Header("Runtime Debug")]
    [SerializeField]
    private float debugNextAllowedTime;

    public bool CanUseSpecial => Time.time >= debugNextAllowedTime;
    public float RemainingCooldown => Mathf.Max(0f, debugNextAllowedTime - Time.time);

    public void NotifySpecialUsed()
    {
        NotifySpecialUsed(defaultCooldownAfterSpecial);
    }

    public void NotifySpecialUsed(float cooldown)
    {
        debugNextAllowedTime = Mathf.Max(debugNextAllowedTime, Time.time + Mathf.Max(0f, cooldown));
    }

    public void ForceBlock(float duration)
    {
        debugNextAllowedTime = Mathf.Max(debugNextAllowedTime, Time.time + Mathf.Max(0f, duration));
    }

    public void Clear()
    {
        debugNextAllowedTime = 0f;
    }

    private void OnValidate()
    {
        defaultCooldownAfterSpecial = Mathf.Max(0f, defaultCooldownAfterSpecial);
    }
}
