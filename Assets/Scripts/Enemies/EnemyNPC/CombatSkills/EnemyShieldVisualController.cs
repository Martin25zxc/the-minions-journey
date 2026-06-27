using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyShieldVisualController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Visual del escudo equipado en la mano. Ejemplo: shield_badge.")]
    [SerializeField]
    private Transform shieldVisual;

    [Header("Behaviour")]
    [SerializeField]
    private bool restoreShieldOnDisable = true;

    private bool shieldIsAway;

    public bool ShieldIsAway => shieldIsAway;
    public bool IsShieldAvailable => !shieldIsAway;

    public bool TryMarkShieldThrown()
    {
        if (shieldIsAway)
        {
            return false;
        }

        shieldIsAway = true;
        SetShieldVisible(false);
        return true;
    }

    public void MarkShieldReturned()
    {
        shieldIsAway = false;
        SetShieldVisible(true);
    }

    public void ForceRestoreShield()
    {
        MarkShieldReturned();
    }

    private void OnDisable()
    {
        if (restoreShieldOnDisable)
        {
            MarkShieldReturned();
        }
    }

    private void SetShieldVisible(bool visible)
    {
        if (shieldVisual == null)
        {
            return;
        }

        shieldVisual.gameObject.SetActive(visible);
    }
}