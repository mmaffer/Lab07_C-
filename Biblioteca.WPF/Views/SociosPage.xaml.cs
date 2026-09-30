using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Capa de Presentación: solo recoge datos, llama a Negocio y muestra resultados.
public partial class SociosPage : Page
{
    private readonly SocioNegocio _socioNegocio = new SocioNegocio();

    private readonly DispatcherTimer _timerBusqueda = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };

    // Socio en edición. null = se está registrando uno nuevo.
    private Socio _editando;

    public SociosPage()
    {
        InitializeComponent();

        _timerBusqueda.Tick += async (s, e) =>
        {
            _timerBusqueda.Stop();
            await CargarAsync();
        };

        Loaded += async (s, e) => await CargarAsync();
        LimpiarFormulario();
    }

    private async Task CargarAsync()
    {
        string texto = txtBuscar.Text;

        await UiServices.RunAsync(async () =>
        {
            var socios = await _socioNegocio.BuscarAsync(texto);

            dgSocios.ItemsSource = socios;
            txtVacio.Text = string.IsNullOrWhiteSpace(texto)
                ? "No hay socios registrados."
                : $"Ningún socio coincide con \"{texto}\".";
            panelVacio.Visibility = socios.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_editando != null)
                dgSocios.SelectedItem = socios.FirstOrDefault(s => s.SocioId == _editando.SocioId);
        }, EstablecerOcupado);
    }

    private async Task GuardarAsync()
    {
        var socio = new Socio
        {
            SocioId = _editando?.SocioId ?? 0,
            DNI = txtDni.Text,
            Nombre = txtNombre.Text,
            Email = txtEmail.Text
        };
        bool esNuevo = socio.SocioId == 0;

        bool ok = await UiServices.RunAsync(async () =>
        {
            if (esNuevo)
                await _socioNegocio.InsertarAsync(socio);
            else
                await _socioNegocio.ActualizarAsync(socio);
        }, EstablecerOcupado);

        if (!ok) return;

        UiServices.ShowSuccess(esNuevo ? $"Se registró a {socio.Nombre}." : $"Se actualizó a {socio.Nombre}.");
        if (esNuevo) LimpiarFormulario();
        await CargarAsync();
    }

    private async Task EliminarAsync()
    {
        if (_editando == null) return;

        var socio = _editando;
        bool confirmado = await UiServices.ConfirmAsync(
            "Dar de baja al socio",
            $"¿Desea dar de baja a {socio.Nombre} (DNI {socio.DNI})?\nDejará de mostrarse, pero se conserva su historial de préstamos.",
            "Dar de baja");
        if (!confirmado) return;

        bool ok = await UiServices.RunAsync(() => _socioNegocio.EliminarAsync(socio.SocioId), EstablecerOcupado);
        if (!ok) return;

        UiServices.ShowSuccess($"{socio.Nombre} fue dado de baja.");
        LimpiarFormulario();
        await CargarAsync();
    }

    private void EditarSocio(Socio socio)
    {
        _editando = socio;
        txtDni.Text = socio.DNI;
        txtNombre.Text = socio.Nombre;
        txtEmail.Text = socio.Email;

        txtTituloForm.Text = "Editar socio";
        txtAyudaForm.Text = $"Editando a {socio.Nombre}.";
        ActualizarBotones();
    }

    private void LimpiarFormulario()
    {
        _editando = null;
        dgSocios.SelectedItem = null;
        txtDni.Clear();
        txtNombre.Clear();
        txtEmail.Clear();

        txtTituloForm.Text = "Nuevo socio";
        txtAyudaForm.Text = "Complete los datos y presione Guardar.";
        ActualizarBotones();
        txtDni.Focus();
    }

    private void EstablecerOcupado(bool ocupado)
    {
        anilloCarga.Visibility = ocupado ? Visibility.Visible : Visibility.Collapsed;
        btnGuardar.IsEnabled = !ocupado;
        btnNuevo.IsEnabled = !ocupado;
        btnActualizar.IsEnabled = !ocupado;
        ActualizarBotones(ocupado);
    }

    private void ActualizarBotones(bool ocupado = false) =>
        btnEliminar.IsEnabled = !ocupado && _editando != null;

    // ----- Eventos de la pantalla -----

    private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        _timerBusqueda.Stop();
        _timerBusqueda.Start();
    }

    private async void btnActualizar_Click(object sender, RoutedEventArgs e) => await CargarAsync();

    private void dgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgSocios.SelectedItem is Socio socio)
            EditarSocio(socio);
    }

    private void btnNuevo_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

    private async void btnGuardar_Click(object sender, RoutedEventArgs e) => await GuardarAsync();

    private async void btnEliminar_Click(object sender, RoutedEventArgs e) => await EliminarAsync();
}
