using System.Windows;
using System.Windows.Controls;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Reporte de préstamos por intervalo de fechas. La consulta (INNER JOIN) vive en la capa de Datos.
public partial class ReportePage : Page
{
    private readonly PrestamoNegocio _prestamoNegocio = new PrestamoNegocio();

    public ReportePage()
    {
        InitializeComponent();

        dpHasta.SelectedDate = DateTime.Today;
        dpDesde.SelectedDate = DateTime.Today.AddDays(-30);

        Loaded += async (s, e) => await ConsultarAsync();
    }

    private async Task ConsultarAsync()
    {
        if (dpDesde.SelectedDate == null || dpHasta.SelectedDate == null)
        {
            UiServices.ShowWarning("Seleccione las fechas 'Desde' y 'Hasta'.");
            return;
        }

        DateTime desde = dpDesde.SelectedDate.Value;
        DateTime hasta = dpHasta.SelectedDate.Value;

        await UiServices.RunAsync(async () =>
        {
            var filas = await _prestamoNegocio.ReporteAsync(desde, hasta);

            dgReporte.ItemsSource = filas;
            panelVacio.Visibility = filas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtTotal.Text = $"{filas.Count} libro(s) en {filas.Select(f => f.PrestamoId).Distinct().Count()} préstamo(s).";
        }, EstablecerOcupado);
    }

    private void EstablecerOcupado(bool ocupado)
    {
        anilloCarga.Visibility = ocupado ? Visibility.Visible : Visibility.Collapsed;
        btnConsultar.IsEnabled = !ocupado;
    }

    private async void btnConsultar_Click(object sender, RoutedEventArgs e) => await ConsultarAsync();
}
