using Biblioteca.Datos;
using Biblioteca.Datos.Interfaces;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio;

// Solo expone los autores activos para poder elegirlos al registrar un libro.
public class AutorNegocio
{
    private readonly IAutorRepositorio _autores;

    public AutorNegocio() : this(new AutorRepositorio()) { }

    public AutorNegocio(IAutorRepositorio autores) => _autores = autores;

    public Task<List<Autor>> ListarAsync() => _autores.ListarActivosAsync();
}
