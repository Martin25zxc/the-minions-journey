using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona las piedras de la arena.
/// Lanza OnRocksReady cuando todas terminaron de aparecer.
/// </summary>
public class ArenaRockManager : MonoBehaviour
{
    public event Action OnRocksReady;   // el FSM espera este evento

    [Tooltip("Todos los cubos/piedras de la arena. Layer = Rock, Collider no-trigger.")]
    public GameObject[] rocks;

    [Tooltip("Escala final de cada piedra al aparecer. Debe coincidir con la escala en escena.")]
    public Vector3 targetScale = new Vector3(2f, 2f, 2f);

    [Tooltip("Segundos de animación de aparición.")]
    public float riseTime = 0.5f;

    // ─────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────
    public void ActivateRocks()
    {
        StartCoroutine(ActivateRoutine());
    }

    public void DeactivateRocks()
    {
        foreach (var r in rocks)
            if (r != null) r.SetActive(false);
    }

    // ─────────────────────────────────────────
    //  Rutina de aparición
    // ─────────────────────────────────────────
    private IEnumerator ActivateRoutine()
    {
        // Activar todos y animar
        foreach (var r in rocks)
        {
            if (r == null) continue;
            r.SetActive(true);
            r.transform.localScale = Vector3.zero;
        }

        float elapsed = 0f;
        while (elapsed < riseTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseTime);
            foreach (var r in rocks)
                if (r != null) r.transform.localScale = targetScale * t;
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var r in rocks)
            if (r != null) r.transform.localScale = targetScale;

        // Lanzar evento: el FSM puede avanzar
        OnRocksReady?.Invoke();
    }
}