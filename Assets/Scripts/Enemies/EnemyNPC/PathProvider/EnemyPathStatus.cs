/// <summary>
/// Resultado conceptual de una consulta de navegacion.
/// No mueve al enemigo; solo describe si se pudo resolver un proximo punto.
/// </summary>
public enum EnemyPathStatus
{
    None,
    Valid,
    Complete,
    NoProvider,
    InvalidStart,
    InvalidDestination,
    InvalidPath,
    PartialPath
}
