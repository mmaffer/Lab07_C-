using Biblioteca.Negocio;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Biblioteca.WPF.Services;

// Utilidades de interfaz compartidas por las páginas: notificaciones y confirmaciones.
// Solo pertenece a la capa de Presentación; no contiene reglas de negocio.
public static class UiServices
{
    private static readonly SnackbarService Snackbar = new SnackbarService();
    private static readonly ContentDialogService Dialogos = new ContentDialogService();

    public static void Initialize(SnackbarPresenter snackbarPresenter, ContentDialogHost dialogHost)
    {
        Snackbar.SetSnackbarPresenter(snackbarPresenter);
        Dialogos.SetDialogHost(dialogHost);
    }

    public static void ShowSuccess(string mensaje) =>
        Snackbar.Show("Listo", mensaje, ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24), TimeSpan.FromSeconds(3));

    // Regla de negocio incumplida: el usuario puede corregir el dato.
    public static void ShowWarning(string mensaje) =>
        Snackbar.Show("Revise los datos", mensaje, ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24), TimeSpan.FromSeconds(5));

    // Error inesperado (por ejemplo, no hay conexión con la base de datos).
    public static void ShowError(string mensaje) =>
        Snackbar.Show("Algo salió mal", mensaje, ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(6));

    public static async Task<bool> ConfirmAsync(string titulo, string mensaje, string textoConfirmar)
    {
        var dialogo = new ContentDialog
        {
            Title = titulo,
            Content = mensaje,
            PrimaryButtonText = textoConfirmar,
            PrimaryButtonAppearance = ControlAppearance.Danger,
            CloseButtonText = "Cancelar"
        };

        var resultado = await Dialogos.ShowAsync(dialogo, CancellationToken.None);
        return resultado == ContentDialogResult.Primary;
    }

    // Ejecuta una operación mostrando el indicador de carga y traduciendo las excepciones a mensajes.
    // Solo se captura ReglaNegocioException y Exception: nunca SqlException.
    public static async Task<bool> RunAsync(Func<Task> accion, Action<bool> setBusy)
    {
        try
        {
            setBusy(true);
            await accion();
            return true;
        }
        catch (ReglaNegocioException ex)
        {
            ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            setBusy(false);
        }
        return false;
    }
}
