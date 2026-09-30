using Biblioteca.Datos;
using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Reglas de libros: valida antes de llamar a Datos.
public class LibroNegocio
{
    private const int MaxTitulo = 150;
    private const int MaxIsbn = 20;

    private readonly ILibroRepositorio _libros;
    private readonly IAutorRepositorio _autores;
    private readonly IDetallePrestamoRepositorio _detalles;

    // Constructor por defecto: usa las implementaciones con SQL Server (así WPF no toca Datos).
    public LibroNegocio() : this(new LibroRepositorio(), new AutorRepositorio(), new DetallePrestamoRepositorio()) { }

    // Inyección por constructor: Negocio depende de interfaces, no de clases concretas.
    public LibroNegocio(ILibroRepositorio libros, IAutorRepositorio autores, IDetallePrestamoRepositorio detalles)
    {
        _libros = libros;
        _autores = autores;
        _detalles = detalles;
    }

    public Task<List<Libro>> ListarAsync() => _libros.ListarAsync();

    // Busca por título o por autor.
    public Task<List<Libro>> BuscarAsync(string texto) =>
        string.IsNullOrWhiteSpace(texto) ? ListarAsync() : _libros.BuscarAsync(texto.Trim());

    public async Task InsertarAsync(Libro libro)
    {
        await ValidarAsync(libro);
        await _libros.InsertarAsync(libro);
    }

    public async Task ActualizarAsync(Libro libro)
    {
        if (libro.LibroId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un libro para actualizar.");

        await ValidarAsync(libro);
        await _libros.ActualizarAsync(libro);
    }

    // Eliminación lógica (Activo = 0). No se da de baja un libro con préstamos pendientes.
    public async Task EliminarAsync(int libroId)
    {
        if (libroId <= 0)
            throw new ReglaNegocioException("Debe seleccionar un libro.");

        if (await _detalles.ContarPendientesPorLibroAsync(libroId) > 0)
            throw new ReglaNegocioException("No se puede dar de baja el libro: tiene préstamos pendientes de devolución.");

        await _libros.DesactivarAsync(libroId);
    }

    private async Task ValidarAsync(Libro libro)
    {
        // Normalizar antes de validar.
        libro.Titulo = libro.Titulo?.Trim();
        libro.ISBN = libro.ISBN?.Trim();

        if (string.IsNullOrWhiteSpace(libro.Titulo))
            throw new ReglaNegocioException("El título es obligatorio.");

        if (libro.Titulo.Length > MaxTitulo)
            throw new ReglaNegocioException($"El título no puede superar los {MaxTitulo} caracteres.");

        if (string.IsNullOrWhiteSpace(libro.ISBN))
            throw new ReglaNegocioException("El ISBN es obligatorio.");

        if (libro.ISBN.Length > MaxIsbn)
            throw new ReglaNegocioException($"El ISBN no puede superar los {MaxIsbn} caracteres.");

        if (libro.Ejemplares < 0)
            throw new ReglaNegocioException("Los ejemplares no pueden ser negativos.");

        var autor = libro.AutorId > 0 ? await _autores.ObtenerAsync(libro.AutorId) : null;
        if (autor == null || !autor.Activo)
            throw new ReglaNegocioException("Debe seleccionar un autor válido.");

        if (await _libros.ExisteIsbnAsync(libro.ISBN, libro.LibroId))
            throw new ReglaNegocioException($"El ISBN '{libro.ISBN}' ya está registrado.");
    }
}
