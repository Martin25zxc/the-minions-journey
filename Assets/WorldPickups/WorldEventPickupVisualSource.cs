/// <summary>
/// Define de dónde obtiene su modelo visual un WorldEventPickup.
///
/// InstantiateFromDefinition:
///     El script instancia el WorldModelPrefab declarado en WorldEventPickupDefinition.
///     Útil para pickeables repetibles o simples.
///
/// UseExistingSceneVisual:
///     El script no instancia modelo. Usa el visual ya colocado manualmente como hijo del pickup.
///     Útil para objetos con escala, composición, rotación o armado especial en escena/prefab.
/// </summary>
public enum WorldEventPickupVisualSource
{
    InstantiateFromDefinition = 10,
    UseExistingSceneVisual = 20
}
