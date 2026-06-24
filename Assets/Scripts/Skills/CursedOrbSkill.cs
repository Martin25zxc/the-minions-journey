using System.Collections.Generic;
using UnityEngine;

public class CursedOrbSkill : MonoBehaviour, ISkillBehaviour
{
    public string SkillID => "cursedorb";

    [Header("Orb settings")]
    [SerializeField]
    private CursedOrbBehavior cursedOrbPrefab;

    [SerializeField, Min(0f)]
    private float expulsionForce = 50f;

    [SerializeField, Min(0f)]
    private float healAmount = 20f;

    [SerializeField, Min(1), Tooltip("Cantidad de orbes que se instancian cada vez que se usa la habilidad.")]
    private int orbsPerCast = 1;

    [SerializeField, Min(1), Tooltip("Cantidad máxima de orbes activos permitidos. Si se supera, se destruyen los más antiguos.")]
    private int maxOrbsCount = 3;

    [SerializeField, Min(0f), Tooltip("Radio de distribución al invocar varios orbes. Si hay un solo orbe, se mantiene en el centro.")]
    private float spawnRadius = 0.5f;

    private readonly List<CursedOrbBehavior> activeOrbs = new();

    public void Execute()
    {
        if (cursedOrbPrefab == null)
        {
            Debug.LogWarning("CursedOrbSkill: cursedOrbPrefab no está asignado.", this);
            return;
        }

        float angleOffset = Random.Range(0f, 360f);

        for (int i = 0; i < orbsPerCast; i++)
        {
            SpawnOrb(i, angleOffset);
        }

        TrimActiveOrbsToLimit();
    }

    private void SpawnOrb(int index, float angleOffset)
    {
        Vector3 spawnDirection = GetSpawnDirection(index, angleOffset);
        Vector3 spawnPosition = GetSpawnPosition(spawnDirection);

        CursedOrbBehavior orbInstance = Instantiate(cursedOrbPrefab, spawnPosition, Quaternion.identity);
        orbInstance.ChangeHealAmount(healAmount);
        orbInstance.OnOrbDestroyed += HandleOrbDestroyed;

        activeOrbs.Add(orbInstance);
        ApplyInitialForce(orbInstance, spawnDirection);
    }

    private Vector3 GetSpawnDirection(int index, float angleOffset)
    {
        if (orbsPerCast <= 1)
        {
            return RandomPlanarDirection();
        }

        float angleStep = 360f / orbsPerCast;
        float angle = angleOffset + angleStep * index;
        float radians = angle * Mathf.Deg2Rad;

        return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)).normalized;
    }

    private Vector3 GetSpawnPosition(Vector3 spawnDirection)
    {
        if (orbsPerCast <= 1 || spawnRadius <= 0f)
        {
            return transform.position;
        }

        return transform.position + spawnDirection * spawnRadius;
    }

    private void ApplyInitialForce(CursedOrbBehavior orbInstance, Vector3 direction)
    {
        if (orbInstance == null || expulsionForce <= 0f)
        {
            return;
        }

        Rigidbody rb = orbInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            return;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = RandomPlanarDirection();
        }

        rb.AddForce(direction.normalized * expulsionForce, ForceMode.Impulse);
    }

    private Vector3 RandomPlanarDirection()
    {
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        if (direction.sqrMagnitude <= 0.001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }

    private void HandleOrbDestroyed(CursedOrbBehavior orb)
    {
        if (orb != null)
        {
            orb.OnOrbDestroyed -= HandleOrbDestroyed;
        }

        activeOrbs.Remove(orb);
    }

    private void TrimActiveOrbsToLimit()
    {
        activeOrbs.RemoveAll(orb => orb == null);

        while (activeOrbs.Count > maxOrbsCount)
        {
            CursedOrbBehavior oldestOrb = activeOrbs[0];

            if (oldestOrb == null)
            {
                activeOrbs.RemoveAt(0);
                continue;
            }

            oldestOrb.DestroyOrb();
        }
    }

    private void OnValidate()
    {
        expulsionForce = Mathf.Max(0f, expulsionForce);
        healAmount = Mathf.Max(0f, healAmount);
        orbsPerCast = Mathf.Max(1, orbsPerCast);
        maxOrbsCount = Mathf.Max(1, maxOrbsCount);
        spawnRadius = Mathf.Max(0f, spawnRadius);
    }
}
