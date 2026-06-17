using UnityEngine;
using System;


public class BossBattleStartController : MonoBehaviour
{
    //Evento para avisar que la batalla contra el jefe ha comenzado. Puede ser suscripto por el BossArenaManager o por cualquier otro sistema que necesite saberlo.
    public event Action OnBossBattleStart;

    //Inspector
    [SerializeField] GameObject arenaDoor; //La puerta que se cerrará al iniciar la batalla.
    [SerializeField] GameObject arenaWalls; //Las paredes que se desctaivaran al terminar la batalla.

    private bool battleStarted = false; //Para asegurarnos de que la batalla solo se inicie una vez.

    private void Awake()
    {
        if (arenaDoor == null)
        {
            Debug.LogError("BossBattleStartController: No se ha asignado la puerta del arena en el inspector.");
        }
        arenaDoor.SetActive(false); //Asegurarse de que la puerta esté abierta al inicio.
    }

    private void StartBossBattle()
    {
        arenaDoor.SetActive(true); //Cerrar la puerta del arena.
        OnBossBattleStart?.Invoke(); //Disparar el evento para avisar que la batalla ha comenzado.
    }

    private void OnTriggerEnter(Collider other)
    {
        if (battleStarted) return; //Si la batalla ya ha comenzado, no hacer nada.
        if (other.CompareTag("Player"))
        {
            StartBossBattle();
            battleStarted = true;
            
        }
    }

    public void EndBossBattle()
    {
        arenaDoor.SetActive(false); //Abrir la puerta del arena.
        arenaWalls.SetActive(false); //Desactivar las paredes del arena.
    }
}
