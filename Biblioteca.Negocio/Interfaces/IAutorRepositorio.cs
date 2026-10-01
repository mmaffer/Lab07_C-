using Biblioteca.Entidades;

namespace Biblioteca.Negocio.Interfaces;

public interface IAutorRepositorio
{
    Task<List<Autor>> ListarActivosAsync();
    Task<Autor> ObtenerAsync(int autorId);
}
