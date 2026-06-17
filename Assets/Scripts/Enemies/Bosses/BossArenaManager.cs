using System.Collections;
using UnityEngine;

/// <summary>
/// FSM de la pelea contra el jefe.
/// Estados: Idle → Moving → ArenaSetup → Attacking → Cleanup → Cooldown → (repeat)
/// Toda la comunicación es por eventos.
/// </summary>
public class BossArenaManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  FSM
    // ─────────────────────────────────────────────────────────────────────
    private enum State { Idle, Moving, ArenaSetup, Attacking, Cleanup, Cooldown }
    private State currentState = State.Idle;

    // ─────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────
    [Header("Setup")]
    [SerializeField] private BossDataSO  bossData;
    [SerializeField] private Transform   spawnPoint;    // centro de la arena
    [SerializeField] private ArenaRockManager rockManager;
    [SerializeField] private BossBattleStartController battleStartController;

    [Header("Cooldown entre ataques")]
    [SerializeField] private float minCooldown = 1.2f;
    [SerializeField] private float maxCooldown = 2.5f;

    // ─────────────────────────────────────────────────────────────────────
    //  Referencias runtime
    // ─────────────────────────────────────────────────────────────────────
    private BossController    boss;
    private Attack01_Slash    atk01;
    private Attack02_SpinArms atk02;
    private Attack03_Laser    atk03;
    private Attack04_Drops    atk04;
    private Attack05_Wave     atk05;
    private Attack06_Bullets  atk06;
    private Attack07_Ring     atk07;
    private Attack08_Jump     atk08;

    private int  lastAttackIndex = -1;
    private int  chosenAttack    = -1;
    private bool fightActive     = false;

    // ─────────────────────────────────────────────────────────────────────
    //  Start
    // ─────────────────────────────────────────────────────────────────────
    private void Start() => LoadBoss();

    public void LoadBoss()
    {
        if (bossData?.prefab == null)
        {
            Debug.LogError("[FSM] BossDataSO o prefab no asignado.");
            return;
        }

        // Instanciar prefab
        Vector3 pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var go = Instantiate(bossData.prefab, pos, Quaternion.identity);

        // Obtener componentes (InChildren para cubrir root + cualquier hijo)
        boss  = go.GetComponentInChildren<BossController>();
        atk01 = go.GetComponentInChildren<Attack01_Slash>();
        atk02 = go.GetComponentInChildren<Attack02_SpinArms>();
        atk03 = go.GetComponentInChildren<Attack03_Laser>();
        atk04 = go.GetComponentInChildren<Attack04_Drops>();
        atk05 = go.GetComponentInChildren<Attack05_Wave>();
        atk06 = go.GetComponentInChildren<Attack06_Bullets>();
        atk07 = go.GetComponentInChildren<Attack07_Ring>();
        atk08 = go.GetComponentInChildren<Attack08_Jump>();

        if (boss == null) { Debug.LogError("[FSM] BossController no encontrado en prefab."); return; }

        Debug.Log($"[FSM] Ataques — 01:{atk01 != null} 02:{atk02 != null} 03:{atk03 != null} 04:{atk04 != null} 05:{atk05 != null} 06:{atk06 != null} 07:{atk07 != null} 08:{atk08 != null}");

        boss.Initialize(bossData);

        // Suscribir eventos del BossController
        boss.OnArrived       += HandleArrived;
        boss.OnHealthChanged += HandleHealthChanged;
        boss.OnDeath         += HandleDeath;

        // Suscribir OnAttackEnded de cada ataque
        if (atk01 != null) atk01.OnAttackEnded += HandleAttackEnded;
        if (atk01 != null) atk01.OnCycleEnded  += HandleSlashCycleEnded;
        if (atk02 != null) atk02.OnAttackEnded += HandleAttackEnded;
        if (atk03 != null) atk03.OnAttackEnded += HandleAttackEnded;
        if (atk04 != null) atk04.OnAttackEnded += HandleAttackEnded;
        if (atk05 != null) atk05.OnAttackEnded += HandleAttackEnded;
        if (atk06 != null) atk06.OnAttackEnded += HandleAttackEnded;
        if (atk07 != null) atk07.OnAttackEnded += HandleAttackEnded;
        if (atk07 != null) atk07.OnCycleEnded  += HandleRingCycleEnded;
        if (atk08 != null) atk08.OnAttackEnded += HandleAttackEnded;
        if (atk08 != null) atk08.OnCycleEnded  += HandleJumpCycleEnded;

        // Suscribir OnRocksReady si hay rockManager
        if (rockManager != null) rockManager.OnRocksReady += HandleRocksReady;

        // Suscribir evento de inicio de batalla desde el BossBattleStartController
        if (battleStartController != null)
        {
            battleStartController.OnBossBattleStart += StartFight;
        }
        else
        {
            Debug.LogWarning("[FSM] No se encontró BossBattleStartController.");
           
        }

        
    }

    private void StartFight()
    {
        fightActive = true;
        EnterIdle();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  IDLE — elegir ataque y destino
    // ─────────────────────────────────────────────────────────────────────
    private void EnterIdle()
    {
        currentState = State.Idle;

        // Elegir ataque (evitar repetir el último)
        chosenAttack = PickAttack();

        // Ataques 1 y 7 van a la posición del jugador; el resto al centro
        Vector3 target;
        if (chosenAttack == 1 || chosenAttack == 7)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform.position : transform.position;
        }
        else if (chosenAttack == 8)
        {
            // Inicializar ataque y saltar directo al jugador
            atk08?.StartAttack();
            var player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform.position : transform.position;
        }
        else
        {
            target = spawnPoint ? spawnPoint.position : Vector3.zero;
        }

        EnterMoving(target);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MOVING
    // ─────────────────────────────────────────────────────────────────────
    private void EnterMoving(Vector3 target)
    {
        currentState = State.Moving;
        if (chosenAttack == 8 && atk08 != null)
            boss.JumpTo(target, atk08.jumpHeight, atk08.jumpDuration);
        else
            boss.GoTo(target);
    }

    private void HandleArrived()
    {
        if (currentState != State.Moving) return;

        if (chosenAttack == 3 && rockManager != null)
            EnterArenaSetup();
        else if (chosenAttack == 8)
            HandleJumpLanded();
        else
            EnterAttacking();
    }

    private void HandleJumpLanded()
    {
        // El jefe aterrizó — notificar al ataque para anillo y animación
        currentState = State.Attacking;
        atk08?.OnLanded();
        // Espera OnCycleEnded o OnAttackEnded desde atk08
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ARENASETUP — solo para ataque 3
    // ─────────────────────────────────────────────────────────────────────
    private void EnterArenaSetup()
    {
        currentState = State.ArenaSetup;
        rockManager.ActivateRocks();
        // Espera evento OnRocksReady → HandleRocksReady
    }

    private void HandleRocksReady()
    {
        if (currentState != State.ArenaSetup) return;
        EnterAttacking();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ATTACKING
    // ─────────────────────────────────────────────────────────────────────
    private void EnterAttacking()
    {
        currentState = State.Attacking;

        switch (chosenAttack)
        {
            case 1:
                // Si la espada ya está activa es un ciclo intermedio → StartCycle()
                // Si no, es el primer ciclo → StartAttack() inicializa todo
                if (atk01 != null)
                {
                    if (atk01.IsActive) atk01.StartCycle();
                    else                atk01.StartAttack();
                }
                break;
            case 2: atk02?.StartAttack(); break;
            case 3: atk03?.StartAttack(); break;
            case 4: atk04?.StartAttack(); break;
            case 5: atk05?.StartAttack(); break;
            case 6: atk06?.StartAttack(); break;
            case 7:
                if (atk07 != null)
                {
                    if (atk07.IsActive) atk07.StartCycle();
                    else                atk07.StartAttack();
                }
                break;
        }
    }

    private void HandleAttackEnded()
    {
        if (currentState != State.Attacking) return;
        EnterCleanup();
    }

    /// <summary>
    /// Un ciclo del slash terminó pero quedan más.
    /// Volver a Moving apuntando a la posición actual del jugador.
    /// </summary>
    private void HandleSlashCycleEnded()
    {
        if (currentState != State.Attacking) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 target = player != null ? player.transform.position : boss.transform.position;
        currentState = State.Moving;
        boss.GoTo(target);
    }

    private void HandleRingCycleEnded()
    {
        if (currentState != State.Attacking) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 target = player != null ? player.transform.position : boss.transform.position;
        currentState = State.Moving;
        boss.GoTo(target);
    }

    private void HandleJumpCycleEnded()
    {
        if (currentState != State.Attacking) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 target = player != null ? player.transform.position : boss.transform.position;
        currentState = State.Moving;
        atk08?.PrepareJump();
        boss.JumpTo(target, atk08.jumpHeight, atk08.jumpDuration);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP — desactivar lo que quede del entorno
    // ─────────────────────────────────────────────────────────────────────
    private void EnterCleanup()
    {
        currentState = State.Cleanup;

        // Las piedras solo se desactivan tras el ataque 3
        if (chosenAttack == 3 && rockManager != null)
            rockManager.DeactivateRocks();

        // Los meshes de espada/brazos ya los apagó cada ataque antes de su OnAttackEnded.
        // Cleanup queda para cosas de entorno o efectos globales.

        EnterCooldown();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  COOLDOWN
    // ─────────────────────────────────────────────────────────────────────
    private void EnterCooldown()
    {
        currentState = State.Cooldown;
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(Random.Range(minCooldown, maxCooldown));
        if (fightActive) EnterIdle();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Fase / muerte
    // ─────────────────────────────────────────────────────────────────────
    private void HandleHealthChanged(float pct)
    {
        // Ejemplo de transición de fase
        if (pct <= 0.5f)
        {
            minCooldown = Mathf.Max(0.6f, minCooldown - 0.2f);
            maxCooldown = Mathf.Max(1.2f, maxCooldown - 0.3f);
        }
    }

    private void HandleDeath()
    {
        fightActive = false;
        StopAllCoroutines();
        rockManager?.DeactivateRocks();
        battleStartController?.EndBossBattle();
        Debug.Log("[FSM] Jefe derrotado.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Selección de ataque (sin repetir el último)
    // ─────────────────────────────────────────────────────────────────────
    private int PickAttack()
    {
        // Pool de ataques disponibles
        var pool = new System.Collections.Generic.List<int>();
        if (atk01 != null) pool.Add(1);
        if (atk02 != null) pool.Add(2);
        if (atk03 != null && rockManager != null) pool.Add(3);
        if (atk04 != null) pool.Add(4);
        if (atk05 != null) pool.Add(5);
        if (atk06 != null) pool.Add(6);
        if (atk07 != null) pool.Add(7);
        if (atk08 != null) pool.Add(8);

        if (pool.Count == 0) return 1;

        // Quitar el último si hay más opciones
        if (pool.Count > 1) pool.Remove(lastAttackIndex);

        int chosen = pool[Random.Range(0, pool.Count)];
        lastAttackIndex = chosen;
        return chosen;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Limpieza de eventos
    // ─────────────────────────────────────────────────────────────────────
    private void OnDestroy()
    {
        if (boss  != null) { boss.OnArrived -= HandleArrived; boss.OnHealthChanged -= HandleHealthChanged; boss.OnDeath -= HandleDeath; }
        if (atk01 != null) { atk01.OnAttackEnded -= HandleAttackEnded; atk01.OnCycleEnded -= HandleSlashCycleEnded; }
        if (atk02 != null) atk02.OnAttackEnded -= HandleAttackEnded;
        if (atk03 != null) atk03.OnAttackEnded -= HandleAttackEnded;
        if (atk04 != null) atk04.OnAttackEnded -= HandleAttackEnded;
        if (atk05 != null) atk05.OnAttackEnded -= HandleAttackEnded;
        if (atk06 != null) atk06.OnAttackEnded -= HandleAttackEnded;
        if (atk07 != null) { atk07.OnAttackEnded -= HandleAttackEnded; atk07.OnCycleEnded -= HandleRingCycleEnded; }
        if (atk08 != null) { atk08.OnAttackEnded -= HandleAttackEnded; atk08.OnCycleEnded -= HandleJumpCycleEnded; }
        if (rockManager != null) rockManager.OnRocksReady -= HandleRocksReady;
    }
}