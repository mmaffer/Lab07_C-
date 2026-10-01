using Biblioteca.Entidades;

namespace Biblioteca.Negocio.Interfaces;

public interface ILibroRepositorio
{
    Task<List<Libro>> ListarAsync();
    Task<List<Libro>> BuscarAsync(string texto);
    Task<Libro> ObtenerAsync(int libroId);
    Task<bool> ExisteIsbnAsync(string isbn, int excluirLibroId);
    Task<int> InsertarAsync(Libro libro);
    Task ActualizarAsync(Libro libro);
    Task DesactivarAsync(int libroId);
}
