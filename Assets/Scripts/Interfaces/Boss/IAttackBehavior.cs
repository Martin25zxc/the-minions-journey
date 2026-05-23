using System.Collections;

/// <summary>
/// Cada ataque implementa esta interfaz.
/// El BossController llama Execute() y espera a que la coroutine termine.
/// </summary>
public interface IAttackBehavior
{
    IEnumerator Execute(BossController boss);
}