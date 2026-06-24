using UnityEngine;
using System.Collections.Generic;

public class CursedOrbSkill : MonoBehaviour, ISkillBehaviour
{
    public string SkillID => "cursedorb";

    [Header("Orb settings")]
    [SerializeField] CursedOrbBehavior cursedOrbPrefab;
    [SerializeField] float expulsionForce = 50f;
    [SerializeField] float healAmount = 20f;
    [SerializeField] int maxOrbsCount = 3;

    private List<CursedOrbBehavior> activeOrbs = new List<CursedOrbBehavior>();

    public void Execute()
    {
        CursedOrbBehavior orbInstance = Instantiate(cursedOrbPrefab, transform.position, Quaternion.identity);

        Rigidbody rb = orbInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            rb.AddForce(randomDirection * expulsionForce, ForceMode.Impulse);
        }

        orbInstance.OnOrbDestroyed += HandleOrbDestroyed; // <-- suscripción
        activeOrbs.Add(orbInstance);
        EvaluateOrbsCount();
        //Debug.Log("¡Lanzar orbe maldito!");
    }

    private void HandleOrbDestroyed(CursedOrbBehavior orb)
    {
        activeOrbs.Remove(orb);
        //Debug.Log($"Orbe destruido por sí mismo. Orbes activos: {activeOrbs.Count}");
    }

    private void EvaluateOrbsCount()
    {
        //Debug.Log($"Orbes activos: {activeOrbs.Count}");
        if (activeOrbs.Count > maxOrbsCount)
        {
            //Debug.Log("¡Límite de orbes alcanzado! Destruyendo el orbe más antiguo.");
            CursedOrbBehavior oldestOrb = activeOrbs[0];
            oldestOrb.OnOrbDestroyed -= HandleOrbDestroyed; // <-- evitar doble remove
            activeOrbs.RemoveAt(0);
            if (oldestOrb != null)
            {
                Destroy(oldestOrb.gameObject);
            }
        }
    }
}