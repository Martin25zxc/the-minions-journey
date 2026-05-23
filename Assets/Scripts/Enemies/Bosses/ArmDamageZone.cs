using UnityEngine;

/// <summary>
/// Se pone en cada uno de los 4 GameObjects de brazo (hijos del jefe).
/// Reenvía OnTriggerEnter al Attack02_SpinArms para que centralice el cooldown.
/// Requiere: Collider con isTrigger = true en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArmDamageZone : MonoBehaviour
{
    [Tooltip("Referencia al ataque padre. Se puede asignar en inspector o buscar en Awake.")]
    public Attack02_SpinArms parentAttack;

    public string playerTag = "Player";

    private void Awake()
    {
        if (parentAttack == null)
            parentAttack = GetComponentInParent<Attack02_SpinArms>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        parentAttack?.OnArmHit(other);
    }
}