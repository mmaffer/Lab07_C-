using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Formulario de préstamo: elige un socio, agrega libros y delega TODO el registro en Negocio.
public partial class PrestamoPage : Page
{
    private readonly PrestamoNegocio _prestamoNegocio = new PrestamoNegocio();
    private readonly SocioNegocio _socioNegocio = new SocioNegocio();
    private readonly LibroNegocio _libroNegocio = new LibroNegocio();

    // Libros que se van agregando al préstamo antes de guardarlo.
    private readonly ObservableCollection<Libro> _libros = new ObservableCollection<Libro>();

    public PrestamoPage()
    {
        InitializeComponent();
        dgCarrito.ItemsSource = _libros;
        Loaded += async (s, e) => await CargarListasAsync();
    }

    private async Task CargarListasAsync()
    {
        await UiServices.RunAsync(async () =>
        {
            var socios = await _socioNegocio.ListarAsync();
            var libros = await _libroNegocio.ListarAsync();

            cmbSocio.ItemsSource = socios;
            cmbLibro.ItemsSource = libros;
        }, EstablecerOcupado);
    }

    private void Agregar()
    {
        if (cmbLibro.SelectedItem is not Libro libro)
        {
            UiServices.ShowWarning("Seleccione un libro para agregar.");
            return;
        }

        if (_libros.Any(l => l.LibroId == libro.LibroId))
        {
            UiServices.ShowWarning($"\"{libro.Titulo}\" ya está en el préstamo.");
            return;
        }

        _libros.Add(libro);
        cmbLibro.SelectedIndex = -1;
    }

    private async Task RegistrarAsync()
    {
        int socioId = cmbSocio.SelectedValue is int id ? id : 0;
        var libroIds = _libros.Select(l => l.LibroId).ToList();
        int prestamoId = 0;

        bool ok = await UiServices.RunAsync(async () =>
        {
            prestamoId = await _prestamoNegocio.RegistrarAsync(socioId, libroIds);
        }, EstablecerOcupado);

        if (!ok) return;

        UiServices.ShowSuccess($"Préstamo N° {prestamoId} registrado con {libroIds.Count} libro(s).");
        _libros.Clear();
        cmbSocio.SelectedIndex = -1;

        // Recarga los libros: los ejemplares disponibles cambiaron.
        await CargarListasAsync();
    }

    private void EstablecerOcupado(bool ocupado)
    {
        anilloCarga.Visibility = ocupado ? Visibility.Visible : Visibility.Collapsed;
        btnRegistrar.IsEnabled = !ocupado;
        btnAgregar.IsEnabled = !ocupado;
        btnQuitar.IsEnabled = !ocupado;
    }

    // ----- Eventos de la pantalla -----

    private void btnAgregar_Click(object sender, RoutedEventArgs e) => Agregar();

    private void btnQuitar_Click(object sender, RoutedEventArgs e)
    {
        if (dgCarrito.SelectedItem is Libro libro)
            _libros.Remove(libro);
    }

    private async void btnRegistrar_Click(object sender, RoutedEventArgs e) => await RegistrarAsync();
}
