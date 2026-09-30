using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Formulario de devolución: la multa se CALCULA en Negocio; aquí solo se muestra.
public partial class DevolucionPage : Page
{
    private readonly PrestamoNegocio _prestamoNegocio = new PrestamoNegocio();
    private readonly SocioNegocio _socioNegocio = new SocioNegocio();

    public DevolucionPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await CargarSociosAsync();
        MostrarDetalle(null);
    }

    private async Task CargarSociosAsync()
    {
        await UiServices.RunAsync(async () =>
        {
            cmbSocio.ItemsSource = await _socioNegocio.ListarAsync();
        }, EstablecerOcupado);
    }

    private async Task CargarPendientesAsync()
    {
        int socioId = cmbSocio.SelectedValue is int id ? id : 0;
        if (socioId == 0)
        {
            dgPendientes.ItemsSource = null;
            txtVacio.Text = "Seleccione un socio.";
            panelVacio.Visibility = Visibility.Visible;
            return;
        }

        await UiServices.RunAsync(async () =>
        {
            var pendientes = await _prestamoNegocio.ListarPendientesAsync(socioId);

            dgPendientes.ItemsSource = pendientes;
            txtVacio.Text = "Este socio no tiene libros pendientes.";
            panelVacio.Visibility = pendientes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }, EstablecerOcupado);
    }

    private async Task DevolverAsync()
    {
        if (dgPendientes.SelectedItem is not PrestamoPendiente pendiente)
        {
            UiServices.ShowWarning("Seleccione el libro que se devuelve.");
            return;
        }

        ResultadoDevolucion resultado = null;
        bool ok = await UiServices.RunAsync(async () =>
        {
            resultado = await _prestamoNegocio.RegistrarDevolucionAsync(pendiente.PrestamoId, pendiente.LibroId);
        }, EstablecerOcupado);

        if (!ok) return;

        string mensaje = resultado.DiasRetraso > 0
            ? $"\"{pendiente.Titulo}\" devuelto con {resultado.DiasRetraso} día(s) de retraso. Multa: S/ {resultado.Multa:0.00}."
            : $"\"{pendiente.Titulo}\" devuelto a tiempo. Sin multa.";
        if (resultado.PrestamoCerrado)
            mensaje += $" El préstamo N° {pendiente.PrestamoId} quedó Devuelto.";

        UiServices.ShowSuccess(mensaje);
        await CargarPendientesAsync();
    }

    // Muestra fechas y multa del libro elegido. El cálculo lo hace PrestamoNegocio.
    private void MostrarDetalle(PrestamoPendiente pendiente)
    {
        if (pendiente == null)
        {
            txtLibro.Text = "Seleccione un libro de la lista.";
            txtLimite.Text = txtHoy.Text = txtDias.Text = txtMulta.Text = "–";
            btnDevolver.IsEnabled = false;
            return;
        }

        DateTime hoy = DateTime.Today;
        txtLibro.Text = pendiente.Titulo;
        txtLimite.Text = pendiente.FechaLimite.ToString("dd/MM/yyyy");
        txtHoy.Text = hoy.ToString("dd/MM/yyyy");
        txtDias.Text = PrestamoNegocio.CalcularDiasRetraso(pendiente.FechaLimite, hoy).ToString();
        txtMulta.Text = $"S/ {PrestamoNegocio.CalcularMulta(pendiente.FechaLimite, hoy):0.00}";
        btnDevolver.IsEnabled = true;
    }

    private void EstablecerOcupado(bool ocupado)
    {
        anilloCarga.Visibility = ocupado ? Visibility.Visible : Visibility.Collapsed;
        btnDevolver.IsEnabled = !ocupado && dgPendientes.SelectedItem != null;
    }

    // ----- Eventos de la pantalla -----

    private async void cmbSocio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MostrarDetalle(null);
        await CargarPendientesAsync();
    }

    private void dgPendientes_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        MostrarDetalle(dgPendientes.SelectedItem as PrestamoPendiente);

    private async void btnDevolver_Click(object sender, RoutedEventArgs e) => await DevolverAsync();
}
