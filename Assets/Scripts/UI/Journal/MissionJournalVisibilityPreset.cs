/// <summary>
/// Presets de visibilidad del Mission Journal.
///
/// Para el MVP se recomienda MvpActivesAvailableCompleted:
/// - ReadyToTurnIn / Active arriba.
/// - Available después.
/// - Completed abajo.
/// - Inactive ocultas.
///
/// Custom queda preparado para ajustar desde Inspector sin agregar filtros visibles en UI.
/// </summary>
public enum MissionJournalVisibilityPreset
{
    MvpActivesAvailableCompleted,
    OnlyActives,
    ActivesAndCompleted,
    EverythingExceptInactive,
    Custom
}
