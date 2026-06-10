using System;
using System.Collections;
using UnityEngine;

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

    [Header("Origen del láser (empty hijo apuntando al frente)")]
    public Transform laserOrigin;

    [Header("Circulo magico")]
    public GameObject magicCircle;  // círculo que aparece al centro durante el warning

    [Header("Layers")]
    public LayerMask blockingLayers;   // Rock + borde arena
    public LayerMask playerLayer;

    [Header("Parámetros")]
    public float laserDuration  = 3f;
    public float laserRange     = 30f;
    public float damagePerSecond = 8f;
    public float rotateSpeed    = 90f;   // grados/segundo

    [Header("Visual")]
    public Color warningColor = Color.yellow;  // fase de aviso (sin daño)
    public Color damageColor  = Color.red;     // fase activa (con daño)
    public float warningDuration = 2f;       // segundos en amarillo antes de hacer daño
    public float laserWidth = 0.05f;

    private LineRenderer lr;
    private Transform    playerTransform;
    private Coroutine    routine;
    private bool         dealingDamage = false;

    private Animator anim;

    // ─────────────────────────────────────────
    //  Awake
    // ─────────────────────────────────────────
    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth    = laserWidth;
        lr.endWidth      = laserWidth;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.enabled       = false;

        if (laserOrigin == null) laserOrigin = transform;
        anim = GetComponentInParent<Animator>();
    }

    // ─────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────
    public void StartAttack()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) { OnAttackEnded?.Invoke(); return; }
        playerTransform = playerGO.transform;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AttackRoutine());
    }

    // ─────────────────────────────────────────
    //  Rutina interna
    // ─────────────────────────────────────────
    private IEnumerator AttackRoutine()
    {
        lr.enabled    = true;
        dealingDamage = false;
        SetLaserColor(warningColor);
        magicCircle?.SetActive(true);

        float elapsed = 0f;
        if (anim != null) anim.SetBool("IsCasting", true);

        // Fase warning — amarillo, sin daño
        while (elapsed < warningDuration)
        {
            RotateTowardsPlayer();
            FireLaser();
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fase daño — rojo
        dealingDamage = true;
        SetLaserColor(damageColor);
        elapsed = 0f;
        CameraShake.Trigger(0.3f, laserDuration);  // shake durante toda la fase activa

        while (elapsed < laserDuration)
        {
            RotateTowardsPlayer();
            FireLaser();
            elapsed += Time.deltaTime;
            yield return null;
        }

        lr.enabled    = false;
        dealingDamage = false;
        if (anim != null) anim.SetBool("IsCasting", false);
        magicCircle?.SetActive(false);

        routine = null;
        OnAttackEnded?.Invoke();
    }

    // ─────────────────────────────────────────
    //  Lógica frame-a-frame
    // ─────────────────────────────────────────
        private void RotateTowardsPlayer()
    {
        Transform root = transform.parent != null ? transform.parent : transform;
        Vector3 dir = playerTransform.position - root.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        root.rotation = Quaternion.RotateTowards(
            root.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }


    private void FireLaser()
    {
        Vector3 origin    = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;

        bool blocked = Physics.Raycast(origin, direction, out RaycastHit blockHit,
                                       laserRange, blockingLayers);
        float maxDist = blocked ? blockHit.distance : laserRange;

        // Solo daña en fase roja
        if (dealingDamage)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDist, playerLayer);
            foreach (var h in hits)
            {
                if (h.collider.CompareTag("Player"))
                    h.collider.GetComponent<PlayerHealth>()?.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }

        lr.SetPosition(0, origin);
        lr.SetPosition(1, blocked ? blockHit.point : origin + direction * laserRange);
    }

    private void SetLaserColor(Color c)
    {
        lr.startColor = c;
        lr.endColor   = c;
    }
}