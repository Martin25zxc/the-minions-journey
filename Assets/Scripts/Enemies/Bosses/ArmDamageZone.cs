using UnityEngine;

/// <summary>
/// Se pone en cada uno de los 4 GameObjects de brazo hijos del jefe.
/// Solo reenvía colliders al Attack02_SpinArms para que el ataque padre centralice
/// targetLayers, cooldown y aplicación de daño.
/// Requiere Collider con isTrigger = true.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArmDamageZone : MonoBehaviour
{
    [Tooltip("Referencia al ataque padre. Se puede asignar en inspector o buscar en Awake.")]
    public Attack02_SpinArms parentAttack;

    void Awake()
    {
        if (parentAttack == null)
        {
            parentAttack = GetComponentInParent<Attack02_SpinArms>();
        }

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        parentAttack?.OnArmHit(other);
    }

    void OnTriggerStay(Collider other)
    {
        parentAttack?.OnArmHit(other);
    }
}
