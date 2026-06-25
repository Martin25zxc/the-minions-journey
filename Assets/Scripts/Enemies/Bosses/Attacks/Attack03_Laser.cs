using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Ataque 3 — Láser de arena.
/// El FSM ya movió al jefe al centro y activó las piedras antes de llamar StartAttack().
/// Internamente: activa láser → rota siguiendo al jugador → desactiva láser → OnAttackEnded.
/// Las piedras las desactiva el FSM en Cleanup (no este script).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Attack03_Laser : MonoBehaviour
{
    public event Action OnAttackEnded;

    [Header("Origen del láser")]
    public Transform laserOrigin;

    [Header("Circulo magico")]
    public GameObject magicCircle;

    [Header("Layers")]
    public LayerMask blockingLayers;

    [FormerlySerializedAs("playerLayer")]
    public LayerMask targetLayers;

    [SerializeField]
    GameObject damageOwner;

    [Header("Parámetros")]
    public float laserDuration = 3f;
    public float laserRange = 30f;
    public float damagePerSecond = 8f;
    public float rotateSpeed = 90f;

    [Header("Visual")]
    public Color warningColor = Color.yellow;  // fase de aviso (sin daño)
    public Color damageColor = Color.red;     // fase activa (con daño)
    public float warningDuration = 2f;       // segundos en amarillo antes de hacer daño
    public float laserWidth = 0.05f;

    LineRenderer lr;
    Transform playerTransform;
    Coroutine routine;
    bool dealingDamage;
    Animator anim;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.enabled = false;

        if (laserOrigin == null)
        {
            laserOrigin = transform;
        }

        anim = GetComponentInParent<Animator>();

        if (damageOwner == null)
        {
            BossController boss = GetComponentInParent<BossController>();
            damageOwner = boss != null ? boss.gameObject : transform.root.gameObject;
        }
    }

    public void StartAttack()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            OnAttackEnded?.Invoke();
            return;
        }

        playerTransform = playerGO.transform;

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        lr.enabled = true;
        dealingDamage = false;
        SetLaserColor(warningColor);
        magicCircle?.SetActive(true);

        float elapsed = 0f;
        if (anim != null)
        {
            anim.SetBool("IsCasting", true);
        }

        // Fase warning — amarillo, sin daño
        while (elapsed < warningDuration)
        {
            RotateTowardsPlayer();
            FireLaser();
            elapsed += Time.deltaTime;
            yield return null;
        }

        dealingDamage = true;
        SetLaserColor(damageColor);
        elapsed = 0f;
        CameraShake.Trigger(0.3f, laserDuration);

        while (elapsed < laserDuration)
        {
            RotateTowardsPlayer();
            FireLaser();
            elapsed += Time.deltaTime;
            yield return null;
        }

        lr.enabled = false;
        dealingDamage = false;

        if (anim != null)
        {
            anim.SetBool("IsCasting", false);
        }

        magicCircle?.SetActive(false);
        routine = null;
        OnAttackEnded?.Invoke();
    }

    void RotateTowardsPlayer()
    {
        if (playerTransform == null)
        {
            return;
        }

        Transform root = transform.parent != null ? transform.parent : transform;
        Vector3 dir = playerTransform.position - root.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        root.rotation = Quaternion.RotateTowards(root.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    void FireLaser()
    {
        Vector3 origin = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;

        bool blocked = Physics.Raycast(origin, direction, out RaycastHit blockHit, laserRange, blockingLayers);
        float maxDist = blocked ? blockHit.distance : laserRange;

        if (dealingDamage)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDist, targetLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                TMJ_DamageUtility.TryDamageCollider(
                    hit.collider,
                    damagePerSecond * Time.deltaTime,
                    origin,
                    gameObject,
                    targetLayers,
                    damageOwner);
            }
        }

        lr.SetPosition(0, origin);
        lr.SetPosition(1, blocked ? blockHit.point : origin + direction * laserRange);
    }

    void SetLaserColor(Color c)
    {
        lr.startColor = c;
        lr.endColor = c;
    }

    public void ForceStop()
    {
        StopAllCoroutines();
        lr.enabled = false;
        dealingDamage = false;

        if (anim != null)
        {
            anim.SetBool("IsCasting", false);
        }

        magicCircle?.SetActive(false);
        routine = null;
        OnAttackEnded?.Invoke();
    }
}
