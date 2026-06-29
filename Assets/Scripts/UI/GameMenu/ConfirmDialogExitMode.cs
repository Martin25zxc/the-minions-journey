/// <summary>
/// Modo de salida usado por ConfirmDialogController al confirmar.
/// Mantener simple para MVP:
/// - QuitApplication: desktop/editor.
/// - LoadScene: volver a una escena de menú si se configura Main Menu Scene Name.
/// - NotificationOnly: útil en WebGL o durante prototipo.
/// </summary>
public enum ConfirmDialogExitMode
{
    NotificationOnly,
    QuitApplication,
    LoadScene
}
