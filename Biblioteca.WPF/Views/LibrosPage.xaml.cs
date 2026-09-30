using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Biblioteca.Entidades;
using Biblioteca.Negocio;
using Biblioteca.WPF.Services;

namespace Biblioteca.WPF.Views;

// Capa de Presentación: solo recoge datos, llama a Negocio y muestra resultados.
// No hay SQL ni reglas aquí, y no se captura SqlException.
public partial class LibrosPage : Page
{
    private readonly LibroNegocio _libroNegocio = new LibroNegocio();
    private readonly AutorNegocio _autorNegocio = new AutorNegocio();

    // Espera un momento después de cada tecla antes de buscar.
    private readonly DispatcherTimer _timerBusqueda = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };

    // Libro en edición. null = se está registrando uno nuevo.
    private Libro _editando;

    public LibrosPage()
    {
        InitializeComponent();

        _timerBusqueda.Tick += async (s, e) =>
        {
            _timerBusqueda.Stop();
            await CargarAsync();
        };

        Loaded += async (s, e) =>
        {
            await CargarAutoresAsync();
            await CargarAsync();
        };
        LimpiarFormulario();
    }

    private async Task CargarAutoresAsync()
    {
        await UiServices.RunAsync(async () =>
        {
            cmbAutor.ItemsSource = await _autorNegocio.ListarAsync();
        }, EstablecerOcupado);
    }

    private async Task CargarAsync()
    {
        string texto = txtBuscar.Text;

        await UiServices.RunAsync(async () =>
        {
            var libros = await _libroNegocio.BuscarAsync(texto);

            dgLibros.ItemsSource = libros;
            txtVacio.Text = string.IsNullOrWhiteSpace(texto)
                ? "No hay libros registrados."
                : $"Ningún libro coincide con \"{texto}\".";
            panelVacio.Visibility = libros.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_editando != null)
                dgLibros.SelectedItem = libros.FirstOrDefault(l => l.LibroId == _editando.LibroId);
        }, EstablecerOcupado);
    }

    private async Task GuardarAsync()
    {
        if (!int.TryParse(txtEjemplares.Text, out int ejemplares))
        {
            UiServices.ShowWarning("Los ejemplares deben ser un número entero.");
            return;
        }

        var libro = new Libro
        {
            LibroId = _editando?.LibroId ?? 0,
            Titulo = txtTitulo.Text,
            ISBN = txtIsbn.Text,
            AutorId = cmbAutor.SelectedValue is int id ? id : 0,
            Ejemplares = ejemplares
        };
        bool esNuevo = libro.LibroId == 0;

        bool ok = await UiServices.RunAsync(async () =>
        {
            if (esNuevo)
                await _libroNegocio.InsertarAsync(libro);
            else
                await _libroNegocio.ActualizarAsync(libro);
        }, EstablecerOcupado);

        if (!ok) return;

        UiServices.ShowSuccess(esNuevo ? $"Se registró \"{libro.Titulo}\"." : $"Se actualizó \"{libro.Titulo}\".");
        if (esNuevo) LimpiarFormulario();
        await CargarAsync();
    }

    private async Task EliminarAsync()
    {
        if (_editando == null) return;

        var libro = _editando;
        bool confirmado = await UiServices.ConfirmAsync(
            "Dar de baja el libro",
            $"¿Desea dar de baja \"{libro.Titulo}\"?\nDejará de mostrarse, pero se conserva su historial de préstamos.",
            "Dar de baja");
        if (!confirmado) return;

        bool ok = await UiServices.RunAsync(() => _libroNegocio.EliminarAsync(libro.LibroId), EstablecerOcupado);
        if (!ok) return;

        UiServices.ShowSuccess($"\"{libro.Titulo}\" fue dado de baja.");
        LimpiarFormulario();
        await CargarAsync();
    }

    private void EditarLibro(Libro libro)
    {
        _editando = libro;
        txtTitulo.Text = libro.Titulo;
        txtIsbn.Text = libro.ISBN;
        cmbAutor.SelectedValue = libro.AutorId;
        txtEjemplares.Text = libro.Ejemplares.ToString();

        txtTituloForm.Text = "Editar libro";
        txtAyudaForm.Text = $"Editando \"{libro.Titulo}\".";
        ActualizarBotones();
    }

    private void LimpiarFormulario()
    {
        _editando = null;
        dgLibros.SelectedItem = null;
        txtTitulo.Clear();
        txtIsbn.Clear();
        cmbAutor.SelectedIndex = -1;
        txtEjemplares.Clear();

        txtTituloForm.Text = "Nuevo libro";
        txtAyudaForm.Text = "Complete los datos y presione Guardar.";
        ActualizarBotones();
        txtTitulo.Focus();
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

    private void dgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgLibros.SelectedItem is Libro libro)
            EditarLibro(libro);
    }

    private void btnNuevo_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

    private async void btnGuardar_Click(object sender, RoutedEventArgs e) => await GuardarAsync();

    private async void btnEliminar_Click(object sender, RoutedEventArgs e) => await EliminarAsync();
}
